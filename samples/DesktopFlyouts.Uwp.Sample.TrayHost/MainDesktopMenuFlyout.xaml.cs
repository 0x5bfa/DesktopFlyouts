// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

using Windows.System;

namespace DesktopFlyouts
{
    public sealed partial class MainDesktopMenuFlyout : DesktopMenuFlyout
    {
        public MainDesktopMenuFlyout()
        {
            InitializeComponent();
        }

        private async void OpenSecondaryApp(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("tif-secondaryapp:"));
        }
    }
}
