// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace DesktopFlyouts.UnitTests.WinUI;

[TestClass]
[DoNotParallelize]
public sealed partial class DesktopFlyoutSizingTests
{
	[UITestMethod]
	public void Public_definitions_are_nonvisual_and_creation_is_host_free()
	{
		using var flyout = CreateFlyout();
		Assert.IsFalse(typeof(UIElement).IsAssignableFrom(typeof(DesktopFlyout)));
		Assert.IsFalse(typeof(UIElement).IsAssignableFrom(typeof(DesktopFlyoutIsland)));
		Assert.IsNull(typeof(DesktopFlyout).GetField("_host", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flyout));
		foreach (var type in new[] { typeof(DesktopFlyout), typeof(DesktopFlyoutIsland) })
			foreach (var name in new[] { "Style", "Template", "Background", "BorderBrush", "BorderThickness", "CornerRadius", "Margin", "Shadow", "FlyoutPresenterStyle", "IslandContainerStyle", "PlacementMargin" })
				Assert.IsNull(type.GetProperty(name), $"Unexpected public chrome property: {type.Name}.{name}");
	}

	[UITestMethod]
	public void Invalid_dimensions_and_sources_are_rejected_without_losing_previous_values()
	{
		using var flyout = CreateFlyout();
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => flyout.Width = -1);
		Assert.IsTrue(double.IsNaN(flyout.Width));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => flyout.Height = double.PositiveInfinity);
		Assert.IsTrue(double.IsNaN(flyout.Height));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => flyout.MinWidth = double.NaN);
		Assert.AreEqual(0, flyout.MinWidth);
		Assert.ThrowsExactly<ArgumentException>(() => flyout.IslandsSource = 123);
		Assert.IsNull(flyout.IslandsSource);
	}

	[UITestMethod]
	public async Task Dimensions_preserve_bindings_and_fixed_shadow_padding()
	{
		using var flyout = CreateFlyout();
		var source = new Border { Width = 240 };
		BindingOperations.SetBinding(flyout, DesktopFlyout.WidthProperty, new Binding
		{
			Source = source, Path = new PropertyPath(nameof(FrameworkElement.Width)), Mode = BindingMode.OneWay,
		});
		var binding = flyout.ReadLocalValue(DesktopFlyout.WidthProperty);
		flyout.Height = 120;
		await Open(flyout);
		var presenter = Presenter(flyout);
		var frame = (FrameworkElement)VisualTreeHelper.GetParent(presenter);
		Assert.AreEqual(240, presenter.ActualWidth, 1);
		Assert.AreEqual(120, presenter.ActualHeight, 1);
		Assert.AreEqual(264, frame.ActualWidth, 2);
		Assert.AreEqual(144, frame.ActualHeight, 2);
		var island = (FrameworkElement)presenter.ContainerFromIndex(0);
		var islandOrigin = island.TransformToVisual(presenter).TransformPoint(new(0, 0));
		Assert.AreEqual(0, islandOrigin.X, 1);
		Assert.AreEqual(0, islandOrigin.Y, 1);
		Assert.AreEqual(presenter.ActualWidth, island.ActualWidth, 1);
		var presenterOrigin = presenter.TransformToVisual(frame).TransformPoint(new(0, 0));
		Assert.AreEqual(12, presenterOrigin.X, 1);
		Assert.AreEqual(12, presenterOrigin.Y, 1);
		Assert.AreSame(binding, flyout.ReadLocalValue(DesktopFlyout.WidthProperty));
		source.Width = 360;
		await WaitUntil(() => Math.Abs(frame.ActualWidth - 384) <= 2);
		Assert.AreEqual(360, presenter.ActualWidth, 1);
		Assert.AreEqual(360, island.ActualWidth, 1);
		Assert.AreSame(binding, flyout.ReadLocalValue(DesktopFlyout.WidthProperty));
		flyout.ClearValue(DesktopFlyout.WidthProperty);
		await WaitUntil(() => Math.Abs(presenter.ActualWidth - 82) <= 1);
	}

	[UITestMethod]
	public async Task Minimum_maximum_and_stretch_constraints_apply_without_rewriting_requests()
	{
		using var flyout = CreateFlyout();
		flyout.MinWidth = 200;
		flyout.MaxWidth = 300;
		flyout.Width = 800;
		await Open(flyout);
		Assert.AreEqual(300, Presenter(flyout).ActualWidth, 1);
		Assert.AreEqual(800, flyout.Width);
		flyout.Width = 100;
		await WaitUntil(() => Math.Abs(Presenter(flyout).ActualWidth - 200) <= 1);
		flyout.MaxHeight = 300;
		flyout.VerticalAlignment = VerticalAlignment.Stretch;
		await WaitUntil(() => Math.Abs(Presenter(flyout).ActualHeight - 300) <= 1);
		flyout.VerticalAlignment = VerticalAlignment.Top;
		await WaitUntil(() => Math.Abs(Presenter(flyout).ActualHeight - 62) <= 1);
	}

	[UITestMethod]
	public async Task Auto_parent_is_not_stretched_by_island_stars()
	{
		using var flyout = CreateFlyout();
		var definition = (DesktopFlyoutIsland)flyout.Islands[0];
		definition.IslandHeight = new GridLength(1, GridUnitType.Star);
		definition.IslandWidth = new GridLength(1, GridUnitType.Star);
		await Open(flyout);
		Assert.AreEqual(82, Presenter(flyout).ActualWidth, 1);
		Assert.AreEqual(62, Presenter(flyout).ActualHeight, 1);
	}

	[UITestMethod]
	public async Task Source_changes_templates_and_return_to_direct_items_work_while_open()
	{
		using var flyout = CreateFlyout();
		var source = new StringCollection { "First", "Second" };
		flyout.IslandTemplate = Template("normal");
		flyout.IslandsSource = source;
		Assert.ThrowsExactly<InvalidOperationException>(() => flyout.Islands.Add("blocked"));
		Assert.ThrowsExactly<InvalidOperationException>(() => flyout.Islands.Clear());
		await Open(flyout);
		await WaitUntil(() => Find<TextBlock>(Presenter(flyout).ContainerFromIndex(0))?.Text == "First");
		source.Add("Third");
		await WaitUntil(() => Find<TextBlock>(Presenter(flyout).ContainerFromIndex(2))?.Text == "Third");
		Assert.AreEqual("Third", Find<TextBlock>(Presenter(flyout).ContainerFromIndex(2))?.Text);
		source.Move(2, 0);
		await WaitUntil(() => Find<TextBlock>(Presenter(flyout).ContainerFromIndex(0))?.Text == "Third");
		source.RemoveAt(1);
		await WaitUntil(() => Presenter(flyout).Items.Count == 2);
		flyout.IslandTemplate = null;
		flyout.IslandTemplateSelector = new LengthSelector { Short = Template("short"), Long = Template("long") };
		await WaitUntil(() => Equals(Find<TextBlock>(Presenter(flyout).ContainerFromIndex(1))?.Tag, "long"));
		flyout.IslandsSource = null;
		flyout.IslandTemplateSelector = null;
		await WaitUntil(() => Presenter(flyout).Items.Count == 1 && Find<Border>(Presenter(flyout).ContainerFromIndex(0)) is not null);
		flyout.Islands.Add(new DesktopFlyoutIsland { Content = new TextBlock { Text = "Direct" } });
		await WaitUntil(() => Presenter(flyout).Items.Count == 2);
	}

	[UITestMethod]
	public async Task Application_resources_and_content_cannot_replace_the_outer_chrome()
	{
		const string key = "DesktopFlyoutIslandTemplate";
		var resources = Application.Current.Resources;
		var hadValue = resources.ContainsKey(key);
		var previous = hadValue ? resources[key] : null;
		var hadGridStyle = resources.ContainsKey(typeof(Grid));
		var previousGridStyle = hadGridStyle ? resources[typeof(Grid)] : null;
		resources[typeof(Grid)] = new Style(typeof(Grid))
		{
			Setters = { new Setter(FrameworkElement.MarginProperty, new Thickness(99)), new Setter(Grid.BorderThicknessProperty, new Thickness(99)) },
		};
		resources[key] = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><Grid /></ControlTemplate>");
		try
		{
			using var flyout = CreateFlyout();
			flyout.Width = 240;
			((Border)((DesktopFlyoutIsland)flyout.Islands[0]).Content!).Background = new SolidColorBrush(Microsoft.UI.Colors.Red);
			await Open(flyout);
			var container = (Control)Presenter(flyout).ContainerFromIndex(0);
			var chrome = Find<Grid>(container)!;
			Assert.AreEqual(new Thickness(1), chrome.BorderThickness);
			Assert.AreEqual(new CornerRadius(8), chrome.CornerRadius);
			Assert.IsNotNull(chrome.BorderBrush);
			if (!new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast) Assert.IsNotNull(chrome.Shadow);
			Assert.AreEqual(new Thickness(0), container.Margin);
			Assert.AreEqual(240, container.ActualWidth, 1);
			Assert.AreEqual(240, Presenter(flyout).ActualWidth, 1);
		}
		finally
		{
			if (hadValue) resources[key] = previous!; else resources.Remove(key);
			if (hadGridStyle) resources[typeof(Grid)] = previousGridStyle!; else resources.Remove(typeof(Grid));
		}
	}

	[UITestMethod]
	public async Task Island_weights_visibility_and_orientation_change_without_reopening()
	{
		using var flyout = CreateFlyout();
		flyout.Width = 240;
		flyout.Height = 360;
		var first = (DesktopFlyoutIsland)flyout.Islands[0];
		first.IslandHeight = new GridLength(1, GridUnitType.Star);
		first.IslandWidth = new GridLength(1, GridUnitType.Star);
		var second = new DesktopFlyoutIsland { IslandHeight = new GridLength(2, GridUnitType.Star), IslandWidth = new GridLength(2, GridUnitType.Star) };
		flyout.Islands.Add(second);
		await Open(flyout);
		var one = (FrameworkElement)Presenter(flyout).ContainerFromIndex(0);
		var two = (FrameworkElement)Presenter(flyout).ContainerFromIndex(1);
		Assert.AreEqual(one.ActualHeight * 2, two.ActualHeight, 1);
		Assert.AreEqual(240, one.ActualWidth, 1);
		Assert.AreEqual(240, two.ActualWidth, 1);
		Assert.AreEqual(360, one.ActualHeight + 12 + two.ActualHeight, 1);
		var verticalOrigin = two.TransformToVisual(one).TransformPoint(new(0, 0));
		Assert.AreEqual(0, verticalOrigin.X, 1);
		Assert.AreEqual(one.ActualHeight + 12, verticalOrigin.Y, 1);
		second.Visibility = Visibility.Collapsed;
		await WaitUntil(() => Math.Abs(one.ActualHeight - 360) <= 1);
		second.Visibility = Visibility.Visible;
		flyout.IslandsOrientation = Orientation.Horizontal;
		await WaitUntil(() => one.ActualWidth > 0 && two.ActualWidth > 0
			&& Math.Abs(one.ActualWidth * 2 - two.ActualWidth) <= 1
			&& Math.Abs(one.ActualWidth + 12 + two.ActualWidth - 240) <= 1);
		Assert.AreEqual(240, one.ActualWidth + 12 + two.ActualWidth, 1);
		Assert.AreEqual(360, one.ActualHeight, 1);
		Assert.AreEqual(360, two.ActualHeight, 1);
		var horizontalOrigin = two.TransformToVisual(one).TransformPoint(new(0, 0));
		Assert.AreEqual(one.ActualWidth + 12, horizontalOrigin.X, 1);
		Assert.AreEqual(0, horizontalOrigin.Y, 1);
		Assert.AreEqual(DesktopFlyoutTransitionState.Open, flyout.TransitionState);
	}

	[UITestMethod]
	public async Task Bounded_scroll_content_gets_a_finite_viewport()
	{
		using var flyout = CreateFlyout();
		flyout.MaxHeight = 180;
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new Border { Height = 1000 } };
		((DesktopFlyoutIsland)flyout.Islands[0]).Content = scroll;
		await Open(flyout);
		Assert.IsTrue(Presenter(flyout).ActualHeight <= 181);
		Assert.IsTrue(scroll.ViewportHeight > 0 && scroll.ViewportHeight <= 181);
		Assert.IsTrue(scroll.ScrollableHeight > 0);
	}

	[UITestMethod]
	public async Task Large_requested_dimensions_keep_the_frame_bounded_and_clip_overflowing_items()
	{
		using var flyout = CreateFlyout();
		flyout.Width = 1_000_000;
		flyout.MaxHeight = 180;
		flyout.IslandTemplate = Template("overflow");
		var source = new StringCollection();
		for (var index = 0; index < 50; index++) source.Add($"Item {index}");
		flyout.IslandsSource = source;
		await Open(flyout);
		var presenter = Presenter(flyout);
		Assert.IsTrue(presenter.ActualWidth < 1_000_000);
		Assert.AreEqual(1_000_000, flyout.Width);
		Assert.IsTrue(presenter.ActualHeight <= 181);
		Assert.IsNull(Find<ScrollViewer>(presenter));
		var last = (FrameworkElement)presenter.ContainerFromIndex(source.Count - 1);
		var lastOrigin = last.TransformToVisual(presenter).TransformPoint(new(0, 0));
		Assert.IsTrue(lastOrigin.Y + last.ActualHeight > presenter.ActualHeight);
		var frame = (FrameworkElement)VisualTreeHelper.GetParent(presenter);
		Assert.IsInstanceOfType<RectangleGeometry>(frame.Clip);
		var clip = ((RectangleGeometry)frame.Clip).Rect;
		Assert.AreEqual(frame.ActualWidth, clip.Width, 1);
		Assert.AreEqual(frame.ActualHeight, clip.Height, 1);
	}

	[UITestMethod]
	public async Task Opening_changes_and_reusing_detached_content_preserve_layout()
	{
		var definition = new DesktopFlyoutIsland { Content = new Border { Width = 80, Height = 60 } };
		using (var first = CreateFlyout())
		{
			first.Islands.Clear(); first.Islands.Add(definition);
			first.IsTransitionAnimationEnabled = true;
			first.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
			await Task.Delay(30);
			first.Width = 280;
			await WaitUntil(() => first.IsOpen && Math.Abs(Presenter(first).ActualWidth - 280) <= 1);
		}
		using var second = CreateFlyout();
		second.Islands.Clear(); second.Islands.Add(definition);
		await Open(second);
		Assert.AreEqual(82, Presenter(second).ActualWidth, 1);
	}

	private static DesktopFlyout CreateFlyout()
	{
		var flyout = new DesktopFlyout { IsTransitionAnimationEnabled = false, IsLightDismissEnabled = false, ActivationMode = DesktopFlyoutActivationMode.NeverActivate };
		flyout.Islands.Add(new DesktopFlyoutIsland { Content = new Border { Width = 80, Height = 60 } });
		return flyout;
	}

	private static ItemsControl Presenter(DesktopFlyout flyout)
	{
		return (ItemsControl)typeof(DesktopFlyout).GetProperty("Presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flyout)!;
	}

	private static DataTemplate Template(string tag)
	{
		return (DataTemplate)XamlReader.Load($"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{{Binding}}' Tag='{tag}' Margin='16'/></DataTemplate>");
	}

	[WinRT.GeneratedWinRTExposedType]
	private sealed partial class StringCollection : ObservableCollection<string>
	{
	}

	private sealed partial class LengthSelector : DataTemplateSelector
	{
		public DataTemplate Short { get; set; } = null!;
		public DataTemplate Long { get; set; } = null!;
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return ((string)item).Length > 5 ? Long : Short;
		}
	}

	private static T? Find<T>(DependencyObject? element) where T : DependencyObject
	{
		if (element is null) return null;
		if (element is T found) return found;
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
			if (Find<T>(VisualTreeHelper.GetChild(element, i)) is T child) return child;
		return null;
	}

	private static async Task Open(DesktopFlyout flyout)
	{
		flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		await WaitUntil(() => flyout.IsOpen);
	}

	private static async Task WaitUntil(Func<bool> predicate)
	{
		var timeout = Stopwatch.StartNew();
		while (!predicate())
		{
			Assert.IsTrue(timeout.Elapsed < TimeSpan.FromSeconds(5), "The flyout did not converge to the expected layout.");
			await Task.Delay(10);
		}
	}
}
