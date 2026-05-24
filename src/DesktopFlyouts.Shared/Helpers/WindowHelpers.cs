// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts
{
    internal enum TaskbarEdge
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    internal unsafe static partial class WindowHelpers
    {
        internal static Point GetBottomRightCornerPoint()
        {
            var rect = GetFlyoutWorkAreaRect();

            return new(rect.Right, rect.Bottom);
        }

        internal static Rectangle GetFlyoutWorkAreaRect(Point? anchorPoint = null)
        {
            if (anchorPoint is Point point && TryGetMonitorWorkAreaRect(point, out var monitorWorkArea))
                return monitorWorkArea;

            var workArea = GetSystemWorkAreaRect();
            if (!TryGetTaskbarInfo(out var taskbarRect, out var edge))
                return workArea;

            return edge switch
            {
                TaskbarEdge.Left => Rectangle.FromLTRB(
                    Math.Max(workArea.Left, taskbarRect.Right),
                    workArea.Top,
                    workArea.Right,
                    workArea.Bottom),
                TaskbarEdge.Top => Rectangle.FromLTRB(
                    workArea.Left,
                    Math.Max(workArea.Top, taskbarRect.Bottom),
                    workArea.Right,
                    workArea.Bottom),
                TaskbarEdge.Right => Rectangle.FromLTRB(
                    workArea.Left,
                    workArea.Top,
                    Math.Min(workArea.Right, taskbarRect.Left),
                    workArea.Bottom),
                TaskbarEdge.Bottom => Rectangle.FromLTRB(
                    workArea.Left,
                    workArea.Top,
                    workArea.Right,
                    Math.Min(workArea.Bottom, taskbarRect.Top)),
                _ => workArea,
            };
        }

        private static bool TryGetMonitorWorkAreaRect(Point point, out Rectangle workArea)
        {
            RECT anchorRect = new()
            {
                left = point.X,
                top = point.Y,
                right = point.X + 1,
                bottom = point.Y + 1,
            };
            var monitor = PInvoke.MonitorFromRect(&anchorRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitor.IsNull)
            {
                workArea = default;
                return false;
            }

            MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
            if (!PInvoke.GetMonitorInfo(monitor, &monitorInfo))
            {
                workArea = default;
                return false;
            }

            workArea = Rectangle.FromLTRB(
                monitorInfo.rcWork.left,
                monitorInfo.rcWork.top,
                monitorInfo.rcWork.right,
                monitorInfo.rcWork.bottom);

            return true;
        }

        internal static bool TryGetTaskbarInfoForPoint(Point point, out Rectangle rect, out TaskbarEdge edge)
        {
            if (!TryGetTaskbarInfo(out rect, out edge))
                return false;

            const int tolerance = 8;
            var testRect = rect;
            testRect.Inflate(tolerance, tolerance);

            return testRect.Contains(point);
        }

        private static Rectangle GetSystemWorkAreaRect()
        {
            RECT rect;
            PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rect, 0);

            return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        }

        private static bool TryGetTaskbarInfo(out Rectangle rect, out TaskbarEdge edge)
        {
            APPBARDATA data = default;
            data.cbSize = (uint)sizeof(APPBARDATA);

            if (PInvoke.SHAppBarMessage(PInvoke.ABM_GETTASKBARPOS, &data) == 0U)
            {
                rect = default;
                edge = default;
                return false;
            }

            rect = Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
            edge = data.uEdge switch
            {
                PInvoke.ABE_LEFT => TaskbarEdge.Left,
                PInvoke.ABE_TOP => TaskbarEdge.Top,
                PInvoke.ABE_RIGHT => TaskbarEdge.Right,
                _ => TaskbarEdge.Bottom,
            };

            return true;
        }
    }
}
