// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics;
using FoundationPoint = Windows.Foundation.Point;

#if (UWP || WASDK) && !HAS_UNO
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
#endif

#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
#endif

namespace DesktopFlyouts;

public partial class DesktopFlyout
{
	private bool AreSystemAnimationsEnabled
	{
		get
		{
#if (UWP || WASDK) && !HAS_UNO
			return _systemAnimationsEnabled;
#else
			return true;
#endif
		}
	}

	private bool IsTransitionAnimationRunning => _transitionAnimation is not null;

	private bool AreTransitionAnimationsEnabled => IsTransitionAnimationEnabled && AreSystemAnimationsEnabled;

	private bool IsVerticalDirection => _activePopupDirection is DesktopFlyoutPopupDirection.BottomToTop or DesktopFlyoutPopupDirection.TopToBottom;

	private bool _isPopupAnimationPlaying;
	private bool _hideAfterOpen;
	private bool _showAfterClose;
	private DesktopFlyoutPosition? _showAfterClosePosition;
	private CompositionAnimationOperation? _transitionAnimation;

	/// <summary>
	/// Opens the flyout using a position request resolved for this show operation.
	/// </summary>
	/// <remarks>
	/// A call during an opening transition cancels a deferred close request. A call during closing
	/// is replayed after the close transition completes. <see cref="IsOpen"/> becomes <see langword="true"/> after
	/// the open transition completes.
	/// </remarks>
	public void Show(DesktopFlyoutPosition position)
	{
		ArgumentNullException.ThrowIfNull(position);

		if (_disposed)
		{
			return;
		}

		if (_isPopupAnimationPlaying)
		{
			if (TransitionState is DesktopFlyoutTransitionState.Opening)
			{
				_hideAfterOpen = false;
			}
			else
			{
				_showAfterClose = true;
				_showAfterClosePosition = position;
			}

			return;
		}

		_showAfterClose = false;
		_showAfterClosePosition = null;
		if (!position.TryResolveTrayIcon(out var anchorPosition))
		{
			return;
		}

		EnsureHost();
		_requestedPosition = position;
		position = anchorPosition;
		_lastTrayIconBounds = position.IsTaskbarAnchor ? position.Anchor : null;
		CancelOpenFlyoutLayoutUpdate();

#if !HAS_UNO
		UpdateTaskbarFlyoutVisibilityLease(position);
#endif

		Point? cursorPosition = null;
		if (!position.Anchor.HasValue && position.MonitorSelection is DesktopFlyoutMonitorSelection.Cursor)
		{
			cursorPosition = WindowHelpers.GetCursorPosition();
		}

		_monitorReferenceBounds = position.Anchor ?? new Rectangle(cursorPosition ?? Point.Empty, new Size(1, 1));
		RefreshPlacement();
		StartEnvironmentMonitoring();

		StopAutoCloseTimer();
		StopRestoreActivationTimer();
		_isPopupAnimationPlaying = true;
		SetTransitionState(DesktopFlyoutTransitionState.Opening);
		var shouldActivateOnOpen = ShouldActivateOnOpen();
		if (!shouldActivateOnOpen)
		{
			_host!.PreserveActivationState();
		}

		_host!.SetActivationMode(ActivationMode);
#if !HAS_UNO
		UpdateHostBounds(shouldActivateOnOpen);
		PrepareHostContainerLayout();
#else
		_host!.Maximize(_activeWorkArea, shouldActivateOnOpen);
#endif

#if UWP
		_ = Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, async () =>
#elif WASDK
		DispatcherQueue.TryEnqueue(async () =>
#endif
		{
			if (_disposed)
			{
				return;
			}

			RefreshPlacement();
#if !HAS_UNO
			UpdateHostBounds(false);
			PrepareHostContainerLayout();
#endif
			Presenter.UpdateFlyoutTheme();
			MeasureFlyoutLayout();
			UpdateFocusSuppression();
			_activePopupDirection = UpdateFlyoutRegion(shouldActivateOnOpen);
			_layoutUpdatePending = false;

			// Ensure to hide first
			SetClosedTransform(_activePopupDirection);

			_hostContainer.UpdateLayout();
			await Task.Delay(1);
			if (_disposed)
			{
				return;
			}

			await _host.UpdateWindowVisibility(true, shouldActivateOnOpen);
			if (_disposed)
			{
				return;
			}

#if WASDK && !HAS_UNO
			// Reconnect island backdrops after the host is visible so the backdrop target has a live XamlRoot.
			Presenter.UpdateFlyoutTheme(forceBackdropUpdate: true);
#endif

			if (!shouldActivateOnOpen)
			{
				RestartRestoreActivationTimer();
			}

			if (AreTransitionAnimationsEnabled)
			{
				var animation = CreateFlyoutTranslationAnimation(opening: true);
				_transitionAnimation = animation;
				animation.Completed += OpenCompositionAnimation_Completed;
				animation.Start();
			}
			else
			{
				CompleteOpen();
			}
		});
	}

