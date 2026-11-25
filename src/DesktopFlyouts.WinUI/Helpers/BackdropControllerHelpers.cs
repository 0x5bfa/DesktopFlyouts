// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition.SystemBackdrops;
using Windows.UI.ViewManagement;

namespace DesktopFlyouts;

internal static class BackdropControllerHelpers
{
	private static UISettings UISettings => field ??= new UISettings();

	internal static DesktopFlyoutBackdropPalette GetPalette(bool useLightTheme)
	{
		var accent = GeneralHelpers.IsTaskbarColorPrevalenceEnabled()
			? UISettings.GetColorValue(UIColorType.AccentDark2)
			: (Windows.UI.Color?)null;
		return DesktopFlyoutBackdropPalette.ForTheme(useLightTheme, accent);
	}

	internal static DesktopAcrylicController? GetAcrylicController(bool useLightTheme)
	{
		if (!DesktopAcrylicController.IsSupported())
		{
			return null;
		}

		var palette = GetPalette(useLightTheme);
		var controller = new DesktopAcrylicController();
		try
		{
			controller.FallbackColor = palette.FallbackColor;
			controller.TintColor = palette.TintColor;
			controller.TintOpacity = (float)palette.TintOpacity;
			controller.LuminosityOpacity = (float)palette.LuminosityOpacity;
			return controller;
		}
		catch
		{
			controller.Dispose();
			throw;
		}
	}

	internal static MicaController? GetMicaController(bool useLightTheme)
	{
		if (!MicaController.IsSupported())
		{
			return null;
		}

		var palette = GetPalette(useLightTheme);
		var controller = new MicaController();
		try
		{
			controller.FallbackColor = palette.FallbackColor;
			controller.TintColor = palette.TintColor;
			controller.TintOpacity = (float)palette.TintOpacity;
			controller.LuminosityOpacity = (float)palette.LuminosityOpacity;
			return controller;
		}
		catch
		{
			controller.Dispose();
			throw;
		}
	}
}
