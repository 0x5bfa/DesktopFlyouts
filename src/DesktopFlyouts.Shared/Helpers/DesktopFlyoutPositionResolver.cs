// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts
{
    internal readonly record struct DesktopFlyoutPositionResult(
        Rectangle Bounds,
        DesktopFlyoutPopupDirection TransitionDirection);

    /// <summary>
    /// Resolves positions using only supplied coordinates and sizes. Platform-specific monitor and
    /// taskbar information is collected by <see cref="WindowHelpers"/> before calling this class.
    /// </summary>
    internal static class DesktopFlyoutPositionResolver
    {
        public static DesktopFlyoutPositionResult Resolve(
            DesktopFlyoutPosition position,
            Rectangle workArea,
            Size flyoutSize)
        {
            DesktopFlyoutAnchorSide side;
            Rectangle bounds;
            if (position.IsAnchored)
            {
                bounds = ResolveAnchorPosition(position, workArea, flyoutSize, out side);
            }
            else
            {
                bounds = ResolveWorkAreaPosition(position, workArea, flyoutSize, out side);
            }

            bounds = ClampToWorkArea(bounds, workArea);
            var direction = ResolveTransitionDirection(position.TransitionDirection, side, bounds, workArea);
            return new DesktopFlyoutPositionResult(bounds, direction);
        }

        public static DesktopFlyoutAnchorSide ResolveTaskbarAnchorSide(
            Rectangle anchor,
            Rectangle? monitorBounds,
            TaskbarEdge? detectedTaskbarEdge)
        {
            if (detectedTaskbarEdge is TaskbarEdge taskbarEdge)
            {
                return taskbarEdge switch
                {
                    TaskbarEdge.Top => DesktopFlyoutAnchorSide.Below,
                    TaskbarEdge.Left => DesktopFlyoutAnchorSide.Right,
                    TaskbarEdge.Right => DesktopFlyoutAnchorSide.Left,
                    _ => DesktopFlyoutAnchorSide.Above,
                };
            }

            if (monitorBounds is not Rectangle monitor || monitor.Width <= 0 || monitor.Height <= 0)
            {
                return DesktopFlyoutAnchorSide.Above;
            }

            var distances = new (DesktopFlyoutAnchorSide Side, long Distance)[]
            {
                (DesktopFlyoutAnchorSide.Below, Math.Abs((long)anchor.Top - monitor.Top)),
                (DesktopFlyoutAnchorSide.Above, Math.Abs((long)monitor.Bottom - anchor.Bottom)),
                (DesktopFlyoutAnchorSide.Right, Math.Abs((long)anchor.Left - monitor.Left)),
                (DesktopFlyoutAnchorSide.Left, Math.Abs((long)monitor.Right - anchor.Right)),
            };

            var nearest = distances[0];
            foreach (var distance in distances)
            {
                if (distance.Distance < nearest.Distance)
                {
                    nearest = distance;
                }
            }

            return nearest.Side;
        }

        private static Rectangle ResolveAnchorPosition(
            DesktopFlyoutPosition position,
            Rectangle workArea,
            Size flyoutSize,
            out DesktopFlyoutAnchorSide side)
        {
            var anchor = position.Anchor!.Value;
            side = position.AnchorSide;

            var horizontalAlignment = ResolveHorizontalAlignment(anchor, flyoutSize.Width, position.AnchorAlignment);
            var verticalAlignment = ResolveVerticalAlignment(anchor, flyoutSize.Height, position.AnchorAlignment);

            return side switch
            {
                DesktopFlyoutAnchorSide.Above => new Rectangle(horizontalAlignment, anchor.Top - flyoutSize.Height, flyoutSize.Width, flyoutSize.Height),
                DesktopFlyoutAnchorSide.Below => new Rectangle(horizontalAlignment, anchor.Bottom, flyoutSize.Width, flyoutSize.Height),
                DesktopFlyoutAnchorSide.Left => new Rectangle(anchor.Left - flyoutSize.Width, verticalAlignment, flyoutSize.Width, flyoutSize.Height),
                DesktopFlyoutAnchorSide.Right => new Rectangle(anchor.Right, verticalAlignment, flyoutSize.Width, flyoutSize.Height),
                _ => throw new ArgumentOutOfRangeException(nameof(position), "Unsupported anchor side."),
            };
        }

        private static Rectangle ResolveWorkAreaPosition(
            DesktopFlyoutPosition position,
            Rectangle workArea,
            Size flyoutSize,
            out DesktopFlyoutAnchorSide side)
        {
            var placement = position.WorkAreaPosition ?? DesktopFlyoutWorkAreaPosition.BottomRight;
            side = placement switch
            {
                DesktopFlyoutWorkAreaPosition.TopLeft or DesktopFlyoutWorkAreaPosition.TopCenter or DesktopFlyoutWorkAreaPosition.TopRight => DesktopFlyoutAnchorSide.Below,
                DesktopFlyoutWorkAreaPosition.LeftCenter => DesktopFlyoutAnchorSide.Right,
                DesktopFlyoutWorkAreaPosition.RightCenter => DesktopFlyoutAnchorSide.Left,
                _ => DesktopFlyoutAnchorSide.Above,
            };

            var left = placement switch
            {
                DesktopFlyoutWorkAreaPosition.TopLeft or DesktopFlyoutWorkAreaPosition.BottomLeft => workArea.Left,
                DesktopFlyoutWorkAreaPosition.TopCenter or DesktopFlyoutWorkAreaPosition.BottomCenter => workArea.Left + ((workArea.Width - flyoutSize.Width) / 2),
                DesktopFlyoutWorkAreaPosition.TopRight or DesktopFlyoutWorkAreaPosition.BottomRight => workArea.Right - flyoutSize.Width,
                DesktopFlyoutWorkAreaPosition.LeftCenter => workArea.Left,
                DesktopFlyoutWorkAreaPosition.RightCenter => workArea.Right - flyoutSize.Width,
                _ => workArea.Right - flyoutSize.Width,
            };

            var top = placement switch
            {
                DesktopFlyoutWorkAreaPosition.TopLeft or DesktopFlyoutWorkAreaPosition.TopCenter or DesktopFlyoutWorkAreaPosition.TopRight => workArea.Top,
                DesktopFlyoutWorkAreaPosition.LeftCenter or DesktopFlyoutWorkAreaPosition.RightCenter => workArea.Top + ((workArea.Height - flyoutSize.Height) / 2),
                _ => workArea.Bottom - flyoutSize.Height,
            };

            return new Rectangle(left, top, flyoutSize.Width, flyoutSize.Height);
        }

        private static int ResolveHorizontalAlignment(Rectangle anchor, int flyoutWidth, DesktopFlyoutAnchorAlignment alignment)
        {
            return alignment switch
            {
                DesktopFlyoutAnchorAlignment.Start => anchor.Left,
                DesktopFlyoutAnchorAlignment.End => anchor.Right - flyoutWidth,
                _ => anchor.Left + ((anchor.Width - flyoutWidth) / 2),
            };
        }

        private static int ResolveVerticalAlignment(Rectangle anchor, int flyoutHeight, DesktopFlyoutAnchorAlignment alignment)
        {
            return alignment switch
            {
                DesktopFlyoutAnchorAlignment.Start => anchor.Top,
                DesktopFlyoutAnchorAlignment.End => anchor.Bottom - flyoutHeight,
                _ => anchor.Top + ((anchor.Height - flyoutHeight) / 2),
            };
        }

        private static Rectangle ClampToWorkArea(Rectangle bounds, Rectangle workArea)
        {
            var maxLeft = Math.Max(workArea.Left, workArea.Right - bounds.Width);
            var maxTop = Math.Max(workArea.Top, workArea.Bottom - bounds.Height);
            var left = Math.Min(Math.Max(bounds.Left, workArea.Left), maxLeft);
            var top = Math.Min(Math.Max(bounds.Top, workArea.Top), maxTop);
            return new Rectangle(left, top, bounds.Width, bounds.Height);
        }

        private static DesktopFlyoutPopupDirection ResolveTransitionDirection(
            DesktopFlyoutPopupDirection requestedDirection,
            DesktopFlyoutAnchorSide side,
            Rectangle bounds,
            Rectangle workArea)
        {
            return requestedDirection switch
            {
                DesktopFlyoutPopupDirection.Auto => side switch
                {
                    DesktopFlyoutAnchorSide.Above => DesktopFlyoutPopupDirection.BottomToTop,
                    DesktopFlyoutAnchorSide.Below => DesktopFlyoutPopupDirection.TopToBottom,
                    DesktopFlyoutAnchorSide.Left => DesktopFlyoutPopupDirection.RightToLeft,
                    DesktopFlyoutAnchorSide.Right => DesktopFlyoutPopupDirection.LeftToRight,
                    _ => DesktopFlyoutPopupDirection.BottomToTop,
                },
                DesktopFlyoutPopupDirection.Vertical => IsBottomHalf(bounds, workArea)
                    ? DesktopFlyoutPopupDirection.BottomToTop
                    : DesktopFlyoutPopupDirection.TopToBottom,
                DesktopFlyoutPopupDirection.Horizontal => IsRightHalf(bounds, workArea)
                    ? DesktopFlyoutPopupDirection.RightToLeft
                    : DesktopFlyoutPopupDirection.LeftToRight,
                _ => requestedDirection,
            };
        }

        private static bool IsBottomHalf(Rectangle bounds, Rectangle workArea)
        {
            return bounds.Top + (bounds.Height / 2D) >= workArea.Top + (workArea.Height / 2D);
        }

        private static bool IsRightHalf(Rectangle bounds, Rectangle workArea)
        {
            return bounds.Left + (bounds.Width / 2D) >= workArea.Left + (workArea.Width / 2D);
        }
    }
}
