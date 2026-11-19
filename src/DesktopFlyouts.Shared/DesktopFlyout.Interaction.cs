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
	private bool _isPressAnimationActive;
	private DispatcherTimer? _autoCloseTimer;
	private DispatcherTimer? _restoreActivationTimer;
	private CompositionAnimationOperation? _pressScaleAnimation;
	private CompositionAnimationOperation? _swipeDismissRestoreAnimation;
	private FoundationPoint _swipeDismissStartPoint;
	private double _pressScaleTargetScale = 1.0D;
	private uint _swipeDismissPointerId;
	private int _restoreActivationTickCount;
	private readonly List<(Control Control, bool IsTabStop)> _suppressedTabStopStates = [];
	private readonly List<(FrameworkElement Element, bool AllowFocusOnInteraction)> _suppressedInteractionFocusStates = [];
	private bool _isSwipeDismissTracking;
	private bool _isSwipeDismissDragging;
	private bool _isFocusManagerGettingFocusSubscribed;
	private const double SwipeDismissDragStartThreshold = 4.0D;
	private const double SwipeDismissAxisDominanceRatio = 1.2D;

	/// <summary>
	/// Moves focus into the flyout's XAML island.
	/// </summary>
	/// <param name="reason">The focus navigation reason used by the XAML hosting layer.</param>
	/// <remarks>
	/// Focus cannot be moved into the flyout while <see cref="ActivationMode"/> is
	/// <see cref="DesktopFlyoutActivationMode.NeverActivate"/>.
	/// </remarks>
	public void NavigateFocus(XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic)
	{
		_host?.NavigateFocus(reason);
	}

#if UWP
	/// <summary>
	/// Lets the XAML island process a native keyboard message before dispatch.
	/// </summary>
	/// <param name="message">A pointer to the native MSG structure.</param>
	/// <returns><see langword="true"/> if the message was handled; otherwise, <see langword="false"/>.</returns>
	/// <remarks>
	/// UWP desktop-host scenarios should call this from their native message loop so keyboard
	/// navigation and accelerator processing can reach the hosted XAML island.
	/// </remarks>
	public bool TryPreTranslateMessage(nint message)
	{
		return _host?.TryPreTranslateMessage(message) ?? false;
	}
