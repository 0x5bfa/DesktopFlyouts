// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Composition;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
#if !HAS_UNO
using Microsoft.UI.Composition;
#endif
#endif

#if !HAS_UNO
using Windows.UI.ViewManagement;
#endif

namespace DesktopFlyouts;

internal sealed partial class DesktopFlyoutIslandPresenter : ContentControl
{
	private readonly DesktopFlyout _owner;
	private object? _item;
	private DesktopFlyoutIsland? _definition;
	private Grid? _root;
	private ContentPresenter? _contentPresenter;
	private bool _hasItem;
	private bool _isUpdatingProtectedChrome;
	private readonly HashSet<Brush> _expectedRootBorderBrushes = [];
	private readonly HashSet<Thickness> _expectedRootBorderThicknesses = [];
	private Style? _expectedContentPresenterStyle;
#if WASDK && !HAS_UNO
	private Style? _expectedBackdropStyle;
#endif
	private readonly Dictionary<(DependencyObject Element, DependencyProperty Property), object?> _protectedChromeValues = [];
	private readonly List<(DependencyObject Element, DependencyProperty Property, long Token)> _chromePropertyCallbacks = [];
#if !HAS_UNO
	private CompositionRoundedRectangleGeometry? _clipGeometry;
	private CompositionGeometricClip? _contentClip;
	private ThemeShadow? _rootShadow;
#endif
#if WASDK && !HAS_UNO
	private SystemBackdropElement? _backdrop;
#endif

	internal GridLength IslandWidth => _definition?.IslandWidth ?? GridLength.Auto;
	internal GridLength IslandHeight => _definition?.IslandHeight ?? GridLength.Auto;

	internal DesktopFlyoutIslandPresenter(DesktopFlyout owner)
	{
		_owner = owner;
		Style = new Style(typeof(ContentControl));
		Template = DesktopFlyoutChromeResources.Get<ControlTemplate>("DesktopFlyoutIslandTemplate");
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		IsTabStop = false;
		Margin = new Thickness(0);
		Padding = new Thickness(0);
		Loaded += (_, _) => { UpdateChrome(force: true); UpdateContentClip(); };
		Unloaded += (_, _) => { ClearOwnerBackdrop(); ReleaseClip(); };
		SizeChanged += (_, _) => _owner.OnIslandLayoutChanged();
	}

	internal void SetItem(object item)
	{
		ClearItem();
		_item = item;
		_hasItem = true;
		_definition = item as DesktopFlyoutIsland;
		if (_definition is not null)
		{
			_definition.Attach(this);
			_definition.Changed += Definition_Changed;
		}
		RefreshContent();
	}

	private void Definition_Changed(object? sender, EventArgs e)
	{
		RefreshContent();
		_owner.Presenter.InvalidateLayout();
	}

	internal void RefreshContent()
	{
		ContentTemplate = _definition?.ContentTemplate ?? _owner.IslandTemplate;
		ContentTemplateSelector = _definition?.ContentTemplate is null ? _owner.IslandTemplateSelector : null;
		Content = _definition is null ? _item : _definition.Content;
		Visibility = _definition?.Visibility ?? Visibility.Visible;
		UpdateContentClip();
	}

