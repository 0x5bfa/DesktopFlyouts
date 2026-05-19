// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics;
using FoundationPoint = Windows.Foundation.Point;

#if UWP
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
#elif WASDK
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace U5BFA.Libraries
{
    /// <summary>
    /// Displays a tray icon flyout hosted in a XAML island window.
    /// </summary>
    [ContentProperty(Name = nameof(Islands))]
    public partial class TrayIconFlyout : Control, IDisposable
    {
        private const string PART_RootGrid = "PART_RootGrid";
        private const string PART_IslandsGrid = "PART_IslandsGrid";
        private const double SwipeDismissDragStartThreshold = 4.0D;
        private const double SwipeDismissAxisDominanceRatio = 1.2D;

#if WASDK
        private static readonly PersistentAcrylicBackdrop _persistentBackdrop = new();
        private bool? _wasTaskbarLightLastTimeChecked;
#endif

        private readonly XamlIslandHostWindow? _host;
        private bool _isPopupAnimationPlaying;
        private bool _isPressAnimationActive;
        private Point? _customPlacementBottomCenterPoint;
        private FlyoutPopupDirection _activePopupDirection = FlyoutPopupDirection.BottomToTop;
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
        private Grid? IslandsGrid;

#if WASDK
        internal ContentBackdropManager? BackdropManager { get; private set; }
#endif

        /// <summary>
        /// Initializes a new instance of <see cref="TrayIconFlyout"/>.
        /// </summary>
        public TrayIconFlyout()
        {
            DefaultStyleKey = typeof(TrayIconFlyout);

            _host = new XamlIslandHostWindow();
            _host.SetContent(this);
            _host.UpdateWindowVisibility(false);
            _host.WindowInactivated += HostWindow_Inactivated;
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

            RootGrid = GetTemplateChild(PART_RootGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(TrayIconFlyout)}'s style.");
            IslandsGrid = GetTemplateChild(PART_IslandsGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_IslandsGrid} in the given {nameof(TrayIconFlyout)}'s style.");

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

#if WASDK
            LayoutUpdated += TrayIconFlyout_LayoutUpdated;
#endif

            UpdateIslands();
        }

        /// <summary>
        /// Opens the flyout using its configured placement.
        /// </summary>
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
            _host.Maximize(shouldActivateOnOpen);

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
#if WASDK
                    UpdateBackdropManager();
#endif
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
        /// <param name="bottomCenterPoint">The bottom-center screen point for the flyout.</param>
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
        public unsafe bool TryPreTranslateMessage(MSG* msg)
        {
            return _host?.TryPreTranslateMessage(msg) ?? false;
        }
#endif

#if WASDK
        private void UpdateBackdropManager(bool coerce = false)
        {
            if (IslandsGrid is null)
                return;

            var isTaskbarLight = GeneralHelpers.IsTaskbarLight();
            var isTaskbarColorPrevalence = GeneralHelpers.IsTaskbarColorPrevalenceEnabled();

            ISystemBackdropControllerWithTargets? controller = null;
            if (coerce)
            {
                controller = BackdropKind is BackdropKind.Acrylic
                ? (isTaskbarColorPrevalence
                  ? BackdropControllerHelpers.GetAccentedAcrylicController()
                  : isTaskbarLight
                    ? BackdropControllerHelpers.GetLightAcrylicController()
                    : BackdropControllerHelpers.GetDarkAcrylicController())
                : (isTaskbarColorPrevalence
                  ? BackdropControllerHelpers.GetAccentedMicaController()
                  : isTaskbarLight
                    ? BackdropControllerHelpers.GetLightMicaController()
                    : BackdropControllerHelpers.GetDarkMicaController());
            }
            else if (isTaskbarColorPrevalence)  // Force update backdrop when color prevalence is on, as the accent color might change
            {
                controller = BackdropKind is BackdropKind.Acrylic
                  ? BackdropControllerHelpers.GetAccentedAcrylicController()
                  : BackdropControllerHelpers.GetAccentedMicaController();
            }
            else if (_wasTaskbarLightLastTimeChecked != isTaskbarLight)
            {
                controller = BackdropKind is BackdropKind.Acrylic
                  ? (isTaskbarLight
                    ? BackdropControllerHelpers.GetLightAcrylicController()
                    : BackdropControllerHelpers.GetDarkAcrylicController())
                  : (isTaskbarLight
                    ? BackdropControllerHelpers.GetLightMicaController()
                    : BackdropControllerHelpers.GetDarkMicaController());
                _wasTaskbarLightLastTimeChecked = isTaskbarLight;
            }
            if (controller is null)
                return;

            BackdropManager?.Dispose();
            BackdropManager = null;
            BackdropManager = ContentBackdropManager.Create(controller, ElementCompositionPreview.GetElementVisual(IslandsGrid).Compositor, ActualTheme);

            UpdateBackdrop(true);
        }

        private void UpdateBackdrop(bool coerce = false)
        {
            foreach (var island in Islands)
                island.UpdateBackdrop(IsBackdropEnabled, coerce);
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

        private void UpdateIslands()
        {
            if (IslandsGrid is null)
                return;

            IslandsGrid.Children.Clear();
            IslandsGrid.RowDefinitions.Clear();
            IslandsGrid.ColumnDefinitions.Clear();

            if (IslandsOrientation is Orientation.Vertical)
            {
                for (int index = 0; index < Islands.Count; index++)
                {
                    if (Islands[index] is not TrayIconFlyoutIsland island)
                        continue;

                    IslandsGrid.RowDefinitions.Add(new() { Height = island.IslandHeight });
                    Grid.SetRow(island, index);
                    Grid.SetColumn(island, 0);
                    island.SetOwner(this);
                    IslandsGrid.Children.Add(island);
                }
            }
            else
            {
                for (int index = 0; index < Islands.Count; index++)
                {

                    if (Islands[index] is not TrayIconFlyoutIsland island)
                        continue;

                    IslandsGrid.ColumnDefinitions.Add(new() { Width = island.IslandWidth });
                    Grid.SetRow(island, 0);
                    Grid.SetColumn(island, index);
                    island.SetOwner(this);
                    IslandsGrid.Children.Add(island);
                }
            }
        }

        private FlyoutPopupDirection UpdateFlyoutRegion()
        {
            if (_host?.DesktopWindowXamlSource is null || IslandsGrid is null)
                return ResolvePopupDirection(PopupDirection, default, 0, 0);

            var scale = _host.XamlIslandRasterizationScale;
            var flyoutWidth = GetCurrentFlyoutWidth();
            var flyoutHeight = GetCurrentFlyoutHeight();
            var scaledMargin = GetScaledMargin(Margin, scale);
            var frameWidth = (flyoutWidth + Margin.Left + Margin.Right) * scale;
            var frameHeight = (flyoutHeight + Margin.Top + Margin.Bottom) * scale;
            var hostWidth = _host.WindowSize.Width;
            var hostHeight = _host.WindowSize.Height;
            var regionWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(frameWidth, hostWidth)));
            var regionHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(frameHeight, hostHeight)));
            var customBottomCenterPoint = _customPlacementBottomCenterPoint;
            var requestedPopupDirection = PopupDirection;
            _customPlacementBottomCenterPoint = null;

            double left;
            double top;

            if (customBottomCenterPoint is Point bottomCenterPoint)
            {
                left = bottomCenterPoint.X - ((flyoutWidth * scale) / 2) - scaledMargin.Left;
                top = bottomCenterPoint.Y - (flyoutHeight * scale) - scaledMargin.Top;
            }
            else
            {
                (left, top) = GetPlacementOrigin(Placement, regionWidth, regionHeight, hostWidth, hostHeight);
            }

            left = Clamp(left, 0, hostWidth - regionWidth);
            top = Clamp(top, 0, hostHeight - regionHeight);

            var region = new RectInt32(
                (int)Math.Round(left),
                (int)Math.Round(top),
                (int)regionWidth,
                (int)regionHeight);

            _host.MoveAndResize(region, ShouldActivateOnOpen());
            _host.SetHWndRectRegion(new(0, 0, region.Width, region.Height));

            return ResolvePopupDirection(requestedPopupDirection, region, hostWidth, hostHeight);
        }

        internal void OnIslandSizeChanged()
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
#if WASDK
            UpdateBackdrop(true);
