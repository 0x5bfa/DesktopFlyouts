// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

namespace DesktopFlyouts;

public sealed partial class DesktopFlyoutPosition
{
	/// <summary>
	/// Creates a position request that follows the current bounds of a tray icon.
	/// </summary>
	/// <remarks>
	/// The icon must have shell-reported bounds when the flyout is first shown. While open,
	/// the last available bounds are retained if the shell temporarily cannot locate it.
	/// The icon remains owned by the caller and is not disposed with the flyout.
	/// </remarks>
	public static DesktopFlyoutPosition ForTrayIcon(SystemTrayIcon trayIcon)
	{
		ArgumentNullException.ThrowIfNull(trayIcon);
		return FromTrayIconBounds(() => trayIcon.TryGetBounds(out var bounds) ? bounds : null);
	}
}
