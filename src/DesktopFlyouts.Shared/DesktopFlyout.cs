// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Graphics;
using FoundationPoint = Windows.Foundation.Point;
using FoundationRect = Windows.Foundation.Rect;
using FoundationSize = Windows.Foundation.Size;

#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Win32.UI.WindowsAndMessaging;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace DesktopFlyouts
{
    /// <summary>
    /// Displays a desktop flyout using independent XAML island windows.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DesktopFlyout"/> when you need lightweight desktop surfaces that can be
    /// opened from a tray icon or from application code. Each <see cref="DesktopFlyoutIsland"/>
    /// is hosted by its own desktop XAML island so islands can participate in native window
    /// movement independently.
    /// </remarks>
    [ContentProperty(Name = nameof(Islands))]
    public partial class DesktopFlyout : Control, IDisposable
    {
        private const double IslandSpacing = 12.0D;
        private const double IslandShadowMargin = 12.0D;
        private const double SwipeDismissDragStartThreshold = 4.0D;
        private const double SwipeDismissAxisDominanceRatio = 1.2D;
        private static readonly Thickness s_islandShadowMargin = new(IslandShadowMargin);

        private readonly Dictionary<DesktopFlyoutIsland, XamlIslandHostWindow> _islandHosts = [];
        private readonly Dictionary<DesktopFlyoutIsland, FoundationRect> _islandLayoutRects = [];
        private readonly List<(Control Control, bool IsTabStop)> _suppressedTabStopStates = [];
        private readonly List<(FrameworkElement Element, bool AllowFocusOnInteraction)> _suppressedInteractionFocusStates = [];
        private readonly List<DesktopFlyoutDragRegion> _dragRegions = [];
        private readonly List<Storyboard> _transitionStoryboards = [];
        private readonly Dictionary<DesktopFlyoutIsland, Storyboard> _pressedScaleStoryboards = [];
        private readonly Dictionary<Storyboard, (DesktopFlyoutIsland Island, double TargetScale)> _pressedScaleStoryboardStates = [];
        private readonly HashSet<DesktopFlyoutIsland> _pressedScaleIslands = [];
        private bool _isPopupAnimationPlaying;
        private bool _isSwipeDismissTracking;
        private bool _isSwipeDismissDragging;
        private bool _isPointerCaptured;
        private bool _isClosingTransition;
        private bool _isNavigatingFocusAcrossHosts;
        private bool _disposed;
        private int _pendingTransitionStoryboardCount;
        private int _restoreActivationTickCount;
        private Point? _customPlacementBottomCenterPoint;
        private DesktopFlyoutIsland? _capturedPointerIsland;
        private DesktopFlyoutIsland? _swipeDismissIsland;
        private DesktopFlyoutIsland? _swipeDismissRestoreIsland;
        private DesktopFlyoutPopupDirection _activePopupDirection = DesktopFlyoutPopupDirection.BottomToTop;
        private DispatcherTimer? _autoCloseTimer;
        private DispatcherTimer? _restoreActivationTimer;
        private DispatcherTimer? _lostFocusCloseTimer;
        private Storyboard? _swipeDismissRestoreStoryboard;
        private FoundationPoint _swipeDismissStartPoint;
        private double _xamlIslandRasterizationScale = 1.0D;
        private double _currentFlyoutWidth = 1.0D;
        private double _currentFlyoutHeight = 1.0D;
        private uint _swipeDismissPointerId;

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopFlyout"/>.
        /// </summary>
        public DesktopFlyout()
        {
            DefaultStyleKey = typeof(DesktopFlyout);
        }

        /// <summary>
        /// Opens the flyout using its configured placement.
        /// </summary>
        public void Show()
        {
            if (_disposed || _isPopupAnimationPlaying)
            {
                _customPlacementBottomCenterPoint = null;
                return;
            }

            EnsureIslandHosts();
            if (_islandHosts.Count == 0)
            {
                _customPlacementBottomCenterPoint = null;
                return;
            }

            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            StopLostFocusCloseTimer();
            StopTransitionStoryboards();
            StopSwipeDismissRestoreStoryboard();
            ResetPressedScale();
            ResetSwipeDismissTracking();

            _isPopupAnimationPlaying = true;
            var shouldActivateOnOpen = ShouldActivateOnOpen();
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(_customPlacementBottomCenterPoint);

            foreach (var host in _islandHosts.Values)
            {
                host.SetActivationMode(ActivationMode);
                host.SetDragMode(DragMode);
                host.Maximize(workArea, shouldActivateOnOpen);

                if (!shouldActivateOnOpen)
                    host.PreserveActivationState();
            }

            QueueShowStep(shouldActivateOnOpen, 0);
        }

        /// <summary>
        /// Opens the flyout at the specified bottom-center screen point.
        /// </summary>
        /// <param name="bottomCenterPoint">The desired bottom-center point of the flyout in physical screen pixels.</param>
        public void Show(Point bottomCenterPoint)
        {
            if (_isPopupAnimationPlaying)
                return;

            _customPlacementBottomCenterPoint = bottomCenterPoint;
            Show();
        }

        /// <summary>
        /// Closes the flyout.
        /// </summary>
        public void Hide()
        {
            Hide(false);
        }

#if WASDK
        internal SystemBackdrop? CreateIslandSystemBackdrop()
        {
            if (!IsBackdropEnabled)
                return null;

            return BackdropKind switch
            {
                DesktopFlyoutBackdropKind.Mica => new DesktopFlyoutMicaBackdrop(),
                _ => new DesktopFlyoutAcrylicBackdrop(),
            };
        }
#endif

        /// <summary>
        /// Moves focus into the first hosted flyout island.
        /// </summary>
        /// <param name="reason">The focus navigation reason used by the XAML hosting layer.</param>
        public void NavigateFocus(XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic)
        {
            foreach (var island in GetSpatiallyOrderedIslands())
            {
                if (_islandHosts.TryGetValue(island, out var host) && host.NavigateFocus(reason))
                    return;
            }
        }

#if UWP
        /// <summary>
        /// Lets the hosted XAML islands process a native keyboard message before dispatch.
        /// </summary>
        /// <param name="msg">The native message to process.</param>
        /// <returns><see langword="true"/> if the message was handled; otherwise, <see langword="false"/>.</returns>
        public unsafe bool TryPreTranslateMessage(MSG* msg)
        {
            foreach (var host in _islandHosts.Values)
            {
                if (host.TryPreTranslateMessage(msg))
                    return true;
            }

            return false;
        }
#endif

        internal void OnIslandSizeChanged()
        {
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        internal void OnIslandPositionChanged(DesktopFlyoutIsland island)
        {
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        internal Thickness GetIslandShadowMargin()
        {
            return s_islandShadowMargin;
        }

        internal void RegisterDragRegion(DesktopFlyoutDragRegion region)
        {
            if (!_dragRegions.Contains(region))
                _dragRegions.Add(region);

            if (DragMode is DesktopFlyoutDragMode.Region)
                UpdateHostDragRegions();
        }

        internal void UnregisterDragRegion(DesktopFlyoutDragRegion region)
        {
            _dragRegions.Remove(region);

            if (DragMode is DesktopFlyoutDragMode.Region)
                UpdateHostDragRegions();
        }

        internal void OnDragRegionChanged()
        {
            if (DragMode is DesktopFlyoutDragMode.Region)
                UpdateHostDragRegions();
        }

        internal void BeginDragMove(DesktopFlyoutDragRegion region, PointerRoutedEventArgs e)
        {
            if (DragMode is not DesktopFlyoutDragMode.Region)
                return;

            if (FindAncestorIsland(region) is DesktopFlyoutIsland island)
                BeginIslandNativeDragMove(island, e);
        }

        private void QueueShowStep(bool shouldActivateOnOpen, int step)
        {
            if (!TryEnqueueOnFlyoutDispatcher(() => RunQueuedShowStep(shouldActivateOnOpen, step)))
                AbortQueuedShow();
        }

        private void RunQueuedShowStep(bool shouldActivateOnOpen, int step)
        {
            if (_disposed || _islandHosts.Count == 0 || !_isPopupAnimationPlaying)
            {
                if (!_disposed)
                    AbortQueuedShow();

                return;
            }

            switch (step)
            {
                case 0:
                    ResetIslandLayoutSizes();
                    UpdateHostedIslandLayouts();
                    QueueShowStep(shouldActivateOnOpen, 1);
                    break;
                case 1:
                    UpdateFlyoutTheme();
                    UpdateIslandBackdrops();
                    UpdateFocusSuppression();
                    UpdateXamlIslandRasterizationScale();
                    _activePopupDirection = UpdateFlyoutRegion();
                    SetClosedTransform(_activePopupDirection);
                    QueueShowStep(shouldActivateOnOpen, 2);
                    break;
                case 2:
                    foreach (var host in _islandHosts.Values)
                        host.UpdateWindowVisibility(true, shouldActivateOnOpen);

                    if (!shouldActivateOnOpen)
                        RestartRestoreActivationTimer();

                    if (IsTransitionAnimationEnabled)
                        BeginTransition(isClosing: false, fromCurrentTransform: false);
                    else
                        CompleteOpen();
                    break;
                default:
                    AbortQueuedShow();
                    break;
            }
        }

        private void AbortQueuedShow()
        {
            _isPopupAnimationPlaying = false;
            _customPlacementBottomCenterPoint = null;
        }

        private void Hide(bool closeFromCurrentTransform)
        {
            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            StopLostFocusCloseTimer();
            StopSwipeDismissRestoreStoryboard();
            ReleaseCapturedPointers();
            ResetSwipeDismissTracking();

            if (_disposed || _isPopupAnimationPlaying)
                return;

            _isPopupAnimationPlaying = true;
            if (!closeFromCurrentTransform)
                SetOpenTransform();

            if (IsTransitionAnimationEnabled)
                BeginTransition(isClosing: true, closeFromCurrentTransform);
            else
                CompleteClose();
        }

        private void EnsureIslandHosts()
        {
            List<DesktopFlyoutIsland> detachedIslands = [];
            foreach (var item in _islandHosts)
            {
                if (!Islands.Contains(item.Key))
                    detachedIslands.Add(item.Key);
            }

            foreach (var island in detachedIslands)
            {
                if (_islandHosts.TryGetValue(island, out var host))
                {
                    ResetPressedScale(island);
                    if (ReferenceEquals(_swipeDismissIsland, island))
                        ResetSwipeDismissTracking();

                    if (ReferenceEquals(_swipeDismissRestoreIsland, island))
                        StopSwipeDismissRestoreStoryboard();

                    DetachIslandEvents(island);
                    host.WindowInactivated -= HostWindow_Inactivated;
#if WASDK
                    host.NativeMoveSizeStarted -= HostWindow_NativeMoveSizeStarted;
                    host.NativeMoveSizeEnded -= HostWindow_NativeMoveSizeEnded;
#endif
                    host.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
                    host.TakeFocusRequested -= HostWindow_TakeFocusRequested;
                    host.Dispose();
                    _islandHosts.Remove(island);
                }
            }

            foreach (var island in Islands)
            {
                island.SetOwner(this);

                if (_islandHosts.ContainsKey(island))
                    continue;

                var host = new XamlIslandHostWindow();
                host.SetActivationMode(ActivationMode);
                host.SetDragMode(DragMode);
                host.SetContentMargin(island.TemplateSettings.ShadowMargin);
                host.SetContent(island);
                host.UpdateWindowVisibility(false);
                host.WindowInactivated += HostWindow_Inactivated;
#if WASDK
                host.NativeMoveSizeStarted += HostWindow_NativeMoveSizeStarted;
                host.NativeMoveSizeEnded += HostWindow_NativeMoveSizeEnded;
#endif
                host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
                host.TakeFocusRequested += HostWindow_TakeFocusRequested;
                _islandHosts[island] = host;

                AttachIslandEvents(island);
            }
        }

        private void AttachIslandEvents(DesktopFlyoutIsland island)
        {
            DetachIslandEvents(island);
            island.PointerPressed += Island_PointerPressed;
            island.PointerMoved += Island_PointerMoved;
            island.PointerReleased += Island_PointerReleased;
            island.PointerCanceled += Island_PointerCanceled;
            island.PointerCaptureLost += Island_PointerCaptureLost;
            island.PointerExited += Island_PointerExited;
        }

        private void DetachIslandEvents(DesktopFlyoutIsland island)
        {
            island.PointerPressed -= Island_PointerPressed;
            island.PointerMoved -= Island_PointerMoved;
            island.PointerReleased -= Island_PointerReleased;
            island.PointerCanceled -= Island_PointerCanceled;
            island.PointerCaptureLost -= Island_PointerCaptureLost;
            island.PointerExited -= Island_PointerExited;
        }

        private void UpdateIslands()
        {
            foreach (var island in Islands)
                island.SetOwner(this);

            if (_islandHosts.Count > 0 || IsOpen)
            {
                EnsureIslandHosts();
                UpdateHostDragMode();
                UpdateHostDragRegions();
            }
        }

        private void UpdateOpenFlyoutLayout()
        {
            if (!IsOpen || _isPopupAnimationPlaying || _islandHosts.Count == 0)
                return;

            UpdateXamlIslandRasterizationScale();
            _activePopupDirection = UpdateFlyoutRegion();
            SetOpenTransform();
        }

        private void UpdateHostedIslandLayouts()
        {
            foreach (var island in Islands)
                island.UpdateLayout();
        }

        private void ResetIslandLayoutSizes()
        {
            foreach (var island in Islands)
            {
                island.Width = double.NaN;
                island.Height = double.NaN;
            }
        }

        private DesktopFlyoutPopupDirection UpdateFlyoutRegion()
        {
            if (_islandHosts.Count == 0)
                return ResolvePopupDirection(PopupDirection, default, WindowHelpers.GetFlyoutWorkAreaRect(_customPlacementBottomCenterPoint));

            var customBottomCenterPoint = _customPlacementBottomCenterPoint;
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(customBottomCenterPoint);
            UpdateIslandLayoutRects(workArea);

            var scale = _xamlIslandRasterizationScale;
            var scaledMargin = GetScaledMargin(Margin, scale);
            var islandShadowMargin = GetIslandShadowMargin();
            var scaledIslandShadowMargin = GetScaledMargin(islandShadowMargin, scale);
            var frameWidth = (_currentFlyoutWidth * scale) + scaledMargin.Left + scaledMargin.Right + scaledIslandShadowMargin.Left + scaledIslandShadowMargin.Right;
            var frameHeight = (_currentFlyoutHeight * scale) + scaledMargin.Top + scaledMargin.Bottom + scaledIslandShadowMargin.Top + scaledIslandShadowMargin.Bottom;
            var regionWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(frameWidth, workArea.Width)));
            var regionHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(frameHeight, workArea.Height)));
            var requestedPopupDirection = PopupDirection;
            _customPlacementBottomCenterPoint = null;

            double left;
            double top;
            if (customBottomCenterPoint is Point bottomCenterPoint)
            {
                (left, top) = GetCustomPlacementOrigin(
                    bottomCenterPoint,
                    regionWidth,
                    regionHeight,
                    new(_currentFlyoutWidth * scale, _currentFlyoutHeight * scale),
                    AddThickness(scaledMargin, scaledIslandShadowMargin),
                    workArea);
            }
            else
            {
                (left, top) = GetPlacementOrigin(Placement, regionWidth, regionHeight, workArea);
            }

            left = Clamp(left, workArea.Left, workArea.Right - regionWidth);
            top = Clamp(top, workArea.Top, workArea.Bottom - regionHeight);

            var flyoutRegion = new RectInt32(
                (int)Math.Round(left),
                (int)Math.Round(top),
                regionWidth,
                regionHeight);

            foreach (var item in _islandHosts)
            {
                if (!_islandLayoutRects.TryGetValue(item.Key, out var rect))
                    continue;

                var islandLeft = flyoutRegion.X + (int)Math.Round((Margin.Left + rect.X) * scale);
                var islandTop = flyoutRegion.Y + (int)Math.Round((Margin.Top + rect.Y) * scale);
                var islandWidth = Math.Max(1, (int)Math.Ceiling(rect.Width * scale));
                var islandHeight = Math.Max(1, (int)Math.Ceiling(rect.Height * scale));
                var shadowLeft = Math.Max(0, (int)Math.Floor(islandShadowMargin.Left * scale));
                var shadowTop = Math.Max(0, (int)Math.Floor(islandShadowMargin.Top * scale));
                var shadowRight = Math.Max(0, (int)Math.Ceiling(islandShadowMargin.Right * scale));
                var shadowBottom = Math.Max(0, (int)Math.Ceiling(islandShadowMargin.Bottom * scale));
                var hostRect = new RectInt32(
                    islandLeft,
                    islandTop,
                    islandWidth + shadowLeft + shadowRight,
                    islandHeight + shadowTop + shadowBottom);

                item.Value.SetContentMargin(islandShadowMargin);
                item.Value.MoveAndResize(hostRect, ShouldActivateOnOpen());
                item.Value.SetHWndRectRegion(new(0, 0, hostRect.Width, hostRect.Height));
            }

            UpdateHostDragRegions();
            return ResolvePopupDirection(requestedPopupDirection, flyoutRegion, workArea);
        }

        private void UpdateIslandLayoutRects(Rectangle workArea)
        {
            _islandLayoutRects.Clear();
            var availableSize = GetAvailableFlyoutSizeInDips(workArea);

            if (IslandLayoutMode is DesktopFlyoutIslandLayoutMode.Freeform)
            {
                UpdateFreeformIslandLayoutRects(availableSize);
                return;
            }

            if (IslandsOrientation is Orientation.Horizontal)
                UpdateHorizontalStackIslandLayoutRects(availableSize);
            else
                UpdateVerticalStackIslandLayoutRects(availableSize);
        }

        private void UpdateVerticalStackIslandLayoutRects((double Width, double Height) availableSize)
        {
            var desiredSizes = GetIslandDesiredSizes(availableSize);
            var rootWidth = ResolveStackRootWidth(availableSize, desiredSizes);
            var rootHeight = ResolveVerticalStackRootHeight(availableSize, desiredSizes);
            var starHeight = GetVerticalStackStarLength(rootHeight, desiredSizes);
            var top = 0.0D;

            foreach (var island in Islands)
            {
                var desired = desiredSizes[island];
                var height = island.IslandHeight.IsStar && starHeight > 0
                    ? starHeight * Math.Max(1.0D, island.IslandHeight.Value)
                    : ResolveIslandLength(island.IslandHeight, desired.Height);
                var width = island.IslandWidth.IsAbsolute
                    ? island.IslandWidth.Value
                    : rootWidth;

                SetIslandLayoutRect(island, new(0, top, Math.Max(1, width), Math.Max(1, height)));
                top += height + IslandSpacing;
            }

            _currentFlyoutWidth = Math.Max(1, rootWidth);
            _currentFlyoutHeight = Math.Max(1, rootHeight);
        }

        private void UpdateHorizontalStackIslandLayoutRects((double Width, double Height) availableSize)
        {
            var desiredSizes = GetIslandDesiredSizes(availableSize);
            var rootWidth = ResolveHorizontalStackRootWidth(availableSize, desiredSizes);
            var rootHeight = ResolveStackRootHeight(availableSize, desiredSizes);
            var starWidth = GetHorizontalStackStarLength(rootWidth, desiredSizes);
            var left = 0.0D;

            foreach (var island in Islands)
            {
                var desired = desiredSizes[island];
                var width = island.IslandWidth.IsStar && starWidth > 0
                    ? starWidth * Math.Max(1.0D, island.IslandWidth.Value)
                    : ResolveIslandLength(island.IslandWidth, desired.Width);
                var height = island.IslandHeight.IsAbsolute
                    ? island.IslandHeight.Value
                    : rootHeight;

                SetIslandLayoutRect(island, new(left, 0, Math.Max(1, width), Math.Max(1, height)));
                left += width + IslandSpacing;
            }

            _currentFlyoutWidth = Math.Max(1, rootWidth);
            _currentFlyoutHeight = Math.Max(1, rootHeight);
        }

        private void UpdateFreeformIslandLayoutRects((double Width, double Height) availableSize)
        {
            var desiredSizes = GetIslandDesiredSizes(availableSize);
            var right = 0.0D;
            var bottom = 0.0D;

            foreach (var island in Islands)
            {
                var desired = desiredSizes[island];
                var left = GetFiniteOrZero(island.CanvasLeft);
                var top = GetFiniteOrZero(island.CanvasTop);
                var width = ResolveIslandLength(island.IslandWidth, desired.Width);
                var height = ResolveIslandLength(island.IslandHeight, desired.Height);
                var rect = new FoundationRect(left, top, Math.Max(1, width), Math.Max(1, height));

                SetIslandLayoutRect(island, rect);
                right = Math.Max(right, rect.X + rect.Width);
                bottom = Math.Max(bottom, rect.Y + rect.Height);
            }

            _currentFlyoutWidth = ResolveFlyoutLength(FlyoutWidth, availableSize.Width, false, Math.Max(1, right));
            _currentFlyoutHeight = ResolveFlyoutLength(FlyoutHeight, availableSize.Height, false, Math.Max(1, bottom));
        }

        private Dictionary<DesktopFlyoutIsland, FoundationSize> GetIslandDesiredSizes((double Width, double Height) availableSize)
        {
            Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes = [];

            foreach (var island in Islands)
            {
                island.Width = island.IslandWidth.IsAbsolute ? Math.Max(1, island.IslandWidth.Value) : double.NaN;
                island.Height = island.IslandHeight.IsAbsolute ? Math.Max(1, island.IslandHeight.Value) : double.NaN;
                island.Measure(new(Math.Max(1, availableSize.Width), Math.Max(1, availableSize.Height)));
                island.UpdateLayout();

                var desired = island.DesiredSize;
                var width = desired.Width > 0 ? desired.Width : island.ActualWidth;
                var height = desired.Height > 0 ? desired.Height : island.ActualHeight;
                desiredSizes[island] = new(Math.Max(1, width), Math.Max(1, height));
            }

            return desiredSizes;
        }

        private void SetIslandLayoutRect(DesktopFlyoutIsland island, FoundationRect rect)
        {
            _islandLayoutRects[island] = rect;
            island.Width = rect.Width;
            island.Height = rect.Height;
            island.UpdateLayout();
        }

        private double ResolveStackRootWidth((double Width, double Height) availableSize, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var desiredWidth = 1.0D;
            foreach (var island in Islands)
            {
                var width = island.IslandWidth.IsAbsolute
                    ? island.IslandWidth.Value
                    : desiredSizes[island].Width;
                desiredWidth = Math.Max(desiredWidth, width);
            }

            return ResolveFlyoutLength(FlyoutWidth, availableSize.Width, HasStarIslandWidth(), desiredWidth);
        }

        private double ResolveStackRootHeight((double Width, double Height) availableSize, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var desiredHeight = 1.0D;
            foreach (var island in Islands)
            {
                var height = island.IslandHeight.IsAbsolute
                    ? island.IslandHeight.Value
                    : desiredSizes[island].Height;
                desiredHeight = Math.Max(desiredHeight, height);
            }

            return ResolveFlyoutLength(FlyoutHeight, availableSize.Height, HasStarIslandHeight(), desiredHeight);
        }

        private double ResolveVerticalStackRootHeight((double Width, double Height) availableSize, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var desiredHeight = 0.0D;
            for (var index = 0; index < Islands.Count; index++)
            {
                var island = Islands[index];
                desiredHeight += island.IslandHeight.IsStar
                    ? desiredSizes[island].Height
                    : ResolveIslandLength(island.IslandHeight, desiredSizes[island].Height);
            }

            desiredHeight += Math.Max(0, Islands.Count - 1) * IslandSpacing;
            return ResolveFlyoutLength(FlyoutHeight, availableSize.Height, HasStarIslandHeight(), Math.Max(1, desiredHeight));
        }

        private double ResolveHorizontalStackRootWidth((double Width, double Height) availableSize, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var desiredWidth = 0.0D;
            for (var index = 0; index < Islands.Count; index++)
            {
                var island = Islands[index];
                desiredWidth += island.IslandWidth.IsStar
                    ? desiredSizes[island].Width
                    : ResolveIslandLength(island.IslandWidth, desiredSizes[island].Width);
            }

            desiredWidth += Math.Max(0, Islands.Count - 1) * IslandSpacing;
            return ResolveFlyoutLength(FlyoutWidth, availableSize.Width, HasStarIslandWidth(), Math.Max(1, desiredWidth));
        }

        private double GetVerticalStackStarLength(double rootHeight, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var fixedHeight = Math.Max(0, Islands.Count - 1) * IslandSpacing;
            var starWeight = 0.0D;

            foreach (var island in Islands)
            {
                if (island.IslandHeight.IsStar)
                    starWeight += Math.Max(1.0D, island.IslandHeight.Value);
                else
                    fixedHeight += ResolveIslandLength(island.IslandHeight, desiredSizes[island].Height);
            }

            return starWeight <= 0 ? 0 : Math.Max(0, rootHeight - fixedHeight) / starWeight;
        }

        private double GetHorizontalStackStarLength(double rootWidth, Dictionary<DesktopFlyoutIsland, FoundationSize> desiredSizes)
        {
            var fixedWidth = Math.Max(0, Islands.Count - 1) * IslandSpacing;
            var starWeight = 0.0D;

            foreach (var island in Islands)
            {
                if (island.IslandWidth.IsStar)
                    starWeight += Math.Max(1.0D, island.IslandWidth.Value);
                else
                    fixedWidth += ResolveIslandLength(island.IslandWidth, desiredSizes[island].Width);
            }

            return starWeight <= 0 ? 0 : Math.Max(0, rootWidth - fixedWidth) / starWeight;
        }

        private static double ResolveIslandLength(GridLength length, double desiredLength)
        {
            if (length.IsAbsolute)
                return Math.Max(1, length.Value);

            return Math.Max(1, desiredLength);
        }

        private (double Width, double Height) GetAvailableFlyoutSizeInDips(Rectangle workArea)
        {
            var scale = _xamlIslandRasterizationScale;
            var islandShadowMargin = GetIslandShadowMargin();
            var availableWidth = (workArea.Width / scale) - Margin.Left - Margin.Right - islandShadowMargin.Left - islandShadowMargin.Right;
            var availableHeight = (workArea.Height / scale) - Margin.Top - Margin.Bottom - islandShadowMargin.Top - islandShadowMargin.Bottom;

            return (Math.Max(1, availableWidth), Math.Max(1, availableHeight));
        }

        private void UpdateXamlIslandRasterizationScale()
        {
            foreach (var host in _islandHosts.Values)
            {
                _xamlIslandRasterizationScale = NormalizeRasterizationScale(host.XamlIslandRasterizationScale);
                return;
            }

            _xamlIslandRasterizationScale = 1.0D;
        }

        private static double NormalizeRasterizationScale(double scale)
        {
            return double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0 ? 1.0D : scale;
        }

        private void UpdateHostDragMode()
        {
            foreach (var host in _islandHosts.Values)
                host.SetDragMode(DragMode);
        }

        private void UpdateHostDragRegions()
        {
            Dictionary<DesktopFlyoutIsland, List<RectInt32>> dragRegionsByIsland = [];

            if (DragMode is DesktopFlyoutDragMode.Region)
            {
                foreach (var region in _dragRegions)
                {
                    if (FindAncestorIsland(region) is not DesktopFlyoutIsland island ||
                        !_islandHosts.TryGetValue(island, out var host) ||
                        !TryGetScaledDragRegionRect(region, island, NormalizeRasterizationScale(host.XamlIslandRasterizationScale), island.TemplateSettings.ShadowMargin, out var rect))
                    {
                        continue;
                    }

                    if (!dragRegionsByIsland.TryGetValue(island, out var dragRegions))
                    {
                        dragRegions = [];
                        dragRegionsByIsland[island] = dragRegions;
                    }

                    dragRegions.Add(rect);
                }
            }

            foreach (var item in _islandHosts)
            {
                if (dragRegionsByIsland.TryGetValue(item.Key, out var dragRegions))
                    item.Value.SetDragRegions(dragRegions);
                else
                    item.Value.SetDragRegions([]);
            }
        }

        private void UpdateFlyoutTheme()
        {
            if (GeneralHelpers.IsTaskbarLight())
            {
                foreach (var island in Islands)
                    island.RequestedTheme = ElementTheme.Light;
            }
            else
            {
                foreach (var island in Islands)
                    island.RequestedTheme = ElementTheme.Dark;
            }
        }

        private void UpdateIslandBackdrops()
        {
#if WASDK
            foreach (var island in Islands)
                island.UpdateOwnerBackdrop();
#endif
        }

        private void CompleteOpen()
        {
            StopSwipeDismissRestoreStoryboard();
            ResetSwipeDismissTracking();
            SetOpenTransform();
            _isPopupAnimationPlaying = false;
            IsOpen = true;

            if (ShouldActivateOnOpen())
                PrepareInitialFocus();
            else
                RestartRestoreActivationTimer();

            RestartAutoCloseTimer();
        }

        private void CompleteClose()
        {
            StopAutoCloseTimer();
            StopSwipeDismissRestoreStoryboard();
            StopTransitionStoryboards();
            ResetSwipeDismissTracking();
            RestoreFocusSuppression();
            ResetPressedScale();
            SetClosedTransform(_activePopupDirection);
            _isPopupAnimationPlaying = false;
            IsOpen = false;

            foreach (var host in _islandHosts.Values)
                host.UpdateWindowVisibility(false);
        }

        private void PrepareInitialFocus()
        {
            NavigateFocus(XamlSourceFocusNavigationReason.Programmatic);
        }

        private void RestartAutoCloseTimer()
        {
            StopAutoCloseTimer();

            if (_disposed || !IsOpen || AutoCloseDelay <= TimeSpan.Zero)
                return;

            _autoCloseTimer = new() { Interval = AutoCloseDelay };
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
        }

        private void StopAutoCloseTimer()
        {
            if (_autoCloseTimer is null)
                return;

            _autoCloseTimer.Stop();
            _autoCloseTimer.Tick -= AutoCloseTimer_Tick;
            _autoCloseTimer = null;
        }

        private void RestartRestoreActivationTimer()
        {
            StopRestoreActivationTimer();

            if (_disposed || ShouldActivateOnOpen())
                return;

            _restoreActivationTickCount = 0;
            RestoreActivationState();
            _restoreActivationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
            _restoreActivationTimer.Tick += RestoreActivationTimer_Tick;
            _restoreActivationTimer.Start();
        }

        private void StopRestoreActivationTimer()
        {
            if (_restoreActivationTimer is null)
                return;

            _restoreActivationTimer.Stop();
            _restoreActivationTimer.Tick -= RestoreActivationTimer_Tick;
            _restoreActivationTimer = null;
            _restoreActivationTickCount = 0;
        }

        private void RestartLostFocusCloseTimer()
        {
            StopLostFocusCloseTimer();

            _lostFocusCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
            _lostFocusCloseTimer.Tick += LostFocusCloseTimer_Tick;
            _lostFocusCloseTimer.Start();
        }

        private void StopLostFocusCloseTimer()
        {
            if (_lostFocusCloseTimer is null)
                return;

            _lostFocusCloseTimer.Stop();
            _lostFocusCloseTimer.Tick -= LostFocusCloseTimer_Tick;
            _lostFocusCloseTimer = null;
        }

        private void AutoCloseTimer_Tick(object? sender, object e)
        {
            StopAutoCloseTimer();

            if (IsOpen && !_isPopupAnimationPlaying)
                Hide();
        }

        private void RestoreActivationTimer_Tick(object? sender, object e)
        {
            RestoreActivationState();
            _restoreActivationTickCount++;

            if (_restoreActivationTickCount >= 8)
                StopRestoreActivationTimer();
        }

        private void LostFocusCloseTimer_Tick(object? sender, object e)
        {
            StopLostFocusCloseTimer();

            if (IsOpen && !_isPopupAnimationPlaying && !ContainsForegroundWindow())
                Hide();
        }

        private void RestoreActivationState()
        {
            foreach (var host in _islandHosts.Values)
                host.RestoreActivationState();
        }

        private bool ContainsForegroundWindow()
        {
            foreach (var host in _islandHosts.Values)
            {
                if (host.ContainsForegroundWindow())
                    return true;
            }

            return false;
        }

        private void Island_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not DesktopFlyoutIsland island || _isPopupAnimationPlaying)
                return;

            if (DragMode is DesktopFlyoutDragMode.Full)
            {
#if UWP
                BeginIslandNativeDragMove(island, e);
#endif
                return;
            }

            var isPressedScaleEnabled = IsPressedScaleEnabled();
            var isSwipeDismissEnabled = CanStartSwipeDismiss();
            if (!isPressedScaleEnabled && !isSwipeDismissEnabled)
                return;

            StopSwipeDismissRestoreStoryboard();

            if (isSwipeDismissEnabled)
                StartSwipeDismiss(island, e);

            _capturedPointerIsland = island;
            _isPointerCaptured = island.CapturePointer(e.Pointer);

            if (isPressedScaleEnabled)
                StartPressedScale(island);
        }

        private void Island_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (UpdateSwipeDismiss(e))
                e.Handled = true;
        }

        private void Island_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var island = _capturedPointerIsland ?? sender as DesktopFlyoutIsland;
            var dismissedBySwipe = CompleteSwipeDismiss(e);

            ReleaseCapturedPointers();

            if (!dismissedBySwipe && island is not null)
                RestorePressedScale(island);
        }

        private void Island_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var island = _capturedPointerIsland ?? sender as DesktopFlyoutIsland;
            CancelSwipeDismiss();
            ReleaseCapturedPointers();
            if (island is not null)
                RestorePressedScale(island);
        }

        private void Island_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            var island = _capturedPointerIsland ?? sender as DesktopFlyoutIsland;
            CancelSwipeDismiss();
            _isPointerCaptured = false;
            _capturedPointerIsland = null;
            if (island is not null)
                RestorePressedScale(island);
        }

        private void Island_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is DesktopFlyoutIsland island && (!_isSwipeDismissDragging || !ReferenceEquals(_swipeDismissIsland, island)))
                RestorePressedScale(island);
        }

        private void BeginIslandNativeDragMove(DesktopFlyoutIsland island, PointerRoutedEventArgs e)
        {
            if (!IsOpen || _isPopupAnimationPlaying || !_islandHosts.TryGetValue(island, out var host))
                return;

            StopAutoCloseTimer();
            ReleaseCapturedPointers();
            ResetSwipeDismissTracking();
            RestorePressedScale(island);

            e.Handled = true;
            host.BeginNativeDragMove();

            if (IsOpen)
                RestartAutoCloseTimer();
        }

        private void ReleaseCapturedPointers()
        {
            if (!_isPointerCaptured || _capturedPointerIsland is null)
                return;

            _capturedPointerIsland.ReleasePointerCaptures();
            _capturedPointerIsland = null;
            _isPointerCaptured = false;
        }

        private bool CanStartSwipeDismiss()
        {
            return IsSwipeToDismissEnabled &&
                IsOpen &&
                GetSwipeDismissMaxDistance() > 0;
        }

        private void StartSwipeDismiss(DesktopFlyoutIsland island, PointerRoutedEventArgs e)
        {
            _swipeDismissPointerId = e.Pointer.PointerId;
            _swipeDismissStartPoint = GetSwipeDismissPointerPosition(e);
            _swipeDismissIsland = island;
            _isSwipeDismissTracking = true;
            _isSwipeDismissDragging = false;
        }

        private bool UpdateSwipeDismiss(PointerRoutedEventArgs e)
        {
            var island = _swipeDismissIsland;
            if (!_isSwipeDismissTracking || island is null || e.Pointer.PointerId != _swipeDismissPointerId)
                return false;

            var currentPoint = GetSwipeDismissPointerPosition(e);
            var deltaX = currentPoint.X - _swipeDismissStartPoint.X;
            var deltaY = currentPoint.Y - _swipeDismissStartPoint.Y;

            if (!TryGetSwipeDismissTranslation(deltaX, deltaY, out var translateX, out var translateY))
                return false;

            if (!_isSwipeDismissDragging)
            {
                _isSwipeDismissDragging = true;
                StopAutoCloseTimer();
                ResetPressedScale(island);
            }

            SetIslandTransformTranslation(island, translateX, translateY);
            return true;
        }

        private bool CompleteSwipeDismiss(PointerRoutedEventArgs e)
        {
            var island = _swipeDismissIsland;
            if (!_isSwipeDismissTracking || island is null || e.Pointer.PointerId != _swipeDismissPointerId)
                return false;

            var shouldDismiss = _isSwipeDismissDragging && GetSwipeDismissDistance(island) >= GetResolvedSwipeDismissThreshold();
            var shouldRestore = _isSwipeDismissDragging && !shouldDismiss;
            ResetSwipeDismissTracking();

            if (shouldDismiss)
            {
                ResetPressedScale(island);
                Hide(true);
                return true;
            }

            if (shouldRestore)
                AnimateSwipeDismissRestore(island);

            return false;
        }

        private void CancelSwipeDismiss()
        {
            var island = _swipeDismissIsland;
            var shouldRestore = _isSwipeDismissDragging;
            ResetSwipeDismissTracking();

            if (shouldRestore && island is not null)
                AnimateSwipeDismissRestore(island);
        }

        private void ResetSwipeDismissTracking()
        {
            _isSwipeDismissTracking = false;
            _isSwipeDismissDragging = false;
            _swipeDismissIsland = null;
            _swipeDismissPointerId = 0;
        }

        private static FoundationPoint GetSwipeDismissPointerPosition(PointerRoutedEventArgs e)
        {
            return e.GetCurrentPoint(null).Position;
        }

        private bool TryGetSwipeDismissTranslation(double deltaX, double deltaY, out double translateX, out double translateY)
        {
            translateX = 0;
            translateY = 0;

            var closedX = GetClosedXOffset(_activePopupDirection);
            if (closedX != 0)
            {
                var primaryDistance = Math.Max(0, Math.Sign(closedX) * deltaX);
                if (!CanStartSwipeDismissDrag(primaryDistance, Math.Abs(deltaY)))
                    return false;

                translateX = Math.Sign(closedX) * Math.Min(primaryDistance, Math.Abs(closedX));
                return true;
            }

            var closedY = GetClosedYOffset(_activePopupDirection);
            if (closedY == 0)
                return false;

            var verticalPrimaryDistance = Math.Max(0, Math.Sign(closedY) * deltaY);
            if (!CanStartSwipeDismissDrag(verticalPrimaryDistance, Math.Abs(deltaX)))
                return false;

            translateY = Math.Sign(closedY) * Math.Min(verticalPrimaryDistance, Math.Abs(closedY));
            return true;
        }

        private bool CanStartSwipeDismissDrag(double primaryDistance, double secondaryDistance)
        {
            if (_isSwipeDismissDragging)
                return true;

            if (primaryDistance < SwipeDismissDragStartThreshold)
                return false;

            return primaryDistance >= secondaryDistance * SwipeDismissAxisDominanceRatio;
        }

        private double GetSwipeDismissDistance(DesktopFlyoutIsland island)
        {
            var transform = EnsureIslandTransform(island);

            var closedX = GetClosedXOffset(_activePopupDirection);
            if (closedX != 0)
                return Math.Max(0, Math.Sign(closedX) * transform.TranslateX);

            var closedY = GetClosedYOffset(_activePopupDirection);
            return closedY == 0
                ? 0
                : Math.Max(0, Math.Sign(closedY) * transform.TranslateY);
        }

        private double GetSwipeDismissMaxDistance()
        {
            var closedX = GetClosedXOffset(_activePopupDirection);
            if (closedX != 0)
                return Math.Abs(closedX);

            return Math.Abs(GetClosedYOffset(_activePopupDirection));
        }

        private double GetResolvedSwipeDismissThreshold()
        {
            if (double.IsNaN(SwipeDismissThreshold) || double.IsInfinity(SwipeDismissThreshold))
                return Math.Min(80.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));

            return Clamp(SwipeDismissThreshold, 1.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));
        }

        private void AnimateSwipeDismissRestore(DesktopFlyoutIsland island)
        {
            StopSwipeDismissRestoreStoryboard();

            if (!IsTransitionAnimationEnabled)
            {
                SetIslandTransformTranslation(island, 0, 0);
                RestartAutoCloseTimer();
                return;
            }

            var transform = EnsureIslandTransform(island);
            _swipeDismissRestoreIsland = island;
            _swipeDismissRestoreStoryboard = GetOpenStoryboard(transform, _activePopupDirection, true);
            _swipeDismissRestoreStoryboard.Completed += SwipeDismissRestoreStoryboard_Completed;
            _swipeDismissRestoreStoryboard.Begin();
        }

        private void StopSwipeDismissRestoreStoryboard()
        {
            if (_swipeDismissRestoreStoryboard is null)
                return;

            var island = _swipeDismissRestoreIsland;
            _swipeDismissRestoreStoryboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
            _swipeDismissRestoreStoryboard.Stop();
            _swipeDismissRestoreStoryboard = null;
            _swipeDismissRestoreIsland = null;

            if (island is not null)
                SetIslandTransformTranslation(island, 0, 0);
        }

        private void SwipeDismissRestoreStoryboard_Completed(object? sender, object e)
        {
            if (sender is not Storyboard storyboard)
                return;

            storyboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
            storyboard.Stop();

            var island = _swipeDismissRestoreIsland;
            if (ReferenceEquals(_swipeDismissRestoreStoryboard, storyboard))
            {
                _swipeDismissRestoreStoryboard = null;
                _swipeDismissRestoreIsland = null;
            }

            if (island is not null)
                SetIslandTransformTranslation(island, 0, 0);

            RestartAutoCloseTimer();
        }

        private void BeginTransition(bool isClosing, bool fromCurrentTransform)
        {
            StopTransitionStoryboards();

            _isClosingTransition = isClosing;
            _pendingTransitionStoryboardCount = 0;

            foreach (var island in Islands)
            {
                var transform = EnsureIslandTransform(island);
                var storyboard = isClosing
                    ? GetCloseStoryboard(transform, _activePopupDirection, fromCurrentTransform)
                    : GetOpenStoryboard(transform, _activePopupDirection, fromCurrentTransform);

                storyboard.Completed += TransitionStoryboard_Completed;
                _transitionStoryboards.Add(storyboard);
                _pendingTransitionStoryboardCount++;
                storyboard.Begin();
            }

            if (_pendingTransitionStoryboardCount == 0)
            {
                if (isClosing)
                    CompleteClose();
                else
                    CompleteOpen();
            }
        }

        private void TransitionStoryboard_Completed(object? sender, object e)
        {
            if (sender is Storyboard storyboard)
            {
                storyboard.Completed -= TransitionStoryboard_Completed;
                storyboard.Stop();
                _transitionStoryboards.Remove(storyboard);
            }

            _pendingTransitionStoryboardCount--;
            if (_pendingTransitionStoryboardCount > 0)
                return;

            if (_isClosingTransition)
                CompleteClose();
            else
                CompleteOpen();
        }

        private void StopTransitionStoryboards()
        {
            foreach (var storyboard in _transitionStoryboards)
            {
                storyboard.Completed -= TransitionStoryboard_Completed;
                storyboard.Stop();
            }

            _transitionStoryboards.Clear();
            _pendingTransitionStoryboardCount = 0;
        }

        private void StartPressedScale(DesktopFlyoutIsland island)
        {
            _pressedScaleIslands.Add(island);
            AnimatePressedScale(island, GetResolvedPressedScale(), TimeSpan.FromMilliseconds(110));
        }

        private void RestorePressedScale()
        {
            List<DesktopFlyoutIsland> islands = [.. _pressedScaleIslands];
            foreach (var island in islands)
                RestorePressedScale(island);
        }

        private void RestorePressedScale(DesktopFlyoutIsland island)
        {
            if (!_pressedScaleIslands.Remove(island))
                return;

            AnimatePressedScale(island, 1.0D, TimeSpan.FromMilliseconds(240));
        }

        private void ResetPressedScale()
        {
            StopPressedScaleStoryboards();
            _pressedScaleIslands.Clear();

            foreach (var island in Islands)
                SetIslandScale(island, 1.0D);
        }

        private void ResetPressedScale(DesktopFlyoutIsland island)
        {
            StopPressedScaleStoryboard(island);
            _pressedScaleIslands.Remove(island);
            SetIslandScale(island, 1.0D);
        }

        private void AnimatePressedScale(DesktopFlyoutIsland island, double scale, TimeSpan duration)
        {
            StopPressedScaleStoryboard(island);

            var transform = EnsureIslandTransform(island);
            var storyboard = TransitionHelpers.GetPressedScaleTransitionStoryboard(
                transform,
                transform.ScaleX,
                transform.ScaleY,
                scale,
                duration);
            _pressedScaleStoryboards[island] = storyboard;
            _pressedScaleStoryboardStates[storyboard] = (island, scale);
            storyboard.Completed += PressScaleStoryboard_Completed;
            storyboard.Begin();
        }

        private void StopPressedScaleStoryboard(DesktopFlyoutIsland island)
        {
            if (!_pressedScaleStoryboards.Remove(island, out var storyboard))
                return;

            _pressedScaleStoryboardStates.Remove(storyboard);
            storyboard.Completed -= PressScaleStoryboard_Completed;
            storyboard.Stop();
        }

        private void StopPressedScaleStoryboards()
        {
            List<Storyboard> storyboards = [.. _pressedScaleStoryboards.Values];
            _pressedScaleStoryboards.Clear();
            _pressedScaleStoryboardStates.Clear();

            foreach (var storyboard in storyboards)
            {
                storyboard.Completed -= PressScaleStoryboard_Completed;
                storyboard.Stop();
            }
        }

        private void PressScaleStoryboard_Completed(object? sender, object e)
        {
            if (sender is not Storyboard storyboard)
                return;

            storyboard.Completed -= PressScaleStoryboard_Completed;
            storyboard.Stop();

            if (!_pressedScaleStoryboardStates.Remove(storyboard, out var state))
                return;

            if (_pressedScaleStoryboards.TryGetValue(state.Island, out var currentStoryboard) &&
                ReferenceEquals(currentStoryboard, storyboard))
            {
                _pressedScaleStoryboards.Remove(state.Island);
            }

            SetIslandScale(state.Island, state.TargetScale);
            if (Math.Abs(state.TargetScale - 1.0D) <= 0.001D)
                _pressedScaleIslands.Remove(state.Island);
        }

        private bool IsPressedScaleEnabled()
        {
            return Math.Abs(GetResolvedPressedScale() - 1.0D) > 0.001D;
        }

        private double GetResolvedPressedScale()
        {
            if (double.IsNaN(PressedScale) || double.IsInfinity(PressedScale))
                return 1.0D;

            return Clamp(PressedScale, 0.1D, 2.0D);
        }

        private void UpdateFocusSuppression()
        {
            if (ActivationMode is DesktopFlyoutActivationMode.NeverActivate)
                SuppressFocus();
            else
                RestoreFocusSuppression();
        }

        private void SuppressFocus()
        {
            RestoreFocusSuppression();

            foreach (var island in Islands)
                SuppressFocus(island);
        }

        private void SuppressFocus(DependencyObject element)
        {
            if (element is Control control)
            {
                _suppressedTabStopStates.Add((control, control.IsTabStop));
                control.IsTabStop = false;
            }

            if (element is FrameworkElement frameworkElement)
            {
                _suppressedInteractionFocusStates.Add((frameworkElement, frameworkElement.AllowFocusOnInteraction));
                frameworkElement.AllowFocusOnInteraction = false;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (var index = 0; index < childCount; index++)
                SuppressFocus(VisualTreeHelper.GetChild(element, index));
        }

        private void RestoreFocusSuppression()
        {
            foreach (var (element, allowFocusOnInteraction) in _suppressedInteractionFocusStates)
                element.AllowFocusOnInteraction = allowFocusOnInteraction;

            foreach (var (control, isTabStop) in _suppressedTabStopStates)
                control.IsTabStop = isTabStop;

            _suppressedInteractionFocusStates.Clear();
            _suppressedTabStopStates.Clear();
        }

        private Storyboard GetOpenStoryboard(CompositeTransform transform, DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
        {
            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : GetClosedYOffset(popupDirection), 0)
                : TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : GetClosedXOffset(popupDirection), 0);
        }

        private Storyboard GetCloseStoryboard(CompositeTransform transform, DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
        {
            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11TopToBottomTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : 0, GetClosedYOffset(popupDirection))
                : TransitionHelpers.GetWindows11LeftToRightTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : 0, GetClosedXOffset(popupDirection));
        }

        private void SetOpenTransform()
        {
            SetTransformTranslation(0, 0);
        }

        private void SetClosedTransform(DesktopFlyoutPopupDirection popupDirection)
        {
            SetTransformTranslation(GetClosedXOffset(popupDirection), GetClosedYOffset(popupDirection));
        }

        private void SetTransformTranslation(double translateX, double translateY)
        {
            foreach (var island in Islands)
                SetIslandTransformTranslation(island, translateX, translateY);
        }

        private static void SetIslandTransformTranslation(DesktopFlyoutIsland island, double translateX, double translateY)
        {
            var transform = EnsureIslandTransform(island);
            transform.TranslateX = translateX;
            transform.TranslateY = translateY;
        }

        private static void SetIslandScale(DesktopFlyoutIsland island, double scale)
        {
            var transform = EnsureIslandTransform(island);
            transform.ScaleX = scale;
            transform.ScaleY = scale;
        }

        private static CompositeTransform EnsureIslandTransform(DesktopFlyoutIsland island)
        {
            if (island.RenderTransform is CompositeTransform compositeTransform)
            {
                UpdateIslandTransformCenter(island, compositeTransform);
                return compositeTransform;
            }

            var transform = new CompositeTransform()
            {
                ScaleX = 1.0D,
                ScaleY = 1.0D,
            };

            if (island.RenderTransform is TranslateTransform translateTransform)
            {
                transform.TranslateX = translateTransform.X;
                transform.TranslateY = translateTransform.Y;
            }

            island.RenderTransform = transform;
            UpdateIslandTransformCenter(island, transform);
            return transform;
        }

        private static void UpdateIslandTransformCenter(DesktopFlyoutIsland island, CompositeTransform transform)
        {
            var width = GetActualOrExplicitLength(island.ActualWidth, island.Width);
            var height = GetActualOrExplicitLength(island.ActualHeight, island.Height);
            if (width > 0)
                transform.CenterX = width / 2.0D;

            if (height > 0)
                transform.CenterY = height / 2.0D;
        }

        private static double GetActualOrExplicitLength(double actualLength, double explicitLength)
        {
            if (actualLength > 0 && !double.IsNaN(actualLength) && !double.IsInfinity(actualLength))
                return actualLength;

            if (explicitLength > 0 && !double.IsNaN(explicitLength) && !double.IsInfinity(explicitLength))
                return explicitLength;

            return 0;
        }

        private int GetClosedXOffset(DesktopFlyoutPopupDirection popupDirection)
        {
            var shadowMargin = GetIslandShadowMargin();
            var shadowWidth = shadowMargin.Left + shadowMargin.Right;
            return popupDirection switch
            {
                DesktopFlyoutPopupDirection.LeftToRight => -(int)Math.Ceiling(_currentFlyoutWidth + Margin.Left + shadowWidth),
                DesktopFlyoutPopupDirection.RightToLeft => (int)Math.Ceiling(_currentFlyoutWidth + Margin.Right + shadowWidth),
                _ => 0,
            };
        }

        private int GetClosedYOffset(DesktopFlyoutPopupDirection popupDirection)
        {
            var shadowMargin = GetIslandShadowMargin();
            var shadowHeight = shadowMargin.Top + shadowMargin.Bottom;
            return popupDirection switch
            {
                DesktopFlyoutPopupDirection.TopToBottom => -(int)Math.Ceiling(_currentFlyoutHeight + Margin.Top + shadowHeight),
                DesktopFlyoutPopupDirection.BottomToTop => (int)Math.Ceiling(_currentFlyoutHeight + Margin.Bottom + shadowHeight),
                _ => 0,
            };
        }

        private bool HasStarIslandWidth()
        {
            foreach (var island in Islands)
            {
                if (island.IslandWidth.IsStar)
                    return true;
            }

            return false;
        }

        private bool HasStarIslandHeight()
        {
            foreach (var island in Islands)
            {
                if (island.IslandHeight.IsStar)
                    return true;
            }

            return false;
        }

        private List<DesktopFlyoutIsland> GetSpatiallyOrderedIslands()
        {
            List<DesktopFlyoutIsland> orderedIslands = [];
            foreach (var island in Islands)
                orderedIslands.Add(island);

            orderedIslands.Sort(CompareIslandSpatialOrder);
            return orderedIslands;
        }

        private int CompareIslandSpatialOrder(DesktopFlyoutIsland x, DesktopFlyoutIsland y)
        {
            var xRect = GetIslandSpatialRect(x);
            var yRect = GetIslandSpatialRect(y);
            var rowThreshold = Math.Max(16.0D, Math.Min(xRect.Height, yRect.Height) * 0.5D);
            if (Math.Abs(xRect.Y - yRect.Y) > rowThreshold)
                return xRect.Y.CompareTo(yRect.Y);

            if (Math.Abs(xRect.X - yRect.X) > 0.001D)
                return xRect.X.CompareTo(yRect.X);

            return Islands.IndexOf(x).CompareTo(Islands.IndexOf(y));
        }

        private FoundationRect GetIslandSpatialRect(DesktopFlyoutIsland island)
        {
            if (_islandHosts.TryGetValue(island, out var host) && IsOpen)
                return host.ContentWindowSize;

            if (_islandLayoutRects.TryGetValue(island, out var rect))
                return rect;

            return new(GetFiniteOrZero(island.CanvasLeft), GetFiniteOrZero(island.CanvasTop), Math.Max(1, island.ActualWidth), Math.Max(1, island.ActualHeight));
        }

        private static DesktopFlyoutIsland? FindAncestorIsland(DependencyObject element)
        {
            var current = element;
            while (current is not null)
            {
                if (current is DesktopFlyoutIsland island)
                    return island;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static bool TryGetScaledDragRegionRect(DesktopFlyoutDragRegion region, DesktopFlyoutIsland island, double scale, Thickness shadowMargin, out RectInt32 rect)
        {
            rect = default;
            if (region.Visibility is not Visibility.Visible || region.ActualWidth <= 0 || region.ActualHeight <= 0)
                return false;

            FoundationRect bounds;
            try
            {
                bounds = region.TransformToVisual(island).TransformBounds(new(0, 0, region.ActualWidth, region.ActualHeight));
            }
            catch (ArgumentException)
            {
                return false;
            }

            var islandWidth = island.ActualWidth > 0 ? island.ActualWidth : island.Width;
            var islandHeight = island.ActualHeight > 0 ? island.ActualHeight : island.Height;
            var left = Clamp(bounds.X, 0, islandWidth);
            var top = Clamp(bounds.Y, 0, islandHeight);
            var right = Clamp(bounds.X + bounds.Width, 0, islandWidth);
            var bottom = Clamp(bounds.Y + bounds.Height, 0, islandHeight);
            if (right <= left || bottom <= top)
                return false;

            var scaledLeft = (int)Math.Floor(left * scale);
            var scaledTop = (int)Math.Floor(top * scale);
            var scaledRight = (int)Math.Ceiling(right * scale);
            var scaledBottom = (int)Math.Ceiling(bottom * scale);
            var scaledShadowLeft = Math.Max(0, (int)Math.Floor(shadowMargin.Left * scale));
            var scaledShadowTop = Math.Max(0, (int)Math.Floor(shadowMargin.Top * scale));
            rect = new(
                scaledLeft + scaledShadowLeft,
                scaledTop + scaledShadowTop,
                Math.Max(1, scaledRight - scaledLeft),
                Math.Max(1, scaledBottom - scaledTop));
            return true;
        }

        private bool TryEnqueueOnFlyoutDispatcher(Action action)
        {
#if UWP
            _ = Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () => action());
            return true;
#elif WASDK
            return DispatcherQueue.TryEnqueue(() => action());
#endif
        }

        private static double ResolveFlyoutLength(GridLength length, double availableLength, bool stretchWhenAuto, double desiredLength)
        {
            if (length.IsAuto)
                return stretchWhenAuto ? availableLength : Math.Min(Math.Max(1, desiredLength), availableLength);

            if (length.IsStar)
                return availableLength;

            return Clamp(length.Value, 1, availableLength);
        }

        private static Thickness GetScaledMargin(Thickness margin, double scale)
        {
            return new(
                margin.Left * scale,
                margin.Top * scale,
                margin.Right * scale,
                margin.Bottom * scale);
        }

        private static Thickness AddThickness(Thickness left, Thickness right)
        {
            return new(
                left.Left + right.Left,
                left.Top + right.Top,
                left.Right + right.Right,
                left.Bottom + right.Bottom);
        }

        private static (double Left, double Top) GetCustomPlacementOrigin(
            Point point,
            int regionWidth,
            int regionHeight,
            FoundationSize flyoutSize,
            Thickness scaledMargin,
            Rectangle workArea)
        {
            if (WindowHelpers.TryGetTaskbarInfoForPoint(point, out _, out var taskbarEdge))
            {
                return taskbarEdge switch
                {
                    TaskbarEdge.Left => (workArea.Left, point.Y - (regionHeight / 2D)),
                    TaskbarEdge.Top => (point.X - (regionWidth / 2D), workArea.Top),
                    TaskbarEdge.Right => (workArea.Right - regionWidth, point.Y - (regionHeight / 2D)),
                    _ => (point.X - (regionWidth / 2D), workArea.Bottom - regionHeight),
                };
            }

            return (
                point.X - (flyoutSize.Width / 2D) - scaledMargin.Left,
                point.Y - flyoutSize.Height - scaledMargin.Top);
        }

        private static (double Left, double Top) GetPlacementOrigin(DesktopFlyoutPlacementMode placement, double width, double height, Rectangle workArea)
        {
            return placement switch
            {
                DesktopFlyoutPlacementMode.BottomCenter => (workArea.Left + ((workArea.Width - width) / 2), workArea.Bottom - height),
                DesktopFlyoutPlacementMode.BottomLeft => (workArea.Left, workArea.Bottom - height),
                DesktopFlyoutPlacementMode.BottomRight => (workArea.Right - width, workArea.Bottom - height),
                DesktopFlyoutPlacementMode.TopCenter => (workArea.Left + ((workArea.Width - width) / 2), workArea.Top),
                DesktopFlyoutPlacementMode.TopLeft => (workArea.Left, workArea.Top),
                DesktopFlyoutPlacementMode.TopRight => (workArea.Right - width, workArea.Top),
                DesktopFlyoutPlacementMode.LeftCenter => (workArea.Left, workArea.Top + ((workArea.Height - height) / 2)),
                DesktopFlyoutPlacementMode.RightCenter => (workArea.Right - width, workArea.Top + ((workArea.Height - height) / 2)),
                _ => (workArea.Right - width, workArea.Bottom - height),
            };
        }

        private static DesktopFlyoutPopupDirection ResolvePopupDirection(DesktopFlyoutPopupDirection requestedDirection, RectInt32 region, Rectangle workArea)
        {
            return requestedDirection switch
            {
                DesktopFlyoutPopupDirection.BottomToTop => DesktopFlyoutPopupDirection.BottomToTop,
                DesktopFlyoutPopupDirection.TopToBottom => DesktopFlyoutPopupDirection.TopToBottom,
                DesktopFlyoutPopupDirection.LeftToRight => DesktopFlyoutPopupDirection.LeftToRight,
                DesktopFlyoutPopupDirection.RightToLeft => DesktopFlyoutPopupDirection.RightToLeft,
                DesktopFlyoutPopupDirection.Horizontal => IsRightHalf(region, workArea) ? DesktopFlyoutPopupDirection.RightToLeft : DesktopFlyoutPopupDirection.LeftToRight,
                _ => IsBottomHalf(region, workArea) ? DesktopFlyoutPopupDirection.BottomToTop : DesktopFlyoutPopupDirection.TopToBottom,
            };
        }

        private static bool IsBottomHalf(RectInt32 region, Rectangle workArea)
        {
            return region.Y + (region.Height / 2D) >= workArea.Top + (workArea.Height / 2D);
        }

        private static bool IsRightHalf(RectInt32 region, Rectangle workArea)
        {
            return region.X + (region.Width / 2D) >= workArea.Left + (workArea.Width / 2D);
        }

        private static bool IsVerticalDirection(DesktopFlyoutPopupDirection popupDirection)
        {
            return popupDirection is DesktopFlyoutPopupDirection.BottomToTop or DesktopFlyoutPopupDirection.TopToBottom;
        }

        private bool ShouldActivateOnOpen()
        {
            return ActivationMode is DesktopFlyoutActivationMode.Activate;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), Math.Max(min, max));
        }

        private static double GetFiniteOrZero(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }

        private void HostWindow_Inactivated(object? sender, EventArgs e)
        {
            if (HideOnLostFocus)
                RestartLostFocusCloseTimer();
        }

