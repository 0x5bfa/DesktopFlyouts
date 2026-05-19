// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace U5BFA.Libraries
{
    /// <summary>
    /// Specifies the direction used for flyout placement and transition animations.
    /// </summary>
    public enum FlyoutPopupDirection
    {
        /// <summary>
        /// Opens upward from the bottom edge.
        /// </summary>
        BottomToTop,

        /// <summary>
        /// Opens downward from the top edge.
        /// </summary>
        TopToBottom,

        /// <summary>
        /// Chooses a vertical direction based on placement.
        /// </summary>
        Vertical,

        /// <summary>
        /// Opens rightward from the left edge.
        /// </summary>
        LeftToRight,

        /// <summary>
        /// Opens leftward from the right edge.
        /// </summary>
        RightToLeft,

        /// <summary>
        /// Chooses a horizontal direction based on placement.
        /// </summary>
        Horizontal,
    }
}
