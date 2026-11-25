// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP || (WASDK && !HAS_UNO)
using Windows.UI;

namespace DesktopFlyouts;

internal readonly record struct DesktopFlyoutBackdropPalette(
	Color TintColor, Color FallbackColor, double TintOpacity, double LuminosityOpacity)
{
	internal static DesktopFlyoutBackdropPalette ForTheme(bool useLightTheme, Color? accentColor)
	{
		if (accentColor is Color accent)
		{
			return new(accent, Color.FromArgb(255, accent.R, accent.G, accent.B), 0.8, 0.8);
		}

		return useLightTheme
			? new(Color.FromArgb(255, 243, 243, 243), Color.FromArgb(255, 238, 238, 238), 0.0, 0.9)
			: new(Color.FromArgb(255, 32, 32, 32), Color.FromArgb(255, 28, 28, 28), 0.5, 0.96);
	}
}
#endif
