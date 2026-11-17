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

/// <summary>The private visual and item-generation layer of a desktop flyout.</summary>
internal sealed partial class DesktopFlyoutPresenter : ItemsControl
{
	private readonly HashSet<DesktopFlyoutIslandPresenter> _containers = [];
	private SizeSnapshot _lastSize;
	private Size _viewportLimit = new(double.PositiveInfinity, double.PositiveInfinity);
	internal DesktopFlyout Owner { get; }
	internal Grid? RootGrid { get; private set; }
	internal double PressedScale => 1.0;
	internal IEnumerable<DesktopFlyoutIslandPresenter> Containers => _containers;
	internal event EventHandler? TemplateChanged;
	internal event EventHandler? LayoutInvalidated;

	internal DesktopFlyoutPresenter(DesktopFlyout owner)
	{
		Owner = owner;
		Style = new Style(typeof(ItemsControl));
		Template = DesktopFlyoutChromeResources.Get<ControlTemplate>("DesktopFlyoutPresenterTemplate");
		ItemsPanel = DesktopFlyoutChromeResources.Get<ItemsPanelTemplate>("DesktopFlyoutItemsPanel");
		IsTabStop = false;
		Margin = new Thickness(0);
		Padding = new Thickness(0);
		BorderThickness = new Thickness(0);
		ApplyConfiguration();
		LayoutUpdated += (_, _) =>
		{
			var size = new SizeSnapshot(DesiredSize.Width, DesiredSize.Height, ActualWidth, ActualHeight);
			if (size != _lastSize)
			{
				_lastSize = size;
				LayoutInvalidated?.Invoke(this, EventArgs.Empty);
			}
		};
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		RootGrid = GetTemplateChild("PART_RootGrid") as Grid
			?? throw new InvalidOperationException("The library flyout template has no root grid.");
		RootGrid.Style = new Style(typeof(Grid));
		if (GetTemplateChild("PART_ScrollViewer") is ScrollViewer scroll)
		{
			scroll.Style = new Style(typeof(ScrollViewer));
			// Extend the viewport into the host's shadow allowance so island shadows
			// remain visible beyond the panel's outer inset.
			var shadowInset = DesktopFlyoutChromeMetrics.ShadowMargin;
			scroll.Margin = new Thickness(-shadowInset);
			scroll.Padding = new Thickness(0);
			scroll.Background = null;
			scroll.BorderThickness = new Thickness(0);
			IsolateScrollInfrastructure(scroll);
		}
		if (GetTemplateChild("PART_ItemsPresenter") is ItemsPresenter itemsPresenter)
			itemsPresenter.Style = new Style(typeof(ItemsPresenter));
		TemplateChanged?.Invoke(this, EventArgs.Empty);
	}

	internal void ApplyConfiguration()
	{
		MinWidth = Math.Min(Owner.MinWidth, _viewportLimit.Width);
		MinHeight = Math.Min(Owner.MinHeight, _viewportLimit.Height);
		MaxWidth = Math.Min(Math.Max(Owner.MinWidth, Owner.MaxWidth), _viewportLimit.Width);
		MaxHeight = Math.Min(Math.Max(Owner.MinHeight, Owner.MaxHeight), _viewportLimit.Height);
		Width = double.IsNaN(Owner.Width) ? double.NaN : Math.Clamp(Owner.Width, MinWidth, MaxWidth);
		Height = double.IsNaN(Owner.Height) ? double.NaN : Math.Clamp(Owner.Height, MinHeight, MaxHeight);
		HorizontalAlignment = Owner.HorizontalAlignment;
		VerticalAlignment = Owner.VerticalAlignment;
		ItemTemplate = Owner.IslandTemplate;
		ItemTemplateSelector = Owner.IslandTemplateSelector;
		var source = Owner.IslandsSource ?? Owner.Islands;
		if (!ReferenceEquals(ItemsSource, source)) ItemsSource = source;
		foreach (var container in _containers) container.RefreshContent();
		InvalidateLayout();
	}

	internal void SetViewportLimit(Size size)
	{
		if (_viewportLimit == size) return;
		_viewportLimit = size;
		ApplyConfiguration();
	}

	internal double GetMainAxisBudget(bool isVertical) => isVertical
		? (double.IsNaN(Height) ? MaxHeight : Height)
		: (double.IsNaN(Width) ? MaxWidth : Width);

	private static void IsolateScrollInfrastructure(DependencyObject element)
	{
		// Keep application implicit styles out of the framework's scrolling visuals,
		// while leaving the actual item content free to inherit application resources.
		if (element is ItemsPresenter) return;
		if (element is FrameworkElement frameworkElement && frameworkElement.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue)
			frameworkElement.Style = new Style(frameworkElement.GetType());
		if (element is Control control) control.ApplyTemplate();
		for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
			IsolateScrollInfrastructure(VisualTreeHelper.GetChild(element, index));
	}

	protected override bool IsItemItsOwnContainerOverride(object item) => false;
	protected override DependencyObject GetContainerForItemOverride() => new DesktopFlyoutIslandPresenter(Owner);

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		base.PrepareContainerForItemOverride(element, item);
		var container = (DesktopFlyoutIslandPresenter)element;
		container.SetItem(item);
		_containers.Add(container);
	}

	protected override void ClearContainerForItemOverride(DependencyObject element, object item)
	{
		var container = (DesktopFlyoutIslandPresenter)element;
		container.ClearItem();
		_containers.Remove(container);
		base.ClearContainerForItemOverride(element, item);
	}

	protected override void OnItemsChanged(object e)
	{
		base.OnItemsChanged(e);
		InvalidateLayout();
	}

	internal void InvalidateLayout()
	{
		InvalidateMeasure();
		InvalidatePanels(this);
		LayoutInvalidated?.Invoke(this, EventArgs.Empty);
	}

	private static void InvalidatePanels(DependencyObject element)
	{
		if (element is DesktopFlyoutIslandsPanel panel)
		{
			panel.InvalidateMeasure();
			panel.InvalidateArrange();
			return;
		}
		for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
			InvalidatePanels(VisualTreeHelper.GetChild(element, index));
	}

	internal void UpdateFlyoutTheme(bool forceBackdropUpdate = false)
	{
		RequestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
		foreach (var container in _containers) container.UpdateChrome(forceBackdropUpdate);
	}

	internal void ReleasePresentationResources()
	{
		foreach (var container in _containers) container.ClearOwnerBackdrop();
	}

	internal void ClearItems()
	{
		ItemsSource = null;
		foreach (var container in _containers) container.ClearItem();
		_containers.Clear();
	}

	private readonly record struct SizeSnapshot(double DesiredWidth, double DesiredHeight, double ActualWidth, double ActualHeight);
}
