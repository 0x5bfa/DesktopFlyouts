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

namespace U5BFA.Libraries
{
	public partial class TrayHostApp/* : Application*/
	{
		private static SystemTrayIcon? _systemTrayIcon;
		private static TrayIconFlyout? _trayIconFlyout;
		private static TrayIconMenuFlyout? _trayIconMenuFlyout;

		public unsafe TrayHostApp()
		{
			PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

			_systemTrayIcon = new(
				"Assets\\Tray.ico",
				"TrayIconFlyout sample app (UWP)",
				new("022F5158-F05A-4FE1-B356-34F14B363625"));

			_systemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
			_systemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
			_systemTrayIcon.Show();

			// Initialize XAML Island
			WindowsXamlManager.InitializeForCurrentThread();
			SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

			// Initialize XAML flyouts
			_trayIconFlyout = new MainTrayIconFlyout();
			_trayIconMenuFlyout = new MainTrayIconMeunFlyout();

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
			return (_trayIconFlyout?.TryPreTranslateMessage(msg) ?? false) ||
				(_trayIconMenuFlyout?.TryPreTranslateMessage(msg) ?? false);
		}

		private static void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
		{
			if (_trayIconFlyout is null)
				return;

			if (_trayIconFlyout.IsOpen)
				_trayIconFlyout.Hide();
			else
				_trayIconFlyout.Show();
		}

		private static void SystemTrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
		{
			if (_trayIconMenuFlyout is null)
				return;

			if (_trayIconMenuFlyout.IsOpen)
				_trayIconMenuFlyout.Hide();

			_trayIconMenuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
		}
	}
}
