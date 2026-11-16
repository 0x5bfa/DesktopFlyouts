#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts;

internal sealed unsafe class TaskbarLayoutMonitor : IDisposable
{
    private readonly HashSet<HWND> _windows = [];
    private readonly Func<Action, bool> _tryDispatch;
    private readonly Action _onChanged;
    private readonly WinEventProc _winEventProc;
    private HWINEVENTHOOK _hook;
    private bool _queued;
    private bool _disposed;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void WinEventProc(HWINEVENTHOOK hook, uint eventType, HWND window, int objectId, int childId, uint eventThread, uint eventTime);

    private TaskbarLayoutMonitor(IEnumerable<HWND> windows, Func<Action, bool> tryDispatch, Action onChanged)
    {
        _windows.UnionWith(windows);
        _tryDispatch = tryDispatch;
        _onChanged = onChanged;
        _winEventProc = OnWinEvent;
    }

    internal static TaskbarLayoutMonitor? TryCreate(IEnumerable<HWND> windows, Func<Action, bool> tryDispatch, Action onChanged)
    {
        var monitor = new TaskbarLayoutMonitor(windows, tryDispatch, onChanged);
        // OUTOFCONTEXT callbacks run on this UI thread's message loop. A global hook also
        // survives Explorer restarts; the tracked HWNDs are refreshed by the layout reader.
        monitor._hook = PInvoke.SetWinEventHook(PInvoke.EVENT_OBJECT_CREATE, PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            default, (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)Marshal.GetFunctionPointerForDelegate(monitor._winEventProc),
            0, 0, PInvoke.WINEVENT_OUTOFCONTEXT);
        if (monitor._hook.IsNull)
        {
            monitor.Dispose();
            return null;
        }

        return monitor;
    }

    internal void SetWindows(IEnumerable<HWND> windows)
    {
        if (_disposed)
            return;

        _windows.Clear();
        _windows.UnionWith(windows);
    }

    private void OnWinEvent(HWINEVENTHOOK hook, uint eventType, HWND window, int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (_disposed || _queued || !_windows.Contains(window)
            || objectId != (int)OBJECT_IDENTIFIER.OBJID_WINDOW || childId != PInvoke.CHILDID_SELF
            || (eventType != PInvoke.EVENT_OBJECT_CREATE && eventType != PInvoke.EVENT_OBJECT_DESTROY
                && eventType != PInvoke.EVENT_OBJECT_SHOW && eventType != PInvoke.EVENT_OBJECT_HIDE
                && eventType != PInvoke.EVENT_OBJECT_LOCATIONCHANGE))
            return;

        _queued = true;
        if (!_tryDispatch(NotifyChanged))
            Dispose();
    }

    private void NotifyChanged()
    {
        if (_disposed)
            return;

        _queued = false;
        _onChanged();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (!_hook.IsNull)
        {
            PInvoke.UnhookWinEvent(_hook);
            _hook = default;
        }

        _windows.Clear();
        GC.KeepAlive(_winEventProc);
    }
}
#endif
