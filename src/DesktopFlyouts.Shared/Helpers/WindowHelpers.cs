// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace U5BFA.Libraries
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

        internal static Rectangle GetFlyoutWorkAreaRect()
        {
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
