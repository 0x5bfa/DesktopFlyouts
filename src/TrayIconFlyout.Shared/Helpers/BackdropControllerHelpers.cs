// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Windows.UI;

#if WASDK
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
#endif

namespace U5BFA.Libraries
{
#if WASDK
	internal static class BackdropControllerHelpers
	{
		internal static DesktopAcrylicController? GetDarkAcrylicController(ResourceDictionary resources)
		{
			return new DesktopAcrylicController()
			{
				FallbackColor = Color.FromArgb(0xFF, 0x1C, 0x1C, 0x1C),
				LuminosityOpacity = 0.96F,
				TintColor = Color.FromArgb(0xFF, 0x20, 0x20, 0x20),
				TintOpacity = 0.5F,
			};
		}

		internal static DesktopAcrylicController? GetLightAcrylicController(ResourceDictionary resources)
		{
			return new DesktopAcrylicController()
			{
				FallbackColor = Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE),
				LuminosityOpacity = 0.9F,
				TintColor = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
				TintOpacity = 0.0F,
			};
		}

		internal static DesktopAcrylicController? GetAccentedAcrylicController(ResourceDictionary resources)
		{
			if (!resources.TryGetValue("SystemAccentColorDark2", out var resource_SystemAccentColorDark2) || resource_SystemAccentColorDark2 is not Color systemAccentColorDark2)
				return null;

			return new DesktopAcrylicController()
			{
				FallbackColor = systemAccentColorDark2,
				LuminosityOpacity = 0.8F,
				TintColor = systemAccentColorDark2,
				TintOpacity = 0.8F,
			};
		}

		internal static MicaController? GetDarkMicaController(ResourceDictionary resources)
		{
			return new MicaController()
			{
				FallbackColor = Color.FromArgb(0xFF, 0x1C, 0x1C, 0x1C),
				LuminosityOpacity = 0.96F,
				TintColor = Color.FromArgb(0xFF, 0x20, 0x20, 0x20),
				TintOpacity = 0.5F,
			};
		}

		internal static MicaController? GetLightMicaController(ResourceDictionary resources)
		{
			return new MicaController()
			{
				FallbackColor = Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE),
				LuminosityOpacity = 0.9F,
				TintColor = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
				TintOpacity = 0.0F,
			};
		}

		internal static MicaController? GetAccentedMicaController(ResourceDictionary resources)
		{
			if (!resources.TryGetValue("SystemAccentColorDark2", out var resource_SystemAccentColorDark2) || resource_SystemAccentColorDark2 is not Color systemAccentColorDark2)
				return null;

			return new MicaController()
			{
				FallbackColor = systemAccentColorDark2,
				LuminosityOpacity = 0.8F,
				TintColor = systemAccentColorDark2,
				TintOpacity = 0.8F,
			};
		}
	}
#endif
}
