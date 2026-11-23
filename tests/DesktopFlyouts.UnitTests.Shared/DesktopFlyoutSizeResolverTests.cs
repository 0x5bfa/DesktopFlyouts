// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts.UnitTests;

[TestClass]
public sealed class DesktopFlyoutSizeResolverTests
{
	[TestMethod]
	[DataRow(800d, 240d, false, 0d, double.PositiveInfinity, 0d, 240d)]
	[DataRow(800d, 1200d, false, 0d, double.PositiveInfinity, 0d, 800d)]
	[DataRow(800d, 240d, true, 0d, double.PositiveInfinity, 0d, 800d)]
	[DataRow(800d, 240d, true, 0d, 400d, 20d, 420d)]
	[DataRow(800d, 240d, true, 500d, 400d, 20d, 520d)]
	[DataRow(100d, 200d, true, 200d, 400d, 0d, 100d)]
	[DataRow(0d, 240d, true, 0d, double.PositiveInfinity, 0d, 0d)]
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
