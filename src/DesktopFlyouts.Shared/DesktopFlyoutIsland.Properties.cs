// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;

#if UWP
using Windows.UI.Xaml;
#elif WASDK
using Microsoft.UI.Xaml;
#endif

namespace DesktopFlyouts;

public partial class DesktopFlyoutIsland
{
	/// <summary>Gets or sets the UI element or data displayed inside the fixed chrome.</summary>
	[GeneratedDependencyProperty]
	public partial object? Content { get; set; }

	/// <summary>Gets or sets a template for this definition's content.</summary>
	[GeneratedDependencyProperty]
	public partial DataTemplate? ContentTemplate { get; set; }

	/// <summary>Gets or sets whether this section occupies layout space.</summary>
	[GeneratedDependencyProperty(DefaultValue = Visibility.Visible)]
	public partial Visibility Visibility { get; set; }

	/// <summary>
	/// Gets or sets the width allocation for this island.
	/// </summary>
	/// <value>The width used when the owner flyout arranges islands horizontally. The default is <see cref="GridLength.Auto"/>.</value>
	[GeneratedDependencyProperty(DefaultValueCallback = nameof(GetDefaultIslandLength))]
	public partial GridLength Width { get; set; }

	/// <summary>
	/// Gets or sets the height allocation for this island.
	/// </summary>
	/// <value>The height used when the owner flyout arranges islands vertically. The default is <see cref="GridLength.Auto"/>.</value>
	[GeneratedDependencyProperty(DefaultValueCallback = nameof(GetDefaultIslandLength))]
	public partial GridLength Height { get; set; }

	partial void OnContentPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnIslandPropertyChanged();
	}

	partial void OnContentTemplatePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnIslandPropertyChanged();
	}

	partial void OnVisibilityPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnIslandPropertyChanged();
	}

	partial void OnWidthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnIslandPropertyChanged();
	}

	partial void OnHeightPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnIslandPropertyChanged();
	}

	private static GridLength GetDefaultIslandLength()
	{
		return GridLength.Auto;
	}

	private void OnIslandPropertyChanged()
	{
		Changed?.Invoke(this, System.EventArgs.Empty);
	}
}
