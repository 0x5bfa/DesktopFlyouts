// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Describes the current lifecycle phase of a <see cref="DesktopFlyout"/>.
    /// </summary>
    public enum DesktopFlyoutTransitionState
    {
        /// <summary>The flyout is closed and no transition is running.</summary>
        Closed,

        /// <summary>The flyout is opening.</summary>
        Opening,

        /// <summary>The flyout is open and no transition is running.</summary>
        Open,

        /// <summary>The flyout is closing.</summary>
        Closing,
    }
}
