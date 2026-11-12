// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts
{
    /// <summary>
    /// Specifies a fixed position within a monitor work area.
    /// </summary>
    public enum DesktopFlyoutWorkAreaPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        LeftCenter,
        RightCenter,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    /// <summary>
    /// Legacy work-area positions. Use <see cref="DesktopFlyoutWorkAreaPosition"/> with
    /// <see cref="DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition, DesktopFlyoutMonitorSelection, DesktopFlyoutPopupDirection)"/>.
    /// </summary>
    public enum DesktopFlyoutPlacementMode
    {
        TopCenter,
        TopLeft,
        TopRight,
        BottomCenter,
        BottomLeft,
        BottomRight,
        LeftCenter,
        RightCenter,
    }
}
