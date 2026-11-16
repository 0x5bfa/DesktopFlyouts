// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts;

[TestClass]
public sealed class DesktopFlyoutHostLayoutTests
{
	[TestMethod]
	[DataRow(-1920, -1080, 1.0)]
	[DataRow(0, 0, 1.25)]
	[DataRow(1920, 120, 1.5)]
	[DataRow(-2560, 240, 2.0)]
	public void HostLayoutMapsScreenRegionToMonitorLocalPhysicalAndDipCoordinates(int monitorX, int monitorY, double scale)
	{
		var monitor = new Rectangle(monitorX, monitorY, 2560, 1440);
		var screenRegion = new Rectangle(monitor.Right - 430, monitor.Bottom - 290, 410, 260);

		var layout = DesktopFlyoutHostLayout.Create(monitor, screenRegion, scale);

		Assert.AreEqual(monitor, layout.HostBounds);
		Assert.AreEqual(new Rectangle(2130, 1150, 410, 260), layout.RelativePhysicalRegion);
		Assert.AreEqual(2130 / scale, layout.DipRegion.X, 0.001);
		Assert.AreEqual(1150 / scale, layout.DipRegion.Y, 0.001);
		Assert.AreEqual(410 / scale, layout.DipRegion.Width, 0.001);
		Assert.AreEqual(260 / scale, layout.DipRegion.Height, 0.001);
		Assert.AreEqual(layout.RelativePhysicalRegion.X, layout.DipRegion.X * scale, 0.001);
		Assert.AreEqual(layout.RelativePhysicalRegion.Y, layout.DipRegion.Y * scale, 0.001);
		Assert.AreEqual(layout.RelativePhysicalRegion.Width, layout.DipRegion.Width * scale, 0.001);
		Assert.AreEqual(layout.RelativePhysicalRegion.Height, layout.DipRegion.Height * scale, 0.001);
	}

	[TestMethod]
	public void HostBoundsRemainMonitorSizedAcrossDifferentPhysicalRegions()
	{
		var monitor = new Rectangle(-1920, 0, 1920, 1080);
		var smallContent = DesktopFlyoutHostLayout.Create(monitor, new Rectangle(-1700, 720, 300, 200), 1.25);
		var largerContent = DesktopFlyoutHostLayout.Create(monitor, new Rectangle(-1700, 600, 620, 430), 1.25);
		var reducedWorkArea = DesktopFlyoutHostLayout.Create(monitor, new Rectangle(-1700, 500, 620, 360), 1.25);

		Assert.AreEqual(monitor, smallContent.HostBounds);
		Assert.AreEqual(monitor, largerContent.HostBounds);
		Assert.AreEqual(monitor, reducedWorkArea.HostBounds);
		Assert.AreNotEqual(smallContent.RelativePhysicalRegion, largerContent.RelativePhysicalRegion);
		Assert.AreNotEqual(largerContent.RelativePhysicalRegion, reducedWorkArea.RelativePhysicalRegion);
	}

	[TestMethod]
	public void HostLayoutRejectsNonPositiveOrNonFiniteRasterizationScale()
	{
		var monitor = new Rectangle(-1920, 0, 1920, 1080);
		var region = new Rectangle(-1700, 720, 300, 200);

		foreach (var scale in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DesktopFlyoutHostLayout.Create(monitor, region, scale));
		}
	}

	[TestMethod]
	public void HostLayoutRejectsRegionOutsideMonitorInsteadOfProducingInvalidLocalCoordinates()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DesktopFlyoutHostLayout.Create(
			new Rectangle(-1920, 0, 1920, 1080),
			new Rectangle(0, 1000, 200, 100),
			1.0));
	}
}
