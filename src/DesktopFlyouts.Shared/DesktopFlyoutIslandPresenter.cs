// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
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
#if !HAS_UNO
	private CompositionRoundedRectangleGeometry? _clipGeometry;
	private CompositionGeometricClip? _contentClip;
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
		_root = (Grid)GetTemplateChild("PART_RootGrid");
		_root.SizeChanged += (_, _) => UpdateContentClip();
		_contentPresenter = (ContentPresenter)GetTemplateChild("PART_MainContentPresenter");
		_contentPresenter.Style = new Style(typeof(ContentPresenter));
		_contentPresenter.SizeChanged += (_, _) => UpdateContentClip();
#if !HAS_UNO
		_root.Translation = new Vector3(0, 0, 36);
		_root.Shadow = new ThemeShadow();
#endif
#if WASDK && !HAS_UNO
		_backdrop = new SystemBackdropElement { Style = new Style(typeof(SystemBackdropElement)), CornerRadius = new CornerRadius(7) };
		_root.Children.Insert(0, _backdrop);
#endif
		UpdateChrome();
	}

	internal void UpdateChrome(bool force = false)
	{
		if (_root is null || !_hasItem) return;
		var light = GeneralHelpers.IsTaskbarLight();
		RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark;
		_root.CornerRadius = new CornerRadius(8);
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
		_root.Shadow ??= new ThemeShadow();
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
		_clipGeometry.CornerRadius = new Vector2(7);
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