	/// <summary>
	/// Closes the flyout.
	/// </summary>
	/// <remarks>
	/// A call during an opening transition is deferred until the open transition completes. A
	/// call during closing cancels a queued reopen. <see cref="IsOpen"/> becomes
	/// <see langword="false"/> after the close transition completes.
	/// </remarks>
	public void Hide()
	{
		Hide(false);
	}

	private void Hide(bool closeFromCurrentTransform)
	{
		StopAutoCloseTimer();
		StopRestoreActivationTimer();
		StopSwipeDismissRestoreAnimation();
		ResetSwipeDismissTracking();

		if (_disposed || RootGrid is null)
		{
			return;
		}

		if (_isPopupAnimationPlaying)
		{
			if (TransitionState is DesktopFlyoutTransitionState.Opening)
			{
				_hideAfterOpen = true;
			}
			else
			{
				_showAfterClose = false;
				_showAfterClosePosition = null;
			}

			return;
		}

		if (TransitionState is DesktopFlyoutTransitionState.Closed)
		{
			_showAfterClose = false;
			_showAfterClosePosition = null;
			return;
		}

		CancelOpenFlyoutLayoutUpdate();
		_isPopupAnimationPlaying = true;
		SetTransitionState(DesktopFlyoutTransitionState.Closing);
		if (!closeFromCurrentTransform)
		{
			SetOpenTransform();
		}

		if (AreTransitionAnimationsEnabled)
		{
			var animation = CreateFlyoutTranslationAnimation(opening: false, fromCurrentTransform: closeFromCurrentTransform);
			_transitionAnimation = animation;
			animation.Completed += CloseCompositionAnimation_Completed;
			animation.Start();
		}
		else
		{
			CompleteClose();
		}
	}

	private void StopTransitionAnimation()
	{
		if (_transitionAnimation is null)
		{
			return;
		}

		var animation = _transitionAnimation;
		_transitionAnimation = null;
		animation.Completed -= OpenCompositionAnimation_Completed;
		animation.Completed -= CloseCompositionAnimation_Completed;
		animation.Dispose();
	}

	private void CompleteOpen()
	{
		StopSwipeDismissRestoreAnimation();
		ResetSwipeDismissTracking();
		SetOpenTransform();
		_isPopupAnimationPlaying = false;

		if (_hideAfterOpen)
		{
			_hideAfterOpen = false;
			Hide();
			return;
		}

		IsOpen = true;
		if (ShouldActivateOnOpen())
		{
			PrepareInitialFocus();
		}
		else
		{
			RestartRestoreActivationTimer();
		}

		RestartAutoCloseTimer();
		SetTransitionState(DesktopFlyoutTransitionState.Open);
		if (_placementUpdatePending)
		{
			UpdateOpenFlyoutLayout();
		}
	}

	private void CompleteClose()
	{
		CancelOpenFlyoutLayoutUpdate();
		StopEnvironmentMonitoring();
		_requestedPosition = null;
		_lastTrayIconBounds = null;
		_placementUpdatePending = false;
		StopAutoCloseTimer();
		StopSwipeDismissRestoreAnimation();
		ResetSwipeDismissTracking();
		RestoreFocusSuppression();
		ResetPressedScale();
		SetClosedTransform(_activePopupDirection);
		IsOpen = false;
		_ = _host?.UpdateWindowVisibility(false);
#if !HAS_UNO
		ReleaseTaskbarFlyoutVisibilityLease();
#endif
		_isPopupAnimationPlaying = false;
		SetTransitionState(DesktopFlyoutTransitionState.Closed);

		if (_showAfterClose)
		{
			QueueShowAfterClose();
		}
	}

