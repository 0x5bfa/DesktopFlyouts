// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts;

internal static class DesktopFlyoutChromeMetrics
{
	internal const double EdgeGap = 12;
	internal const double ShadowInset = 8;
	internal const double IslandSpacing = 12;

	internal static Rectangle InsetWorkArea(Rectangle area, double scale)
	{
		var gap = (int)Math.Ceiling(EdgeGap * scale);
		var horizontal = Math.Min(gap, Math.Max(0, (area.Width - 1) / 2));
		var vertical = Math.Min(gap, Math.Max(0, (area.Height - 1) / 2));
		return Rectangle.FromLTRB(area.Left + horizontal, area.Top + vertical, area.Right - horizontal, area.Bottom - vertical);
	}

	internal static Rectangle IncludeShadow(Rectangle visual, Rectangle monitor, double scale)
	{
		var shadow = (int)Math.Ceiling(ShadowInset * scale);
		return Rectangle.Intersect(Rectangle.FromLTRB(visual.Left - shadow, visual.Top - shadow, visual.Right + shadow, visual.Bottom + shadow), monitor);
	}
}
