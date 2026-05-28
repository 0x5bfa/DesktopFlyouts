// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Defines how a <see cref="DesktopFlyout"/> can be moved by native window dragging.
    /// </summary>
    public enum DesktopFlyoutDragMode
    {
        /// <summary>
        /// The flyout cannot be moved by dragging.
        /// </summary>
        None,

        /// <summary>
        /// The whole flyout surface behaves as a native window drag area.
        /// </summary>
        Full,

        /// <summary>
        /// Only registered <see cref="DesktopFlyoutDragRegion"/> elements behave as native window drag areas.
        /// </summary>
        Region,
    }
}
