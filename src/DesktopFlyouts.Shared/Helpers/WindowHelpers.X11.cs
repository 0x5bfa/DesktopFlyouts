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

        internal static Point GetCursorPosition()
        {
            ThrowHelper.ThrowIfNotLinux();

            var display = OpenDisplay();
            if (display == IntPtr.Zero)
            {
                return Point.Empty;
            }

            try
            {
                var rootWindow = XDefaultRootWindow(display);
                if (rootWindow == IntPtr.Zero ||
                    XQueryPointer(display, rootWindow, out _, out _, out var rootX, out var rootY, out _, out _, out _) == 0)
                {
                    return Point.Empty;
                }

                return new Point(rootX, rootY);
            }
            finally
            {
                XCloseDisplay(display);
            }
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

        internal static bool TryGetMonitorScreenRect(Rectangle anchorBounds, out Rectangle screenBounds)
        {
            ThrowHelper.ThrowIfNotLinux();

            // X11 reports screen bounds for the root display; per-monitor rectangles are not
            // exposed through the display APIs used by this helper.
            screenBounds = default;

            var display = OpenDisplay();
            if (display == IntPtr.Zero)
                return false;

            try
            {
                var rootWindow = XDefaultRootWindow(display);
                if (rootWindow == IntPtr.Zero)
                    return false;

                return TryReadScreenDimensions(display, out screenBounds);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }

        internal static bool TryGetTaskbarInfoForBounds(Rectangle bounds, out Rectangle rect, out TaskbarEdge edge)
        {
            ThrowHelper.ThrowIfNotLinux();

            rect = default;
            edge = default;

            var display = OpenDisplay();
            if (display == IntPtr.Zero)
                return false;

            try
            {
                var rootWindow = XDefaultRootWindow(display);
                if (rootWindow == IntPtr.Zero || !TryReadScreenDimensions(display, out var screenRect))
                    return false;

                // EWMH struts identify panels that reserve screen space. Third-party panels that
                // do not publish these properties cannot be detected here.
                return TryFindPanelForBounds(display, rootWindow, bounds, screenRect, out rect, out edge);
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

        private static bool TryFindPanelForBounds(
            IntPtr display, IntPtr rootWindow,
            Rectangle anchorBounds, Rectangle screenRect,
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
                            var panelRectCandidate = new Rectangle(0, (int)struts[4], leftStrut, Math.Max(1, (int)struts[5] - (int)struts[4] + 1));
                            if (IsRectNearRect(anchorBounds, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(0, 0, leftStrut, screenRect.Height);
                                panelEdge = TaskbarEdge.Left;
                                return true;
                            }
                        }

                        if (rightStrut > 0)
                        {
                            var panelX = screenRect.Width - rightStrut;
                            var panelRectCandidate = new Rectangle(panelX, (int)struts[6], rightStrut, Math.Max(1, (int)struts[7] - (int)struts[6] + 1));
                            if (IsRectNearRect(anchorBounds, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(panelX, 0, rightStrut, screenRect.Height);
                                panelEdge = TaskbarEdge.Right;
                                return true;
                            }
                        }

                        if (topStrut > 0)
                        {
                            var panelRectCandidate = new Rectangle((int)struts[8], 0, Math.Max(1, (int)struts[9] - (int)struts[8] + 1), topStrut);
                            if (IsRectNearRect(anchorBounds, panelRectCandidate, tolerance))
                            {
                                panelRect = new Rectangle(0, 0, screenRect.Width, topStrut);
                                panelEdge = TaskbarEdge.Top;
                                return true;
                            }
                        }

                        if (bottomStrut > 0)
                        {
                            var panelY = screenRect.Height - bottomStrut;
                            var panelRectCandidate = new Rectangle((int)struts[10], panelY, Math.Max(1, (int)struts[11] - (int)struts[10] + 1), bottomStrut);
                            if (IsRectNearRect(anchorBounds, panelRectCandidate, tolerance))
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
                            if (IsRectNearRect(anchorBounds, candidate, tolerance))
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
                            if (IsRectNearRect(anchorBounds, candidate, tolerance))
                            {
                                panelRect = candidate;
                                panelEdge = TaskbarEdge.Right;
                                return true;
                            }
                        }

                        if (topStrut > 0)
                        {
                            var candidate = new Rectangle(0, 0, screenRect.Width, topStrut);
                            if (IsRectNearRect(anchorBounds, candidate, tolerance))
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
                            if (IsRectNearRect(anchorBounds, candidate, tolerance))
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

        private static bool IsRectNearRect(Rectangle bounds, Rectangle candidate, int tolerance)
        {
            var testRect = candidate;
            testRect.Inflate(tolerance, tolerance);
            return testRect.IntersectsWith(bounds);
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
