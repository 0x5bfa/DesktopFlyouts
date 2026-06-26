// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics;
using FoundationPoint = Windows.Foundation.Point;
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
    /// Displays a desktop flyout in an independent XAML island window.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DesktopFlyout"/> when you need a lightweight desktop surface that can be opened
    /// from a tray icon or from application code. Add one or more <see cref="DesktopFlyoutIsland"/>
    /// instances to define the visible sections, then call <see cref="Show()"/> or the point-based
    /// overload to display the flyout. Dispose the instance when it is no longer used
    /// so the underlying host window and XAML island can be released.
    /// </remarks>
    [ContentProperty(Name = nameof(Islands))]
    public partial class DesktopFlyout : Control, IDisposable
    {
        private const string PART_RootGrid = "PART_RootGrid";
        private const string PART_IslandsItemsControl = "PART_IslandsItemsControl";
        private const double SwipeDismissDragStartThreshold = 4.0D;
        private const double SwipeDismissAxisDominanceRatio = 1.2D;

        private readonly XamlIslandHostWindow? _host;
        private bool _isPopupAnimationPlaying;
        private bool _isPressAnimationActive;
        private Point? _customPlacementBottomCenterPoint;
        private DesktopFlyoutPopupDirection _activePopupDirection = DesktopFlyoutPopupDirection.BottomToTop;
        private DispatcherTimer? _autoCloseTimer;
        private DispatcherTimer? _restoreActivationTimer;
        private Storyboard? _pressScaleStoryboard;
        private Storyboard? _swipeDismissRestoreStoryboard;
        private FoundationPoint _swipeDismissStartPoint;
        private double _pressScaleTargetScale = 1.0D;
        private uint _swipeDismissPointerId;
        private int _restoreActivationTickCount;
        private readonly List<(Control Control, bool IsTabStop)> _suppressedTabStopStates = [];
        private readonly List<(FrameworkElement Element, bool AllowFocusOnInteraction)> _suppressedInteractionFocusStates = [];
        private bool _isSwipeDismissTracking;
        private bool _isSwipeDismissDragging;
        private bool _isFocusManagerGettingFocusSubscribed;
        private bool _disposed;

        private Grid? RootGrid;
        private ItemsControl? IslandsItemsControl;

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopFlyout"/>.
        /// </summary>
        /// <remarks>
        /// The constructor creates the hidden desktop host window used to display the flyout.
        /// </remarks>
        public DesktopFlyout()
        {
            DefaultStyleKey = typeof(DesktopFlyout);

            _host = new XamlIslandHostWindow();
            _host.SetContent(this);
            _host.UpdateWindowVisibility(false);
            _host.WindowInactivated += HostWindow_Inactivated;
            _host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RootGrid?.GettingFocus -= RootGrid_GettingFocus;
            RootGrid?.PointerPressed -= RootGrid_PointerPressed;
            RootGrid?.PointerMoved -= RootGrid_PointerMoved;
            RootGrid?.PointerReleased -= RootGrid_PointerReleased;
            RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
            RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
            RootGrid?.PointerExited -= RootGrid_PointerExited;
            if (IslandsItemsControl is not null)
                IslandsItemsControl.ItemsSource = null;

            RootGrid = GetTemplateChild(PART_RootGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(DesktopFlyout)}'s style.");
            IslandsItemsControl = GetTemplateChild(PART_IslandsItemsControl) as ItemsControl
                ?? throw new MissingFieldException($"Could not find {PART_IslandsItemsControl} in the given {nameof(DesktopFlyout)}'s style.");
            IslandsItemsControl.ItemsSource = Islands;

            EnsureRootTransform();

            RootGrid.GettingFocus += RootGrid_GettingFocus;
            RootGrid.PointerPressed += RootGrid_PointerPressed;
            RootGrid.PointerMoved += RootGrid_PointerMoved;
            RootGrid.PointerReleased += RootGrid_PointerReleased;
            RootGrid.PointerCanceled += RootGrid_PointerCanceled;
            RootGrid.PointerCaptureLost += RootGrid_PointerCaptureLost;
            RootGrid.PointerExited += RootGrid_PointerExited;

            if (!_isFocusManagerGettingFocusSubscribed)
            {
                FocusManager.GettingFocus += FocusManager_GettingFocus;
                _isFocusManagerGettingFocusSubscribed = true;
            }

            UpdateIslands();
        }

        /// <summary>
        /// Opens the flyout using its configured placement.
        /// </summary>
        /// <remarks>
        /// The flyout is positioned from <see cref="Placement"/> and animated from the direction
        /// resolved by <see cref="PopupDirection"/>. Calls made while an open or close transition is
        /// running are ignored. <see cref="IsOpen"/> becomes <see langword="true"/> after the open
        /// transition completes.
        /// </remarks>
        public void Show()
        {
            if (_disposed || _host?.DesktopWindowXamlSource is null || RootGrid is null || _isPopupAnimationPlaying)
            {
                _customPlacementBottomCenterPoint = null;
                return;
            }

            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            _isPopupAnimationPlaying = true;
            var shouldActivateOnOpen = ShouldActivateOnOpen();
            if (!shouldActivateOnOpen)
                _host.PreserveActivationState();

            _host.SetActivationMode(ActivationMode);
            _host.Maximize(WindowHelpers.GetFlyoutWorkAreaRect(_customPlacementBottomCenterPoint), shouldActivateOnOpen);

            _ = Task.Run(async () =>
            {
#if UWP
                await RootGrid.Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, async () =>
#elif WASDK
                RootGrid.DispatcherQueue.TryEnqueue(async () =>
#endif
                {
                    ResetResolvedFlyoutSize();
                    UpdateLayout();
                    await Task.Delay(1);
                    ApplyResolvedFlyoutSize();

                    UpdateLayout();
                    await Task.Delay(1);

                    UpdateFlyoutTheme();
                    UpdateFocusSuppression();
                    _activePopupDirection = UpdateFlyoutRegion();

                    // Ensure to hide first
                    SetClosedTransform(_activePopupDirection);

                    UpdateLayout();
                    await Task.Delay(1);

                    _host.UpdateWindowVisibility(true, shouldActivateOnOpen);
                    if (!shouldActivateOnOpen)
                        RestartRestoreActivationTimer();

                    if (IsTransitionAnimationEnabled)
                    {
                        var storyboard = GetOpenStoryboard(_activePopupDirection);
                        storyboard.Completed += OpenAnimationStoryboard_Completed;
                        storyboard.Begin();
                    }
                    else
                    {
                        CompleteOpen();
                    }
                });
            });
        }

        /// <summary>
        /// Opens the flyout at the specified bottom-center screen point.
        /// </summary>
        /// <param name="bottomCenterPoint">The desired bottom-center point of the flyout in physical screen pixels.</param>
        /// <remarks>
        /// This overload is useful when opening from a tray icon or another screen-space target.
        /// The requested point is used for this open operation only; later calls to <see cref="Show()"/>
        /// return to the configured <see cref="Placement"/>.
        /// </remarks>
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
        /// <remarks>
        /// Calls made while an open or close transition is running are ignored. <see cref="IsOpen"/>
        /// becomes <see langword="false"/> after the close transition completes.
        /// </remarks>
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

        private void Hide(bool closeFromCurrentTransform)
        {
            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            StopSwipeDismissRestoreStoryboard();
            ResetSwipeDismissTracking();

            if (_disposed || RootGrid is null || _isPopupAnimationPlaying)
                return;

            _isPopupAnimationPlaying = true;
            if (!closeFromCurrentTransform)
                SetOpenTransform();

            if (IsTransitionAnimationEnabled)
            {
                var storyboard = GetCloseStoryboard(_activePopupDirection, closeFromCurrentTransform);
                storyboard.Completed += CloseAnimationStoryboard_Completed;
                storyboard.Begin();
            }
            else
            {
                CompleteClose();
            }
        }

        /// <summary>
        /// Moves focus into the flyout's XAML island.
        /// </summary>
        /// <param name="reason">The focus navigation reason used by the XAML hosting layer.</param>
        /// <remarks>
        /// Focus cannot be moved into the flyout while <see cref="ActivationMode"/> is
        /// <see cref="DesktopFlyoutActivationMode.NeverActivate"/>.
        /// </remarks>
        public void NavigateFocus(XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic)
        {
            _host?.NavigateFocus(reason);
        }

#if UWP
        /// <summary>
        /// Lets the XAML island process a native keyboard message before dispatch.
        /// </summary>
        /// <param name="msg">The native message to process.</param>
        /// <returns><see langword="true"/> if the message was handled; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// UWP desktop-host scenarios should call this from their native message loop so keyboard
        /// navigation and accelerator processing can reach the hosted XAML island.
        /// </remarks>
        public unsafe bool TryPreTranslateMessage(MSG* msg)
        {
            return _host?.TryPreTranslateMessage(msg) ?? false;
        }
#endif

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

        private void UpdateIslands()
        {
            if (IslandsItemsControl is null)
                return;

            for (var index = 0; index < Islands.Count; index++)
            {
                if (Islands[index] is DesktopFlyoutIsland island)
                    island.SetOwner(this);
            }

            IslandsItemsControl.InvalidateMeasure();
        }

        private DesktopFlyoutPopupDirection UpdateFlyoutRegion()
        {
            if (_host?.DesktopWindowXamlSource is null || IslandsItemsControl is null)
                return ResolvePopupDirection(PopupDirection, default, WindowHelpers.GetFlyoutWorkAreaRect(_customPlacementBottomCenterPoint));

            var customBottomCenterPoint = _customPlacementBottomCenterPoint;
            var scale = _host.XamlIslandRasterizationScale;
            var flyoutWidth = GetCurrentFlyoutWidth();
            var flyoutHeight = GetCurrentFlyoutHeight();
            var scaledFlyoutSize = new FoundationSize(flyoutWidth * scale, flyoutHeight * scale);
            var scaledMargin = GetScaledMargin(Margin, scale);
            var frameWidth = scaledFlyoutSize.Width + scaledMargin.Left + scaledMargin.Right;
            var frameHeight = scaledFlyoutSize.Height + scaledMargin.Top + scaledMargin.Bottom;
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(customBottomCenterPoint);
            var hostWidth = workArea.Width;
            var hostHeight = workArea.Height;
            var regionWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(frameWidth, hostWidth)));
            var regionHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(frameHeight, hostHeight)));
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
                    scaledFlyoutSize,
                    scaledMargin,
                    workArea);
            }
            else
            {
                (left, top) = GetPlacementOrigin(Placement, regionWidth, regionHeight, workArea);
            }

            left = Clamp(left, workArea.Left, workArea.Right - regionWidth);
            top = Clamp(top, workArea.Top, workArea.Bottom - regionHeight);

            var region = new RectInt32(
                (int)Math.Round(left),
                (int)Math.Round(top),
                (int)regionWidth,
                (int)regionHeight);

            _host.MoveAndResize(region, ShouldActivateOnOpen());
            _host.SetHWndRectRegion(new(0, 0, region.Width, region.Height));

            return ResolvePopupDirection(requestedPopupDirection, region, workArea);
        }

        internal void OnIslandSizeChanged()
        {
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        internal void OnIslandVisibilityChanged()
        {
            UpdateIslands();
            UpdateOpenFlyoutLayout();
        }

        private void UpdateOpenFlyoutLayout()
        {
            if (!IsOpen || _isPopupAnimationPlaying || RootGrid is null || _host?.DesktopWindowXamlSource is null)
                return;

            ResetResolvedFlyoutSize();
            UpdateLayout();
            ApplyResolvedFlyoutSize();
            UpdateLayout();
            _activePopupDirection = UpdateFlyoutRegion();
            SetOpenTransform();
        }

        private void ResetResolvedFlyoutSize()
        {
            if (RootGrid is null)
                return;

            RootGrid.Width = double.NaN;
            RootGrid.Height = double.NaN;
        }

        private void ApplyResolvedFlyoutSize()
        {
            if (RootGrid is null || _host is null)
                return;

            var (availableWidth, availableHeight) = GetAvailableFlyoutSizeInDips();
            RootGrid.Width = ResolveFlyoutLength(FlyoutWidth, availableWidth, HasStarIslandWidth());
            RootGrid.Height = ResolveFlyoutLength(FlyoutHeight, availableHeight, HasStarIslandHeight());
        }

        private (double Width, double Height) GetAvailableFlyoutSizeInDips()
        {
            if (_host is null)
                return (0, 0);

            var scale = _host.XamlIslandRasterizationScale;
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(_customPlacementBottomCenterPoint);
            var availableWidth = (workArea.Width / scale) - Margin.Left - Margin.Right;
            var availableHeight = (workArea.Height / scale) - Margin.Top - Margin.Bottom;

            return (Math.Max(0, availableWidth), Math.Max(0, availableHeight));
        }

        private bool HasStarIslandWidth()
        {
            foreach (var item in Islands)
            {
                if (item is DesktopFlyoutIsland island && IsIslandVisible(island) && island.IslandWidth.IsStar)
                    return true;
            }

            return false;
        }

        private bool HasStarIslandHeight()
        {
            foreach (var item in Islands)
            {
                if (item is DesktopFlyoutIsland island && IsIslandVisible(island) && island.IslandHeight.IsStar)
                    return true;
            }

            return false;
        }

        private static bool IsIslandVisible(DesktopFlyoutIsland island)
        {
            return island.Visibility is Visibility.Visible;
        }

        private void OpenAnimationStoryboard_Completed(object? sender, object e)
        {
            if (sender is not Storyboard storyboard)
                return;

            storyboard.Completed -= OpenAnimationStoryboard_Completed;
            storyboard.Stop();
            CompleteOpen();
        }

        private void CloseAnimationStoryboard_Completed(object? sender, object e)
        {
            if (sender is not Storyboard storyboard)
                return;

            storyboard.Completed -= CloseAnimationStoryboard_Completed;
            storyboard.Stop();
            CompleteClose();
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
            ResetSwipeDismissTracking();
            RestoreFocusSuppression();
            ResetPressedScale();
            SetClosedTransform(_activePopupDirection);
            _isPopupAnimationPlaying = false;
            IsOpen = false;
            _host?.UpdateWindowVisibility(false);
        }

        private void PrepareInitialFocus()
        {
            if (_host is null)
                return;

            IsTabStop = true;
            _host.NavigateFocus(XamlSourceFocusNavigationReason.Programmatic);

            // Keep the first real child unfocused until the user presses Tab.
            Focus(FocusState.Programmatic);
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
            _host?.RestoreActivationState();
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

        private void AutoCloseTimer_Tick(object? sender, object e)
        {
            StopAutoCloseTimer();

            if (IsOpen && !_isPopupAnimationPlaying)
                Hide();
        }

        private void RestoreActivationTimer_Tick(object? sender, object e)
        {
            _host?.RestoreActivationState();
            _restoreActivationTickCount++;

            if (_restoreActivationTickCount >= 8)
                StopRestoreActivationTimer();
        }

        private void RootGrid_GettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate)
                return;

            args.Cancel = true;
            args.Handled = true;
            _host?.RestoreActivationState();
        }

        private void FocusManager_GettingFocus(object? sender, GettingFocusEventArgs args)
        {
            if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate || args.NewFocusedElement is not DependencyObject newFocusedElement)
                return;

            if (!IsFlyoutElement(newFocusedElement))
                return;

            args.Cancel = true;
            args.Handled = true;
            _host?.RestoreActivationState();
        }

        private bool IsFlyoutElement(DependencyObject element)
        {
            var rootGrid = RootGrid;
            if (rootGrid is null)
                return false;

            if (ReferenceEquals(element, this) || ReferenceEquals(element, rootGrid))
                return true;

            var current = element;
            while (current is not null)
            {
                if (ReferenceEquals(current, this) || ReferenceEquals(current, rootGrid))
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return element is FrameworkElement frameworkElement &&
                rootGrid.XamlRoot is not null &&
                ReferenceEquals(frameworkElement.XamlRoot, rootGrid.XamlRoot);
        }

        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (RootGrid is null || _isPopupAnimationPlaying)
                return;

            var isPressedScaleEnabled = IsPressedScaleEnabled();
            var isSwipeDismissEnabled = CanStartSwipeDismiss();
            if (!isPressedScaleEnabled && !isSwipeDismissEnabled)
                return;

            StopSwipeDismissRestoreStoryboard();
            RootGrid.CapturePointer(e.Pointer);

            if (isSwipeDismissEnabled)
                StartSwipeDismiss(e);

            if (isPressedScaleEnabled)
            {
                _isPressAnimationActive = true;
                AnimatePressedScale(GetResolvedPressedScale(), TimeSpan.FromMilliseconds(110));
            }
        }

        private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (UpdateSwipeDismiss(e))
                e.Handled = true;
        }

        private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var dismissedBySwipe = CompleteSwipeDismiss(e);

            if (RootGrid is not null)
                RootGrid.ReleasePointerCapture(e.Pointer);

            if (!dismissedBySwipe)
                RestorePressedScale();
        }

        private void RootGrid_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            CancelSwipeDismiss();
            RestorePressedScale();
        }

        private void RootGrid_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            CancelSwipeDismiss();
            RestorePressedScale();
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isSwipeDismissDragging)
                RestorePressedScale();
        }

        private bool CanStartSwipeDismiss()
        {
            return IsSwipeToDismissEnabled &&
                IsOpen &&
                RootGrid is not null &&
                GetSwipeDismissMaxDistance() > 0;
        }

        private void StartSwipeDismiss(PointerRoutedEventArgs e)
        {
            if (RootGrid is null)
                return;

            _swipeDismissPointerId = e.Pointer.PointerId;
            _swipeDismissStartPoint = e.GetCurrentPoint(RootGrid).Position;
            _isSwipeDismissTracking = true;
            _isSwipeDismissDragging = false;
        }

        private bool UpdateSwipeDismiss(PointerRoutedEventArgs e)
        {
            if (!_isSwipeDismissTracking || RootGrid is null || e.Pointer.PointerId != _swipeDismissPointerId)
                return false;

            var currentPoint = e.GetCurrentPoint(RootGrid).Position;
            var deltaX = currentPoint.X - _swipeDismissStartPoint.X;
            var deltaY = currentPoint.Y - _swipeDismissStartPoint.Y;

            if (!TryGetSwipeDismissTranslation(deltaX, deltaY, out var translateX, out var translateY))
                return false;

            if (!_isSwipeDismissDragging)
            {
                _isSwipeDismissDragging = true;
                StopAutoCloseTimer();
                RestorePressedScale();
            }

            var transform = GetRootTransform();
            if (transform is null)
                return false;

            transform.TranslateX = translateX;
            transform.TranslateY = translateY;
            return true;
        }

        private bool CompleteSwipeDismiss(PointerRoutedEventArgs e)
        {
            if (!_isSwipeDismissTracking || e.Pointer.PointerId != _swipeDismissPointerId)
                return false;

            var shouldDismiss = _isSwipeDismissDragging && GetSwipeDismissDistance() >= GetResolvedSwipeDismissThreshold();
            var shouldRestore = _isSwipeDismissDragging && !shouldDismiss;
            ResetSwipeDismissTracking();

            if (shouldDismiss)
            {
                ResetPressedScale();
                Hide(true);
                return true;
            }

            if (shouldRestore)
                AnimateSwipeDismissRestore();

            return false;
        }

        private void CancelSwipeDismiss()
        {
            var shouldRestore = _isSwipeDismissDragging;
            ResetSwipeDismissTracking();

            if (shouldRestore)
                AnimateSwipeDismissRestore();
        }

        private void ResetSwipeDismissTracking()
        {
            _isSwipeDismissTracking = false;
            _isSwipeDismissDragging = false;
            _swipeDismissPointerId = 0;
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

        private double GetSwipeDismissDistance()
        {
            var transform = GetRootTransform();
            if (transform is null)
                return 0;

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

        private void AnimateSwipeDismissRestore()
        {
            StopSwipeDismissRestoreStoryboard();

            if (!IsTransitionAnimationEnabled)
            {
                SetOpenTransform();
                RestartAutoCloseTimer();
                return;
            }

            _swipeDismissRestoreStoryboard = GetOpenStoryboard(_activePopupDirection, true);
            _swipeDismissRestoreStoryboard.Completed += SwipeDismissRestoreStoryboard_Completed;
            _swipeDismissRestoreStoryboard.Begin();
        }

        private void StopSwipeDismissRestoreStoryboard()
        {
            if (_swipeDismissRestoreStoryboard is null)
                return;

            _swipeDismissRestoreStoryboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
            _swipeDismissRestoreStoryboard.Stop();
            _swipeDismissRestoreStoryboard = null;
        }

        private void SwipeDismissRestoreStoryboard_Completed(object? sender, object e)
        {
            if (sender is not Storyboard storyboard)
                return;

            storyboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
            storyboard.Stop();

            if (ReferenceEquals(_swipeDismissRestoreStoryboard, storyboard))
                _swipeDismissRestoreStoryboard = null;

            SetOpenTransform();
            RestartAutoCloseTimer();
        }

        private void RestorePressedScale()
        {
            if (!_isPressAnimationActive)
                return;

            _isPressAnimationActive = false;
            AnimatePressedScale(1.0D, TimeSpan.FromMilliseconds(240));
        }

        private void ResetPressedScale()
        {
            _isPressAnimationActive = false;
            StopPressedScaleStoryboard();
            _pressScaleStoryboard = null;
            _pressScaleTargetScale = 1.0D;

            var transform = GetRootTransform();
            if (transform is null)
                return;

            transform.ScaleX = 1.0D;
            transform.ScaleY = 1.0D;
        }

        private void AnimatePressedScale(double scale, TimeSpan duration)
        {
            var transform = GetRootTransform();
            if (transform is null)
                return;

            var currentScaleX = transform.ScaleX;
            var currentScaleY = transform.ScaleY;

            StopPressedScaleStoryboard();
            transform.ScaleX = currentScaleX;
            transform.ScaleY = currentScaleY;
            _pressScaleTargetScale = scale;
            _pressScaleStoryboard = TransitionHelpers.GetPressedScaleTransitionStoryboard(
                transform,
                currentScaleX,
                currentScaleY,
                scale,
                duration);
            _pressScaleStoryboard.Completed += PressScaleStoryboard_Completed;
            _pressScaleStoryboard.Begin();
        }

        private void StopPressedScaleStoryboard()
        {
            if (_pressScaleStoryboard is null)
                return;

            _pressScaleStoryboard.Completed -= PressScaleStoryboard_Completed;
            _pressScaleStoryboard.Stop();
            _pressScaleStoryboard = null;
        }

        private void PressScaleStoryboard_Completed(object? sender, object e)
        {
            if (!ReferenceEquals(sender, _pressScaleStoryboard))
                return;

            StopPressedScaleStoryboard();

            var transform = GetRootTransform();
            if (transform is null)
                return;

            transform.ScaleX = _pressScaleTargetScale;
            transform.ScaleY = _pressScaleTargetScale;
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
            if (RootGrid is null)
                return;

            RestoreFocusSuppression();
            SuppressFocus(RootGrid);
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

        private Storyboard GetOpenStoryboard(DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
        {
            var transform = GetRootTransform() ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : GetClosedYOffset(popupDirection), 0)
                : TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : GetClosedXOffset(popupDirection), 0);
        }

        private Storyboard GetCloseStoryboard(DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
        {
            var transform = GetRootTransform() ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11TopToBottomTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : 0, GetClosedYOffset(popupDirection))
                : TransitionHelpers.GetWindows11LeftToRightTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : 0, GetClosedXOffset(popupDirection));
        }

        private void SetOpenTransform()
        {
            var transform = GetRootTransform();
            if (transform is null)
                return;

            transform.TranslateX = 0;
            transform.TranslateY = 0;
        }

        private void SetClosedTransform(DesktopFlyoutPopupDirection popupDirection)
        {
            var transform = GetRootTransform();
            if (transform is null)
                return;

            transform.TranslateX = GetClosedXOffset(popupDirection);
            transform.TranslateY = GetClosedYOffset(popupDirection);
        }

        private CompositeTransform? GetRootTransform()
        {
            if (RootGrid is null)
                return null;

            return EnsureRootTransform();
        }

        private CompositeTransform EnsureRootTransform()
        {
            var rootGrid = RootGrid ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            if (rootGrid.RenderTransform is CompositeTransform compositeTransform)
                return compositeTransform;

            var transform = new CompositeTransform()
            {
                ScaleX = 1.0D,
                ScaleY = 1.0D,
            };

            if (rootGrid.RenderTransform is TranslateTransform translateTransform)
            {
                transform.TranslateX = translateTransform.X;
                transform.TranslateY = translateTransform.Y;
            }

            rootGrid.RenderTransform = transform;
            return transform;
        }

        private int GetClosedXOffset(DesktopFlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                DesktopFlyoutPopupDirection.LeftToRight => -(int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Left),
                DesktopFlyoutPopupDirection.RightToLeft => (int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Right),
                _ => 0,
            };
        }

        private int GetClosedYOffset(DesktopFlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                DesktopFlyoutPopupDirection.TopToBottom => -(int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Top),
                DesktopFlyoutPopupDirection.BottomToTop => (int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Bottom),
                _ => 0,
            };
        }

        private double GetCurrentFlyoutWidth()
        {
            return RootGrid?.ActualWidth > 0 ? RootGrid.ActualWidth : DesiredSize.Width;
        }

        private double GetCurrentFlyoutHeight()
        {
            return RootGrid?.ActualHeight > 0 ? RootGrid.ActualHeight : DesiredSize.Height;
        }

        private static double ResolveFlyoutLength(GridLength length, double availableLength, bool stretchWhenAuto)
        {
            if (length.IsAuto)
                return stretchWhenAuto ? availableLength : double.NaN;

            if (length.IsStar)
                return availableLength;

            return Clamp(length.Value, 0, availableLength);
        }

        private static Thickness GetScaledMargin(Thickness margin, double scale)
        {
            return new(
                margin.Left * scale,
                margin.Top * scale,
                margin.Right * scale,
                margin.Bottom * scale);
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

        private void HostWindow_Inactivated(object? sender, EventArgs e)
        {
            if (HideOnLostFocus) Hide();
        }

        private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            UpdateFlyoutTheme();
            UpdateIslandBackdrops();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopAutoCloseTimer();
            StopRestoreActivationTimer();
            RestoreFocusSuppression();

            _host?.WindowInactivated -= HostWindow_Inactivated;
            _host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
            RootGrid?.GettingFocus -= RootGrid_GettingFocus;
            RootGrid?.PointerPressed -= RootGrid_PointerPressed;
            RootGrid?.PointerMoved -= RootGrid_PointerMoved;
            RootGrid?.PointerReleased -= RootGrid_PointerReleased;
            RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
            RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
            RootGrid?.PointerExited -= RootGrid_PointerExited;
            if (IslandsItemsControl is not null)
                IslandsItemsControl.ItemsSource = null;

            if (_isFocusManagerGettingFocusSubscribed)
            {
                FocusManager.GettingFocus -= FocusManager_GettingFocus;
                _isFocusManagerGettingFocusSubscribed = false;
            }
            _host?.Dispose();
            IsOpen = false;

            GC.SuppressFinalize(this);
        }
    }
}
