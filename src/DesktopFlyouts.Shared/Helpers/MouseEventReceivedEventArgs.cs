// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts
{
    /// <summary>
    /// Provides the screen point associated with a tray icon mouse event.
    /// </summary>
    /// <remarks>
    /// The point is the center of the tray icon in physical screen pixels.
    /// </remarks>
    public class MouseEventReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the screen point associated with the mouse event.
        /// </summary>
        /// <value>The screen point in physical pixels.</value>
        public Point Point { get; }

        internal MouseEventReceivedEventArgs(Point point)
        {
            Point = point;
        }
    }
}
