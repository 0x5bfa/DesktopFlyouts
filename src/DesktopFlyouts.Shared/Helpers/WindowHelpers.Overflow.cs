#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts
{
    internal unsafe static partial class WindowHelpers
    {
        internal static bool TryGetTaskbarOverflowWindowForBounds(Rectangle bounds, out HWND overflowWindow)
        {
            var anchorBounds = Rectangle.FromLTRB(
                bounds.Left,
                bounds.Top,
                Math.Max(bounds.Right, bounds.Left + 1),
                Math.Max(bounds.Bottom, bounds.Top + 1));

            foreach (var className in new[] { "TopLevelWindowForOverflowXamlIsland", "NotifyIconOverflowWindow" })
            {
                var candidate = FindWindowByClass(className, HWND.Null);
                while (!candidate.IsNull)
                {
                    if (PInvoke.IsWindowVisible(candidate))
                    {
                        RECT windowBounds;
                        if (PInvoke.GetWindowRect(candidate, &windowBounds))
                        {
                            var rect = Rectangle.FromLTRB(windowBounds.left, windowBounds.top, windowBounds.right, windowBounds.bottom);
                            if (rect.IntersectsWith(anchorBounds))
                            {
                                overflowWindow = candidate;
                                return true;
                            }
                        }
                    }

                    candidate = FindWindowByClass(className, candidate);
                }
            }

            overflowWindow = HWND.Null;
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
