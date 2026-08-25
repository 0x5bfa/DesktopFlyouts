// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using DesktopFlyouts.Shared;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace DesktopFlyouts
{
    public partial class App : Application
    {
        private static SystemTrayIcon? _systemTrayIcon;
        private static DesktopFlyout? _desktopFlyout;
        private static volatile DesktopMenuFlyout? _desktopMenuFlyout;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _systemTrayIcon = new(
                Path.Combine(AppContext.BaseDirectory, "Tray.ico"),
                "DesktopFlyouts sample app (UWP)",
                new("022F5158-F05A-4FE1-B356-34F14B363625"));

            _systemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
            _systemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
            _systemTrayIcon.Show();

            // The first flyout uses XamlHostingKit's main XamlWindow.
            _desktopFlyout = new CustomizableFlyout
            {
                // XamlHostingKit deactivates its CoreWindow during the first host activation.
                HideOnLostFocus = false,
            };

            // System XAML permits one top-level window per thread. The menu therefore uses
            // another XamlHostingKit window and is constructed on that window's XAML thread.
            XamlIslandApplication.CreateWindow(_ =>
            {
                _desktopMenuFlyout = new MainDesktopMenuFlyout();
            });
        }

        internal static void ExitApplication()
        {
            var dispatcher = _desktopFlyout?.Dispatcher;
            if (dispatcher is not null)
                _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, Current.Exit);
        }

        private static void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
        {
            var flyout = _desktopFlyout;
            if (flyout is null)
                return;

            _ = flyout.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (flyout.IsOpen)
                    flyout.Hide();
                else
                    flyout.Show();
            });
        }

        private static void SystemTrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
        {
            var menuFlyout = _desktopMenuFlyout;
            if (menuFlyout is null)
                return;

            _ = menuFlyout.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (menuFlyout.IsOpen)
                    menuFlyout.Hide();

                menuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
            });
        }
    }
}
