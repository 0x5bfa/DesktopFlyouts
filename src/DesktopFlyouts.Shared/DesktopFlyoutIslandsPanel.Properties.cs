// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace DesktopFlyouts;

public partial class DesktopFlyoutIslandsPanel
{
	/// <summary>
	/// Gets or sets the fallback orientation used when the panel is not hosted by a <see cref="DesktopFlyout"/>.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
	public partial Orientation Orientation { get; set; }

	/// <summary>
	/// Gets or sets the spacing between visible islands.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = 0D)]
	public partial double Spacing { get; set; }

	partial void OnOrientationPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		InvalidateMeasure();
	}

	partial void OnSpacingPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		InvalidateMeasure();
	}
}
