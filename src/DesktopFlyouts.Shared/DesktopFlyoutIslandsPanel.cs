// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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

/// <summary>
/// Arranges <see cref="DesktopFlyoutIsland"/> items for a <see cref="DesktopFlyout"/>.
/// </summary>
public partial class DesktopFlyoutIslandsPanel : Panel
{
	/// <summary>Initializes the layout infrastructure without application implicit styles.</summary>
	public DesktopFlyoutIslandsPanel()
	{
		Style = new Style(typeof(DesktopFlyoutIslandsPanel));
	}

	/// <summary>
	/// Identifies the <see cref="Orientation"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(DesktopFlyoutIslandsPanel), new PropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the fallback orientation used when the panel is not hosted by a <see cref="DesktopFlyout"/>.
	/// </summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Spacing"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty SpacingProperty =
		DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(DesktopFlyoutIslandsPanel), new PropertyMetadata(0D, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the spacing between visible islands.
	/// </summary>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
	{
		return GetResolvedOrientation() is Orientation.Vertical
			? MeasureStack(availableSize, isVertical: true)
			: MeasureStack(availableSize, isVertical: false);
	}

	/// <inheritdoc/>
	protected override Size ArrangeOverride(Size finalSize)
	{
		return GetResolvedOrientation() is Orientation.Vertical
			? ArrangeStack(finalSize, isVertical: true)
			: ArrangeStack(finalSize, isVertical: false);
	}

	private Size MeasureStack(Size availableSize, bool isVertical)
	{
		var items = GetVisibleItems(isVertical, shouldMeasureCollapsedItems: true);
		var spacing = GetResolvedSpacing();
		var spacingTotal = GetSpacingTotal(items.Count, spacing);
		var availableLength = GetLength(availableSize, isVertical);
		if (GetOwnerFlyout() is DesktopFlyout owner)
			availableLength = Math.Min(availableLength, owner.Presenter.GetMainAxisBudget(isVertical));
		// The host layout reserves shadow space outside the presenter.
		var availableCross = Math.Max(0D, GetCrossLength(availableSize, isVertical));
		var hasFiniteLength = !double.IsInfinity(availableLength);
		var allocateStars = hasFiniteLength && GetOwnerFlyout()?.IsContentSized(isVertical) != true;
		var itemAvailableLength = hasFiniteLength
			? Math.Max(0D, availableLength - spacingTotal)
			: double.PositiveInfinity;
		var fixedLength = 0D;
		var autoLength = 0D;
		var starLength = 0D;
		var starWeight = 0D;
		var desiredCross = 0D;

		foreach (var item in items)
		{
			if (item.Length.IsStar)
			{
				starWeight += Math.Max(0D, item.Length.Value);
				continue;
			}

			if (item.Length.IsAuto)
			{
				item.Element.Measure(CreateSize(availableCross, itemAvailableLength, isVertical));
				autoLength += GetLength(item.Element.DesiredSize, isVertical);
			}
			else
			{
				var length = Math.Max(0D, item.Length.Value);
				item.Element.Measure(CreateSize(availableCross, length, isVertical));
				fixedLength += length;
			}

			desiredCross = Math.Max(desiredCross, GetCrossLength(item.Element.DesiredSize, isVertical));
		}

		var remainingLength = hasFiniteLength
			? Math.Max(0D, itemAvailableLength - fixedLength - autoLength)
			: double.PositiveInfinity;

		foreach (var item in items)
		{
			if (!item.Length.IsStar)
			{
				continue;
			}

			var allocatedLength = allocateStars
				? GetWeightedLength(remainingLength, item.Length, starWeight)
				: remainingLength;

			item.Element.Measure(CreateSize(availableCross, allocatedLength, isVertical));
			var measuredLength = allocateStars
				? allocatedLength
				: GetLength(item.Element.DesiredSize, isVertical);

			starLength += measuredLength;
			desiredCross = Math.Max(desiredCross, GetCrossLength(item.Element.DesiredSize, isVertical));
		}

		var desiredLength = fixedLength + autoLength + starLength + spacingTotal;
		return CreateSize(desiredCross, desiredLength, isVertical);
	}

	private Size ArrangeStack(Size finalSize, bool isVertical)
	{
		var items = GetVisibleItems(isVertical, shouldMeasureCollapsedItems: false);
		var spacing = GetResolvedSpacing();
		var spacingTotal = GetSpacingTotal(items.Count, spacing);
		var finalLength = GetLength(finalSize, isVertical);
		var finalCross = Math.Max(0D, GetCrossLength(finalSize, isVertical));
		var fixedLength = 0D;
		var autoLength = 0D;
		var starWeight = 0D;

		foreach (var item in items)
		{
			if (item.Length.IsStar)
			{
				starWeight += Math.Max(0D, item.Length.Value);
			}
			else if (item.Length.IsAuto)
			{
				autoLength += GetLength(item.Element.DesiredSize, isVertical);
			}
			else
			{
				fixedLength += Math.Max(0D, item.Length.Value);
			}
		}

		var remainingLength = Math.Max(0D, finalLength - fixedLength - autoLength - spacingTotal);
		var offset = 0D;

		foreach (var item in items)
		{
			var length = item.Length.IsStar
				? GetWeightedLength(remainingLength, item.Length, starWeight)
				: item.Length.IsAuto
					? GetLength(item.Element.DesiredSize, isVertical)
					: Math.Max(0D, item.Length.Value);

			item.Element.Arrange(CreateRect(offset, 0D, finalCross, length, isVertical));
			offset += length + spacing;
		}

		ArrangeCollapsedItems();

		return finalSize;
	}

	private List<LayoutItem> GetVisibleItems(bool isVertical, bool shouldMeasureCollapsedItems)
	{
		var items = new List<LayoutItem>();

		foreach (var child in Children)
		{
			if (child is not UIElement element)
			{
				continue;
			}

			var island = GetIsland(element);
			if (island is null || island.Visibility is Visibility.Visible)
			{
				var length = isVertical
					? island?.IslandHeight ?? GridLength.Auto
					: island?.IslandWidth ?? GridLength.Auto;

				items.Add(new(element, length));
			}
			else if (shouldMeasureCollapsedItems)
			{
				element.Measure(new(0, 0));
			}
		}

		return items;
	}

	private void ArrangeCollapsedItems()
	{
		foreach (var child in Children)
		{
			if (child is not UIElement element)
			{
				continue;
			}

			var island = GetIsland(element);
			if (island is not null && island.Visibility is not Visibility.Visible)
			{
				element.Arrange(new(0, 0, 0, 0));
			}
		}
	}

	private Orientation GetResolvedOrientation()
	{
		return GetOwnerFlyout()?.IslandsOrientation ?? Orientation;
	}

	private DesktopFlyout? GetOwnerFlyout()
	{
		DependencyObject? current = this;

		while (current is not null)
		{
			if (current is DesktopFlyoutPresenter presenter)
			{
				return presenter.Owner;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return null;
	}

	private static DesktopFlyoutIslandPresenter? GetIsland(UIElement element)
	{
		return element switch
		{
			DesktopFlyoutIslandPresenter island => island,
			ContentPresenter { Content: DesktopFlyoutIslandPresenter island } => island,
			_ => null,
		};
	}

	private static double GetWeightedLength(double availableLength, GridLength length, double totalWeight)
	{
		return totalWeight > 0D
			? availableLength * (Math.Max(0D, length.Value) / totalWeight)
			: 0D;
	}

	private double GetResolvedSpacing()
	{
		if (GetOwnerFlyout() is not null) return DesktopFlyoutChromeMetrics.IslandSpacing;
		return double.IsNaN(Spacing) || double.IsInfinity(Spacing)
			? 0D
			: Math.Max(0D, Spacing);
	}

	private static double GetSpacingTotal(int count, double spacing)
	{
		return Math.Max(0, count - 1) * spacing;
	}

	private static double GetLength(Size size, bool isVertical)
	{
		return isVertical ? size.Height : size.Width;
	}

	private static double GetCrossLength(Size size, bool isVertical)
	{
		return isVertical ? size.Width : size.Height;
	}

	private static Size CreateSize(double cross, double length, bool isVertical)
	{
		return isVertical
			? new(cross, length)
			: new(length, cross);
	}

	private static Rect CreateRect(double offset, double crossOffset, double cross, double length, bool isVertical)
	{
		return isVertical
			? new(crossOffset, offset, cross, length)
			: new(offset, crossOffset, length, cross);
	}

	private static void OnLayoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
	{
		if (dependencyObject is not DesktopFlyoutIslandsPanel panel)
		{
			return;
		}

		panel.InvalidateMeasure();
	}

	private readonly struct LayoutItem
	{
		public LayoutItem(UIElement element, GridLength length)
		{
			Element = element;
			Length = length;
		}

		public UIElement Element { get; }
		public GridLength Length { get; }
	}
}
