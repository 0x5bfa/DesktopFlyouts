// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopFlyouts.UnitTests;

[TestClass]
public sealed class DesktopFlyoutPositionResolverTests
{
	[TestMethod]
	[DataRow((int)TaskbarEdge.Left, DesktopFlyoutAnchorSide.Right)]
	[DataRow((int)TaskbarEdge.Top, DesktopFlyoutAnchorSide.Below)]
	[DataRow((int)TaskbarEdge.Right, DesktopFlyoutAnchorSide.Left)]
	[DataRow((int)TaskbarEdge.Bottom, DesktopFlyoutAnchorSide.Above)]
	public void DetectedTaskbarEdgeChoosesAdjacentFlyoutSide(int edge, DesktopFlyoutAnchorSide expectedSide)
	{
		var actual = DesktopFlyoutPositionResolver.ResolveTaskbarAnchorSide(
			new Rectangle(500, 400, 24, 24),
			new Rectangle(-1920, 0, 1920, 1080),
			(TaskbarEdge)edge);

		Assert.AreEqual(expectedSide, actual);
	}

	[TestMethod]
	[DataRow(DesktopFlyoutAnchorSide.Above)]
	[DataRow(DesktopFlyoutAnchorSide.Below)]
	[DataRow(DesktopFlyoutAnchorSide.Left)]
	[DataRow(DesktopFlyoutAnchorSide.Right)]
	public void OverflowAnchorUsesOverflowContactEdgeAndTrayIconPerpendicularAlignment(DesktopFlyoutAnchorSide side)
	{
		var trayIcon = new Rectangle(1180, 860, 22, 20);
		var overflow = new Rectangle(900, 740, 310, 240);

		var anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(trayIcon, overflow, side, adjacentMargin: 9);

		switch (side)
		{
			case DesktopFlyoutAnchorSide.Above:
				Assert.AreEqual(new Rectangle(trayIcon.Left, overflow.Top + 9, trayIcon.Width, 0), anchor);
				break;
			case DesktopFlyoutAnchorSide.Below:
				Assert.AreEqual(new Rectangle(trayIcon.Left, overflow.Bottom - 9, trayIcon.Width, 0), anchor);
				break;
			case DesktopFlyoutAnchorSide.Left:
				Assert.AreEqual(new Rectangle(overflow.Left + 9, trayIcon.Top, 0, trayIcon.Height), anchor);
				break;
			case DesktopFlyoutAnchorSide.Right:
				Assert.AreEqual(new Rectangle(overflow.Right - 9, trayIcon.Top, 0, trayIcon.Height), anchor);
				break;
		}
	}

	[TestMethod]
	[DataRow(DesktopFlyoutAnchorSide.Above, 812)]
	[DataRow(DesktopFlyoutAnchorSide.Below, 900)]
	[DataRow(DesktopFlyoutAnchorSide.Left, 700)]
	[DataRow(DesktopFlyoutAnchorSide.Right, 1030)]
	public void MultiRowOrColumnOverflowKeepsEachTrayIconAligned(DesktopFlyoutAnchorSide side, int perpendicularCoordinate)
	{
		var overflow = new Rectangle(700, 700, 330, 220);
		var trayIcon = side is DesktopFlyoutAnchorSide.Above or DesktopFlyoutAnchorSide.Below
			? new Rectangle(980, perpendicularCoordinate, 20, 20)
			: new Rectangle(perpendicularCoordinate, 875, 20, 20);

		var anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(trayIcon, overflow, side);

		if (side is DesktopFlyoutAnchorSide.Above or DesktopFlyoutAnchorSide.Below)
		{
			Assert.AreEqual(trayIcon.Left, anchor.Left, "Horizontal rows must retain the icon's column.");
			Assert.AreEqual(trayIcon.Width, anchor.Width);
		}
		else
		{
			Assert.AreEqual(trayIcon.Top, anchor.Top, "Vertical columns must retain the icon's row.");
			Assert.AreEqual(trayIcon.Height, anchor.Height);
		}
	}

	[TestMethod]
	[DataRow(DesktopFlyoutAnchorSide.Above)]
	[DataRow(DesktopFlyoutAnchorSide.Below)]
	[DataRow(DesktopFlyoutAnchorSide.Left)]
	[DataRow(DesktopFlyoutAnchorSide.Right)]
	public void OverflowContactMovesToPopupBoundaryAndKeepsClickedIconRowOrColumn(DesktopFlyoutAnchorSide side)
	{
		var trayIcon = side is DesktopFlyoutAnchorSide.Above or DesktopFlyoutAnchorSide.Below
			? new Rectangle(980, 835, 20, 20)
			: new Rectangle(835, 880, 20, 20);
		var overflow = new Rectangle(700, 700, 330, 220);
		var ordinaryAnchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(trayIcon, trayIcon, side);
		var popupAnchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(trayIcon, overflow, side);

		Assert.AreNotEqual(ContactCoordinate(ordinaryAnchor, side), ContactCoordinate(popupAnchor, side));
		if (side is DesktopFlyoutAnchorSide.Above or DesktopFlyoutAnchorSide.Below)
		{
			Assert.AreEqual(trayIcon.Left, popupAnchor.Left);
			Assert.AreEqual(trayIcon.Width, popupAnchor.Width);
		}
		else
		{
			Assert.AreEqual(trayIcon.Top, popupAnchor.Top);
			Assert.AreEqual(trayIcon.Height, popupAnchor.Height);
		}
	}

