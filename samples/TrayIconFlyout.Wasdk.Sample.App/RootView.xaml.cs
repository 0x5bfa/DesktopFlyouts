// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;

namespace U5BFA.Libraries
{
	internal sealed partial class RootView : UserControl
	{
		public RootView()
		{
			InitializeComponent();
			Loaded += RootView_Loaded;
		}

		private void RootView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var flyout = TrayIconManager.Default.TrayIconFlyout;
			if (flyout is null)
				return;

			FlyoutWidthNumberBox.Value = flyout.FlyoutWidth.IsAbsolute ? flyout.FlyoutWidth.Value : double.NaN;
			FlyoutHeightNumberBox.Value = flyout.FlyoutHeight.IsAbsolute ? flyout.FlyoutHeight.Value : double.NaN;
		}

		private void FlyoutWidthNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (TrayIconManager.Default.TrayIconFlyout is not { } flyout)
				return;

			flyout.FlyoutWidth = ToGridLength(args.NewValue);
		}

		private void FlyoutHeightNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (TrayIconManager.Default.TrayIconFlyout is not { } flyout)
				return;

			flyout.FlyoutHeight = ToGridLength(args.NewValue);
		}

		private static Microsoft.UI.Xaml.GridLength ToGridLength(double value)
		{
			return double.IsNaN(value) || value <= 0
				? Microsoft.UI.Xaml.GridLength.Auto
				: new(value);
		}
	}
}
