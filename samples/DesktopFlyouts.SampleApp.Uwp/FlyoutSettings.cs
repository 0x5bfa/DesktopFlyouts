// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.UI.Xaml;

namespace DesktopFlyouts;

// Only plain values cross XAML threads; XAML layout and dependency properties stay on the owner.
internal sealed record FlyoutSettings(
	bool HideOnLostFocus = true,
	bool IsSwipeToDismissEnabled = false,
	double SwipeDismissThreshold = 80,
	DesktopFlyoutActivationMode ActivationMode = DesktopFlyoutActivationMode.Activate,
	double AutoCloseDelaySeconds = 0,
	double Width = 360,
	double Height = double.NaN)
{
	internal void Apply(DesktopFlyout flyout)
	{
		flyout.HideOnLostFocus = HideOnLostFocus;
		flyout.IsSwipeToDismissEnabled = IsSwipeToDismissEnabled;
		flyout.SwipeDismissThreshold = SwipeDismissThreshold;
		flyout.ActivationMode = ActivationMode;
		flyout.AutoCloseDelay = double.IsFinite(AutoCloseDelaySeconds) && AutoCloseDelaySeconds > 0
			? TimeSpan.FromSeconds(AutoCloseDelaySeconds) : TimeSpan.Zero;
		flyout.Width = ToLength(Width);
		flyout.Height = ToLength(Height);
	}

    private static double ToLength(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : double.NaN;
    }
}
