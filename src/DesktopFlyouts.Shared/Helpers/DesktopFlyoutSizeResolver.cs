// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

namespace DesktopFlyouts;

/// <summary>Resolves a visible viewport without changing the element's requested dimensions.</summary>
internal static class DesktopFlyoutSizeResolver
{
	internal static double ResolveLength(double available, double desired, bool stretch, double minimum, double maximum, double elementMargin)
	{
		available = Math.Max(0, available);
		var length = stretch ? Math.Max(0, Math.Max(minimum, maximum) + elementMargin) : Math.Max(0, desired);
		return Math.Min(available, length);
	}

	internal static double NormalizeMargin(double value)
	{
		return double.IsFinite(value) ? Math.Max(0, value) : 0;
	}
}
