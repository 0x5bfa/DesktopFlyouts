// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Drawing;

namespace DesktopFlyouts;

internal readonly record struct DesktopFlyoutPositionResult(
	Rectangle Bounds,
	DesktopFlyoutPopupDirection TransitionDirection);
