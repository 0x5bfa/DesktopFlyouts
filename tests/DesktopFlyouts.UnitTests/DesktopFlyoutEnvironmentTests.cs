// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts.UnitTests;

[TestClass]
public sealed class DesktopFlyoutEnvironmentTests
{
	[TestMethod]
	[DataRow(0, 0, 40, 800, 40, 0, 960, 800)]
	[DataRow(0, 0, 1000, 40, 0, 40, 1000, 760)]
	[DataRow(960, 0, 40, 800, 0, 0, 960, 800)]
	[DataRow(0, 760, 1000, 40, 0, 0, 1000, 760)]
	public void Visible_auto_hide_bar_is_excluded_on_each_edge(int x, int y, int width, int height,
		int expectedX, int expectedY, int expectedWidth, int expectedHeight)
	{
		var monitor = new Rectangle(0, 0, 1000, 800);
		var actual = WindowHelpers.ExcludeTaskbarFromWorkArea(monitor, new(x, y, width, height), monitor);
		Assert.AreEqual(new Rectangle(expectedX, expectedY, expectedWidth, expectedHeight), actual);
	}

	[TestMethod]
	public void Already_reserved_appbar_is_not_subtracted_twice()
	{
		var monitor = new Rectangle(-1200, -100, 1200, 900);
		var workArea = new Rectangle(-1200, -100, 1200, 850);
		Assert.AreEqual(workArea, WindowHelpers.ExcludeTaskbarFromWorkArea(workArea, new(-1200, 750, 1200, 50), monitor));
	}

	[TestMethod]
	public void Only_the_visible_part_of_a_sliding_bar_is_excluded()
	{
		var monitor = new Rectangle(-1200, -100, 1200, 900);
		Assert.AreEqual(new Rectangle(-1200, -100, 1200, 880),
			WindowHelpers.ExcludeTaskbarFromWorkArea(monitor, new(-1200, 780, 1200, 50), monitor));
		Assert.AreEqual(monitor, WindowHelpers.ExcludeTaskbarFromWorkArea(monitor, new(-1200, 800, 1200, 50), monitor));
	}

	[TestMethod]
	public void Bars_on_multiple_edges_combine_with_the_registered_work_area()
	{
		var monitor = new Rectangle(0, 0, 1000, 800);
		var workArea = new Rectangle(0, 30, 1000, 770);
		workArea = WindowHelpers.ExcludeTaskbarFromWorkArea(workArea, new(0, 0, 1000, 30), monitor);
		workArea = WindowHelpers.ExcludeTaskbarFromWorkArea(workArea, new(0, 760, 1000, 40), monitor);
		workArea = WindowHelpers.ExcludeTaskbarFromWorkArea(workArea, new(0, 0, 50, 800), monitor);
		Assert.AreEqual(new Rectangle(50, 30, 950, 730), workArea);
		Assert.AreEqual(workArea, WindowHelpers.ExcludeTaskbarFromWorkArea(workArea, new(1200, 0, 50, 800), monitor));
	}

	[TestMethod]
	public void A_tray_request_reads_fresh_bounds_and_recomputes_the_side()
	{
		Rectangle? icon = new Rectangle(900, 770, 24, 24);
		var request = DesktopFlyoutPosition.FromTrayIconBounds(() => icon);
		Assert.IsTrue(request.TryResolveTrayIcon(out var first));
		var bottom = first.ResolveTaskbarAnchor(TaskbarEdge.Bottom, new(0, 0, 1000, 800));
		Assert.AreEqual(icon, bottom.Anchor);
		Assert.AreEqual(DesktopFlyoutAnchorSide.Above, bottom.AnchorSide);

		icon = new Rectangle(-1180, 200, 24, 24);
		Assert.IsTrue(request.TryResolveTrayIcon(out var second));
		var left = second.ResolveTaskbarAnchor(TaskbarEdge.Left, new(-1200, -100, 1200, 900));
		Assert.AreEqual(icon, left.Anchor);
		Assert.AreEqual(DesktopFlyoutAnchorSide.Right, left.AnchorSide);
		Assert.AreEqual(new Rectangle(900, 770, 24, 24), bottom.Anchor, "Resolving again must not mutate the previous placement.");

		icon = null;
		Assert.IsFalse(request.TryResolveTrayIcon(out _));
		icon = new Rectangle(500, 0, 24, 24);
		Assert.IsTrue(request.TryResolveTrayIcon(out var restored));
		Assert.AreEqual(icon, restored.Anchor);
	}

	[TestMethod]
	public void Explicit_anchors_and_work_area_requests_are_not_reinterpreted()
	{
		var anchor = DesktopFlyoutPosition.ForAnchor(new(200, 300, 40, 20), DesktopFlyoutAnchorSide.Left);
		Assert.IsTrue(anchor.TryResolveTrayIcon(out var resolved));
		Assert.AreSame(anchor, resolved);
		var workArea = DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight);
		Assert.IsTrue(workArea.TryResolveTrayIcon(out resolved));
		Assert.AreSame(workArea, resolved);
	}
}
