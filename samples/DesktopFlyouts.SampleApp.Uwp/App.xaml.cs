// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using XamlHostingKit;

namespace DesktopFlyouts
{
    public partial class App : Application
    {
        private static CoreDispatcher? _dispatcher;
        private static MainWindow? _window;
        private static bool _closing;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _dispatcher = XamlWindow.Current.Dispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(_dispatcher));

            TrayIconManager.Default.Initialize(new SystemTrayIcon(
                Path.Combine(AppContext.BaseDirectory, "Assets\\Tray.ico"),
                "DesktopFlyouts sample app (UWP)",
                new Guid("022F5158-F05A-4FE1-B356-34F14B363625")));

            _window = new MainWindow();
            _window.Show();
        }

        internal static void ExitApplication()
        {
            if (_dispatcher is not null)
                _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await ShutdownAsync());
        }

        private static async Task ShutdownAsync()
        {
            if (_closing)
                return;
            _closing = true;

            try
            {
                await TrayIconManager.Default.ShutdownAsync();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                _window?.Dispose();
                _window = null;
            }
        }
    }
}
