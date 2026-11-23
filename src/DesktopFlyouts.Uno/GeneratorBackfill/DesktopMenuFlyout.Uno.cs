// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Manual dependency property implementation for Uno.
// The GeneratedDependencyProperty source generator does not run under Uno.Sdk.

namespace DesktopFlyouts;

public partial class DesktopMenuFlyout
{
	public partial bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		private set => SetValue(IsOpenProperty, value);
	}
	public static readonly DependencyProperty IsOpenProperty =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(DesktopMenuFlyout), new PropertyMetadata(false));
}
