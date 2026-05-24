// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using CommunityToolkit.WinUI;


#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.Win32.UI.WindowsAndMessaging;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
#endif

namespace DesktopFlyouts
{
    /// <summary>
    /// Displays a menu flyout in an independent XAML island window.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DesktopMenuFlyout"/> when a context menu must be opened at a physical screen
    /// point, such as a tray icon click. Add <see cref="MenuFlyoutItemBase"/> items as children and
    /// call the point-based show overload to display the menu.
    /// </remarks>
    [ContentProperty(Name = nameof(Items))]
    public partial class DesktopMenuFlyout : ItemsControl, IDisposable
    {
        private const string PART_MenuFlyoutTargetControl = "PART_MenuFlyoutTargetControl";

        private readonly XamlIslandHostWindow? _host;
        private MenuFlyout? _menuFlyout;
        private bool _disposed;

        private Border? MenuFlyoutTargetControl;

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
            _host.UpdateWindowVisibility(false);
            _host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            MenuFlyoutTargetControl = GetTemplateChild(PART_MenuFlyoutTargetControl) as Border
                ?? throw new MissingFieldException($"Could not find {PART_MenuFlyoutTargetControl} in the given {nameof(DesktopMenuFlyout)}'s style.");
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);

            if (_disposed)
                return;

            if (_menuFlyout is null)
            {
                _menuFlyout = new MenuFlyout();
                _menuFlyout.Closed += MenuFlyout_Closed;
            }

            _menuFlyout.Items.Clear();

            foreach (var item in Items)
                _menuFlyout.Items.Add((MenuFlyoutItemBase)item);
        }

        /// <summary>
        /// Opens the menu flyout at the specified screen point.
        /// </summary>
        /// <param name="point">The physical screen pixel where the menu host should be positioned.</param>
        /// <remarks>
        /// The menu is built from the current <see cref="ItemsControl.Items"/> collection. Items must
        /// derive from <see cref="MenuFlyoutItemBase"/>.
        /// </remarks>
        public void Show(Point point)
        {
            if (_disposed || _menuFlyout is null)
                return;

            UpdateFlyoutTheme();

            _host?.MoveAndResize(new(point.X, point.Y, 0, 0));
            _host?.SetHWndRectRegion(new(0, 0, 1, 1));
            _host?.UpdateWindowVisibility(true);

            _menuFlyout.ShowAt(MenuFlyoutTargetControl);

            IsOpen = true;
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

            _host?.UpdateWindowVisibility(false);

            _menuFlyout?.Hide();

            IsOpen = false;
        }

        private void MenuFlyout_Closed(object? sender, object e)
        {
            _host?.UpdateWindowVisibility(false);
            IsOpen = false;
        }

        private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
        {
            if (_disposed)
                return;

            UpdateFlyoutTheme();
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
            RequestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_menuFlyout is not null)
            {
                _menuFlyout.Closed -= MenuFlyout_Closed;
                if (IsOpen)
                    _menuFlyout.Hide();

                _menuFlyout.Items.Clear();
                _menuFlyout = null;
            }

            _host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
            _host?.Dispose();
            IsOpen = false;

            GC.SuppressFinalize(this);
        }
    }
}
