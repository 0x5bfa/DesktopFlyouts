// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts
{
	public partial class App : Application
	{
		private static SystemTrayIcon? _systemTrayIcon;
		private static DesktopFlyout? _desktopFlyout;
		private static DesktopMenuFlyout? _desktopMenuFlyout;

		public unsafe App()
		{
			PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

			_systemTrayIcon = new(
				"Tray.ico",
				"DesktopFlyouts sample app (UWP)",
				new("022F5158-F05A-4FE1-B356-34F14B363625"));

			_systemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
			_systemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
			_systemTrayIcon.Show();

			// Initialize XAML Island
			WindowsXamlManager.InitializeForCurrentThread();
			SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

			// Initialize XAML flyouts
			_desktopFlyout = new MainDesktopFlyout();
			_desktopMenuFlyout = new MainDesktopMenuFlyout();

			MSG msg;
			while (PInvoke.GetMessage(&msg, HWND.Null, 0U, 0U))
			{
				if (!TryPreTranslateMessage(&msg))
				{
					PInvoke.TranslateMessage(&msg);
					PInvoke.DispatchMessage(&msg);
				}
			}
		}

		private static unsafe bool TryPreTranslateMessage(MSG* msg)
		{
			return (_desktopFlyout?.TryPreTranslateMessage(msg) ?? false) ||
				(_desktopMenuFlyout?.TryPreTranslateMessage(msg) ?? false);
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
			if (_desktopMenuFlyout is null)
				return;

			if (_desktopMenuFlyout.IsOpen)
				_desktopMenuFlyout.Hide();

			_desktopMenuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
		}
	}
}
