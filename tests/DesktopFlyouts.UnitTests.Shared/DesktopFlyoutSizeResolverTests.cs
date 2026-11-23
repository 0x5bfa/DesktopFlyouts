// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts.UnitTests;

[TestClass]
public sealed class DesktopFlyoutSizeResolverTests
{
	[TestMethod]
	[DataRow(800, 240, false, 0, double.PositiveInfinity, 0, 240)]
	[DataRow(800, 1200, false, 0, double.PositiveInfinity, 0, 800)]
	[DataRow(800, 240, true, 0, double.PositiveInfinity, 0, 800)]
	[DataRow(800, 240, true, 0, 400, 20, 420)]
	[DataRow(800, 240, true, 500, 400, 20, 520)]
	[DataRow(100, 200, true, 200, 400, 0, 100)]
	[DataRow(0, 240, true, 0, double.PositiveInfinity, 0, 0)]
	public void ViewportHonorsContentStretchConstraintsAndWorkArea(double available, double desired, bool stretch, double minimum, double maximum, double margin, double expected)
	{
		Assert.AreEqual(expected, DesktopFlyoutSizeResolver.ResolveLength(available, desired, stretch, minimum, maximum, margin), 0.001);
	}

	[TestMethod]
	public void InvalidPlacementMarginsCannotProduceInvalidNativeGeometry()
	{
		foreach (var margin in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -10.0 })
		{
			Assert.AreEqual(0, DesktopFlyoutSizeResolver.NormalizeMargin(margin));
		}
		Assert.AreEqual(12.5, DesktopFlyoutSizeResolver.NormalizeMargin(12.5));
	}
}
