// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace DesktopFlyouts
{
    internal sealed class DesktopFlyoutResources : ResourceDictionary
    {
        internal Style FlyoutStyle => GetStyle("DefaultWindows11DesktopFlyoutStyle");

        internal Style MenuFlyoutStyle => GetStyle("DefaultWindows11DesktopMenuFlyoutStyle");

        public DesktopFlyoutResources()
        {
            Application.LoadComponent(
                this,
                new Uri("ms-appx:///DesktopFlyouts.Uwp/DesktopFlyout.xaml"),
                ComponentResourceLocation.Nested);
        }

        private Style GetStyle(string key)
        {
            return this[key] as Style
                ?? throw new InvalidOperationException($"Could not find {key}.");
        }
    }
}
