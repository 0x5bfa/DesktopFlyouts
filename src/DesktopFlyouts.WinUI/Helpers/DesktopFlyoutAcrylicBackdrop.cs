// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition.SystemBackdrops;

namespace DesktopFlyouts;

internal sealed partial class DesktopFlyoutAcrylicBackdrop : DesktopFlyoutSystemBackdrop
{
	internal DesktopFlyoutAcrylicBackdrop(bool useLightTheme)
		: base(useLightTheme)
	{
	}

	protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
	{
		return BackdropControllerHelpers.GetAcrylicController(configuration.Theme is SystemBackdropTheme.Light);
	}
}
