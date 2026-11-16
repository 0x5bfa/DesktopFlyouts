// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if !HAS_UNO

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts;

/// <summary>
/// Watches a taskbar notification-area overflow window while its anchored flyout is open.
/// </summary>
internal sealed unsafe class TaskbarOverflowFlyoutMonitor : IDisposable
{
	private readonly HWND _overflowWindow;
	private readonly Func<Action, bool> _tryDispatch;
	private readonly Action _onClosed;
	private readonly WinEventProc _winEventProc;
	private HWINEVENTHOOK _eventHook;
	private bool _closeQueued;
	private bool _disposed;

	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate void WinEventProc(HWINEVENTHOOK hook, uint eventType, HWND window, int objectId, int childId, uint eventThread, uint eventTime);

	private TaskbarOverflowFlyoutMonitor(HWND overflowWindow, Func<Action, bool> tryDispatch, Action onClosed)
	{
		_overflowWindow = overflowWindow;
		_tryDispatch = tryDispatch;
		_onClosed = onClosed;
		_winEventProc = OnWinEvent;
	}

	internal static TaskbarOverflowFlyoutMonitor? TryCreate(Rectangle anchorBounds, Func<Action, bool> tryDispatch, Action onClosed)
	{
		if (!WindowHelpers.TryGetTaskbarOverflowWindowForBounds(anchorBounds, out var overflowWindow))
		{
			return null;
		}

		uint processId = 0;
		var threadId = PInvoke.GetWindowThreadProcessId(overflowWindow, &processId);
		if (threadId == 0 || processId == 0)
		{
			return null;
		}

		var monitor = new TaskbarOverflowFlyoutMonitor(overflowWindow, tryDispatch, onClosed);
		monitor._eventHook = PInvoke.SetWinEventHook(
			PInvoke.EVENT_OBJECT_DESTROY,
			PInvoke.EVENT_OBJECT_HIDE,
			default,
			(delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)Marshal.GetFunctionPointerForDelegate(monitor._winEventProc),
			processId,
			threadId,
			PInvoke.WINEVENT_OUTOFCONTEXT);
		if (monitor._eventHook.IsNull)
		{
			monitor.Dispose();
			return null;
		}

		Debug.WriteLine($"[DesktopFlyout] Notification-area overflow detected; hwnd=0x{(nint)overflowWindow.Value:X}.");

		// Cover a close between finding the visible window and installing the hook.
		if (!PInvoke.IsWindow(overflowWindow) || !PInvoke.IsWindowVisible(overflowWindow))
		{
			monitor.QueueClosed();
		}

		return monitor;
	}

	internal bool TryGetBounds(out Rectangle bounds)
	{
		bounds = default;
		if (_disposed || !PInvoke.IsWindowVisible(_overflowWindow))
		{
			return false;
		}

		RECT rect;
		if (!PInvoke.GetWindowRect(_overflowWindow, &rect) || rect.right <= rect.left || rect.bottom <= rect.top)
		{
			return false;
		}

		bounds = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
		return true;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		if (!_eventHook.IsNull)
		{
			PInvoke.UnhookWinEvent(_eventHook);
			_eventHook = default;
		}

		GC.KeepAlive(_winEventProc);
	}

	private void OnWinEvent(HWINEVENTHOOK hook, uint eventType, HWND window, int objectId, int childId, uint eventThread, uint eventTime)
	{
		// Destroy notifications can arrive after the HWND stops being queryable.
		if (_disposed
			|| window != _overflowWindow
			|| objectId != (int)OBJECT_IDENTIFIER.OBJID_WINDOW
			|| childId != PInvoke.CHILDID_SELF
			|| (eventType != PInvoke.EVENT_OBJECT_HIDE && eventType != PInvoke.EVENT_OBJECT_DESTROY))
		{
			return;
		}

		QueueClosed();
	}

	private void QueueClosed()
	{
		if (_disposed || _closeQueued)
		{
			return;
		}

		_closeQueued = true;
		if (!_tryDispatch(CompleteClose))
		{
			Dispose();
		}
	}

	private void CompleteClose()
	{
		if (_disposed)
		{
			return;
		}

		Debug.WriteLine($"[DesktopFlyout] Notification-area overflow closed; hwnd=0x{(nint)_overflowWindow.Value:X}.");
		Dispose();
		_onClosed();
	}
}
#endif
