// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Graphics;

#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif

namespace DesktopFlyouts;

public partial class DesktopFlyout
{
	private DesktopFlyoutPosition? _position;
	private DesktopFlyoutPosition? _requestedPosition;
	private Rectangle? _lastTrayIconBounds;
	private Rectangle _monitorReferenceBounds;
	private double _lastLayoutScale;
	private bool _placementUpdatePending;
	private DispatcherTimer? _environmentTimer;
	private Rectangle _activeWorkArea;
	private DesktopFlyoutPopupDirection _activePopupDirection = DesktopFlyoutPopupDirection.BottomToTop;
	private bool _isUpdatingOpenFlyoutLayout;
	private bool _isOpenFlyoutLayoutUpdateQueued;
	private bool _layoutUpdatePending;
	private bool _isOverflowAnchor;
#if !HAS_UNO
	// Native window placement and taskbar tracking.
	private Rectangle _hostBounds;
	private TaskbarFlyoutVisibilityLease? _taskbarFlyoutVisibilityLease;
	private TaskbarOverflowFlyoutMonitor? _taskbarOverflowFlyoutMonitor;
	private TaskbarLayoutMonitor? _taskbarLayoutMonitor;
	private List<Windows.Win32.Foundation.HWND> _barWindows = [];
#endif

	internal bool IsContentSized(bool isVertical)
	{
		return isVertical
		? double.IsNaN(Height) && VerticalAlignment is not VerticalAlignment.Stretch
		: double.IsNaN(Width) && HorizontalAlignment is not HorizontalAlignment.Stretch;
	}

	internal void OnIslandLayoutChanged()
	{
		Presenter_LayoutInvalidated(this, EventArgs.Empty);
	}

	private DesktopFlyoutPopupDirection UpdateFlyoutRegion(bool activateHost = false)
	{
		if (_host?.DesktopWindowXamlSource is null || RootGrid is null)
		{
			return _activePopupDirection;
		}

#if !HAS_UNO
		UpdateHostBounds(activateHost);
#endif
		var scale = _host.XamlIslandRasterizationScale;
		var frame = _hostContainer.DesiredSize;
		var workArea = GetVisualWorkArea(scale);
		var shadow = Math.Ceiling(DesktopFlyoutChromeMetrics.ShadowMargin * scale);
		var regionWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(Math.Max(0, frame.Width * scale - 2 * shadow), workArea.Width)));
		var regionHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(Math.Max(0, frame.Height * scale - 2 * shadow), workArea.Height)));
		var request = _position ?? DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight);
		if (request.Anchor is Rectangle anchor && !_isOverflowAnchor)
		{
			var gap = (int)Math.Ceiling(DesktopFlyoutChromeMetrics.AnchorGap * scale);
			anchor.Offset(request.AnchorSide switch { DesktopFlyoutAnchorSide.Left => -gap, DesktopFlyoutAnchorSide.Right => gap, _ => 0 },
				request.AnchorSide switch { DesktopFlyoutAnchorSide.Above => -gap, DesktopFlyoutAnchorSide.Below => gap, _ => 0 });
			request = DesktopFlyoutPosition.ForAnchor(anchor, request.AnchorSide, request.AnchorAlignment, request.TransitionDirection);
		}
		var resolvedPosition = DesktopFlyoutPositionResolver.Resolve(
			request,
			workArea,
			new Size(regionWidth, regionHeight));
		var visualBounds = resolvedPosition.Bounds;
#if !HAS_UNO
		var monitor = _hostBounds;
#else
		var monitor = _monitorReferenceBounds;
#endif
		var bounds = DesktopFlyoutChromeMetrics.IncludeShadow(visualBounds, monitor, scale);
		_hostContainer.SetShadowInsets(new((visualBounds.Left - bounds.Left) / scale, (visualBounds.Top - bounds.Top) / scale,
			(bounds.Right - visualBounds.Right) / scale, (bounds.Bottom - visualBounds.Bottom) / scale));

#if !HAS_UNO
		// Apply position and size in one XAML layout without resizing the native viewport.
		var hostLayout = DesktopFlyoutHostLayout.Create(_hostBounds, bounds, scale);
		_hostContainer.Width = hostLayout.DipRegion.Width;
		_hostContainer.Height = hostLayout.DipRegion.Height;
		Canvas.SetLeft(_hostContainer, hostLayout.DipRegion.X);
		Canvas.SetTop(_hostContainer, hostLayout.DipRegion.Y);
		_hostCanvas.UpdateLayout();
		_host.SetHWndRectRegion(new RectInt32
		{
			X = hostLayout.RelativePhysicalRegion.X,
			Y = hostLayout.RelativePhysicalRegion.Y,
			Width = hostLayout.RelativePhysicalRegion.Width,
			Height = hostLayout.RelativePhysicalRegion.Height,
		});
#else
		var region = new RectInt32() { X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height };
		_host.MoveAndResize(region, activateHost);
		_hostContainer.Arrange(new(0, 0, bounds.Width / scale, bounds.Height / scale));
		_hostContainer.UpdateLayout();
