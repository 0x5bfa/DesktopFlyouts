// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace U5BFA.Libraries
{
    /// <summary>
    /// Specifies where a desktop flyout is placed in the work area.
    /// </summary>
    /// <remarks>
    /// Placement is used by <see cref="DesktopFlyout.Show()"/>. It is bypassed for the single open
    /// operation started by the point-based show overload.
    /// </remarks>
    public enum DesktopFlyoutPlacementMode
    {
        /// <summary>
        /// Places the flyout at the top center of the work area.
        /// </summary>
        TopCenter,

        /// <summary>
        /// Places the flyout at the top-left corner of the work area.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Places the flyout at the top-right corner of the work area.
        /// </summary>
        TopRight,

        /// <summary>
        /// Places the flyout at the bottom center of the work area.
        /// </summary>
        BottomCenter,

        /// <summary>
        /// Places the flyout at the bottom-left corner of the work area.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Places the flyout at the bottom-right corner of the work area.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Places the flyout at the left center of the work area.
        /// </summary>
        LeftCenter,

        /// <summary>
        /// Places the flyout at the right center of the work area.
        /// </summary>
        RightCenter,
    }
}
