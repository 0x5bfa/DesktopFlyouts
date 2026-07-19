#if HAS_UNO
// Real implementation: detects work area and panel position via X11 EWMH properties.
// _NET_WORKAREA gives the usable screen area excluding all panels.
// _NET_WM_STRUT_PARTIAL on child windows identifies individual panel positions.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using static DesktopFlyouts.X11PInvoke;

namespace DesktopFlyouts
{
    internal enum TaskbarEdge
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    internal static partial class WindowHelpers
    {
        private static IntPtr OpenDisplay()
        {
            return XOpenDisplay(IntPtr.Zero);
        }

        internal static Point GetBottomRightCornerPoint()
        {
            ThrowHelper.ThrowIfNotLinux();

            var rect = GetFlyoutWorkAreaRect();
            return new(rect.Right, rect.Bottom);
        }

        internal static Rectangle GetFlyoutWorkAreaRect(Point? anchorPoint = null)
        {
            ThrowHelper.ThrowIfNotLinux();

            var display = OpenDisplay();
            if (display == IntPtr.Zero)
            {
                return new Rectangle(0, 0, 1920, 1080);
            }

            try
            {
                var rootWindow = XDefaultRootWindow(display);
                if (rootWindow == IntPtr.Zero)
                {
                    return new Rectangle(0, 0, 1920, 1080);
                }

                // Read _NET_WORKAREA (4 CARDINALs: left, top, width, height)
                if (TryReadNetWorkArea(display, rootWindow, out var workArea))
                {
                    return workArea;
                }

                // Fallback: full screen
                if (TryReadScreenDimensions(display, out var screenRect))
                {
                    return screenRect;
                }

                // all methods failed, using default 1920x1080
                return new Rectangle(0, 0, 1920, 1080);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }

        internal static bool TryGetTaskbarInfoForPoint(Point point, out Rectangle rect, out TaskbarEdge edge)
        {
            ThrowHelper.ThrowIfNotLinux();

            rect = default;
            edge = TaskbarEdge.Bottom;

            var display = OpenDisplay();
            if (display == IntPtr.Zero)
                return false;

            try
            {
                var rootWindow = XDefaultRootWindow(display);
                if (rootWindow == IntPtr.Zero)
                    return false;

                // Enumerate child windows and check _NET_WM_STRUT_PARTIAL / _NET_WM_STRUT
                if (!TryReadNetWorkArea(display, rootWindow, out var workArea))
                    return false;

                var screenRect = workArea;
                if (TryReadScreenDimensions(display, out var full))
                    screenRect = full;

                return TryFindPanelAtPoint(display, rootWindow, point, screenRect, out rect, out edge);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }

        private static bool TryReadNetWorkArea(IntPtr display, IntPtr rootWindow, out Rectangle workArea)
        {
            workArea = default;

            var netWorkAreaAtom = XInternAtom(display, "_NET_WORKAREA", false);
            if (ReadCardinals(display, rootWindow, netWorkAreaAtom, out var cardinals) && cardinals.Length >= 4)
            {
                workArea = new Rectangle(
                    (int)cardinals[0],
                    (int)cardinals[1],
                    (int)cardinals[2],
                    (int)cardinals[3]);
                return workArea.Width > 0 && workArea.Height > 0;
            }

            // _NET_WORKAREA not available or empty
            return false;
        }

        private static bool TryReadScreenDimensions(IntPtr display, out Rectangle screenRect)
        {
            screenRect = default;
            var rootWindow = XDefaultRootWindow(display);
            if (rootWindow == IntPtr.Zero)
            {
                return false;
            }

            var attrs = new XWindowAttributes();
            if (XGetWindowAttributes(display, rootWindow, ref attrs) != 0)
            {
                screenRect = new Rectangle(0, 0, attrs.Width, attrs.Height);
                return attrs.Width > 0 && attrs.Height > 0;
            }

            return false;
        }

        private static bool TryFindPanelAtPoint(
            IntPtr display, IntPtr rootWindow,
            Point point, Rectangle screenRect,
            out Rectangle panelRect, out TaskbarEdge panelEdge)
        {
            panelRect = default;
            panelEdge = TaskbarEdge.Bottom;

            const int tolerance = 8;

            if (XQueryTree(display, rootWindow, out _, out _, out var children, out var nchildren) == 0)
                return false;

            try
            {
                if (nchildren == 0 || children == IntPtr.Zero)
                    return false;

                var netWmStrutPartial = XInternAtom(display, "_NET_WM_STRUT_PARTIAL", false);
                var netWmStrut = XInternAtom(display, "_NET_WM_STRUT", false);
                var cardinalAtom = XInternAtom(display, "CARDINAL", false);

                for (int i = 0; i < nchildren; i++)
                {
                    var child = Marshal.ReadIntPtr(children, i * IntPtr.Size);

                    // Try _NET_WM_STRUT_PARTIAL first (12 CARDINALs)
                    if (ReadCardinals(display, child, netWmStrutPartial, out var struts) && struts.Length >= 12)
                    {
                        // Format: left, right, top, bottom, left_start_y, left_end_y,
                        //         right_start_y, right_end_y, top_start_x, top_end_x,
                        //         bottom_start_x, bottom_end_x
                        var leftStrut = (int)struts[0];
                        var rightStrut = (int)struts[1];
                        var topStrut = (int)struts[2];
                        var bottomStrut = (int)struts[3];

                        if (leftStrut > 0)
                        {
                            var panelRectCandidate = new Rectangle(0, (int)struts[4], leftStrut, (int)struts[5] - (int)struts[4]);
                            if (IsPointNearRect(point, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(0, 0, leftStrut, screenRect.Height);
                                panelEdge = TaskbarEdge.Left;
                                return true;
                            }
                        }

                        if (rightStrut > 0)
                        {
                            var panelX = screenRect.Width - rightStrut;
                            var panelRectCandidate = new Rectangle(panelX, (int)struts[6], rightStrut, (int)struts[7] - (int)struts[6]);
                            if (IsPointNearRect(point, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(panelX, 0, rightStrut, screenRect.Height);
                                panelEdge = TaskbarEdge.Right;
                                return true;
                            }
                        }

                        if (topStrut > 0)
                        {
                            var panelRectCandidate = new Rectangle((int)struts[8], 0, (int)struts[9] - (int)struts[8], topStrut);
                            if (IsPointNearRect(point, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(0, 0, screenRect.Width, topStrut);
                                panelEdge = TaskbarEdge.Top;
                                return true;
                            }
                        }

                        if (bottomStrut > 0)
                        {
                            var panelY = screenRect.Height - bottomStrut;
                            var panelRectCandidate = new Rectangle((int)struts[10], panelY, (int)struts[11] - (int)struts[10], bottomStrut);
                            if (IsPointNearRect(point, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(0, panelY, screenRect.Width, bottomStrut);
                                panelEdge = TaskbarEdge.Bottom;
                                return true;
                            }
                        }
                    }
                    // Fallback to _NET_WM_STRUT (4 CARDINALs)
                    else if (ReadCardinals(display, child, netWmStrut, out struts) && struts.Length >= 4)
                    {
                        var leftStrut = (int)struts[0];
                        var rightStrut = (int)struts[1];
                        var topStrut = (int)struts[2];
                        var bottomStrut = (int)struts[3];

                        if (leftStrut > 0)
                        {
                            var candidate = new Rectangle(0, 0, leftStrut, screenRect.Height);
                            if (IsPointNearRect(point, candidate, tolerance))
                            {
                                panelRect = candidate;
                                panelEdge = TaskbarEdge.Left;
                                return true;
                            }
                        }

                        if (rightStrut > 0)
                        {
                            var panelX = screenRect.Width - rightStrut;
                            var candidate = new Rectangle(panelX, 0, rightStrut, screenRect.Height);
                            if (IsPointNearRect(point, candidate, tolerance))
                            {
                                panelRect = candidate;
                                panelEdge = TaskbarEdge.Right;
                                return true;
                            }
                        }

                        if (topStrut > 0)
                        {
                            var candidate = new Rectangle(0, 0, screenRect.Width, topStrut);
                            if (IsPointNearRect(point, candidate, tolerance))
                            {
                                panelRect = candidate;
                                panelEdge = TaskbarEdge.Top;
                                return true;
                            }
                        }

                        if (bottomStrut > 0)
                        {
                            var panelY = screenRect.Height - bottomStrut;
                            var candidate = new Rectangle(0, panelY, screenRect.Width, bottomStrut);
                            if (IsPointNearRect(point, candidate, tolerance))
                            {
                                panelRect = candidate;
                                panelEdge = TaskbarEdge.Bottom;
                                return true;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (children != IntPtr.Zero)
                    XFree(children);
            }

            return false;
        }

        private static bool IsPointNearRect(Point point, Rectangle rect, int tolerance)
        {
            var testRect = rect;
            testRect.Inflate(tolerance, tolerance);
            return testRect.Contains(point);
        }

        private static bool ReadCardinals(IntPtr display, nint window, nint atom, out nuint[] cardinals)
        {
            cardinals = [];

            var result = XGetWindowProperty(
                display, (nuint)window, (nuint)atom,
                0, 256,
                false, 6, // CARDINAL = 6
                out var actualType, out _,
                out var nItems, out _, out var prop);

            if (result != 0 || actualType is 0 || nItems is 0 || prop is 0)
                return false;

            try
            {
                var count = (int)nItems;
                cardinals = new nuint[count];
                for (int i = 0; i < count; i++)
                {
                    cardinals[i] = (nuint)Marshal.ReadIntPtr(prop, i * nuint.Size);
                }
                return count > 0;
            }
            finally
            {
                XFree(prop);
            }
        }
    }
}
#endif
