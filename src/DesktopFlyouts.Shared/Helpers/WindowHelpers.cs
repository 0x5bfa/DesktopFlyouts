#if !HAS_UNO
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
        internal static Point GetCursorPosition()
        {
            Point point = default;
            return PInvoke.GetCursorPos(&point)
                ? point
                : Point.Empty;
        }

        internal static Rectangle GetFlyoutWorkAreaRect(DesktopFlyoutPosition position, Point? cursorPosition = null)
        {
            if (position.Anchor is Rectangle anchor)
            {
                return GetFlyoutWorkAreaRect(new Point(anchor.Left + (anchor.Width / 2), anchor.Top + (anchor.Height / 2)));
            }

            if (position.MonitorSelection is DesktopFlyoutMonitorSelection.Cursor)
            {
                return GetFlyoutWorkAreaRect(cursorPosition ?? GetCursorPosition());
            }

            return GetFlyoutWorkAreaRect();
        }

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

        internal static bool TryGetMonitorScreenRect(Rectangle anchor, out Rectangle screenBounds)
        {
            RECT anchorRect = new()
            {
                left = anchor.Left,
                top = anchor.Top,
                right = Math.Max(anchor.Right, anchor.Left + 1),
                bottom = Math.Max(anchor.Bottom, anchor.Top + 1),
            };
            var monitor = PInvoke.MonitorFromRect(&anchorRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitor.IsNull)
            {
                screenBounds = default;
                return false;
            }

            MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
            if (!PInvoke.GetMonitorInfo(monitor, &monitorInfo))
            {
                screenBounds = default;
                return false;
            }

            screenBounds = Rectangle.FromLTRB(
                monitorInfo.rcMonitor.left,
                monitorInfo.rcMonitor.top,
                monitorInfo.rcMonitor.right,
                monitorInfo.rcMonitor.bottom);
            return true;
        }

        internal static bool TryGetTaskbarInfoForBounds(Rectangle bounds, out Rectangle rect, out TaskbarEdge edge)
        {
            if (!TryGetTaskbarInfo(out rect, out edge))
                return false;

            const int tolerance = 8;
            var taskbarSearchBounds = rect;
            taskbarSearchBounds.Inflate(tolerance, tolerance);
            if (taskbarSearchBounds.IntersectsWith(bounds))
                return true;

            rect = default;
            edge = default;
            return false;
        }

        internal static Rectangle ExcludeTaskbarFromWorkArea(
            Rectangle workArea,
            Rectangle taskbarBounds,
            Rectangle monitorBounds)
        {
            // An auto-hide taskbar can overlay an rcWork that spans the whole monitor. Subtract
            // only its actual overlap, so a taskbar already excluded from rcWork is not counted twice.
            var overlap = Rectangle.Intersect(workArea, taskbarBounds);
            if (overlap.Width <= 0 || overlap.Height <= 0)
                return workArea;

            var edge = taskbarBounds.Width >= taskbarBounds.Height
                ? Math.Abs((long)taskbarBounds.Top - monitorBounds.Top) <= Math.Abs((long)monitorBounds.Bottom - taskbarBounds.Bottom)
                    ? TaskbarEdge.Top
                    : TaskbarEdge.Bottom
                : Math.Abs((long)taskbarBounds.Left - monitorBounds.Left) <= Math.Abs((long)monitorBounds.Right - taskbarBounds.Right)
                    ? TaskbarEdge.Left
                    : TaskbarEdge.Right;

            return edge switch
            {
                TaskbarEdge.Left => Rectangle.FromLTRB(
                    Math.Max(workArea.Left, overlap.Right),
                    workArea.Top,
                    workArea.Right,
                    workArea.Bottom),
                TaskbarEdge.Top => Rectangle.FromLTRB(
                    workArea.Left,
                    Math.Max(workArea.Top, overlap.Bottom),
                    workArea.Right,
                    workArea.Bottom),
                TaskbarEdge.Right => Rectangle.FromLTRB(
                    workArea.Left,
                    workArea.Top,
                    Math.Min(workArea.Right, overlap.Left),
                    workArea.Bottom),
                TaskbarEdge.Bottom => Rectangle.FromLTRB(
                    workArea.Left,
                    workArea.Top,
                    workArea.Right,
                    Math.Min(workArea.Bottom, overlap.Top)),
                _ => workArea,
            };
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

            var taskbarEdge = data.uEdge switch
            {
                PInvoke.ABE_LEFT => TaskbarEdge.Left,
                PInvoke.ABE_TOP => TaskbarEdge.Top,
                PInvoke.ABE_RIGHT => TaskbarEdge.Right,
                PInvoke.ABE_BOTTOM => TaskbarEdge.Bottom,
                _ => (TaskbarEdge?)null,
            };

            rect = Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
            if (taskbarEdge is null || rect.Width <= 0 || rect.Height <= 0)
            {
                rect = default;
                edge = default;
                return false;
            }

            edge = taskbarEdge.Value;
            return true;
        }
    }
}
#endif
