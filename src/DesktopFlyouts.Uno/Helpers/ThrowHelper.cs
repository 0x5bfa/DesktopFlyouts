// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

namespace DesktopFlyouts;

internal static class ThrowHelper
{
	internal static void ThrowIfNotLinux()
	{
		if (!OperatingSystem.IsLinux())
			throw new PlatformNotSupportedException("DesktopFlyouts X11 APIs are only supported on Linux.");
	}
}