	private CompositionAnimationOperation CreateFlyoutTranslationAnimation(bool opening, bool fromCurrentTransform = false)
	{
		var rootGrid = RootGrid ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");
		var popupDirection = _activePopupDirection;
		var isVertical = IsVerticalDirection;
#if HAS_UNO
		// Uno's Visual exposes Offset, while WinUI and UWP expose Translation on UIElement.
		var targetProperty = isVertical ? "Offset.Y" : "Offset.X";
#else
		var targetProperty = isVertical ? "Translation.Y" : "Translation.X";
#endif
		var closedOffset = isVertical ? GetClosedYOffset(popupDirection) : GetClosedXOffset(popupDirection);
		var currentOffset = isVertical ? GetRootTranslationY() : GetRootTranslationX();
		var from = opening
			? fromCurrentTransform ? currentOffset : closedOffset
			: fromCurrentTransform ? currentOffset : 0;
		var to = opening ? 0 : closedOffset;
		return CompositionTransitionHelpers.CreateFlyoutTranslationAnimation(
			rootGrid,
			targetProperty,
			(float)from,
			(float)to,
			opening,
			isVertical);
	}

	private void SetOpenTransform()
	{
		SetRootTranslation(0, 0);
	}

	private void SetClosedTransform(DesktopFlyoutPopupDirection popupDirection)
	{
		SetRootTranslation(GetClosedXOffset(popupDirection), GetClosedYOffset(popupDirection));
	}

	private double GetRootTranslationX()
	{
#if HAS_UNO
		return RootGrid is null ? 0 : ElementCompositionPreview.GetElementVisual(RootGrid).Offset.X;
#else
		return RootGrid?.Translation.X ?? 0;
#endif
	}

	private double GetRootTranslationY()
	{
#if HAS_UNO
		return RootGrid is null ? 0 : ElementCompositionPreview.GetElementVisual(RootGrid).Offset.Y;
#else
		return RootGrid?.Translation.Y ?? 0;
#endif
	}

	private void SetRootTranslation(double x, double y)
	{
		if (RootGrid is not null)
		{
#if HAS_UNO
			ElementCompositionPreview.GetElementVisual(RootGrid).Offset = new Vector3((float)x, (float)y, 0);
#else
			RootGrid.Translation = new Vector3((float)x, (float)y, 0);
#endif
		}
	}

	private double GetRootScaleX()
	{
		return RootGrid is null ? 1 : ElementCompositionPreview.GetElementVisual(RootGrid).Scale.X;
	}

	private double GetRootScaleY()
	{
		return RootGrid is null ? 1 : ElementCompositionPreview.GetElementVisual(RootGrid).Scale.Y;
	}

	private void SetRootScale(double scaleX, double scaleY)
	{
		if (RootGrid is not null)
		{
			ElementCompositionPreview.GetElementVisual(RootGrid).Scale = new Vector3((float)scaleX, (float)scaleY, 1);
		}
	}

	private void UpdateRootScaleCenterPoint()
	{
		if (RootGrid is not null)
		{
			ElementCompositionPreview.GetElementVisual(RootGrid).CenterPoint = new Vector3((float)(RootGrid.ActualWidth / 2), (float)(RootGrid.ActualHeight / 2), 0);
		}
	}

	private double GetClosedXOffset(DesktopFlyoutPopupDirection popupDirection)
	{
		return popupDirection switch
		{
			DesktopFlyoutPopupDirection.LeftToRight => -Math.Ceiling(_hostContainer.ContentBounds.Right),
			DesktopFlyoutPopupDirection.RightToLeft => Math.Ceiling(_hostContainer.ActualWidth - _hostContainer.ContentBounds.Left),
			_ => 0,
		};
	}

	private double GetClosedYOffset(DesktopFlyoutPopupDirection popupDirection)
	{
		return popupDirection switch
		{
			DesktopFlyoutPopupDirection.TopToBottom => -Math.Ceiling(_hostContainer.ContentBounds.Bottom),
			DesktopFlyoutPopupDirection.BottomToTop => Math.Ceiling(_hostContainer.ActualHeight - _hostContainer.ContentBounds.Top),
			_ => 0,
		};
	}

	private bool ShouldActivateOnOpen()
	{
		return ActivationMode is DesktopFlyoutActivationMode.Activate;
	}

	private void SetTransitionState(DesktopFlyoutTransitionState transitionState)
	{
		if (TransitionState == transitionState)
		{
			return;
		}

		TransitionState = transitionState;
		TransitionStateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void QueueShowAfterClose()
	{
#if UWP
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, ShowAfterClose);
#elif WASDK
		DispatcherQueue.TryEnqueue(ShowAfterClose);
#endif
	}

	private void ShowAfterClose()
	{
		if (_disposed || TransitionState is not DesktopFlyoutTransitionState.Closed || !_showAfterClose || _showAfterClosePosition is null)
		{
			return;
		}

		var position = _showAfterClosePosition!;
		_showAfterClose = false;
		_showAfterClosePosition = null;
		Show(position);
	}
}
