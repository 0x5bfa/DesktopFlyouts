// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition.SystemBackdrops;

namespace DesktopFlyouts;

internal sealed partial class DesktopFlyoutMicaBackdrop : DesktopFlyoutSystemBackdrop
{
	internal DesktopFlyoutMicaBackdrop(bool useLightTheme)
		: base(useLightTheme)
	{
	}

	protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
	{
		var controller = BackdropControllerHelpers.GetMicaController(configuration.Theme is SystemBackdropTheme.Light);
		if (controller is not null)
			controller.Kind = MicaKind.Base;

		return controller;
	}
}
