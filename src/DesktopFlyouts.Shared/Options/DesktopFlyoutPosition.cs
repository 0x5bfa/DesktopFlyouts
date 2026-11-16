// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts;

/// <summary>
/// Specifies the side of an anchor where a flyout is placed.
/// </summary>
public enum DesktopFlyoutAnchorSide
{
	Above,
	Below,
	Left,
	Right,
}

/// <summary>
/// Specifies how a flyout aligns with the anchor on its perpendicular axis.
/// </summary>
public enum DesktopFlyoutAnchorAlignment
{
	Start,
	Center,
	End,
}

/// <summary>
/// Specifies which monitor work area to use for an unanchored position.
/// </summary>
public enum DesktopFlyoutMonitorSelection
{
	Primary,
	Cursor,
}

/// <summary>
/// Describes where a <see cref="DesktopFlyout"/> should appear for one call to
/// <see cref="DesktopFlyout.Show(DesktopFlyoutPosition)"/>.
/// </summary>
/// <remarks>
/// Anchor rectangles use physical screen pixels. Explicit anchors remain fixed; tray-icon
/// anchors are resolved again as the desktop layout changes. The transition direction defaults
/// to the direction implied by placement.
/// </remarks>
public sealed class DesktopFlyoutPosition
{
	private readonly Func<Rectangle?>? _getTrayIconBounds;

	private DesktopFlyoutPosition(
		Rectangle? anchor,
		bool isTaskbarAnchor,
		DesktopFlyoutAnchorSide anchorSide,
		DesktopFlyoutAnchorAlignment anchorAlignment,
		DesktopFlyoutWorkAreaPosition? workAreaPosition,
		DesktopFlyoutMonitorSelection monitorSelection,
		DesktopFlyoutPopupDirection transitionDirection,
		Func<Rectangle?>? getTrayIconBounds = null)
	{
		Anchor = anchor;
		IsTaskbarAnchor = isTaskbarAnchor;
		AnchorSide = anchorSide;
		AnchorAlignment = anchorAlignment;
		WorkAreaPosition = workAreaPosition;
		MonitorSelection = monitorSelection;
		TransitionDirection = transitionDirection;
		_getTrayIconBounds = getTrayIconBounds;
	}

	internal Rectangle? Anchor { get; }

	internal bool IsTaskbarAnchor { get; }

	internal DesktopFlyoutAnchorSide AnchorSide { get; }

	internal DesktopFlyoutAnchorAlignment AnchorAlignment { get; }

	internal DesktopFlyoutWorkAreaPosition? WorkAreaPosition { get; }

	internal DesktopFlyoutMonitorSelection MonitorSelection { get; }

	internal DesktopFlyoutPopupDirection TransitionDirection { get; }

	internal bool IsAnchored => Anchor.HasValue;

	/// <summary>
	/// Creates a position request relative to a screen-space anchor rectangle.
	/// </summary>
	public static DesktopFlyoutPosition ForAnchor(
		Rectangle anchor,
		DesktopFlyoutAnchorSide side,
		DesktopFlyoutAnchorAlignment alignment = DesktopFlyoutAnchorAlignment.Center,
		DesktopFlyoutPopupDirection transitionDirection = DesktopFlyoutPopupDirection.Auto)
	{
		if (anchor.Width < 0 || anchor.Height < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor dimensions cannot be negative.");
		}

		if (side is not DesktopFlyoutAnchorSide.Above
			and not DesktopFlyoutAnchorSide.Below
			and not DesktopFlyoutAnchorSide.Left
			and not DesktopFlyoutAnchorSide.Right)
		{
			throw new ArgumentOutOfRangeException(nameof(side));
		}

		if (alignment is not DesktopFlyoutAnchorAlignment.Start
			and not DesktopFlyoutAnchorAlignment.Center
			and not DesktopFlyoutAnchorAlignment.End)
		{
			throw new ArgumentOutOfRangeException(nameof(alignment));
		}

		return new DesktopFlyoutPosition(
			anchor,
			isTaskbarAnchor: false,
			anchorSide: side,
			anchorAlignment: alignment,
			workAreaPosition: null,
			monitorSelection: DesktopFlyoutMonitorSelection.Cursor,
			transitionDirection: transitionDirection);
	}

#if UWP || WASDK
	/// <summary>
	/// Creates a position request that follows the current bounds of a tray icon.
	/// </summary>
	/// <remarks>
	/// The icon must have shell-reported bounds when the flyout is first shown. While open,
	/// the last available bounds are retained if the shell temporarily cannot locate it.
	/// The icon remains owned by the caller and is not disposed with the flyout.
	/// </remarks>
	public static DesktopFlyoutPosition ForTrayIcon(SystemTrayIcon trayIcon)
	{
		ArgumentNullException.ThrowIfNull(trayIcon);
		return FromTrayIconBounds(() => trayIcon.TryGetBounds(out var bounds) ? bounds : null);
	}
#endif

