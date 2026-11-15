// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Windows.UI.Xaml;

namespace DesktopFlyouts
{
    public sealed partial class MainDesktopMenuFlyout : DesktopMenuFlyout
    {
        public MainDesktopMenuFlyout()
        {
            InitializeComponent();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.ExitApplication();
        }
    }
}
