// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace DesktopFlyouts
{
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
    /// Anchor rectangles use physical screen pixels. An explicit anchor side comes from the request;
    /// a taskbar anchor side is resolved when shown and then remains fixed during size changes. The
    /// transition direction defaults to the direction implied by placement.
    /// </remarks>
    public sealed class DesktopFlyoutPosition
    {
        private DesktopFlyoutPosition(
            Rectangle? anchor,
            bool isTaskbarAnchor,
            DesktopFlyoutAnchorSide anchorSide,
            DesktopFlyoutAnchorAlignment anchorAlignment,
            DesktopFlyoutWorkAreaPosition? workAreaPosition,
            DesktopFlyoutMonitorSelection monitorSelection,
            DesktopFlyoutPopupDirection transitionDirection)
        {
            Anchor = anchor;
            IsTaskbarAnchor = isTaskbarAnchor;
            AnchorSide = anchorSide;
            AnchorAlignment = anchorAlignment;
            WorkAreaPosition = workAreaPosition;
            MonitorSelection = monitorSelection;
            TransitionDirection = transitionDirection;
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

        /// <summary>
        /// Creates a position request for an anchor on a taskbar or supported desktop panel.
        /// </summary>
        /// <param name="bounds">The anchor bounds in physical screen pixels.</param>
        /// <remarks>
        /// The containing taskbar edge is detected when the platform exposes its geometry. If no
        /// taskbar is detected, the nearest monitor edge is used as a fallback. The chosen side is
        /// fixed for the lifetime of this request.
        /// </remarks>
        public static DesktopFlyoutPosition ForTaskbarAnchor(Rectangle bounds)
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

        /// <summary>
        /// Creates a fixed work-area position request using the legacy placement enum.
        /// </summary>
        [Obsolete("Use the DesktopFlyoutWorkAreaPosition overload instead.")]
        public static DesktopFlyoutPosition InWorkArea(
            DesktopFlyoutPlacementMode position,
            DesktopFlyoutMonitorSelection monitor = DesktopFlyoutMonitorSelection.Primary,
            DesktopFlyoutPopupDirection transitionDirection = DesktopFlyoutPopupDirection.Auto)
        {
            var workAreaPosition = position switch
            {
                DesktopFlyoutPlacementMode.TopLeft => DesktopFlyoutWorkAreaPosition.TopLeft,
                DesktopFlyoutPlacementMode.TopCenter => DesktopFlyoutWorkAreaPosition.TopCenter,
                DesktopFlyoutPlacementMode.TopRight => DesktopFlyoutWorkAreaPosition.TopRight,
                DesktopFlyoutPlacementMode.LeftCenter => DesktopFlyoutWorkAreaPosition.LeftCenter,
                DesktopFlyoutPlacementMode.RightCenter => DesktopFlyoutWorkAreaPosition.RightCenter,
                DesktopFlyoutPlacementMode.BottomLeft => DesktopFlyoutWorkAreaPosition.BottomLeft,
                DesktopFlyoutPlacementMode.BottomCenter => DesktopFlyoutWorkAreaPosition.BottomCenter,
                DesktopFlyoutPlacementMode.BottomRight => DesktopFlyoutWorkAreaPosition.BottomRight,
                _ => DesktopFlyoutWorkAreaPosition.BottomRight,
            };

            return InWorkArea(workAreaPosition, monitor, transitionDirection);
        }

        internal DesktopFlyoutPosition ResolveTaskbarAnchor(TaskbarEdge? taskbarEdge, Rectangle? monitorBounds)
        {
            if (!IsTaskbarAnchor || Anchor is not Rectangle anchor)
            {
                return this;
            }

            var side = DesktopFlyoutPositionResolver.ResolveTaskbarAnchorSide(anchor, monitorBounds, taskbarEdge);
            return ForAnchor(anchor, side, AnchorAlignment, TransitionDirection);
        }
    }
}
