// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Specifies the backdrop behavior used by a <see cref="DesktopFlyout"/>.
    /// </summary>
    public enum DesktopFlyoutBackdropKind
    {
        /// <summary>
        /// Uses the Desktop Acrylic material.
        /// </summary>
        DesktopAcrylic = 0,

        /// <summary>
        /// Uses the base Mica material.
        /// </summary>
        Mica = 1,

        /// <summary>
        /// Does not apply a library-owned backdrop.
        /// </summary>
        None = 2,
    }
}
