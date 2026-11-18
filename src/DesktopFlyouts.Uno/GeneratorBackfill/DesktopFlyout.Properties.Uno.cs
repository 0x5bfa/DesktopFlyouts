// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Manual dependency property implementations for Uno.
// The GeneratedDependencyProperty source generator does not run under Uno.Sdk.
// Each property below is the implementation part matching the partial declaration in the shared file.

using Microsoft.UI.Xaml;

namespace DesktopFlyouts;

public partial class DesktopFlyout
{
	partial void OnActivationModePropertyChanged(DependencyPropertyChangedEventArgs e);

	public partial bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		private set => SetValue(IsOpenProperty, value);
	}
	public static readonly DependencyProperty IsOpenProperty =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(false));

	public partial bool IsTransitionAnimationEnabled
	{
		get => (bool)GetValue(IsTransitionAnimationEnabledProperty);
		set => SetValue(IsTransitionAnimationEnabledProperty, value);
	}
	public static readonly DependencyProperty IsTransitionAnimationEnabledProperty =
		DependencyProperty.Register(nameof(IsTransitionAnimationEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(true));

	public partial bool IsSwipeToDismissEnabled
	{
		get => (bool)GetValue(IsSwipeToDismissEnabledProperty);
		set => SetValue(IsSwipeToDismissEnabledProperty, value);
	}
	public static readonly DependencyProperty IsSwipeToDismissEnabledProperty =
		DependencyProperty.Register(nameof(IsSwipeToDismissEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(false));

	public partial double SwipeDismissThreshold
	{
		get => (double)GetValue(SwipeDismissThresholdProperty);
		set => SetValue(SwipeDismissThresholdProperty, value);
	}
	public static readonly DependencyProperty SwipeDismissThresholdProperty =
		DependencyProperty.Register(nameof(SwipeDismissThreshold), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(80.0D));

	public partial bool IsLightDismissEnabled
	{
		get => (bool)GetValue(IsLightDismissEnabledProperty);
		set => SetValue(IsLightDismissEnabledProperty, value);
	}
	public static readonly DependencyProperty IsLightDismissEnabledProperty =
		DependencyProperty.Register(nameof(IsLightDismissEnabled), typeof(bool), typeof(DesktopFlyout), new PropertyMetadata(true));

	public partial DesktopFlyoutActivationMode ActivationMode
	{
		get => (DesktopFlyoutActivationMode)GetValue(ActivationModeProperty);
		set => SetValue(ActivationModeProperty, value);
	}
	public static readonly DependencyProperty ActivationModeProperty =
		DependencyProperty.Register(nameof(ActivationMode), typeof(DesktopFlyoutActivationMode), typeof(DesktopFlyout), new PropertyMetadata(DesktopFlyoutActivationMode.Activate, (d, e) => ((DesktopFlyout)d).OnActivationModePropertyChanged(e)));

	partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial object? IslandsSource
	{
		get => (object?)GetValue(IslandsSourceProperty);
		set => SetValue(IslandsSourceProperty, value);
	}
	public static readonly DependencyProperty IslandsSourceProperty =
		DependencyProperty.Register(nameof(IslandsSource), typeof(object), typeof(DesktopFlyout), new PropertyMetadata(null, (d,e) => ((DesktopFlyout)d).OnIslandsSourcePropertyChanged(e)));

	partial void OnIslandTemplatePropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial DataTemplate? IslandTemplate
	{
		get => (DataTemplate?)GetValue(IslandTemplateProperty);
		set => SetValue(IslandTemplateProperty, value);
	}
	public static readonly DependencyProperty IslandTemplateProperty =
		DependencyProperty.Register(nameof(IslandTemplate), typeof(DataTemplate), typeof(DesktopFlyout), new PropertyMetadata(null, (d,e) => ((DesktopFlyout)d).OnIslandTemplatePropertyChanged(e)));

	partial void OnIslandTemplateSelectorPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial DataTemplateSelector? IslandTemplateSelector
	{
		get => (DataTemplateSelector?)GetValue(IslandTemplateSelectorProperty);
		set => SetValue(IslandTemplateSelectorProperty, value);
	}
	public static readonly DependencyProperty IslandTemplateSelectorProperty =
		DependencyProperty.Register(nameof(IslandTemplateSelector), typeof(DataTemplateSelector), typeof(DesktopFlyout), new PropertyMetadata(null, (d,e) => ((DesktopFlyout)d).OnIslandTemplateSelectorPropertyChanged(e)));

	partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial Orientation IslandsOrientation
	{
		get => (Orientation)GetValue(IslandsOrientationProperty);
		set => SetValue(IslandsOrientationProperty, value);
	}
	public static readonly DependencyProperty IslandsOrientationProperty =
		DependencyProperty.Register(nameof(IslandsOrientation), typeof(Orientation), typeof(DesktopFlyout), new PropertyMetadata(Orientation.Vertical, (d,e) => ((DesktopFlyout)d).OnIslandsOrientationPropertyChanged(e)));

	partial void OnWidthPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double Width
	{
		get => (double)GetValue(WidthProperty);
		set => SetValue(WidthProperty, value);
	}
	public static readonly DependencyProperty WidthProperty =
		DependencyProperty.Register(nameof(Width), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(double.NaN, (d,e) => ((DesktopFlyout)d).OnWidthPropertyChanged(e)));

	partial void OnHeightPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double Height
	{
		get => (double)GetValue(HeightProperty);
		set => SetValue(HeightProperty, value);
	}
	public static readonly DependencyProperty HeightProperty =
		DependencyProperty.Register(nameof(Height), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(double.NaN, (d,e) => ((DesktopFlyout)d).OnHeightPropertyChanged(e)));

	partial void OnMinWidthPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double MinWidth
	{
		get => (double)GetValue(MinWidthProperty);
		set => SetValue(MinWidthProperty, value);
	}
	public static readonly DependencyProperty MinWidthProperty =
		DependencyProperty.Register(nameof(MinWidth), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(0D, (d,e) => ((DesktopFlyout)d).OnMinWidthPropertyChanged(e)));

	partial void OnMinHeightPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double MinHeight
	{
		get => (double)GetValue(MinHeightProperty);
		set => SetValue(MinHeightProperty, value);
	}
	public static readonly DependencyProperty MinHeightProperty =
		DependencyProperty.Register(nameof(MinHeight), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(0D, (d,e) => ((DesktopFlyout)d).OnMinHeightPropertyChanged(e)));

	partial void OnMaxWidthPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double MaxWidth
	{
		get => (double)GetValue(MaxWidthProperty);
		set => SetValue(MaxWidthProperty, value);
	}
	public static readonly DependencyProperty MaxWidthProperty =
		DependencyProperty.Register(nameof(MaxWidth), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(double.PositiveInfinity, (d,e) => ((DesktopFlyout)d).OnMaxWidthPropertyChanged(e)));

	partial void OnMaxHeightPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial double MaxHeight
	{
		get => (double)GetValue(MaxHeightProperty);
		set => SetValue(MaxHeightProperty, value);
	}
	public static readonly DependencyProperty MaxHeightProperty =
		DependencyProperty.Register(nameof(MaxHeight), typeof(double), typeof(DesktopFlyout), new PropertyMetadata(double.PositiveInfinity, (d,e) => ((DesktopFlyout)d).OnMaxHeightPropertyChanged(e)));

	partial void OnHorizontalAlignmentPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial HorizontalAlignment HorizontalAlignment
	{
		get => (HorizontalAlignment)GetValue(HorizontalAlignmentProperty);
		set => SetValue(HorizontalAlignmentProperty, value);
	}
	public static readonly DependencyProperty HorizontalAlignmentProperty =
		DependencyProperty.Register(nameof(HorizontalAlignment), typeof(HorizontalAlignment), typeof(DesktopFlyout), new PropertyMetadata(HorizontalAlignment.Left, (d,e) => ((DesktopFlyout)d).OnHorizontalAlignmentPropertyChanged(e)));

	partial void OnVerticalAlignmentPropertyChanged(DependencyPropertyChangedEventArgs e);
	public partial VerticalAlignment VerticalAlignment
	{
		get => (VerticalAlignment)GetValue(VerticalAlignmentProperty);
		set => SetValue(VerticalAlignmentProperty, value);
	}
	public static readonly DependencyProperty VerticalAlignmentProperty =
		DependencyProperty.Register(nameof(VerticalAlignment), typeof(VerticalAlignment), typeof(DesktopFlyout), new PropertyMetadata(VerticalAlignment.Top, (d,e) => ((DesktopFlyout)d).OnVerticalAlignmentPropertyChanged(e)));
}
