// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
#endif

namespace DesktopFlyouts;

/// <summary>Defines the content and allocation of a section inside a desktop flyout.</summary>
/// <remarks>The library owns the visual container, background, border, corners, and shadow.</remarks>
[ContentProperty(Name = nameof(Content))]
public partial class DesktopFlyoutIsland : DependencyObject
{
	private DesktopFlyoutIslandPresenter? _container;
	internal event EventHandler? Changed;

	/// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
	public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
		nameof(Content), typeof(object), typeof(DesktopFlyoutIsland), new PropertyMetadata(null, OnIslandSizePropertyChanged));
	/// <summary>Gets or sets the UI element or data displayed inside the fixed chrome.</summary>
	public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

	/// <summary>Identifies the <see cref="ContentTemplate"/> dependency property.</summary>
	public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
		nameof(ContentTemplate), typeof(DataTemplate), typeof(DesktopFlyoutIsland), new PropertyMetadata(null, OnIslandSizePropertyChanged));
	/// <summary>Gets or sets a template for this definition's content.</summary>
	public DataTemplate? ContentTemplate { get => (DataTemplate?)GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

	/// <summary>Identifies the <see cref="Visibility"/> dependency property.</summary>
	public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register(
		nameof(Visibility), typeof(Visibility), typeof(DesktopFlyoutIsland), new PropertyMetadata(Visibility.Visible, OnIslandSizePropertyChanged));
	/// <summary>Gets or sets whether this section occupies layout space.</summary>
	public Visibility Visibility { get => (Visibility)GetValue(VisibilityProperty); set => SetValue(VisibilityProperty, value); }

	internal void Attach(DesktopFlyoutIslandPresenter container)
	{
		if (_container is not null && !ReferenceEquals(_container, container))
			throw new InvalidOperationException("An island definition cannot be displayed in multiple containers simultaneously.");
		_container = container;
	}

	internal void Detach(DesktopFlyoutIslandPresenter container)
	{
		if (ReferenceEquals(_container, container)) _container = null;
	}

	private static void OnIslandSizePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		((DesktopFlyoutIsland)sender).Changed?.Invoke(sender, EventArgs.Empty);
	}
}
