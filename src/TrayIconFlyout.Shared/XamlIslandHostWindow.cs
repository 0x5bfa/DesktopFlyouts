// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.WinRT.Xaml;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.System.WinRT;
using Windows.Win32.System.Com;
using WinRT;
using System.Runtime.InteropServices.JavaScript;


#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
#elif WASDK
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
#endif

namespace U5BFA.Libraries
{
	internal unsafe partial class XamlIslandHostWindow : IDisposable
	{
		private const string WindowClassName = "TrayIconFlyoutHostClass";
		private const string WindowName = "TrayIconFlyoutHostWindow";

		private readonly WNDPROC _wndProc;

		private HWND _xamlHwnd = default;
		private bool _disposed;

#if UWP
		private HWND _coreHwnd = default;
		private bool _xamlInitialized = false;
		private WindowsXamlManager? _xamlManager = null;
		private CoreWindow? _coreWindow = null;

		private IDesktopWindowXamlSourceNative2 _pdwxsn2 = null!;
#endif

		internal HWND HWnd
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set;
		}

		internal DesktopWindowXamlSource? DesktopWindowXamlSource
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set;
		}

		internal Rect WindowSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				RECT rect;
				PInvoke.GetWindowRect(HWnd, &rect);
				return new(rect.X, rect.Y, rect.Width, rect.Height);
			}
		}

		internal double XamlIslandRasterizationScale
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if UWP
				return 1.0D;
#else
				return DesktopWindowXamlSource?.SiteBridge.SiteView.RasterizationScale ?? 1.0D;
#endif
			}
		}

		internal event EventHandler? WindowInactivated;

		internal XamlIslandHostWindow()
		{
			_wndProc = new(WndProc);

			WNDCLASSW wndClass = default;
			wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
			wndClass.hInstance = PInvoke.GetModuleHandle(null);
			wndClass.lpszClassName = (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in WindowClassName.GetPinnableReference()));
			PInvoke.RegisterClass(&wndClass);

			HWnd = PInvoke.CreateWindowEx(
				WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST,
				(PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in WindowClassName.GetPinnableReference())),
				(PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in WindowName.GetPinnableReference())),
				WINDOW_STYLE.WS_POPUP, 0, 0, 0, 0, HWND.Null, HMENU.Null, wndClass.hInstance, null);

			InitializeDesktopWindowXamlSource();
		}

		internal void SetContent(UIElement content)
		{
			if (_disposed)
				return;

			DesktopWindowXamlSource!.Content = content;
		}

		internal void MoveAndResize(RectInt32 rect)
		{
			if (_disposed)
				return;

			PInvoke.SetWindowPos(HWnd, HWND.HWND_TOP, rect.X, rect.Y, rect.Width, rect.Height, 0U);
			PInvoke.SetWindowPos(_xamlHwnd, HWND.HWND_TOP, 0, 0, rect.Width, rect.Height, 0U);
		}

		internal void Maximize()
		{
			if (_disposed)
				return;

			var bottomRightPoint = WindowHelpers.GetBottomRightCornerPoint();
			PInvoke.SetWindowPos(HWnd, HWND.HWND_TOP, 0, 0, bottomRightPoint.X, bottomRightPoint.Y, 0U);
			PInvoke.SetWindowPos(_xamlHwnd, HWND.HWND_TOP, 0, 0, bottomRightPoint.X, bottomRightPoint.Y, 0U);
		}

		internal void SetHWndRectRegion(RectInt32 rect)
		{
			if (_disposed)
				return;

			SetWindowRectRegion(HWnd, rect);
			SetWindowRectRegion(_xamlHwnd, rect);
		}

		private static void SetWindowRectRegion(HWND hWnd, RectInt32 rect)
		{
			HRGN region = PInvoke.CreateRectRgn(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
			if (region.IsNull)
				return;

			if (PInvoke.SetWindowRgn(hWnd, region, false) == 0)
				PInvoke.DeleteObject(region);
		}

		internal void UpdateWindowVisibility(bool isVisible)
		{
			if (_disposed)
				return;

			PInvoke.ShowWindow(HWnd, isVisible ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE);

#if UWP
			PInvoke.ShowWindow(_xamlHwnd, isVisible ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_HIDE);
#else
			if (isVisible) DesktopWindowXamlSource?.SiteBridge.Show(); else DesktopWindowXamlSource?.SiteBridge.Hide();
#endif
		}

#if UWP
		public bool TryPreTranslateMessage(MSG* msg)
		{
			BOOL result = false;

			_pdwxsn2.PreTranslateMessage(msg, &result);

			return result;
		}
#endif

		private void InitializeDesktopWindowXamlSource()
		{
			DesktopWindowXamlSource = new();

#if UWP
			// NOTE: Is this needed anymore? maybe for older builds?
			PInvoke.LoadLibrary((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "twinapi.appcore.dll".GetPinnableReference())));
			PInvoke.LoadLibrary((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "threadpoolwinrt.dll".GetPinnableReference())));

			// QI for IDesktopWindowXamlSourceNative2
			void* ppv;
			((IUnknown*)((IWinRTObject)DesktopWindowXamlSource).NativeObject.ThisPtr)->QueryInterface(
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_IDesktopWindowXamlSourceNative2)), &ppv);

			var sbcw = new StrategyBasedComWrappers();
			_pdwxsn2 = (IDesktopWindowXamlSourceNative2)sbcw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.None);

			// For extra safety
			GC.KeepAlive(DesktopWindowXamlSource);

			// Set the base HWND and get the XAML island HWND
			_pdwxsn2.AttachToWindow(HWnd);
			_pdwxsn2.get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd));

			RECT wRect;
			PInvoke.GetClientRect(HWnd, &wRect);
			PInvoke.SetWindowPos(_xamlHwnd, HWND.Null, 0, 0, wRect.right - wRect.left, wRect.bottom - wRect.top, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

			var xst = Window.Current.As<IXamlSourceTransparency>();
			xst.set_IsBackgroundTransparent(true);

			// Get CoreWindow and its HWND
			_coreWindow = CoreWindow.GetForCurrentThread();
			((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr)->QueryInterface(
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_ICoreWindowInterop)), &ppv);

			var pcwi = (ICoreWindowInterop)sbcw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.None);
			pcwi.get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd));
			
			_xamlInitialized = true;

