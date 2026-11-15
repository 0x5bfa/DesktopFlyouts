// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace DesktopFlyouts
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AppWindow.Resize(new(600, 900));
            ExtendsContentIntoTitleBar = true;
            Content = new RootView();
        }
    }
}
