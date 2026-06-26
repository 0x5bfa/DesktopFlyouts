// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace DesktopFlyouts
{
    public sealed partial class CustomizableFlyout : DesktopFlyout
    {
        public CustomizableFlyout()
        {
            InitializeComponent();
        }

        private void CollapseFirstIslandButton_Click(object sender, RoutedEventArgs e)
        {
            FirstIsland.Visibility = Visibility.Collapsed;
        }

        private void CollapseSecondIslandButton_Click(object sender, RoutedEventArgs e)
        {
            SecondIsland.Visibility = Visibility.Collapsed;
        }

        private void CollapseThirdIslandButton_Click(object sender, RoutedEventArgs e)
        {
            ThirdIsland.Visibility = Visibility.Collapsed;
        }
    }
}
