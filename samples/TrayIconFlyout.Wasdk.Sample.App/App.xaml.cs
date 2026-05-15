// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using System;

namespace U5BFA.Libraries
{
	public partial class App : Application
	{
		private Window? _window;

		public App()
		{
			InitializeComponent();

			AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			TrayIconManager.Default.Initialize(new(
				"Assets\\Tray.ico",
				"TrayIconFlyout sample app (WASDK)",
				new("28DE460A-8BD6-4539-A406-5F685584FD4D")));

			_window = new MainWindow();
			_window.Closed += (_, _) => TrayIconManager.Default.Dispose();
			_window.Activate();
			_window.DispatcherQueue.EnsureSystemDispatcherQueue();
		}

		private void AppDomain_ProcessExit(object? sender, EventArgs e)
		{
			TrayIconManager.Default.Dispose();
		}
	}
}