#endif

		return resolvedPosition.TransitionDirection;
	}

	private void UpdateOpenFlyoutLayout()
	{
		if (_disposed ||
			!IsOpen ||
			_isPopupAnimationPlaying ||
			_isUpdatingOpenFlyoutLayout ||
			_isOpenFlyoutLayoutUpdateQueued ||
			RootGrid is null ||
			_host?.DesktopWindowXamlSource is null)
		{
			return;
		}

		// Coalesce property and LayoutUpdated notifications into one content layout per frame.
		_isOpenFlyoutLayoutUpdateQueued = true;
		CompositionTarget.Rendering += OpenFlyoutLayout_Rendering;
	}

	private void CancelOpenFlyoutLayoutUpdate()
	{
		if (!_isOpenFlyoutLayoutUpdateQueued)
		{
			return;
		}

		CompositionTarget.Rendering -= OpenFlyoutLayout_Rendering;
		_isOpenFlyoutLayoutUpdateQueued = false;
	}

	private void MeasureFlyoutLayout()
	{
		var scale = _host?.XamlIslandRasterizationScale ?? 1.0D;
		var area = GetVisualWorkArea(scale);
		var shadow = Math.Ceiling(DesktopFlyoutChromeMetrics.ShadowMargin * scale) / scale;
		_hostContainer.SetAvailableSize(new(area.Width / scale + 2 * shadow, area.Height / scale + 2 * shadow), new Thickness(shadow));
		Presenter.ApplyTemplate();
		_hostContainer.Measure(new(area.Width / scale + 2 * shadow, area.Height / scale + 2 * shadow));
	}

	private Rectangle GetVisualWorkArea(double scale)
	{
		var area = DesktopFlyoutChromeMetrics.InsetWorkArea(_activeWorkArea, scale);
		if (_isOverflowAnchor && _position is not null)
		{
			area = _position.AnchorSide switch
			{
				DesktopFlyoutAnchorSide.Above => Rectangle.FromLTRB(area.Left, area.Top, area.Right, _activeWorkArea.Bottom),
				DesktopFlyoutAnchorSide.Below => Rectangle.FromLTRB(area.Left, _activeWorkArea.Top, area.Right, area.Bottom),
				DesktopFlyoutAnchorSide.Left => Rectangle.FromLTRB(area.Left, area.Top, _activeWorkArea.Right, area.Bottom),
				_ => Rectangle.FromLTRB(_activeWorkArea.Left, area.Top, area.Right, area.Bottom),
			};
		}
		return area;
	}

#if !HAS_UNO
	private void UpdateHostBounds(bool activate)
	{
		var bounds = WindowHelpers.TryGetMonitorScreenRect(_monitorReferenceBounds, out var monitorBounds)
			? monitorBounds
			: _activeWorkArea;
		if (_hostBounds == bounds)
		{
			return;
		}

		_host?.Maximize(bounds, activate);
		_hostBounds = bounds;
	}

	private void PrepareHostContainerLayout()
	{
		_hostContainer.Width = double.NaN;
		_hostContainer.Height = double.NaN;
	}

	private void UpdateTaskbarFlyoutVisibilityLease(DesktopFlyoutPosition position)
	{
		_taskbarOverflowFlyoutMonitor?.Dispose();
		_taskbarOverflowFlyoutMonitor = null;

		if (!position.IsTaskbarAnchor || position.Anchor is not Rectangle anchorBounds)
		{
			ReleaseTaskbarFlyoutVisibilityLease();
			return;
		}

#if UWP
		_taskbarOverflowFlyoutMonitor = TaskbarOverflowFlyoutMonitor.TryCreate(
			anchorBounds,
			action =>
			{
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
				return true;
			},
			OnTaskbarOverflowFlyoutClosed);
#elif WASDK
		_taskbarOverflowFlyoutMonitor = TaskbarOverflowFlyoutMonitor.TryCreate(
			anchorBounds,
			action => DispatcherQueue.TryEnqueue(() => action()),
			OnTaskbarOverflowFlyoutClosed);
#endif

		EnsureTaskbarFlyoutVisibilityLease(anchorBounds);
	}

	private void EnsureTaskbarFlyoutVisibilityLease(Rectangle anchorBounds)
	{
		if (_taskbarFlyoutVisibilityLease?.IsForAnchor(anchorBounds) == true)
		{
			return;
		}

		var newLease = TaskbarFlyoutVisibilityLease.TryAcquire(anchorBounds);
		var previousLease = _taskbarFlyoutVisibilityLease;
		_taskbarFlyoutVisibilityLease = newLease;
		previousLease?.Dispose();
	}

	private void ReleaseTaskbarFlyoutVisibilityLease()
	{
		_taskbarOverflowFlyoutMonitor?.Dispose();
		_taskbarOverflowFlyoutMonitor = null;
		_taskbarFlyoutVisibilityLease?.Dispose();
		_taskbarFlyoutVisibilityLease = null;
	}

