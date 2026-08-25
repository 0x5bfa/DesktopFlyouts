// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;
using DesktopFlyouts.Shared;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DesktopFlyouts
{
    public partial class App : Application
    {
        private static SystemTrayIcon? _systemTrayIcon;
        private static DesktopFlyout? _desktopFlyout;
        private static volatile DesktopMenuFlyout? _desktopMenuFlyout;

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

            _systemTrayIcon = new(
                Path.Combine(AppContext.BaseDirectory, "Tray.ico"),
                "DesktopFlyouts sample app (UWP)",
                new("022F5158-F05A-4FE1-B356-34F14B363625"));

            _systemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
            _systemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
            _systemTrayIcon.Show();

            // The first flyout uses XamlHostingKit's main XamlWindow.
            _desktopFlyout = CreateDesktopFlyout();

            // System XAML permits one top-level window per thread. The menu therefore uses
            // another XamlHostingKit window and is constructed on that window's XAML thread.
            XamlIslandApplication.CreateWindow(_ =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                _desktopMenuFlyout = CreateDesktopMenuFlyout();
            });
        }

        private static DesktopFlyout CreateDesktopFlyout()
        {
            var flyout = new DesktopFlyout
            {
                Width = 360,
            };

            flyout.Islands.Add(CreateIsland(new Button
            {
                Content = "A",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }));

            flyout.Islands.Add(CreateIsland(new TextBlock
            {
                Text = "Island 2",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }));

            var options = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            options.Items.Add("Option 1");
            options.Items.Add("Option 2");
            options.Items.Add("Option 3");

            var panel = new StackPanel
            {
                Padding = new Thickness(16),
                Spacing = 8,
            };
            panel.Children.Add(new TextBlock
            {
                Text = "Island 3",
                FontSize = 20,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
            });
            panel.Children.Add(new TextBlock { Text = "Select option" });
            panel.Children.Add(options);
            panel.Children.Add(new CalendarDatePicker());
            flyout.Islands.Add(CreateIsland(panel));

            return flyout;
        }

        private static DesktopFlyoutIsland CreateIsland(UIElement content)
        {
            return new()
            {
                Height = 180,
                Background = new SolidColorBrush(Color.FromArgb(245, 32, 32, 32)),
                Content = content,
            };
        }

        private static DesktopMenuFlyout CreateDesktopMenuFlyout()
        {
            var menu = new DesktopMenuFlyout();
            var settings = new MenuFlyoutSubItem
            {
                Text = "Settings",
                Icon = new FontIcon { Glyph = "\uE115" },
            };
            settings.Items.Add(new MenuFlyoutItem { Text = "Theme" });
            settings.Items.Add(new MenuFlyoutItem { Text = "Language" });
            settings.Items.Add(new MenuFlyoutItem { Text = "Privacy" });

            var devices = new MenuFlyoutItem
            {
                Text = "Devices",
                Icon = new FontIcon { Glyph = "\uE975" },
            };
            devices.Click += async (_, _) => await Launcher.LaunchUriAsync(new Uri("tif-secondaryapp:"));

            var exit = new MenuFlyoutItem
            {
                Text = "Exit",
                Icon = new FontIcon { Glyph = "\uE8BB" },
            };
            exit.Click += (_, _) =>
            {
                var dispatcher = _desktopFlyout?.Dispatcher;
                if (dispatcher is not null)
                    _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, Current.Exit);
            };

            menu.Items.Add(settings);
            menu.Items.Add(devices);
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(exit);
            return menu;
        }

        private static void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
        {
            if (_desktopFlyout is null)
                return;

            if (_desktopFlyout.IsOpen)
                _desktopFlyout.Hide();
            else
                _desktopFlyout.Show();
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
