// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if !HAS_UNO

using System;
using System.Drawing;

namespace DesktopFlyouts;

internal static partial class WindowHelpers
{
	internal static Rectangle ExcludeTaskbarFromWorkArea(
		Rectangle workArea,
		Rectangle taskbarBounds,
		Rectangle monitorBounds)
	{
		// An auto-hide taskbar can overlay an rcWork that spans the whole monitor. Subtract
		// only its actual overlap, so a taskbar already excluded from rcWork is not counted twice.
		var overlap = Rectangle.Intersect(workArea, taskbarBounds);
		if (overlap.Width <= 0 || overlap.Height <= 0)
		{
			return workArea;
		}

		var edge = GetTaskbarEdge(taskbarBounds, monitorBounds);

		return edge switch
		{
			TaskbarEdge.Left => Rectangle.FromLTRB(
				Math.Max(workArea.Left, overlap.Right),
				workArea.Top,
				workArea.Right,
				workArea.Bottom),
			TaskbarEdge.Top => Rectangle.FromLTRB(
				workArea.Left,
				Math.Max(workArea.Top, overlap.Bottom),
				workArea.Right,
				workArea.Bottom),
			TaskbarEdge.Right => Rectangle.FromLTRB(
				workArea.Left,
				workArea.Top,
				Math.Min(workArea.Right, overlap.Left),
				workArea.Bottom),
			TaskbarEdge.Bottom => Rectangle.FromLTRB(
				workArea.Left,
				workArea.Top,
				workArea.Right,
				Math.Min(workArea.Bottom, overlap.Top)),
			_ => workArea,
		};
	}

	internal static TaskbarEdge GetTaskbarEdge(Rectangle taskbarBounds, Rectangle monitorBounds)
	{
		return taskbarBounds.Width >= taskbarBounds.Height
			? Math.Abs((long)taskbarBounds.Top - monitorBounds.Top) <= Math.Abs((long)monitorBounds.Bottom - taskbarBounds.Bottom)
				? TaskbarEdge.Top
				: TaskbarEdge.Bottom
			: Math.Abs((long)taskbarBounds.Left - monitorBounds.Left) <= Math.Abs((long)monitorBounds.Right - taskbarBounds.Right)
				? TaskbarEdge.Left
				: TaskbarEdge.Right;
	}

	internal static Rectangle GetTaskbarEdgeAnchor(
		Rectangle sourceAnchor,
		Rectangle taskbarBounds,
		Rectangle monitorBounds,
		out TaskbarEdge edge)
	{
		edge = GetTaskbarEdge(taskbarBounds, monitorBounds);

		var centerX = sourceAnchor.Left + (sourceAnchor.Width / 2);
		var centerY = sourceAnchor.Top + (sourceAnchor.Height / 2);
		return edge switch
		{
			TaskbarEdge.Top => new Rectangle(
				Math.Clamp(centerX, taskbarBounds.Left, Math.Max(taskbarBounds.Left, taskbarBounds.Right - 1)),
				taskbarBounds.Bottom,
				0,
				0),
			TaskbarEdge.Left => new Rectangle(
				taskbarBounds.Right,
				Math.Clamp(centerY, taskbarBounds.Top, Math.Max(taskbarBounds.Top, taskbarBounds.Bottom - 1)),
				0,
				0),
			TaskbarEdge.Right => new Rectangle(
				taskbarBounds.Left,
				Math.Clamp(centerY, taskbarBounds.Top, Math.Max(taskbarBounds.Top, taskbarBounds.Bottom - 1)),
				0,
				0),
			_ => new Rectangle(
				Math.Clamp(centerX, taskbarBounds.Left, Math.Max(taskbarBounds.Left, taskbarBounds.Right - 1)),
				taskbarBounds.Top,
				0,
				0),
		};
	}
}
#endif