#if WASDK
        private void HostWindow_NativeMoveSizeStarted(object? sender, EventArgs e)
        {
            StopAutoCloseTimer();
            ReleaseCapturedPointers();
            ResetSwipeDismissTracking();
            RestorePressedScale();
        }

        private void HostWindow_NativeMoveSizeEnded(object? sender, EventArgs e)
        {
            if (IsOpen && !_isPopupAnimationPlaying)
            {
                UpdateHostDragRegions();
                RestartAutoCloseTimer();
            }
        }
#endif

        private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            UpdateFlyoutTheme();
            UpdateIslandBackdrops();
        }

        private void HostWindow_TakeFocusRequested(object? sender, XamlSourceFocusNavigationRequest request)
        {
            if (_isNavigatingFocusAcrossHosts || ActivationMode is DesktopFlyoutActivationMode.NeverActivate || sender is not XamlIslandHostWindow sourceHost)
                return;

            DesktopFlyoutIsland? sourceIsland = null;
            foreach (var item in _islandHosts)
            {
                if (ReferenceEquals(item.Value, sourceHost))
                {
                    sourceIsland = item.Key;
                    break;
                }
            }

            if (sourceIsland is null)
                return;

            var orderedIslands = GetSpatiallyOrderedIslands();
            var sourceIndex = orderedIslands.IndexOf(sourceIsland);
            if (sourceIndex < 0)
                return;

            var isBackward = request.Reason is XamlSourceFocusNavigationReason.Last;
            var targetIndex = isBackward ? sourceIndex - 1 : sourceIndex + 1;
            if (targetIndex < 0 || targetIndex >= orderedIslands.Count)
                return;

            var targetIsland = orderedIslands[targetIndex];
            if (!_islandHosts.TryGetValue(targetIsland, out var targetHost))
                return;

            _isNavigatingFocusAcrossHosts = true;
            try
            {
                targetHost.NavigateFocus(isBackward ? XamlSourceFocusNavigationReason.Last : XamlSourceFocusNavigationReason.First);
            }
            finally
            {
                _isNavigatingFocusAcrossHosts = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            StopLostFocusCloseTimer();
            StopTransitionStoryboards();
            StopPressedScaleStoryboards();
            StopSwipeDismissRestoreStoryboard();
            ReleaseCapturedPointers();
            RestoreFocusSuppression();

            foreach (var item in _islandHosts)
            {
                DetachIslandEvents(item.Key);
                item.Value.WindowInactivated -= HostWindow_Inactivated;
#if WASDK
                item.Value.NativeMoveSizeStarted -= HostWindow_NativeMoveSizeStarted;
                item.Value.NativeMoveSizeEnded -= HostWindow_NativeMoveSizeEnded;
#endif
                item.Value.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
                item.Value.TakeFocusRequested -= HostWindow_TakeFocusRequested;
                item.Value.Dispose();
            }

            _islandHosts.Clear();
            IsOpen = false;

            GC.SuppressFinalize(this);
        }
    }
}