	internal static DesktopFlyoutPosition FromTaskbarBounds(Rectangle bounds)
	{
		if (bounds.Width < 0 || bounds.Height < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(bounds), "Anchor dimensions cannot be negative.");
		}

		return new DesktopFlyoutPosition(
			bounds,
			isTaskbarAnchor: true,
			anchorSide: DesktopFlyoutAnchorSide.Above,
			anchorAlignment: DesktopFlyoutAnchorAlignment.Center,
			workAreaPosition: null,
			monitorSelection: DesktopFlyoutMonitorSelection.Cursor,
			transitionDirection: DesktopFlyoutPopupDirection.Auto);
	}

	internal static DesktopFlyoutPosition FromTrayIconBounds(Func<Rectangle?> getBounds)
	{
		ArgumentNullException.ThrowIfNull(getBounds);
		return new DesktopFlyoutPosition(null, true, DesktopFlyoutAnchorSide.Above,
			DesktopFlyoutAnchorAlignment.Center, null, DesktopFlyoutMonitorSelection.Cursor,
			DesktopFlyoutPopupDirection.Auto, getBounds);
	}

	internal bool TryResolveTrayIcon(out DesktopFlyoutPosition position)
	{
		position = this;
		if (_getTrayIconBounds is null)
			return true;

		if (_getTrayIconBounds() is not Rectangle bounds || bounds.Width <= 0 || bounds.Height <= 0)
			return false;

		position = FromTaskbarBounds(bounds);
		return true;
	}

	/// <summary>
	/// Creates a fixed work-area position request.
	/// </summary>
	public static DesktopFlyoutPosition InWorkArea(
		DesktopFlyoutWorkAreaPosition position,
		DesktopFlyoutMonitorSelection monitor = DesktopFlyoutMonitorSelection.Primary,
		DesktopFlyoutPopupDirection transitionDirection = DesktopFlyoutPopupDirection.Auto)
	{
		return new DesktopFlyoutPosition(
			anchor: null,
			isTaskbarAnchor: false,
			anchorSide: DesktopFlyoutAnchorSide.Above,
			anchorAlignment: DesktopFlyoutAnchorAlignment.Center,
			workAreaPosition: position,
			monitorSelection: monitor,
			transitionDirection: transitionDirection);
	}

	internal DesktopFlyoutPosition ResolveTaskbarAnchor(TaskbarEdge? taskbarEdge, Rectangle? monitorBounds, Rectangle? overflowBounds = null)
	{
		if (!IsTaskbarAnchor || Anchor is not Rectangle anchor)
		{
			return this;
		}

		var side = DesktopFlyoutPositionResolver.ResolveTaskbarAnchorSide(overflowBounds ?? anchor, monitorBounds, taskbarEdge);
		if (overflowBounds is Rectangle overflow)
			anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(anchor, overflow, side);

		return ForAnchor(anchor, side, AnchorAlignment, TransitionDirection);
	}
}
