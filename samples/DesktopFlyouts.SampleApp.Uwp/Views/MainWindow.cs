// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.UI.Core;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using Windows.Win32.System.WinRT.Xaml;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace DesktopFlyouts;

internal sealed unsafe class MainWindow : IDisposable
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate LRESULT WindowProcedure(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam);

	private const string WindowTitle = "DesktopFlyouts sample app (UWP)";

	private readonly string _windowClassName = $"DesktopFlyouts.SampleApp.Uwp.MainWindow.{Guid.NewGuid():N}";
	private readonly WindowProcedure _wndProc;
	private readonly HWND _hwnd;
	private HWND _xamlHwnd;
	private readonly DesktopWindowXamlSource _desktopWindowXamlSource;
	private readonly IDesktopWindowXamlSourceNative2 _desktopWindowXamlSourceNative;
	private HWND _coreHwnd;
	private bool _disposed;

	internal MainWindow()
	{
		_wndProc = new(WindowProc);

		WNDCLASSW windowClass = default;
		windowClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
		windowClass.hInstance = PInvoke.GetModuleHandle(null);

		fixed (char* className = _windowClassName)
		{
			windowClass.lpszClassName = (PCWSTR)className;
			PInvoke.RegisterClass(&windowClass);

			fixed (char* windowTitle = WindowTitle)
			{
				_hwnd = PInvoke.CreateWindowEx(
					WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP,
					(PCWSTR)className,
					(PCWSTR)windowTitle,
					WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
					100,
					100,
					600,
					900,
					HWND.Null,
					HMENU.Null,
					windowClass.hInstance,
					null);
			}
		}

		if (_hwnd.IsNull)
		{
			UnregisterWindowClass();
			throw new InvalidOperationException("Could not create the sample window.");
		}

		_desktopWindowXamlSource = new();
		void* ppv;
		var desktopWindowXamlSourceNativeId = typeof(IDesktopWindowXamlSourceNative2).GUID;
		((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr)->QueryInterface(
			&desktopWindowXamlSourceNativeId, &ppv);

		var comWrappers = new StrategyBasedComWrappers();
		_desktopWindowXamlSourceNative = (IDesktopWindowXamlSourceNative2)comWrappers.GetOrCreateObjectForComInstance(
			(nint)ppv,
			CreateObjectFlags.None);
		GC.KeepAlive(_desktopWindowXamlSource);

		_desktopWindowXamlSourceNative.AttachToWindow(_hwnd);
		_xamlHwnd = _desktopWindowXamlSourceNative.get_WindowHandle();

		var coreWindow = CoreWindow.GetForCurrentThread();
		var coreWindowInteropId = typeof(ICoreWindowInterop).GUID;
		((IUnknown*)((IWinRTObject)coreWindow).NativeObject.ThisPtr)->QueryInterface(
			&coreWindowInteropId, &ppv);
		var coreWindowInterop = (ICoreWindowInterop)comWrappers.GetOrCreateObjectForComInstance(
			(nint)ppv,
			CreateObjectFlags.None);
		HWND coreHwnd = default;
		coreWindowInterop.get_WindowHandle(&coreHwnd);
		_coreHwnd = coreHwnd;

		_desktopWindowXamlSource.Content = new RootView();
		ResizeXamlWindow();
	}

    internal void Show()
    {
        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    internal bool TryPreTranslateMessage(nint message)
	{
		BOOL result = false;
		_desktopWindowXamlSourceNative.PreTranslateMessage((MSG*)message, &result);
		return result;
	}

	private void ResizeXamlWindow()
	{
		if (_xamlHwnd.IsNull)
		{
			return;
		}

		RECT clientRect;
		PInvoke.GetClientRect(_hwnd, &clientRect);
		var width = clientRect.right - clientRect.left;
		var height = clientRect.bottom - clientRect.top;
		PInvoke.SetWindowPos(
			_xamlHwnd,
			HWND.Null,
			0,
			0,
			width,
			height,
			SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW |
			SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
			SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

		if (!_coreHwnd.IsNull)
		{
			PInvoke.SendMessage(_coreHwnd, PInvoke.WM_SIZE, (WPARAM)(nuint)width, height);
		}
	}

	private LRESULT WindowProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
	{
		if (message == PInvoke.WM_CLOSE)
		{
			App.ExitApplication();
			return (LRESULT)0;
		}

		if (message == PInvoke.WM_SIZE)
		{
			ResizeXamlWindow();
		}

		return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		_desktopWindowXamlSource.Content = null;
		_desktopWindowXamlSource.Dispose();
		PInvoke.DestroyWindow(_hwnd);
		UnregisterWindowClass();
	}

	private void UnregisterWindowClass()
	{
		fixed (char* className = _windowClassName)
		{
			PInvoke.UnregisterClass((PCWSTR)className, PInvoke.GetModuleHandle(null));
		}
	}
}
