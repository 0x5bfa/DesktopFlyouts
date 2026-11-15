#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace DesktopFlyouts
{
    internal unsafe static partial class WindowHelpers
    {
        internal static Rectangle ReadDisplayArea(Rectangle reference, out Rectangle monitorBounds,
            out Rectangle? taskbarBounds, out List<HWND> barWindows)
        {
            var point = new Point(reference.Left + reference.Width / 2, reference.Top + reference.Height / 2);
            var workArea = TryGetMonitorWorkAreaRect(point, out var currentWorkArea) ? currentWorkArea : GetSystemWorkAreaRect();
            var monitor = TryGetMonitorScreenRect(reference, out var currentMonitor) ? currentMonitor : workArea;
            RECT monitorRect = new() { left = monitor.Left, top = monitor.Top, right = monitor.Right, bottom = monitor.Bottom };
            var monitorHandle = PInvoke.MonitorFromRect(&monitorRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            var windows = new List<HWND>();
            var seen = new HashSet<HWND>();
            Rectangle? taskbar = null;

            void AddBar(HWND window)
            {
                if (window.IsNull || !seen.Add(window))
                    return;

                windows.Add(window);
                if (PInvoke.IsWindowVisible(window) && TryGetBarBounds(window, out var bounds))
                    workArea = ExcludeTaskbarFromWorkArea(workArea, bounds, monitor);
            }

            foreach (var className in new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" })
            {
                var window = FindDesktopBarWindow(className, HWND.Null);
                while (!window.IsNull)
                {
                    if (PInvoke.MonitorFromWindow(window, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST) == monitorHandle)
                    {
                        if (taskbar is null && TryGetBarBounds(window, out var bounds))
                            taskbar = bounds;
                        AddBar(window);
                    }

                    window = FindDesktopBarWindow(className, window);
                }
            }

            // Normal registered appbars are already represented by rcWork. Auto-hide bars can
            // overlay it; discover each edge on this monitor and subtract only the visible overlap.
            for (uint edge = 0; edge < 4; edge++)
            {
                APPBARDATA data = new() { cbSize = (uint)sizeof(APPBARDATA), uEdge = edge, rc = monitorRect };
                AddBar((HWND)(nint)(nuint)PInvoke.SHAppBarMessage(PInvoke.ABM_GETAUTOHIDEBAREX, &data));
            }

            monitorBounds = monitor;
            taskbarBounds = taskbar;
            barWindows = windows;
            return workArea;
        }

        private static bool TryGetBarBounds(HWND window, out Rectangle bounds)
        {
            bounds = default;
            RECT rect;
            if (!PInvoke.GetWindowRect(window, &rect) || rect.right <= rect.left || rect.bottom <= rect.top)
                return false;

            bounds = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
            return true;
        }

        private static HWND FindDesktopBarWindow(string className, HWND after)
        {
            fixed (char* name = className)
                return PInvoke.FindWindowEx(HWND.Null, after, (PCWSTR)name, default);
        }
    }
}
#endif
