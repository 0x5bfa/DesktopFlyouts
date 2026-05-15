// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
#endif

namespace U5BFA.Libraries
{
	public partial class TrayIconFlyoutIsland : ContentControl
	{
		private const string PART_RootGrid = "PART_RootGrid";
		private const string PART_BackdropTargetGrid = "PART_BackdropTargetGrid";
		private const string PART_MainContentPresenter = "PART_MainContentPresenter";

		private Grid? RootGrid;
#if WASDK
		private Grid? BackdropTargetGrid;
#endif
		private ContentPresenter? MainContentPresenter;

#if WASDK
		// Use ContentExternalBackdropLink to apply backdrop to normal UI elements
		private ContentExternalBackdropLink? _backdropLink;
		private bool _isBackdropLinkAttached;
#endif

		private WeakReference<TrayIconFlyout>? _owner;
		private long _propertyChangedCallbackTokenForContentProperty;
		private long _propertyChangedCallbackTokenForCornerRadiusProperty;

		public static readonly DependencyProperty IslandWidthProperty =
			DependencyProperty.Register(nameof(IslandWidth), typeof(GridLength), typeof(TrayIconFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

		public GridLength IslandWidth
		{
			get => (GridLength)GetValue(IslandWidthProperty);
			set => SetValue(IslandWidthProperty, value);
		}

		public static readonly DependencyProperty IslandHeightProperty =
			DependencyProperty.Register(nameof(IslandHeight), typeof(GridLength), typeof(TrayIconFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

		public GridLength IslandHeight
		{
			get => (GridLength)GetValue(IslandHeightProperty);
			set => SetValue(IslandHeightProperty, value);
		}

		public TrayIconFlyoutIsland()
		{
			DefaultStyleKey = typeof(TrayIconFlyoutIsland);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			RootGrid = GetTemplateChild(PART_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(TrayIconFlyoutIsland)}'s style.");
#if WASDK
			BackdropTargetGrid = GetTemplateChild(PART_BackdropTargetGrid) as Grid
				?? throw new MissingFieldException($"Could not find {PART_BackdropTargetGrid} in the given {nameof(TrayIconFlyoutIsland)}'s style.");
#endif
			MainContentPresenter = GetTemplateChild(PART_MainContentPresenter) as ContentPresenter
				?? throw new MissingFieldException($"Could not find {PART_MainContentPresenter} in the given {nameof(TrayIconFlyoutIsland)}'s style.");

			_propertyChangedCallbackTokenForContentProperty = RegisterPropertyChangedCallback(ContentProperty, (s, e) => ((TrayIconFlyoutIsland)s).OnContentChanged());
			_propertyChangedCallbackTokenForCornerRadiusProperty = RegisterPropertyChangedCallback(CornerRadiusProperty, (s, e) => ((TrayIconFlyoutIsland)s).OnCornerRadiusChanged());

#if WASDK
			SizeChanged += TrayIconFlyoutIsland_SizeChanged;
			MainContentPresenter.SizeChanged += MainContentPresenter_SizeChanged;
#endif
			Unloaded += TrayIconFlyoutIsland_Unloaded;
		}

		internal void SetOwner(TrayIconFlyout owner)
		{
			_owner = new(owner);
		}

		private static void OnIslandSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			if (dependencyObject is not TrayIconFlyoutIsland island)
				return;

			if (island._owner is not null && island._owner.TryGetTarget(out var owner))
				owner.OnIslandSizeChanged();
		}

#if WASDK
		internal void UpdateBackdrop(bool isEnabled, bool coerce = false)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner) || owner.BackdropManager is null)
				return;

			if (isEnabled)
			{
				if (_isBackdropLinkAttached)
				{
					if (coerce)
					{
						if (_backdropLink is null)
							return;

						owner.BackdropManager.RemoveLink(_backdropLink);
						_backdropLink = null;
						_isBackdropLinkAttached = false;
					}
					else return;
				}

				_backdropLink = owner.BackdropManager.CreateLink();
				_isBackdropLinkAttached = true;
				UpdateBackdropVisual();
			}
			else
			{
				if (_backdropLink is null) return;

				owner.BackdropManager.RemoveLink(_backdropLink);
				_backdropLink = null;
				_isBackdropLinkAttached = false;
			}
		}

		internal void UpdateBackdropVisual()
		{
			if (BackdropTargetGrid is null || _backdropLink is null || MainContentPresenter is null)
				return;

			_backdropLink.PlacementVisual.Size = new((float)ActualWidth, (float)ActualHeight);
			_backdropLink.PlacementVisual.Clip = _backdropLink.PlacementVisual.Compositor.CreateRectangleClip(
				0, 0, (float)MainContentPresenter.ActualWidth, (float)MainContentPresenter.ActualHeight,
				new(Convert.ToSingle(BackdropTargetGrid.CornerRadius.TopLeft - 1), Convert.ToSingle(BackdropTargetGrid.CornerRadius.TopLeft - 1)),
				new(Convert.ToSingle(BackdropTargetGrid.CornerRadius.TopRight - 1), Convert.ToSingle(BackdropTargetGrid.CornerRadius.TopRight - 1)),
				new(Convert.ToSingle(BackdropTargetGrid.CornerRadius.BottomRight - 1), Convert.ToSingle(BackdropTargetGrid.CornerRadius.BottomRight - 1)),
				new(Convert.ToSingle(BackdropTargetGrid.CornerRadius.BottomLeft - 1), Convert.ToSingle(BackdropTargetGrid.CornerRadius.BottomLeft - 1)));

			ElementCompositionPreview.SetElementChildVisual(BackdropTargetGrid, _backdropLink.PlacementVisual);
		}
#endif

		private void OnContentChanged()
		{
			if (Content is not FrameworkElement newContent || MainContentPresenter is null)
				return;

			if (MainContentPresenter.Content is FrameworkElement oldContent)
				oldContent.SizeChanged -= Content_SizeChanged;

			newContent.SizeChanged += Content_SizeChanged;
			MainContentPresenter.Content = Content;
		}

		private void OnCornerRadiusChanged()
		{
#if WASDK
			// Update backdrop visual to reflect the new corner radius
			UpdateBackdropVisual();
#endif
		}

		private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
		{
#if WASDK
			UpdateBackdropVisual();
#endif
		}

#if WASDK
		private void TrayIconFlyoutIsland_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateBackdropVisual();
		}

		private void MainContentPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateBackdropVisual();
		}
#endif

		private void TrayIconFlyoutIsland_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= TrayIconFlyoutIsland_Unloaded;
#if WASDK
			SizeChanged -= TrayIconFlyoutIsland_SizeChanged;

			if (MainContentPresenter is not null)
				MainContentPresenter.SizeChanged -= MainContentPresenter_SizeChanged;
#endif

			UnregisterPropertyChangedCallback(ContentProperty, _propertyChangedCallbackTokenForContentProperty);
			UnregisterPropertyChangedCallback(CornerRadiusProperty, _propertyChangedCallbackTokenForCornerRadiusProperty);
		}
	}
}
