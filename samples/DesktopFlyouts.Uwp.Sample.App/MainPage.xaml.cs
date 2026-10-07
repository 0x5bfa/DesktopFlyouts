// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;

namespace DesktopFlyouts
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private void CloseApp(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			CoreApplication.Exit();
		}
	}
}
