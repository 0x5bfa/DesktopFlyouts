// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts;

public partial class App : Application
{
	private static DispatcherQueue? _dispatcherQueue;
	private static MainWindow? _window;
	private static bool _closing;

	public unsafe App()
	{
		WindowsXamlManager.InitializeForCurrentThread();
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(_dispatcherQueue));

		InitializeComponent();

		TrayIconManager.Default.Initialize(new SystemTrayIcon(
			Path.Combine(AppContext.BaseDirectory, "Assets\\Tray.ico"),
			"DesktopFlyouts sample app (UWP)",
			new Guid("022F5158-F05A-4FE1-B356-34F14B363625")));

		_window = new MainWindow();
		_window.Show();

		MSG message;
		while (PInvoke.GetMessage(&message, HWND.Null, 0U, 0U))
		{
			if (!(_window?.TryPreTranslateMessage((nint)(&message)) ?? false) &&
				!TrayIconManager.Default.TryPreTranslateMessage((nint)(&message)))
			{
				PInvoke.TranslateMessage(&message);
				PInvoke.DispatchMessage(&message);
			}
		}
	}

	internal static void ExitApplication()
	{
		_ = _dispatcherQueue?.TryEnqueue(async () => await ShutdownAsync());
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
			PInvoke.PostQuitMessage(0);
		}
	}
}
