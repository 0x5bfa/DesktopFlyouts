// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.Foundation;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif

namespace DesktopFlyouts;

/// <summary>Keeps measurement independent of the last native viewport and applies placement space once.</summary>
internal sealed partial class DesktopFlyoutLayoutPanel(DesktopFlyoutPresenter flyout) : Panel
{
	private Size _availableSize;
	private Thickness _placementMargin;

	internal Rect ContentBounds { get; private set; }

	internal void AttachContent()
	{
		if (VisualTreeHelper.GetParent(flyout) is not null)
		{
			throw new InvalidOperationException("The flyout already has a visual parent.");
		}
		Children.Add(flyout);
	}

	internal void DetachContent() => Children.Clear();

	internal void SetShadowInsets(Thickness insets)
	{
		_placementMargin = insets;
		InvalidateMeasure();
	}

	internal void SetAvailableSize(Size size, Thickness placementMargin)
	{
		_availableSize = size;
		_placementMargin = new(
			DesktopFlyoutSizeResolver.NormalizeMargin(placementMargin.Left),
			DesktopFlyoutSizeResolver.NormalizeMargin(placementMargin.Top),
			DesktopFlyoutSizeResolver.NormalizeMargin(placementMargin.Right),
			DesktopFlyoutSizeResolver.NormalizeMargin(placementMargin.Bottom));
		InvalidateMeasure();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var horizontalMargin = _placementMargin.Left + _placementMargin.Right;
		var verticalMargin = _placementMargin.Top + _placementMargin.Bottom;
		var contentSize = new Size(Math.Max(0, _availableSize.Width - horizontalMargin), Math.Max(0, _availableSize.Height - verticalMargin));
		flyout.SetViewportLimit(contentSize);
		flyout.Measure(contentSize);
		var width = DesktopFlyoutSizeResolver.ResolveLength(contentSize.Width, flyout.DesiredSize.Width,
			double.IsNaN(flyout.Width) && flyout.HorizontalAlignment is HorizontalAlignment.Stretch,
			flyout.MinWidth, flyout.MaxWidth, flyout.Margin.Left + flyout.Margin.Right);
		var height = DesktopFlyoutSizeResolver.ResolveLength(contentSize.Height, flyout.DesiredSize.Height,
			double.IsNaN(flyout.Height) && flyout.VerticalAlignment is VerticalAlignment.Stretch,
			flyout.MinHeight, flyout.MaxHeight, flyout.Margin.Top + flyout.Margin.Bottom);
		return new(Math.Min(_availableSize.Width, width + horizontalMargin), Math.Min(_availableSize.Height, height + verticalMargin));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var left = Math.Min(_placementMargin.Left, finalSize.Width);
		var top = Math.Min(_placementMargin.Top, finalSize.Height);
		ContentBounds = new(left, top,
			Math.Max(0, finalSize.Width - left - _placementMargin.Right),
			Math.Max(0, finalSize.Height - top - _placementMargin.Bottom));
		flyout.Arrange(ContentBounds);
		Clip = new RectangleGeometry { Rect = new(0, 0, finalSize.Width, finalSize.Height) };
		return finalSize;
	}
}
