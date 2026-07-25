#if !WINDOWS
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using FoundationSize = Windows.Foundation.Size;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace DesktopFlyouts
{
    /// <summary>
    /// Displays a menu flyout in an independent XAML island window.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DesktopMenuFlyout"/> when a context menu must be opened at a physical screen
    /// point, such as a tray icon click. Add <see cref="MenuFlyoutItemBase"/> items as children and
    /// call the point-based show overload to display the menu.
    /// Submenus are not supported in this implementation.
    /// </remarks>
    [ContentProperty(Name = nameof(Items))]
    public partial class DesktopMenuFlyout : ItemsControl, IDisposable
    {
        private const string PART_MenuFlyoutTargetControl = "PART_MenuFlyoutTargetControl";

        private readonly XamlIslandHostWindow? _host;
        private readonly List<MenuFlyoutItem> _subscribedItems = new();
        private MenuFlyoutPresenter? _presenter;
        private bool _disposed;
        private bool _isShowPending;

        private Panel? MenuFlyoutTargetControl;

        /// <summary>
        /// Gets whether the menu flyout is currently open.
        /// </summary>
        /// <value><see langword="true"/> while the hosted menu is open; otherwise, <see langword="false"/>.</value>
        [GeneratedDependencyProperty]
        public partial bool IsOpen { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopMenuFlyout"/>.
        /// </summary>
        /// <remarks>
        /// The constructor creates the hidden desktop host window used to display the menu.
        /// </remarks>
        public DesktopMenuFlyout()
        {
            DefaultStyleKey = typeof(DesktopMenuFlyout);

            _host = new XamlIslandHostWindow();
            _host.SetContent(this);
            _ = _host.UpdateWindowVisibility(false);
            _host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
            _host.WindowInactivated += HostWindow_Inactivated;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            MenuFlyoutTargetControl = GetTemplateChild(PART_MenuFlyoutTargetControl) as Panel;
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);

            if (_disposed)
                return;

            EnsurePresenter();
        }

        private void EnsurePresenter()
        {
            _presenter ??= new MenuFlyoutPresenter();

            _presenter.ItemsSource = new List<MenuFlyoutItemBase>(
                Items.Cast<MenuFlyoutItemBase>());
        }

        private void SubscribeItemClicks()
        {
            if (_presenter is null)
                return;

            foreach (var item in _presenter.Items)
                if (item is MenuFlyoutItemBase mfib)
                    CollectClickableItems(mfib);
        }

        private void CollectClickableItems(MenuFlyoutItemBase item)
        {
            if (item is MenuFlyoutItem mfi)
            {
                // avoid double subscribing in case it happens
                mfi.Click -= OnMenuItemClicked;
                mfi.Click += OnMenuItemClicked;
                _subscribedItems.Add(mfi);
            }

            if (item is MenuFlyoutSubItem mfsi)
            {
                foreach (var child in mfsi.Items)
                    CollectClickableItems(child);
            }
            else if (item is SplitMenuFlyoutItem smfi)
            {
                foreach (var child in smfi.Items)
                    CollectClickableItems(child);
            }
        }

        private void UnsubscribeItemClicks()
        {
            foreach (var mfi in _subscribedItems)
                mfi.Click -= OnMenuItemClicked;

            _subscribedItems.Clear();
        }

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Opens the menu flyout at the specified screen point.
        /// </summary>
        /// <param name="point">The physical screen pixel where the menu host should be positioned.</param>
        /// <remarks>
        /// The menu is built from the current <see cref="ItemsControl.Items"/> collection. Items must
        /// derive from <see cref="MenuFlyoutItemBase"/>. Submenus are not supported in this implementation.
        /// </remarks>
        public void Show(Point point)
        {
            if (_disposed)
                return;

            // If a previous show is still pending its deferred layout, hide first.
            if (_isShowPending || IsOpen)
                HideImmediate();

            EnsurePresenter();
            SubscribeItemClicks();

            UpdateFlyoutTheme();

            // Add the presenter to the visual tree.
            MenuFlyoutTargetControl?.Children.Clear();
            MenuFlyoutTargetControl?.Children.Add(_presenter);

            // Get the work area for positioning.
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(new Point(point.X, point.Y));

            // Step 1: Place the host window at the target position with a generous initial size
            // so that XAML can compute layout for the MenuFlyoutPresenter.
            var initRegion = new RectInt32()
            {
                X = point.X,
                Y = point.Y - workArea.Height / 2,
                Width = workArea.Width / 2,
                Height = workArea.Height / 2
            };

            _host?.MoveAndResize(initRegion);
            _host?.SetHWndRectRegion(new RectInt32() { Width = initRegion.Width, Height = initRegion.Height });
            _ = _host?.UpdateWindowVisibility(true);

            IsOpen = true;
            _isShowPending = true;

            // Step 2: After the window is mapped and XAML has had a layout pass,
            // measure the presenter and resize to the correct size.
            var showPoint = new Point(point.X, point.Y);
            _ = Task.Run(async () =>
            {
#if UWP
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FinalizeShow(showPoint));
#elif WASDK
                DispatcherQueue.TryEnqueue(() => FinalizeShow(showPoint));
#endif
            });
        }

        private void FinalizeShow(Point point)
        {
            if (_disposed || !_isShowPending || _presenter is null || MenuFlyoutTargetControl is null)
            {
                _isShowPending = false;
                return;
            }

            // Force a layout pass so the presenter calculates its desired size.
            _presenter.Measure(new FoundationSize(double.PositiveInfinity, double.PositiveInfinity));
            _presenter.Arrange(new Windows.Foundation.Rect(0, 0, _presenter.DesiredSize.Width, _presenter.DesiredSize.Height));

            // Get the desired size of the presenter.
            var presenterWidth = _presenter.ActualWidth > 0 ? _presenter.ActualWidth : _presenter.DesiredSize.Width;
            var presenterHeight = _presenter.ActualHeight > 0 ? _presenter.ActualHeight : _presenter.DesiredSize.Height;

            // Ensure minimum size.
            var regionWidth = Math.Max(1, (int)Math.Ceiling(presenterWidth));
            var regionHeight = Math.Max(1, (int)Math.Ceiling(presenterHeight));

            // Get the work area for positioning.
            var workArea = WindowHelpers.GetFlyoutWorkAreaRect(new Point(point.X, point.Y));

            // Calculate position: align to mouse X, place above the point (like a context menu).
            // 32 magic number is the offset between window bottom and the actual rendering - probably shadow
            var left = (double)point.X;
            var top = (double)point.Y - regionHeight + 32;

            // Clamp to work area.
            left = Clamp(left, workArea.Left, workArea.Right - regionWidth);
            top = Clamp(top, workArea.Top, workArea.Bottom - regionHeight);

            var region = new RectInt32()
            {
                X = (int)Math.Round((double)left),
                Y = (int)Math.Round((double)top),
                Width = regionWidth,
                Height = regionHeight
            };

            // Resize and reposition the host window to the correct size.
            _host?.MoveAndResize(region);
            _host?.SetHWndRectRegion(new RectInt32() { Width = regionWidth, Height = regionHeight });

            _isShowPending = false;
        }

        /// <summary>
        /// Closes the menu flyout.
        /// </summary>
        /// <remarks>
        /// This also hides the desktop host window.
        /// </remarks>
        public void Hide()
        {
            if (_disposed)
                return;

            _isShowPending = false;
            HideImmediate();
        }

        private void HideImmediate()
        {
            UnsubscribeItemClicks();

            _ = _host?.UpdateWindowVisibility(false);

            // Remove the presenter from the visual tree.
            MenuFlyoutTargetControl?.Children.Clear();

            IsOpen = false;
        }

        private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            UpdateFlyoutTheme();
        }

        private void HostWindow_Inactivated(object? sender, EventArgs e)
        {
            if (_disposed || !IsOpen)
                return;

            Hide();
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
            var isLight = GeneralHelpers.IsTaskbarLight();
            RequestedTheme = isLight ? ElementTheme.Light : ElementTheme.Dark;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _isShowPending = false;

            // Remove the presenter from the visual tree.
            MenuFlyoutTargetControl?.Children.Clear();

            if (_presenter is not null)
            {
                UnsubscribeItemClicks();
                _presenter.ItemsSource = null;
                _presenter = null;
            }

            _host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
            _host?.WindowInactivated -= HostWindow_Inactivated;
            _host?.Dispose();
            IsOpen = false;

            GC.SuppressFinalize(this);
        }
    }
}

#endif