#endif
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
            var hostSize = _host.WindowSize;
            var availableWidth = (hostSize.Width / scale) - Margin.Left - Margin.Right;
            var availableHeight = (hostSize.Height / scale) - Margin.Top - Margin.Bottom;

            return (Math.Max(0, availableWidth), Math.Max(0, availableHeight));
        }

        private bool HasStarIslandWidth()
        {
            foreach (var item in Islands)
            {
                if (item is TrayIconFlyoutIsland island && island.IslandWidth.IsStar)
                    return true;
            }

            return false;
        }

        private bool HasStarIslandHeight()
        {
            foreach (var item in Islands)
            {
                if (item is TrayIconFlyoutIsland island && island.IslandHeight.IsStar)
                    return true;
            }

            return false;
        }

#if WASDK
        private void TrayIconFlyout_LayoutUpdated(object? sender, object e)
        {
            //if (_host?.DesktopWindowXamlSource is null)
            //	return;

            //var flyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(_host.DesktopWindowXamlSource.Content.XamlRoot);
            //foreach (var flyout in flyouts)
            //{
            //	if (flyout.SystemBackdrop is not PersistentAcrylicBackdrop)
            //		flyout.SystemBackdrop = _persistentBackdrop;
            //}
        }
#endif

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
            if (ActivationMode is not FlyoutActivationMode.NeverActivate)
                return;

            args.Cancel = true;
            args.Handled = true;
            _host?.RestoreActivationState();
        }

        private void FocusManager_GettingFocus(object? sender, GettingFocusEventArgs args)
        {
            if (ActivationMode is not FlyoutActivationMode.NeverActivate || args.NewFocusedElement is not DependencyObject newFocusedElement)
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
            if (ActivationMode is FlyoutActivationMode.NeverActivate)
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

        private Storyboard GetOpenStoryboard(FlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
        {
            var transform = GetRootTransform() ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : GetClosedYOffset(popupDirection), 0)
                : TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : GetClosedXOffset(popupDirection), 0);
        }

        private Storyboard GetCloseStoryboard(FlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
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

        private void SetClosedTransform(FlyoutPopupDirection popupDirection)
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

        private int GetClosedXOffset(FlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                FlyoutPopupDirection.LeftToRight => -(int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Left),
                FlyoutPopupDirection.RightToLeft => (int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Right),
                _ => 0,
            };
        }

        private int GetClosedYOffset(FlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                FlyoutPopupDirection.TopToBottom => -(int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Top),
                FlyoutPopupDirection.BottomToTop => (int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Bottom),
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

        private static (double Left, double Top, double Right, double Bottom) GetScaledMargin(Thickness margin, double scale)
        {
            return (
                margin.Left * scale,
                margin.Top * scale,
                margin.Right * scale,
                margin.Bottom * scale);
        }

        private static (double Left, double Top) GetPlacementOrigin(FlyoutPlacementMode placement, double width, double height, double hostWidth, double hostHeight)
        {
            return placement switch
            {
                FlyoutPlacementMode.BottomCenter => ((hostWidth - width) / 2, hostHeight - height),
                FlyoutPlacementMode.BottomLeft => (0, hostHeight - height),
                FlyoutPlacementMode.BottomRight => (hostWidth - width, hostHeight - height),
                FlyoutPlacementMode.TopCenter => ((hostWidth - width) / 2, 0),
                FlyoutPlacementMode.TopLeft => (0, 0),
                FlyoutPlacementMode.TopRight => (hostWidth - width, 0),
                _ => (hostWidth - width, hostHeight - height),
            };
        }

        private static FlyoutPopupDirection ResolvePopupDirection(FlyoutPopupDirection requestedDirection, RectInt32 region, double hostWidth, double hostHeight)
        {
            return requestedDirection switch
            {
                FlyoutPopupDirection.BottomToTop => FlyoutPopupDirection.BottomToTop,
                FlyoutPopupDirection.TopToBottom => FlyoutPopupDirection.TopToBottom,
                FlyoutPopupDirection.LeftToRight => FlyoutPopupDirection.LeftToRight,
                FlyoutPopupDirection.RightToLeft => FlyoutPopupDirection.RightToLeft,
                FlyoutPopupDirection.Horizontal => IsRightHalf(region, hostWidth) ? FlyoutPopupDirection.RightToLeft : FlyoutPopupDirection.LeftToRight,
                _ => IsBottomHalf(region, hostHeight) ? FlyoutPopupDirection.BottomToTop : FlyoutPopupDirection.TopToBottom,
            };
        }

        private static bool IsBottomHalf(RectInt32 region, double hostHeight)
        {
            return region.Y + (region.Height / 2D) >= hostHeight / 2D;
        }

        private static bool IsRightHalf(RectInt32 region, double hostWidth)
        {
            return region.X + (region.Width / 2D) >= hostWidth / 2D;
        }

        private static bool IsVerticalDirection(FlyoutPopupDirection popupDirection)
        {
            return popupDirection is FlyoutPopupDirection.BottomToTop or FlyoutPopupDirection.TopToBottom;
        }

        private bool ShouldActivateOnOpen()
        {
            return ActivationMode is FlyoutActivationMode.Activate;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), Math.Max(min, max));
        }

        private void HostWindow_Inactivated(object? sender, EventArgs e)
        {
            if (HideOnLostFocus) Hide();
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

#if WASDK
            BackdropManager?.Dispose();
            BackdropManager = null;
#endif
            _host?.WindowInactivated -= HostWindow_Inactivated;
            RootGrid?.GettingFocus -= RootGrid_GettingFocus;
            RootGrid?.PointerPressed -= RootGrid_PointerPressed;
            RootGrid?.PointerMoved -= RootGrid_PointerMoved;
            RootGrid?.PointerReleased -= RootGrid_PointerReleased;
            RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
            RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
            RootGrid?.PointerExited -= RootGrid_PointerExited;

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
