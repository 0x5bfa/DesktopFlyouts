// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics;

#if UWP
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace U5BFA.Libraries
{
    [ContentProperty(Name = nameof(Islands))]
    public partial class TrayIconFlyout : Control, IDisposable
    {
        private const string PART_RootGrid = "PART_RootGrid";
        private const string PART_IslandsGrid = "PART_IslandsGrid";

#if WASDK
        private static readonly PersistentAcrylicBackdrop _persistentBackdrop = new();
#endif

        private readonly XamlIslandHostWindow? _host;
        private bool? _wasTaskbarLightLastTimeChecked;
        private bool _isPopupAnimationPlaying;
        private Point? _customPlacementBottomCenterPoint;
        private TrayIconFlyoutPopupDirection _activePopupDirection = TrayIconFlyoutPopupDirection.BottomToTop;
        private DispatcherTimer? _autoCloseTimer;
        private bool _disposed;

        private Grid? RootGrid;
        private Grid? IslandsGrid;

#if WASDK
        internal ContentBackdropManager? BackdropManager { get; private set; }
#endif

        public TrayIconFlyout()
        {
            DefaultStyleKey = typeof(TrayIconFlyout);

            _host = new XamlIslandHostWindow();
            _host.SetContent(this);
            _host.UpdateWindowVisibility(false);
            _host.WindowInactivated += HostWindow_Inactivated;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RootGrid = GetTemplateChild(PART_RootGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(TrayIconFlyout)}'s style.");
            IslandsGrid = GetTemplateChild(PART_IslandsGrid) as Grid
                ?? throw new MissingFieldException($"Could not find {PART_IslandsGrid} in the given {nameof(TrayIconFlyout)}'s style.");

#if WASDK
            LayoutUpdated += TrayIconFlyout_LayoutUpdated;
#endif

            UpdateIslands();
        }

        public void Show()
        {
            if (_disposed || _host?.DesktopWindowXamlSource is null || RootGrid is null || _isPopupAnimationPlaying)
            {
                _customPlacementBottomCenterPoint = null;
                return;
            }

            StopAutoCloseTimer();
            _isPopupAnimationPlaying = true;
            _host.Maximize();

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
                    _activePopupDirection = UpdateFlyoutRegion();

                    // Ensure to hide first
                    SetClosedTransform(_activePopupDirection);

                    UpdateLayout();
                    await Task.Delay(1);

                    _host.UpdateWindowVisibility(true);

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

        public void Show(Point bottomCenterPoint)
        {
            if (_isPopupAnimationPlaying)
                return;

            _customPlacementBottomCenterPoint = bottomCenterPoint;
            Show();
        }

        public void Hide()
        {
            StopAutoCloseTimer();

            if (_disposed || RootGrid is null || _isPopupAnimationPlaying)
                return;

            _isPopupAnimationPlaying = true;
            SetOpenTransform();

            if (IsTransitionAnimationEnabled)
            {
                var storyboard = GetCloseStoryboard(_activePopupDirection);
                storyboard.Completed += CloseAnimationStoryboard_Completed;
                storyboard.Begin();
            }
            else
            {
                CompleteClose();
            }
        }

#if UWP
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

        private TrayIconFlyoutPopupDirection UpdateFlyoutRegion()
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

            _host.MoveAndResize(region);
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
            SetOpenTransform();
            _isPopupAnimationPlaying = false;
            IsOpen = true;
            _host?.NavigateFocus();
            RestartAutoCloseTimer();
        }

        private void CompleteClose()
        {
            StopAutoCloseTimer();
            SetClosedTransform(_activePopupDirection);
            _isPopupAnimationPlaying = false;
            IsOpen = false;
            _host?.UpdateWindowVisibility(false);
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

        private void AutoCloseTimer_Tick(object? sender, object e)
        {
            StopAutoCloseTimer();

            if (IsOpen && !_isPopupAnimationPlaying)
                Hide();
        }

        private Storyboard GetOpenStoryboard(TrayIconFlyoutPopupDirection popupDirection)
        {
            var rootGrid = RootGrid ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(rootGrid, GetClosedYOffset(popupDirection), 0)
                : TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(rootGrid, GetClosedXOffset(popupDirection), 0);
        }

        private Storyboard GetCloseStoryboard(TrayIconFlyoutPopupDirection popupDirection)
        {
            var rootGrid = RootGrid ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

            return IsVerticalDirection(popupDirection)
                ? TransitionHelpers.GetWindows11TopToBottomTransitionStoryboard(rootGrid, 0, GetClosedYOffset(popupDirection))
                : TransitionHelpers.GetWindows11LeftToRightTransitionStoryboard(rootGrid, 0, GetClosedXOffset(popupDirection));
        }

        private void SetOpenTransform()
        {
            if (RootGrid?.RenderTransform is not TranslateTransform translateTransform)
                return;

            translateTransform.X = 0;
            translateTransform.Y = 0;
        }

        private void SetClosedTransform(TrayIconFlyoutPopupDirection popupDirection)
        {
            if (RootGrid?.RenderTransform is not TranslateTransform translateTransform)
                return;

            translateTransform.X = GetClosedXOffset(popupDirection);
            translateTransform.Y = GetClosedYOffset(popupDirection);
        }

        private int GetClosedXOffset(TrayIconFlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                TrayIconFlyoutPopupDirection.LeftToRight => -(int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Left),
                TrayIconFlyoutPopupDirection.RightToLeft => (int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Right),
                _ => 0,
            };
        }

        private int GetClosedYOffset(TrayIconFlyoutPopupDirection popupDirection)
        {
            return popupDirection switch
            {
                TrayIconFlyoutPopupDirection.TopToBottom => -(int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Top),
                TrayIconFlyoutPopupDirection.BottomToTop => (int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Bottom),
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
                FlyoutPlacementMode.Bottom => ((hostWidth - width) / 2, hostHeight - height),
                FlyoutPlacementMode.BottomEdgeAlignedLeft => (0, hostHeight - height),
                FlyoutPlacementMode.Top => ((hostWidth - width) / 2, 0),
                FlyoutPlacementMode.TopEdgeAlignedLeft => (0, 0),
                FlyoutPlacementMode.TopEdgeAlignedRight => (hostWidth - width, 0),
                _ => (hostWidth - width, hostHeight - height),
            };
        }

        private static TrayIconFlyoutPopupDirection ResolvePopupDirection(TrayIconFlyoutPopupDirection requestedDirection, RectInt32 region, double hostWidth, double hostHeight)
        {
            return requestedDirection switch
            {
                TrayIconFlyoutPopupDirection.BottomToTop => TrayIconFlyoutPopupDirection.BottomToTop,
                TrayIconFlyoutPopupDirection.TopToBottom => TrayIconFlyoutPopupDirection.TopToBottom,
                TrayIconFlyoutPopupDirection.LeftToRight => TrayIconFlyoutPopupDirection.LeftToRight,
                TrayIconFlyoutPopupDirection.RightToLeft => TrayIconFlyoutPopupDirection.RightToLeft,
                TrayIconFlyoutPopupDirection.Horizontal => IsRightHalf(region, hostWidth) ? TrayIconFlyoutPopupDirection.RightToLeft : TrayIconFlyoutPopupDirection.LeftToRight,
                _ => IsBottomHalf(region, hostHeight) ? TrayIconFlyoutPopupDirection.BottomToTop : TrayIconFlyoutPopupDirection.TopToBottom,
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

        private static bool IsVerticalDirection(TrayIconFlyoutPopupDirection popupDirection)
        {
            return popupDirection is TrayIconFlyoutPopupDirection.BottomToTop or TrayIconFlyoutPopupDirection.TopToBottom;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), Math.Max(min, max));
        }

        private void HostWindow_Inactivated(object? sender, EventArgs e)
        {
            if (HideOnLostFocus) Hide();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopAutoCloseTimer();

#if WASDK
            BackdropManager?.Dispose();
            BackdropManager = null;
#endif
            _host?.WindowInactivated -= HostWindow_Inactivated;
            _host?.Dispose();
            IsOpen = false;

            GC.SuppressFinalize(this);
        }
    }
}
