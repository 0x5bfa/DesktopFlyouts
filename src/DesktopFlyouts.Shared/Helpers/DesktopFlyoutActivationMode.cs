// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Specifies how a flyout interacts with window activation and focus.
    /// </summary>
    /// <remarks>
    /// Activation behavior is applied when a <see cref="DesktopFlyout"/> opens and while it is hosted
    /// in its desktop island window.
    /// </remarks>
    public enum DesktopFlyoutActivationMode
    {
        /// <summary>
        /// Activates the flyout when it opens.
        /// </summary>
        /// <remarks>
        /// This is the default behavior. The flyout receives focus and can participate in normal
        /// keyboard navigation.
        /// </remarks>
        Activate,

        /// <summary>
        /// Opens the flyout without activating it.
        /// </summary>
        /// <remarks>
        /// The previous foreground window is restored after the flyout opens, but the flyout can still
        /// become active later through user interaction.
        /// </remarks>
        NoActivateOnOpen,

        /// <summary>
        /// Prevents the flyout from becoming active or focused.
        /// </summary>
        /// <remarks>
        /// Use this for passive flyouts that should not take focus away from the current foreground
        /// window. Keyboard focus inside the flyout is suppressed.
        /// </remarks>
        NeverActivate,
    }
}
