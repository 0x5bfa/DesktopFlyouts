// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using System;
using System.Collections;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace DesktopFlyouts;

public partial class DesktopFlyout
{
	/// <summary>
	/// Gets whether the flyout is currently open.
	/// </summary>
	/// <value><see langword="true"/> after the open transition completes; otherwise, <see langword="false"/>.</value>
	[GeneratedDependencyProperty]
	public partial bool IsOpen { get; private set; }

	/// <summary>
	/// Gets or sets whether open and close transitions are enabled.
	/// </summary>
	/// <value><see langword="true"/> to animate open and close transitions; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
	/// <remarks>Windows hosts also respect the user's system animation setting, even when this property is <see langword="true"/>.</remarks>
	[GeneratedDependencyProperty(DefaultValue = true)]
	public partial bool IsTransitionAnimationEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether the flyout can be dismissed by swiping in the opposite direction of the active popup direction.
	/// </summary>
	/// <value><see langword="true"/> to allow swipe-to-dismiss; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
	[GeneratedDependencyProperty(DefaultValue = false)]
	public partial bool IsSwipeToDismissEnabled { get; set; }

	/// <summary>
	/// Gets or sets the swipe distance in DIPs required to dismiss the flyout.
	/// </summary>
	/// <value>The swipe threshold in device-independent pixels. The default is <c>80</c>.</value>
	/// <remarks>
	/// The effective threshold is clamped to the available dismiss distance. <see cref="double.NaN"/>
	/// and infinity use a default threshold based on that available distance.
	/// </remarks>
	[GeneratedDependencyProperty(DefaultValue = 80.0D)]
	public partial double SwipeDismissThreshold { get; set; }

	/// <summary>
	/// Gets or sets whether the flyout closes when its host window is deactivated (light dismissal).
	/// </summary>
	/// <value><see langword="true"/> to close when the host window is deactivated; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
	[GeneratedDependencyProperty(DefaultValue = true)]
	public partial bool IsLightDismissEnabled { get; set; }

	/// <summary>
	/// Gets or sets how the flyout participates in activation and focus.
	/// </summary>
	/// <value>The activation behavior used when opening the flyout. The default is <see cref="DesktopFlyoutActivationMode.Activate"/>.</value>
	[GeneratedDependencyProperty(DefaultValue = DesktopFlyoutActivationMode.Activate)]
	public partial DesktopFlyoutActivationMode ActivationMode { get; set; }

	/// <summary>Gets or sets the delay after opening before automatic close; zero disables it.</summary>
	[GeneratedDependencyProperty(DefaultValueCallback = nameof(GetDefaultAutoCloseDelay))]
	public partial TimeSpan AutoCloseDelay { get; set; }

	/// <summary>Gets or sets the external item source; null restores the direct Islands collection.</summary>
	[GeneratedDependencyProperty]
	public partial object? IslandsSource { get; set; }

	/// <summary>Gets or sets the template for item content inside the fixed island chrome.</summary>
	[GeneratedDependencyProperty]
	public partial DataTemplate? IslandTemplate { get; set; }

	/// <summary>Gets or sets the selector for item content templates.</summary>
	[GeneratedDependencyProperty]
	public partial DataTemplateSelector? IslandTemplateSelector { get; set; }

	/// <summary>Gets or sets the direction of island layout.</summary>
	[GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
	public partial Orientation IslandsOrientation { get; set; }

	/// <summary>Gets or sets the requested width in DIPs; NaN means Auto.</summary>
	[GeneratedDependencyProperty(DefaultValue = double.NaN)]
	public partial double Width { get; set; }

	/// <summary>Gets or sets the requested height in DIPs; NaN means Auto.</summary>
	[GeneratedDependencyProperty(DefaultValue = double.NaN)]
	public partial double Height { get; set; }

	/// <summary>Gets or sets the minimum requested width in DIPs.</summary>
	[GeneratedDependencyProperty(DefaultValue = 0D)]
	public partial double MinWidth { get; set; }

	/// <summary>Gets or sets the minimum requested height in DIPs.</summary>
	[GeneratedDependencyProperty(DefaultValue = 0D)]
	public partial double MinHeight { get; set; }

	/// <summary>Gets or sets the maximum requested width in DIPs.</summary>
	[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity)]
	public partial double MaxWidth { get; set; }

	/// <summary>Gets or sets the maximum requested height in DIPs.</summary>
	[GeneratedDependencyProperty(DefaultValue = double.PositiveInfinity)]
	public partial double MaxHeight { get; set; }

	/// <summary>Gets or sets sizing alignment; Stretch fills an automatic width.</summary>
	[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Left)]
	public partial HorizontalAlignment HorizontalAlignment { get; set; }

	/// <summary>Gets or sets sizing alignment; Stretch fills an automatic height.</summary>
	[GeneratedDependencyProperty(DefaultValue = VerticalAlignment.Top)]
	public partial VerticalAlignment VerticalAlignment { get; set; }

	partial void OnActivationModePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		_host?.SetActivationMode((DesktopFlyoutActivationMode)e.NewValue);
		UpdateFocusSuppression();
	}

	partial void OnAutoCloseDelayPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		RestartAutoCloseTimer();
	}

	partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is not null && e.NewValue is not IEnumerable)
		{
			SetValue(IslandsSourceProperty, e.OldValue);
			throw new ArgumentException("IslandsSource must be an enumerable collection.");
		}
		OnConfigurationChanged();
	}

	partial void OnIslandTemplatePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnConfigurationChanged();
	}

	partial void OnIslandTemplateSelectorPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnConfigurationChanged();
	}

	partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnConfigurationChanged();
	}

	partial void OnWidthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(WidthProperty, e, true, false);
	}

	partial void OnHeightPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(HeightProperty, e, true, false);
	}

	partial void OnMinWidthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(MinWidthProperty, e, false, false);
	}

	partial void OnMinHeightPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(MinHeightProperty, e, false, false);
	}

	partial void OnMaxWidthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(MaxWidthProperty, e, false, true);
	}

	partial void OnMaxHeightPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnDimensionChanged(MaxHeightProperty, e, false, true);
	}

	partial void OnHorizontalAlignmentPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnConfigurationChanged();
	}

	partial void OnVerticalAlignmentPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		OnConfigurationChanged();
	}

	private static TimeSpan GetDefaultAutoCloseDelay()
	{
		return TimeSpan.Zero;
	}

	private void OnDimensionChanged(DependencyProperty property, DependencyPropertyChangedEventArgs e, bool allowAuto, bool allowInfinity)
	{
		var value = (double)e.NewValue;
		if (value < 0 || (double.IsNaN(value) && !allowAuto) || (double.IsInfinity(value) && !allowInfinity))
		{
			SetValue(property, e.OldValue);
			throw new ArgumentOutOfRangeException(nameof(value), "Invalid flyout dimension.");
		}
		OnConfigurationChanged();
	}

	private void OnConfigurationChanged()
	{
		if (_disposed) return;
		Presenter.ApplyConfiguration();
		Presenter_LayoutInvalidated(this, EventArgs.Empty);
	}
}
