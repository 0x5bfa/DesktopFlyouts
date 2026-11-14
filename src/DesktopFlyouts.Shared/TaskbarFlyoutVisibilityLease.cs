#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace DesktopFlyouts
{
    /// <summary>
    /// Keeps the taskbar expanded while a taskbar-anchored flyout is visible.
    /// </summary>
    internal sealed unsafe partial class TaskbarFlyoutVisibilityLease : IDisposable
    {
        private const uint WM_TASKBAR_FLYOUT_VISIBILITY_CHANGE = PInvoke.WM_USER + 0xF6; // Undocumented, found in 26300.

        private static readonly string[] TaskbarWindowClassNames =
        [
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
        ];

        private readonly HWND _taskbarWindow;
        private readonly uint _taskbarProcessId;
        private bool _disposed;

        private TaskbarFlyoutVisibilityLease(HWND taskbarWindow, uint taskbarProcessId)
        {
            _taskbarWindow = taskbarWindow;
            _taskbarProcessId = taskbarProcessId;
        }

        internal bool TryGetTaskbarBounds(out Rectangle taskbarBounds)
        {
            taskbarBounds = default;
            if (!IsCurrentTaskbarWindow(_taskbarWindow, _taskbarProcessId))
                return false;

            RECT windowBounds;
            if (!PInvoke.GetWindowRect(_taskbarWindow, &windowBounds))
                return false;

            taskbarBounds = Rectangle.FromLTRB(
                windowBounds.left,
                windowBounds.top,
                windowBounds.right,
                windowBounds.bottom);
            return taskbarBounds.Width > 0 && taskbarBounds.Height > 0;
        }

        internal static TaskbarFlyoutVisibilityLease? TryAcquire(Rectangle anchorBounds)
        {
            var taskbarWindow = FindTaskbarWindowForAnchor(anchorBounds);
            if (taskbarWindow.IsNull)
                return null;

            uint taskbarProcessId = 0;
            if (PInvoke.GetWindowThreadProcessId(taskbarWindow, &taskbarProcessId) == 0 || taskbarProcessId == 0)
                return null;

            if (!PInvoke.PostMessage(taskbarWindow, WM_TASKBAR_FLYOUT_VISIBILITY_CHANGE, (WPARAM)(nuint)1, (LPARAM)0))
                return null;

            return new TaskbarFlyoutVisibilityLease(taskbarWindow, taskbarProcessId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!IsCurrentTaskbarWindow(_taskbarWindow, _taskbarProcessId))
                return;

            PInvoke.PostMessage(_taskbarWindow, WM_TASKBAR_FLYOUT_VISIBILITY_CHANGE, (WPARAM)0, (LPARAM)0);
        }

        private static HWND FindTaskbarWindowForAnchor(Rectangle anchorBounds)
        {
            RECT anchorRect = new()
            {
                left = anchorBounds.Left,
                top = anchorBounds.Top,
                right = Math.Max(anchorBounds.Right, anchorBounds.Left + 1),
                bottom = Math.Max(anchorBounds.Bottom, anchorBounds.Top + 1),
            };
            var anchorMonitor = PInvoke.MonitorFromRect(&anchorRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (anchorMonitor.IsNull)
                return HWND.Null;

            foreach (var className in TaskbarWindowClassNames)
            {
                var taskbarWindow = FindWindowByClass(className, HWND.Null);
                while (!taskbarWindow.IsNull)
                {
                    if (PInvoke.MonitorFromWindow(taskbarWindow, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST) == anchorMonitor)
                        return taskbarWindow;

                    taskbarWindow = FindWindowByClass(className, taskbarWindow);
                }
            }

            return HWND.Null;
        }

        private static bool IsCurrentTaskbarWindow(HWND taskbarWindow, uint taskbarProcessId)
        {
            if (!PInvoke.IsWindow(taskbarWindow))
                return false;

            uint currentProcessId = 0;
            if (PInvoke.GetWindowThreadProcessId(taskbarWindow, &currentProcessId) == 0
                || currentProcessId != taskbarProcessId)
            {
                return false;
            }

            foreach (var className in TaskbarWindowClassNames)
            {
                var candidate = FindWindowByClass(className, HWND.Null);
                while (!candidate.IsNull)
                {
                    if (candidate == taskbarWindow)
                        return true;

                    candidate = FindWindowByClass(className, candidate);
                }
            }

            return false;
        }

        private static HWND FindWindowByClass(string className, HWND childAfter)
        {
            fixed (char* classNamePointer = className)
            {
                return PInvoke.FindWindowEx(
                    HWND.Null,
                    childAfter,
                    (PCWSTR)classNamePointer,
                    default);
            }
        }
    }
}
#endif
