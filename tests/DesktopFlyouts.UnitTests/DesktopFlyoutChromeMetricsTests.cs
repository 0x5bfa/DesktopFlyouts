// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts;

[TestClass]
public sealed class DesktopFlyoutChromeMetricsTests
{
	[TestMethod]
	[DataRow(-1920, -1080, 1.0)]
	[DataRow(0, 0, 1.25)]
	[DataRow(1920, 120, 1.5)]
	[DataRow(-2560, 240, 2.0)]
	public void VisualEdgeGapIsIndependentOfShadowPadding(int x, int y, double scale)
	{
		var monitor = new Rectangle(x, y, 1920, 1080);
		var area = DesktopFlyoutChromeMetrics.InsetWorkArea(monitor, scale);
		var visual = new Rectangle(area.Right - 400, area.Bottom - 300, 400, 300);
		var frame = DesktopFlyoutChromeMetrics.IncludeShadow(visual, monitor, scale);
		var edge = (int)Math.Ceiling(DesktopFlyoutChromeMetrics.ScreenEdgeGap * scale);
		var shadow = (int)Math.Ceiling(DesktopFlyoutChromeMetrics.ShadowMargin * scale);
		Assert.AreEqual(edge, monitor.Right - visual.Right);
		Assert.AreEqual(edge, monitor.Bottom - visual.Bottom);
		Assert.AreEqual(shadow, frame.Right - visual.Right);
		Assert.AreEqual(shadow, frame.Bottom - visual.Bottom);
		Assert.AreEqual(visual.Width + 2 * shadow, frame.Width);
		Assert.IsTrue(monitor.Contains(frame));
	}

	[TestMethod]
	public void TinyWorkAreaRetainsAValidViewportAndClipsShadowToMonitor()
	{
		var monitor = new Rectangle(-10, -20, 5, 3);
		var visual = DesktopFlyoutChromeMetrics.InsetWorkArea(monitor, 2);
		Assert.IsTrue(visual.Width > 0 && visual.Height > 0);
		Assert.IsTrue(monitor.Contains(visual));
		Assert.AreEqual(monitor, DesktopFlyoutChromeMetrics.IncludeShadow(visual, monitor, 2));
	}
}
