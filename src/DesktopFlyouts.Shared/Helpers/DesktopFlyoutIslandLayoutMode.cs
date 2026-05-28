// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Defines how islands are arranged inside a <see cref="DesktopFlyout"/>.
    /// </summary>
    public enum DesktopFlyoutIslandLayoutMode
    {
        /// <summary>
        /// Arrange islands in the flyout's stack grid using <see cref="DesktopFlyout.IslandsOrientation"/>.
        /// </summary>
        Stack,

        /// <summary>
        /// Arrange islands on a freeform canvas using each island's canvas position.
        /// </summary>
        Freeform,
    }
}
