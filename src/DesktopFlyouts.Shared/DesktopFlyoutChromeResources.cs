// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
#elif WASDK
using Microsoft.UI.Xaml;
#endif

namespace DesktopFlyouts;

internal static class DesktopFlyoutChromeResources
{
	[ThreadStatic]
	private static ResourceDictionary? s_resources;

	private static ResourceDictionary Resources => s_resources ??= new ResourceDictionary
	{
#if UWP
		Source = new Uri("ms-appx:///DesktopFlyouts.Uwp/DesktopFlyout.xaml"),
#elif HAS_UNO
		Source = new Uri("ms-appx:///Themes/DesktopFlyout.xaml"),
#else
		Source = new Uri("ms-appx:///DesktopFlyouts.WinUI/DesktopFlyout.xaml"),
#endif
	};

	internal static T Get<T>(string key)
	{
		return (T)Resources[key];
	}

	internal static T GetTheme<T>(string themeKey, string key)
	{
		return (T)((ResourceDictionary)Resources.ThemeDictionaries[themeKey])[key];
	}
}