	[TestMethod]
	[DataRow(DesktopFlyoutAnchorSide.Above, 712, DesktopFlyoutPopupDirection.BottomToTop)]
	[DataRow(DesktopFlyoutAnchorSide.Below, 908, DesktopFlyoutPopupDirection.TopToBottom)]
	[DataRow(DesktopFlyoutAnchorSide.Left, 712, DesktopFlyoutPopupDirection.RightToLeft)]
	[DataRow(DesktopFlyoutAnchorSide.Right, 1018, DesktopFlyoutPopupDirection.LeftToRight)]
	public void TransparentAdjacentMarginIsRemovedFromFlyoutToShellGap(
		DesktopFlyoutAnchorSide side,
		int expectedContactCoordinate,
		DesktopFlyoutPopupDirection expectedDirection)
	{
		const int ownMargin = 12;
		const int shellInset = 8;
		var workArea = new Rectangle(100, 100, 1000, 900);
		var overflow = new Rectangle(700, 700, 330, 220);
		var trayIcon = new Rectangle(850, 800, 20, 20);
		var anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(trayIcon, overflow, side, ownMargin);
		var availableArea = DesktopFlyoutPositionResolver.GetWorkAreaOutsideAnchor(workArea, anchor, side);
		var position = DesktopFlyoutPosition.ForAnchor(anchor, side);
		var requestedSize = side switch
		{
			DesktopFlyoutAnchorSide.Below => new Size(140, 70),
			DesktopFlyoutAnchorSide.Right => new Size(70, 100),
			_ => new Size(140, 100),
		};
		var resolved = DesktopFlyoutPositionResolver.Resolve(position, availableArea, requestedSize);

		var frameContact = side switch
		{
			DesktopFlyoutAnchorSide.Above => resolved.Bounds.Bottom,
			DesktopFlyoutAnchorSide.Below => resolved.Bounds.Top,
			DesktopFlyoutAnchorSide.Left => resolved.Bounds.Right,
			_ => resolved.Bounds.Left,
		};
		Assert.AreEqual(expectedContactCoordinate, frameContact);
		Assert.AreEqual(expectedDirection, resolved.TransitionDirection);
		Assert.AreEqual(ownMargin, Math.Abs(frameContact - VisualContact(resolved.Bounds, side, ownMargin)));
		Assert.AreEqual(shellInset, Math.Abs(VisualContact(resolved.Bounds, side, ownMargin) - ShellVisualEdge(overflow, side, shellInset)));
	}

	[TestMethod]
	public void ExplicitAnchorIsNotReinterpretedAsTaskbarOrOverflowAnchor()
	{
		var explicitPosition = DesktopFlyoutPosition.ForAnchor(
			new Rectangle(-1450, 150, 26, 24),
			DesktopFlyoutAnchorSide.Right,
			DesktopFlyoutAnchorAlignment.End);

		var resolved = explicitPosition.ResolveTaskbarAnchor(TaskbarEdge.Top, new Rectangle(-1920, 0, 1920, 1080), new Rectangle(0, 0, 500, 500));

		Assert.AreSame(explicitPosition, resolved);
		Assert.AreEqual(new Rectangle(-1450, 150, 26, 24), resolved.Anchor);
		Assert.AreEqual(DesktopFlyoutAnchorSide.Right, resolved.AnchorSide);
	}

	[TestMethod]
	public void ResolvedFlyoutClampsToNegativeOriginWorkArea()
	{
		var workArea = new Rectangle(-1920, -100, 1920, 1040);
		var position = DesktopFlyoutPosition.ForAnchor(
			new Rectangle(-10, -10, 30, 30),
			DesktopFlyoutAnchorSide.Right,
			DesktopFlyoutAnchorAlignment.End);

		var resolved = DesktopFlyoutPositionResolver.Resolve(position, workArea, new Size(480, 320));

		Assert.AreEqual(workArea.Right - 480, resolved.Bounds.Left);
		Assert.AreEqual(workArea.Top, resolved.Bounds.Top);
		Assert.IsTrue(workArea.Contains(resolved.Bounds));
	}

    private static int VisualContact(Rectangle frame, DesktopFlyoutAnchorSide side, int ownMargin)
    {
        return side switch
        {
            DesktopFlyoutAnchorSide.Above => frame.Bottom - ownMargin,
            DesktopFlyoutAnchorSide.Below => frame.Top + ownMargin,
            DesktopFlyoutAnchorSide.Left => frame.Right - ownMargin,
            _ => frame.Left + ownMargin,
        };
    }

    private static int ContactCoordinate(Rectangle anchor, DesktopFlyoutAnchorSide side)
    {
        return side switch
        {
            DesktopFlyoutAnchorSide.Above => anchor.Top,
            DesktopFlyoutAnchorSide.Below => anchor.Bottom,
            DesktopFlyoutAnchorSide.Left => anchor.Left,
            _ => anchor.Right,
        };
    }

    private static int ShellVisualEdge(Rectangle overflow, DesktopFlyoutAnchorSide side, int shellInset)
    {
        return side switch
        {
            DesktopFlyoutAnchorSide.Above => overflow.Top + shellInset,
            DesktopFlyoutAnchorSide.Below => overflow.Bottom - shellInset,
            DesktopFlyoutAnchorSide.Left => overflow.Left + shellInset,
            _ => overflow.Right - shellInset,
        };
    }
}
