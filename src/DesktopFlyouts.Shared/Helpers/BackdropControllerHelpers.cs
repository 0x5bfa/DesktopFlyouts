// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Windows.UI;

#if WASDK
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.UI.ViewManagement;
#endif

namespace DesktopFlyouts
{
#if WASDK
    internal static class BackdropControllerHelpers
    {
        private static UISettings UISettings => field ??= new UISettings();

        internal static DesktopAcrylicController? GetAcrylicController(SystemBackdropTheme theme)
        {
            if (!DesktopAcrylicController.IsSupported())
                return null;

            if (GeneralHelpers.IsTaskbarColorPrevalenceEnabled())
                return GetAccentedAcrylicController();

            return IsLightTheme(theme)
                ? GetLightAcrylicController()
                : GetDarkAcrylicController();
        }

        internal static MicaController? GetMicaController(SystemBackdropTheme theme)
        {
            if (!MicaController.IsSupported())
                return null;

            if (GeneralHelpers.IsTaskbarColorPrevalenceEnabled())
                return GetAccentedMicaController();

            return IsLightTheme(theme)
                ? GetLightMicaController()
                : GetDarkMicaController();
        }

        internal static DesktopAcrylicController? GetDarkAcrylicController()
        {
            return new DesktopAcrylicController()
            {
                FallbackColor = Color.FromArgb(0xFF, 0x1C, 0x1C, 0x1C),
                LuminosityOpacity = 0.96F,
                TintColor = Color.FromArgb(0xFF, 0x20, 0x20, 0x20),
                TintOpacity = 0.5F,
            };
        }

        internal static DesktopAcrylicController? GetLightAcrylicController()
        {
            return new DesktopAcrylicController()
            {
                FallbackColor = Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE),
                LuminosityOpacity = 0.9F,
                TintColor = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
                TintOpacity = 0.0F,
            };
        }

        internal static DesktopAcrylicController? GetAccentedAcrylicController()
        {
            var systemAccentColorDark2 = UISettings.GetColorValue(UIColorType.AccentDark2);
            return new DesktopAcrylicController()
            {
                FallbackColor = systemAccentColorDark2,
                LuminosityOpacity = 0.8F,
                TintColor = systemAccentColorDark2,
                TintOpacity = 0.8F,
            };
        }

        internal static MicaController? GetDarkMicaController()
        {
            return new MicaController()
            {
                FallbackColor = Color.FromArgb(0xFF, 0x1C, 0x1C, 0x1C),
                LuminosityOpacity = 0.96F,
                TintColor = Color.FromArgb(0xFF, 0x20, 0x20, 0x20),
                TintOpacity = 0.5F,
            };
        }

        internal static MicaController? GetLightMicaController()
        {
            return new MicaController()
            {
                FallbackColor = Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE),
                LuminosityOpacity = 0.9F,
                TintColor = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
                TintOpacity = 0.0F,
            };
        }

        internal static MicaController? GetAccentedMicaController()
        {
            var systemAccentColorDark2 = UISettings.GetColorValue(UIColorType.AccentDark2);
            return new MicaController()
            {
                FallbackColor = systemAccentColorDark2,
                LuminosityOpacity = 0.8F,
                TintColor = systemAccentColorDark2,
                TintOpacity = 0.8F,
            };
        }

        private static bool IsLightTheme(SystemBackdropTheme theme)
        {
            return theme switch
            {
                SystemBackdropTheme.Light => true,
                SystemBackdropTheme.Dark => false,
                _ => GeneralHelpers.IsTaskbarLight(),
            };
        }
    }
#endif
}
