// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.UI.Xaml;

namespace DesktopFlyouts;

// Only plain values cross XAML threads; GridLength and dependency properties stay on the owner.
internal sealed record FlyoutSettings(
	bool HideOnLostFocus = true,
	bool IsSwipeToDismissEnabled = false,
	double SwipeDismissThreshold = 80,
	DesktopFlyoutActivationMode ActivationMode = DesktopFlyoutActivationMode.Activate,
	double AutoCloseDelaySeconds = 0,
	double Width = 360,
	double Height = double.NaN,
	DesktopFlyoutBackdropKind BackdropKind = DesktopFlyoutBackdropKind.DesktopAcrylic)
{
	internal void Apply(DesktopFlyout flyout)
	{
		flyout.HideOnLostFocus = HideOnLostFocus;
		flyout.IsSwipeToDismissEnabled = IsSwipeToDismissEnabled;
		flyout.SwipeDismissThreshold = SwipeDismissThreshold;
		flyout.ActivationMode = ActivationMode;
		flyout.AutoCloseDelay = double.IsFinite(AutoCloseDelaySeconds) && AutoCloseDelaySeconds > 0
			? TimeSpan.FromSeconds(AutoCloseDelaySeconds) : TimeSpan.Zero;
		flyout.FlyoutWidth = ToGridLength(Width);
		flyout.FlyoutHeight = ToGridLength(Height);
		flyout.BackdropKind = BackdropKind;
	}

	private static GridLength ToGridLength(double value) => double.IsFinite(value) && value > 0
		? new GridLength(value) : GridLength.Auto;
}