	internal void ClearItem()
	{
		if (_definition is not null)
		{
			_definition.Changed -= Definition_Changed;
			_definition.Detach(this);
		}
		_definition = null;
		_item = null;
		_hasItem = false;
		Content = null;
		ClearOwnerBackdrop();
		ReleaseClip();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		UnregisterChromePropertyCallbacks();
		_root = GetTemplateChild("PART_RootGrid") as Grid
			?? throw new InvalidOperationException("The library island template has no root grid.");
		_root.SizeChanged += (_, _) => UpdateContentClip();
		_contentPresenter = GetTemplateChild("PART_MainContentPresenter") as ContentPresenter
			?? throw new InvalidOperationException("The library island template has no main content presenter.");
		_contentPresenter.SizeChanged += (_, _) => UpdateContentClip();
		_expectedRootBorderBrushes.Clear();
		_expectedRootBorderThicknesses.Clear();
		foreach (var theme in new[] { "Light", "Dark", "HighContrast" })
		{
			_expectedRootBorderBrushes.Add(DesktopFlyoutChromeResources.GetTheme<Brush>(theme, "DesktopFlyoutIslandBorderBrush"));
			_expectedRootBorderThicknesses.Add(DesktopFlyoutChromeResources.GetTheme<Thickness>(theme, "DesktopFlyoutIslandBorderThickness"));
		}
		_expectedContentPresenterStyle = DesktopFlyoutChromeResources.Get<Style>("DesktopFlyoutIslandContentPresenterStyle");
#if !HAS_UNO
		_rootShadow = _root.Shadow as ThemeShadow;
#endif
#if WASDK && !HAS_UNO
		_backdrop = GetTemplateChild("PART_Backdrop") as SystemBackdropElement
			?? throw new InvalidOperationException("The library island template has no system backdrop element.");
		_expectedBackdropStyle = DesktopFlyoutChromeResources.Get<Style>("DesktopFlyoutIslandSystemBackdropElementStyle");
#endif
		ValidateExpectedStyles();
		RegisterChromePropertyCallback(_root, FrameworkElement.StyleProperty);
		RegisterChromePropertyCallback(_root, Grid.BorderBrushProperty);
		RegisterChromePropertyCallback(_root, Grid.BorderThicknessProperty);
		RegisterChromePropertyCallback(_root, Grid.CornerRadiusProperty);
		RegisterChromePropertyCallback(_contentPresenter, FrameworkElement.StyleProperty);
		RegisterChromePropertyCallback(_contentPresenter, ContentPresenter.BorderBrushProperty);
		RegisterChromePropertyCallback(_contentPresenter, ContentPresenter.BorderThicknessProperty);
		RegisterChromePropertyCallback(_contentPresenter, ContentPresenter.CornerRadiusProperty);
#if WASDK && !HAS_UNO
		RegisterChromePropertyCallback(_backdrop, FrameworkElement.StyleProperty);
		RegisterChromePropertyCallback(_backdrop, SystemBackdropElement.SystemBackdropProperty);
#endif
		UpdateChrome();
	}

	internal void UpdateChrome(bool force = false)
	{
		if (_root is null || !_hasItem) return;
		var light = GeneralHelpers.IsTaskbarLight();
		ValidateExpectedStyles();
		_isUpdatingProtectedChrome = true;
		try
		{
			RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark;
			CaptureCurrentRootBorderValues();
		}
		finally
		{
			_isUpdatingProtectedChrome = false;
		}
#if !HAS_UNO
		var highContrast = new AccessibilitySettings().HighContrast;
		if (highContrast)
		{
			_root.Shadow = null;
			var settings = new UISettings();
			_root.Background = new SolidColorBrush(settings.GetColorValue(UIColorType.Background));
			ClearOwnerBackdrop();
			return;
		}
		_root.Shadow ??= _rootShadow;
#endif
#if WASDK && !HAS_UNO
		_root.Background = null;
		if (_backdrop is not null)
		{
			if (force) ClearOwnerBackdrop();
			if (_backdrop.SystemBackdrop is not DesktopFlyoutAcrylicBackdrop current || current.UseLightTheme != light)
			{
				ClearOwnerBackdrop();
				_backdrop.SystemBackdrop = new DesktopFlyoutAcrylicBackdrop(light);
			}
		}
#elif UWP
		if (force || _root.Background is not DesktopFlyoutBackdropBrush brush || brush.UseLightTheme != light || brush.UseAccent != GeneralHelpers.IsTaskbarColorPrevalenceEnabled())
			_root.Background = new DesktopFlyoutBackdropBrush(DesktopFlyoutBackdropKind.DesktopAcrylic, light, GeneralHelpers.IsTaskbarColorPrevalenceEnabled(), _owner.IsHostBackdropBrushEnabled);
#else
		_root.Background = new SolidColorBrush(light ? Color.FromArgb(255, 243, 243, 243) : Color.FromArgb(255, 32, 32, 32));
#endif
	}

	private void RegisterChromePropertyCallback(DependencyObject element, DependencyProperty property)
	{
		_protectedChromeValues[(element, property)] = element.GetValue(property);
		var token = element.RegisterPropertyChangedCallback(property, OnChromePropertyChanged);
		_chromePropertyCallbacks.Add((element, property, token));
	}