#elif WASDK
			DesktopWindowXamlSource!.Initialize(Win32Interop.GetWindowIdFromWindow(HWnd));
			_xamlHwnd = (HWND)(nint)DesktopWindowXamlSource.SiteBridge.WindowId.Value;
#endif
		}

		private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			switch (uMsg)
			{
#if UWP
				case PInvoke.WM_SIZE:
					{
						var x = LOWORD(lParam);
						var y = HIWORD(lParam);

						if (_xamlHwnd != default)
							PInvoke.SetWindowPos(_xamlHwnd, HWND.Null, 0, 0, x, y, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

						if (_coreHwnd != default)
							PInvoke.SendMessage(_coreHwnd, PInvoke.WM_SIZE, (WPARAM)(nuint)x, y);
					}
					break;
				case PInvoke.WM_SETTINGCHANGE:
				case PInvoke.WM_THEMECHANGED:
					{
						// Process CoreWindow message
						if (_coreHwnd != default)
							PInvoke.SendMessage(_coreHwnd, uMsg, wParam, lParam);
					}
					break;
				case PInvoke.WM_SETFOCUS:
					{
						if (_xamlHwnd != default)
							PInvoke.SetFocus(_xamlHwnd);
					}
					break;
				case PInvoke.WM_DESTROY:
					{
						PInvoke.PostQuitMessage(0);
					}
					break;
#endif
				case PInvoke.WM_ACTIVATE:
					{
						if (LOWORD((nint)(nuint)wParam) == PInvoke.WA_INACTIVE)
							WindowInactivated?.Invoke(this, EventArgs.Empty);
					}
					break;
				default:
					{
						return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
					}
			}

			return (LRESULT)0;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			try
			{
				DesktopWindowXamlSource?.Dispose();
			}
			catch { }
			DesktopWindowXamlSource = null;

			PInvoke.DestroyWindow(HWnd);
			PInvoke.UnregisterClass((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in WindowClassName.GetPinnableReference())), PInvoke.GetModuleHandle(null));

			HWnd = default;
			_xamlHwnd = default;

			GC.SuppressFinalize(this);
		}
	}
}