#endif

	private void InvalidatePlacement()
	{
		if (_disposed || _requestedPosition is null)
		{
			return;
		}

		_placementUpdatePending = true;
		UpdateOpenFlyoutLayout();
	}

	private void StartEnvironmentMonitoring()
	{
		StopEnvironmentMonitoring();
#if !HAS_UNO
#if UWP
		_taskbarLayoutMonitor = TaskbarLayoutMonitor.TryCreate(_barWindows, action =>
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
			return true;
		}, InvalidatePlacement);
#elif WASDK
		_taskbarLayoutMonitor = TaskbarLayoutMonitor.TryCreate(_barWindows,
			action => DispatcherQueue.TryEnqueue(() => action()), InvalidatePlacement);
#endif
#endif
		// Shells do not report every icon rearrangement or appbar registration as a WinEvent.
		// Sample only while visible; native notifications provide the immediate update path.
		_environmentTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
		_environmentTimer.Tick += EnvironmentTimer_Tick;
		_environmentTimer.Start();
	}

	private void StopEnvironmentMonitoring()
	{
		if (_environmentTimer is not null)
		{
			_environmentTimer.Stop();
			_environmentTimer.Tick -= EnvironmentTimer_Tick;
			_environmentTimer = null;
		}
#if !HAS_UNO
		_taskbarLayoutMonitor?.Dispose();
		_taskbarLayoutMonitor = null;
#endif
	}

	private bool RefreshPlacement(bool apply = true)
	{
		if (_disposed || _requestedPosition is null || _host?.DesktopWindowXamlSource is null)
		{
			return false;
		}

		if (!_requestedPosition.TryResolveTrayIcon(out var position))
		{
			if (_lastTrayIconBounds is not Rectangle lastBounds)
			{
				return false;
			}

			position = DesktopFlyoutPosition.FromTaskbarBounds(lastBounds);
		}

		var trayBounds = position.IsTaskbarAnchor ? position.Anchor : null;
		var reference = position.Anchor ?? _monitorReferenceBounds;
		Rectangle monitorBounds;
		Rectangle workArea;
		TaskbarEdge? taskbarEdge = null;
		Rectangle? overflowBounds = null;
#if !HAS_UNO
		workArea = WindowHelpers.ReadDisplayArea(reference, out monitorBounds, out var taskbarBounds, out var windows);
		_barWindows = windows;
		_taskbarLayoutMonitor?.SetWindows(windows);
		if (taskbarBounds is Rectangle taskbar)
		{
			taskbarEdge = WindowHelpers.GetTaskbarEdge(taskbar, monitorBounds);
		}

		if (_taskbarOverflowFlyoutMonitor?.TryGetBounds(out var overflow) == true && overflow.IntersectsWith(monitorBounds))
		{
			overflowBounds = overflow;
		}

		if (trayBounds is Rectangle trayIcon && overflowBounds is null && taskbarBounds is Rectangle bar)
		{
			var edgeAnchor = WindowHelpers.GetTaskbarEdgeAnchor(trayIcon, bar, monitorBounds, out _);
			position = DesktopFlyoutPosition.FromTaskbarBounds(edgeAnchor);
		}
#else
		workArea = WindowHelpers.GetFlyoutWorkAreaRect(position, _monitorReferenceBounds.Location);
		if (!WindowHelpers.TryGetMonitorScreenRect(reference, out monitorBounds))
		{
			monitorBounds = workArea;
		}

		if (trayBounds is Rectangle trayIcon && WindowHelpers.TryGetTaskbarInfoForBounds(trayIcon, out _, out var edge))
		{
			taskbarEdge = edge;
		}
#endif
		position = position.ResolveTaskbarAnchor(taskbarEdge, monitorBounds, overflowBounds);
#if !HAS_UNO
		if (trayBounds is Rectangle overflowIcon && overflowBounds is Rectangle overflowArea)
		{
			var anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(overflowIcon, overflowArea,
				position.AnchorSide);
			position = DesktopFlyoutPosition.ForAnchor(anchor, position.AnchorSide, position.AnchorAlignment, position.TransitionDirection);
			workArea = DesktopFlyoutPositionResolver.GetWorkAreaOutsideAnchor(workArea, anchor, position.AnchorSide);
		}
#endif
		var scale = _host.XamlIslandRasterizationScale;
		var changed = _activeWorkArea != workArea || _monitorReferenceBounds != monitorBounds
			|| _position?.Anchor != position.Anchor || _position?.AnchorSide != position.AnchorSide
			|| _lastLayoutScale != scale;
		if (apply)
		{
			_isOverflowAnchor = overflowBounds is not null;
			_position = position;
			_activeWorkArea = workArea;
			_monitorReferenceBounds = monitorBounds;
			_lastLayoutScale = scale;
			_placementUpdatePending = false;
			_lastTrayIconBounds = trayBounds;
#if !HAS_UNO
			if (trayBounds is Rectangle currentTray)
			{
				EnsureTaskbarFlyoutVisibilityLease(currentTray);
			}
#endif
		}

		return changed;
	}

}
