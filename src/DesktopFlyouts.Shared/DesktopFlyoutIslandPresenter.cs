// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Numerics;
#if HAS_UNO
using Windows.UI;
#endif

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
	private ThemeShadow? _rootShadow;
#endif
#if WASDK && !HAS_UNO
	private SystemBackdropElement? _backdrop;
	private DesktopFlyoutAcrylicBackdrop? _ownedBackdrop;
#endif

	internal GridLength IslandWidth => _definition?.IslandWidth ?? GridLength.Auto;
	internal GridLength IslandHeight => _definition?.IslandHeight ?? GridLength.Auto;

	internal DesktopFlyoutIslandPresenter(DesktopFlyout owner)
	{
		_owner = owner;
		DefaultStyleKey = typeof(DesktopFlyoutIslandPresenter);

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
#if WASDK && !HAS_UNO
		_ownedBackdrop = null;
#endif
		ReleaseClip();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_root = GetTemplateChild("PART_RootGrid") as Grid
			?? throw new InvalidOperationException("The library island template has no root grid.");
		_contentPresenter = GetTemplateChild("PART_MainContentPresenter") as ContentPresenter
			?? throw new InvalidOperationException("The library island template has no main content presenter.");

		_root.SizeChanged += (_, _) => UpdateContentClip();
		_contentPresenter.SizeChanged += (_, _) => UpdateContentClip();

#if !HAS_UNO
		_rootShadow = _root.Shadow as ThemeShadow;
#endif

#if WASDK && !HAS_UNO
		_backdrop = GetTemplateChild("PART_Backdrop") as SystemBackdropElement
			?? throw new InvalidOperationException("The library island template has no system backdrop element.");
#endif

		UpdateChrome();
	}

	internal void UpdateChrome(bool force = false)
	{
		if (_root is null || !_hasItem)
		{
			return;
		}

		var light = GeneralHelpers.IsTaskbarLight();
		RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark;

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
			if (_ownedBackdrop is null || _ownedBackdrop.UseLightTheme != light)
			{
				ClearOwnerBackdrop();
				_ownedBackdrop = new DesktopFlyoutAcrylicBackdrop(light);
			}
			else if (force)
			{
				ClearOwnerBackdrop();
			}

			// Reconnect native targets on the UI thread without replacing the backdrop instance.
			if (!ReferenceEquals(_backdrop.SystemBackdrop, _ownedBackdrop))
			{
				_backdrop.SystemBackdrop = _ownedBackdrop;
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
