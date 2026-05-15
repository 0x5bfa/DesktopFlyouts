// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

using Windows.System;
using Windows.UI.Xaml;

namespace U5BFA.Libraries
{
    public sealed partial class MainTrayIconMeunFlyout : TrayIconMenuFlyout
    {
        public MainTrayIconMeunFlyout()
        {
            InitializeComponent();
        }

        private async void OpenSecondaryApp(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("tif-secondaryapp:"));
        }
    }
}
