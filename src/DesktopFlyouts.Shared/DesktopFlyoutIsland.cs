// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
#elif WASDK
using Microsoft.UI.Xaml;
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
}