	private void OnChromePropertyChanged(DependencyObject sender, DependencyProperty property)
	{
		if (_isUpdatingProtectedChrome) return;
		ValidateExpectedStyles();

		if (property == FrameworkElement.StyleProperty)
		{
			_protectedChromeValues[(sender, property)] = sender.GetValue(property);
			return;
		}

		if (ReferenceEquals(sender, _root) && property == Grid.BorderBrushProperty &&
			sender.GetValue(property) is Brush borderBrush && _expectedRootBorderBrushes.Contains(borderBrush))
		{
			_protectedChromeValues[(sender, property)] = sender.GetValue(property);
			return;
		}
		if (ReferenceEquals(sender, _root) && property == Grid.BorderThicknessProperty &&
			sender.GetValue(property) is Thickness borderThickness && _expectedRootBorderThicknesses.Contains(borderThickness))
		{
			_protectedChromeValues[(sender, property)] = sender.GetValue(property);
			return;
		}

#if WASDK && !HAS_UNO
		if (ReferenceEquals(sender, _backdrop) && property == SystemBackdropElement.SystemBackdropProperty &&
			(sender.GetValue(property) is null or DesktopFlyoutAcrylicBackdrop))
		{
			_protectedChromeValues[(sender, property)] = sender.GetValue(property);
			return;
		}
#endif

		if (_protectedChromeValues.TryGetValue((sender, property), out var expectedValue) &&
			Equals(sender.GetValue(property), expectedValue))
		{
			return;
		}

		throw new InvalidOperationException($"The island's protected dependency property {property} was changed unexpectedly.");
	}

	private void ValidateExpectedStyles()
	{
		if (_root is not null && _root.Style is not null)
			throw new InvalidOperationException("The island root grid has an unexpected style.");
		if (_contentPresenter is not null && !ReferenceEquals(_contentPresenter.Style, _expectedContentPresenterStyle))
			throw new InvalidOperationException("The island content presenter has an unexpected style.");
#if WASDK && !HAS_UNO
		if (_backdrop is not null && !ReferenceEquals(_backdrop.Style, _expectedBackdropStyle))
			throw new InvalidOperationException("The island backdrop element has an unexpected style.");
#endif
	}

	private void CaptureCurrentRootBorderValues()
	{
		if (_root is null) return;
		if (!_expectedRootBorderBrushes.Contains(_root.BorderBrush))
			throw new InvalidOperationException("The island root grid has an unexpected border brush.");
		if (!_expectedRootBorderThicknesses.Contains(_root.BorderThickness))
			throw new InvalidOperationException("The island root grid has an unexpected border thickness.");
		_protectedChromeValues[(_root, Grid.BorderBrushProperty)] = _root.GetValue(Grid.BorderBrushProperty);
		_protectedChromeValues[(_root, Grid.BorderThicknessProperty)] = _root.GetValue(Grid.BorderThicknessProperty);
	}

	private void UnregisterChromePropertyCallbacks()
	{
		foreach (var (element, property, token) in _chromePropertyCallbacks)
			element.UnregisterPropertyChangedCallback(property, token);
		_chromePropertyCallbacks.Clear();
		_protectedChromeValues.Clear();
	}

	internal void ClearOwnerBackdrop()
	{
#if WASDK && !HAS_UNO
		if (_backdrop?.SystemBackdrop is DesktopFlyoutSystemBackdrop backdrop) backdrop.ClearFrom(_backdrop);
#elif UWP
		if (_root?.Background is DesktopFlyoutBackdropBrush) _root.Background = null;
#endif
	}

	private void UpdateContentClip()
	{
		if (_contentPresenter is null || !_hasItem) return;
#if !HAS_UNO
		var visual = ElementCompositionPreview.GetElementVisual(_contentPresenter);
		_clipGeometry ??= visual.Compositor.CreateRoundedRectangleGeometry();
		_contentClip ??= visual.Compositor.CreateGeometricClip(_clipGeometry);
		_clipGeometry.CornerRadius = new Vector2((float)_contentPresenter.CornerRadius.TopLeft);
		_clipGeometry.Size = new Vector2(
			(float)Math.Max(0, Math.Min(_contentPresenter.ActualWidth, (_root?.ActualWidth ?? 0) - 2)),
			(float)Math.Max(0, Math.Min(_contentPresenter.ActualHeight, (_root?.ActualHeight ?? 0) - 2)));
		visual.Clip = _contentClip;
#else
		_contentPresenter.Clip = new RectangleGeometry { Rect = new(0, 0,
			Math.Max(0, Math.Min(_contentPresenter.ActualWidth, (_root?.ActualWidth ?? 0) - 2)),
			Math.Max(0, Math.Min(_contentPresenter.ActualHeight, (_root?.ActualHeight ?? 0) - 2))) };
#endif
	}

	private void ReleaseClip()
	{
#if !HAS_UNO
		if (_contentPresenter is not null && _contentClip is not null)
			ElementCompositionPreview.GetElementVisual(_contentPresenter).Clip = null;
		_contentClip?.Dispose();
		_clipGeometry?.Dispose();
		_contentClip = null;
		_clipGeometry = null;
#endif
	}
}
