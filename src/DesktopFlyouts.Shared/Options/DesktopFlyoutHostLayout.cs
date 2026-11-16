// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts;

/// <summary>
/// Maps a screen-space physical flyout region into a monitor-sized host's local physical and
/// XAML device-independent coordinates.
/// </summary>
internal readonly record struct DesktopFlyoutHostLayout(Rectangle HostBounds, Rectangle RelativePhysicalRegion, DesktopFlyoutDipRegion DipRegion)
{
	public static DesktopFlyoutHostLayout Create(Rectangle monitorBounds, Rectangle screenRegion, double rasterizationScale)
	{
		if (monitorBounds.Width <= 0 || monitorBounds.Height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(monitorBounds), "Monitor dimensions must be positive.");
		}

		if (rasterizationScale <= 0 || double.IsNaN(rasterizationScale) || double.IsInfinity(rasterizationScale))
		{
			throw new ArgumentOutOfRangeException(nameof(rasterizationScale), "Rasterization scale must be finite and positive.");
		}

		var relative = new Rectangle(
			screenRegion.X - monitorBounds.X,
			screenRegion.Y - monitorBounds.Y,
			screenRegion.Width,
			screenRegion.Height);

		if (!new Rectangle(Point.Empty, monitorBounds.Size).Contains(relative))
		{
			throw new ArgumentOutOfRangeException(nameof(screenRegion), "The flyout region must fit within the monitor host.");
		}

		return new DesktopFlyoutHostLayout(
			monitorBounds,
			relative,
			new DesktopFlyoutDipRegion(
				relative.X / rasterizationScale,
				relative.Y / rasterizationScale,
				relative.Width / rasterizationScale,
				relative.Height / rasterizationScale));
	}
}