#endif

	private void PrepareInitialFocus()
	{
		if (_host is null)
		{
			return;
		}

		Presenter.IsTabStop = true;
		_host.NavigateFocus(XamlSourceFocusNavigationReason.Programmatic);

		// Keep the first real child unfocused until the user presses Tab.
		Presenter.Focus(FocusState.Programmatic);
	}

	private void RestartAutoCloseTimer()
	{
		StopAutoCloseTimer();

		if (_disposed || !IsOpen || AutoCloseDelay <= TimeSpan.Zero)
		{
			return;
		}

		_autoCloseTimer = new() { Interval = AutoCloseDelay };
		_autoCloseTimer.Tick += AutoCloseTimer_Tick;
		_autoCloseTimer.Start();
	}

	private void StopAutoCloseTimer()
	{
		if (_autoCloseTimer is null)
		{
			return;
		}

		_autoCloseTimer.Stop();
		_autoCloseTimer.Tick -= AutoCloseTimer_Tick;
		_autoCloseTimer = null;
	}

	private void RestartRestoreActivationTimer()
	{
		StopRestoreActivationTimer();

		if (_disposed || ShouldActivateOnOpen())
		{
			return;
		}

		_restoreActivationTickCount = 0;
		_host?.RestoreActivationState();
		_restoreActivationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
		_restoreActivationTimer.Tick += RestoreActivationTimer_Tick;
		_restoreActivationTimer.Start();
	}

	private void StopRestoreActivationTimer()
	{
		if (_restoreActivationTimer is null)
		{
			return;
		}

		_restoreActivationTimer.Stop();
		_restoreActivationTimer.Tick -= RestoreActivationTimer_Tick;
		_restoreActivationTimer = null;
		_restoreActivationTickCount = 0;
	}

	private bool IsFlyoutElement(DependencyObject element)
	{
		var rootGrid = RootGrid;
		if (rootGrid is null)
		{
			return false;
		}

		if (ReferenceEquals(element, Presenter) || ReferenceEquals(element, rootGrid))
		{
			return true;
		}

		var current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, Presenter) || ReferenceEquals(current, rootGrid))
			{
				return true;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return element is FrameworkElement frameworkElement &&
			rootGrid.XamlRoot is not null &&
			ReferenceEquals(frameworkElement.XamlRoot, rootGrid.XamlRoot);
	}

	private bool CanStartSwipeDismiss()
	{
		return IsSwipeToDismissEnabled &&
			IsOpen &&
			RootGrid is not null &&
			GetSwipeDismissMaxDistance() > 0;
	}

	private void StartSwipeDismiss(PointerRoutedEventArgs e)
	{
		if (RootGrid is null)
		{
			return;
		}

		_swipeDismissPointerId = e.Pointer.PointerId;
		_swipeDismissStartPoint = e.GetCurrentPoint(RootGrid).Position;
		_isSwipeDismissTracking = true;
		_isSwipeDismissDragging = false;
	}

	private bool UpdateSwipeDismiss(PointerRoutedEventArgs e)
	{
		if (!_isSwipeDismissTracking || RootGrid is null || e.Pointer.PointerId != _swipeDismissPointerId)
		{
			return false;
		}

		var currentPoint = e.GetCurrentPoint(RootGrid).Position;
		var deltaX = currentPoint.X - _swipeDismissStartPoint.X;
		var deltaY = currentPoint.Y - _swipeDismissStartPoint.Y;

		if (!TryGetSwipeDismissTranslation(deltaX, deltaY, out var translateX, out var translateY))
		{
			return false;
		}

		if (!_isSwipeDismissDragging)
		{
			_isSwipeDismissDragging = true;
			StopAutoCloseTimer();
			RestorePressedScale();
		}

		if (RootGrid is null)
		{
			return false;
		}

		StopSwipeDismissRestoreAnimation();
		SetRootTranslation(translateX, translateY);
		return true;
	}

	private bool CompleteSwipeDismiss(PointerRoutedEventArgs e)
	{
		if (!_isSwipeDismissTracking || e.Pointer.PointerId != _swipeDismissPointerId)
		{
			return false;
		}

		var shouldDismiss = _isSwipeDismissDragging && GetSwipeDismissDistance() >= GetResolvedSwipeDismissThreshold();
		var shouldRestore = _isSwipeDismissDragging && !shouldDismiss;
		ResetSwipeDismissTracking();

		if (shouldDismiss)
		{
			ResetPressedScale();
			Hide(true);
			return true;
		}

		if (shouldRestore)
		{
			AnimateSwipeDismissRestore();
		}

		return false;
	}

	private void CancelSwipeDismiss()
	{
		var shouldRestore = _isSwipeDismissDragging;
		ResetSwipeDismissTracking();

		if (shouldRestore)
		{
			AnimateSwipeDismissRestore();
		}
	}

	private void ResetSwipeDismissTracking()
	{
		_isSwipeDismissTracking = false;
		_isSwipeDismissDragging = false;
		_swipeDismissPointerId = 0;
	}

	private bool TryGetSwipeDismissTranslation(double deltaX, double deltaY, out double translateX, out double translateY)
	{
		translateX = 0;
		translateY = 0;

		var closedX = GetClosedXOffset(_activePopupDirection);
		if (closedX != 0)
		{
			var primaryDistance = Math.Max(0, Math.Sign(closedX) * deltaX);
			if (!CanStartSwipeDismissDrag(primaryDistance, Math.Abs(deltaY)))
			{
				return false;
			}

			translateX = Math.Sign(closedX) * Math.Min(primaryDistance, Math.Abs(closedX));
			return true;
		}

		var closedY = GetClosedYOffset(_activePopupDirection);
		if (closedY == 0)
		{
			return false;
		}

		var verticalPrimaryDistance = Math.Max(0, Math.Sign(closedY) * deltaY);
		if (!CanStartSwipeDismissDrag(verticalPrimaryDistance, Math.Abs(deltaX)))
		{
			return false;
		}

		translateY = Math.Sign(closedY) * Math.Min(verticalPrimaryDistance, Math.Abs(closedY));
		return true;
	}

	private bool CanStartSwipeDismissDrag(double primaryDistance, double secondaryDistance)
	{
		if (_isSwipeDismissDragging)
		{
			return true;
		}

		if (primaryDistance < SwipeDismissDragStartThreshold)
		{
			return false;
		}

		return primaryDistance >= secondaryDistance * SwipeDismissAxisDominanceRatio;
	}

	private double GetSwipeDismissDistance()
	{
		var closedX = GetClosedXOffset(_activePopupDirection);
		if (closedX != 0)
		{
			return Math.Max(0, Math.Sign(closedX) * GetRootTranslationX());
		}

		var closedY = GetClosedYOffset(_activePopupDirection);
		return closedY == 0
			? 0
			: Math.Max(0, Math.Sign(closedY) * GetRootTranslationY());
	}

	private double GetSwipeDismissMaxDistance()
	{
		var closedX = GetClosedXOffset(_activePopupDirection);
		if (closedX != 0)
		{
			return Math.Abs(closedX);
		}

		return Math.Abs(GetClosedYOffset(_activePopupDirection));
	}

	private double GetResolvedSwipeDismissThreshold()
	{
		if (double.IsNaN(SwipeDismissThreshold) || double.IsInfinity(SwipeDismissThreshold))
		{
			return Math.Min(80.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));
		}

		return Clamp(SwipeDismissThreshold, 1.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));
	}

	private void AnimateSwipeDismissRestore()
	{
		StopSwipeDismissRestoreAnimation();

		if (!AreTransitionAnimationsEnabled)
		{
			SetOpenTransform();
			RestartAutoCloseTimer();
			return;
		}

		_swipeDismissRestoreAnimation = CreateFlyoutTranslationAnimation(_activePopupDirection, opening: true, fromCurrentTransform: true);
		_swipeDismissRestoreAnimation.Completed += SwipeDismissRestoreAnimation_Completed;
		_swipeDismissRestoreAnimation.Start();
	}

	private void StopSwipeDismissRestoreAnimation()
	{
		if (_swipeDismissRestoreAnimation is null)
		{
			return;
		}

		var animation = _swipeDismissRestoreAnimation;
		_swipeDismissRestoreAnimation = null;
		animation.Completed -= SwipeDismissRestoreAnimation_Completed;
		animation.Dispose();
	}

	private void RestorePressedScale()
	{
		if (!_isPressAnimationActive)
		{
			return;
		}

		_isPressAnimationActive = false;
		AnimatePressedScale(1.0D, TimeSpan.FromMilliseconds(240));
	}

	private void ResetPressedScale()
	{
		_isPressAnimationActive = false;
		StopPressedScaleAnimation();
		_pressScaleTargetScale = 1.0D;
		SetRootScale(1.0D, 1.0D);
	}

	private void AnimatePressedScale(double scale, TimeSpan duration)
	{
		if (RootGrid is null)
		{
			return;
		}

		if (!AreSystemAnimationsEnabled)
		{
			StopPressedScaleAnimation();
			SetRootScale(scale, scale);
			_pressScaleTargetScale = scale;
			return;
		}

		var currentScaleX = _pressScaleAnimation is null ? GetRootScaleX() : _pressScaleTargetScale;
		var currentScaleY = _pressScaleAnimation is null ? GetRootScaleY() : _pressScaleTargetScale;

		StopPressedScaleAnimation();
		_pressScaleTargetScale = scale;
		SetRootScale(currentScaleX, currentScaleY);
		var animation = CompositionTransitionHelpers.CreateScaleAnimation(
			RootGrid,
			new Vector3((float)currentScaleX, (float)currentScaleY, 1),
			new Vector3((float)scale, (float)scale, 1),
			duration);
		_pressScaleAnimation = animation;
		animation.Completed += PressScaleAnimation_Completed;
		animation.Start();
	}

	private void StopPressedScaleAnimation()
	{
		if (_pressScaleAnimation is null)
		{
			return;
		}

		var animation = _pressScaleAnimation;
		_pressScaleAnimation = null;
		animation.Completed -= PressScaleAnimation_Completed;
		animation.Dispose();
	}

	private bool IsPressedScaleEnabled()
	{
		return AreSystemAnimationsEnabled && Math.Abs(GetResolvedPressedScale() - 1.0D) > 0.001D;
	}

	private double GetResolvedPressedScale()
	{
		if (double.IsNaN(Presenter.PressedScale) || double.IsInfinity(Presenter.PressedScale))
		{
			return 1.0D;
		}

		return Clamp(Presenter.PressedScale, 0.1D, 2.0D);
	}

	private void UpdateFocusSuppression()
	{
		if (ActivationMode is DesktopFlyoutActivationMode.NeverActivate)
		{
			SuppressFocus();
		}
		else
		{
			RestoreFocusSuppression();
		}
	}

	private void SuppressFocus()
	{
		if (RootGrid is null)
		{
			return;
		}

		RestoreFocusSuppression();
		SuppressFocus(RootGrid);
	}

	private void SuppressFocus(DependencyObject element)
	{
		if (element is Control control)
		{
			_suppressedTabStopStates.Add((control, control.IsTabStop));
			control.IsTabStop = false;
		}

		if (element is FrameworkElement frameworkElement)
		{
			_suppressedInteractionFocusStates.Add((frameworkElement, frameworkElement.AllowFocusOnInteraction));
			frameworkElement.AllowFocusOnInteraction = false;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (var index = 0; index < childCount; index++)
		{
			SuppressFocus(VisualTreeHelper.GetChild(element, index));
		}
	}

	private void RestoreFocusSuppression()
	{
		foreach (var (element, allowFocusOnInteraction) in _suppressedInteractionFocusStates)
		{
			element.AllowFocusOnInteraction = allowFocusOnInteraction;
		}

		foreach (var (control, isTabStop) in _suppressedTabStopStates)
		{
			control.IsTabStop = isTabStop;
		}

		_suppressedInteractionFocusStates.Clear();
		_suppressedTabStopStates.Clear();
	}
}
