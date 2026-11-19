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
	private void Presenter_TemplateChanged(object? sender, EventArgs e)
	{
		if (_disposed || ReferenceEquals(RootGrid, Presenter.RootGrid))
		{
			return;
		}

		var transition = TransitionState;
		var wasAnimating = IsTransitionAnimationRunning;
		StopTransitionAnimation();
		StopSwipeDismissRestoreAnimation();
		RestoreFocusSuppression();
		ResetPressedScale();
		ResetSwipeDismissTracking();
		DetachRootHandlers();
		RootGrid = Presenter.RootGrid;
		if (RootGrid is null)
		{
			return;
		}

		RootGrid.SizeChanged += RootGrid_SizeChanged;
		UpdateRootScaleCenterPoint();
		RootGrid.GettingFocus += RootGrid_GettingFocus;
		RootGrid.PointerPressed += RootGrid_PointerPressed;
		RootGrid.PointerMoved += RootGrid_PointerMoved;
		RootGrid.PointerReleased += RootGrid_PointerReleased;
		RootGrid.PointerCanceled += RootGrid_PointerCanceled;
		RootGrid.PointerCaptureLost += RootGrid_PointerCaptureLost;
		RootGrid.PointerExited += RootGrid_PointerExited;
		if (!_isFocusManagerGettingFocusSubscribed)
		{
			FocusManager.GettingFocus += FocusManager_GettingFocus;
			_isFocusManagerGettingFocusSubscribed = true;
		}

		if (transition is DesktopFlyoutTransitionState.Closing)
		{
			CompleteClose();
		}
		else if (transition is DesktopFlyoutTransitionState.Open)
		{
			SetOpenTransform();
		}
		else if (transition is DesktopFlyoutTransitionState.Opening && wasAnimating)
		{
			CompleteOpen();
		}
		UpdateOpenFlyoutLayout();
	}

	private void Presenter_LayoutInvalidated(object? sender, EventArgs e)
	{
		if (!_isUpdatingOpenFlyoutLayout)
		{
			_layoutUpdatePending = true;
			UpdateOpenFlyoutLayout();
		}
	}

	private void OpenFlyoutLayout_Rendering(object? sender, object e)
	{
		CancelOpenFlyoutLayoutUpdate();

		if (_disposed ||
			!IsOpen ||
			_isPopupAnimationPlaying ||
			_isUpdatingOpenFlyoutLayout ||
			RootGrid is null ||
			_host?.DesktopWindowXamlSource is null)
		{
			return;
		}

		_isUpdatingOpenFlyoutLayout = true;

		try
		{
			RefreshPlacement();
#if !HAS_UNO
			UpdateHostBounds(false);
			PrepareHostContainerLayout();
#endif
			MeasureFlyoutLayout();
			_layoutUpdatePending = false;
			UpdateFocusSuppression();
			_activePopupDirection = UpdateFlyoutRegion();
			SetOpenTransform();
		}
		finally
		{
			_isUpdatingOpenFlyoutLayout = false;
		}

		if (_placementUpdatePending || _layoutUpdatePending)
		{
			UpdateOpenFlyoutLayout();
		}
	}

	private void OpenCompositionAnimation_Completed(object? sender, EventArgs e)
	{
		if (sender is not CompositionAnimationOperation animation || !ReferenceEquals(_transitionAnimation, animation))
		{
			return;
		}

		animation.Completed -= OpenCompositionAnimation_Completed;
		_transitionAnimation = null;
		animation.Dispose();
		if (!_disposed)
		{
			CompleteOpen();
		}
	}

	private void CloseCompositionAnimation_Completed(object? sender, EventArgs e)
	{
		if (sender is not CompositionAnimationOperation animation || !ReferenceEquals(_transitionAnimation, animation))
		{
			return;
		}

		animation.Completed -= CloseCompositionAnimation_Completed;
		_transitionAnimation = null;
		animation.Dispose();
		if (!_disposed)
		{
			CompleteClose();
		}
	}

	private void AutoCloseTimer_Tick(object? sender, object e)
	{
		StopAutoCloseTimer();

		if (IsOpen && !_isPopupAnimationPlaying)
		{
			Hide();
		}
	}

	private void RestoreActivationTimer_Tick(object? sender, object e)
	{
		_host?.RestoreActivationState();
		_restoreActivationTickCount++;

		if (_restoreActivationTickCount >= 8)
		{
			StopRestoreActivationTimer();
		}
	}

	private void RootGrid_GettingFocus(UIElement sender, GettingFocusEventArgs args)
	{
		if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate)
		{
			return;
		}

		args.Cancel = true;
		args.Handled = true;
		_host?.RestoreActivationState();
	}

	private void FocusManager_GettingFocus(object? sender, GettingFocusEventArgs args)
	{
		if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate || args.NewFocusedElement is not DependencyObject newFocusedElement)
		{
			return;
		}

		if (!IsFlyoutElement(newFocusedElement))
		{
			return;
		}

		args.Cancel = true;
		args.Handled = true;
		_host?.RestoreActivationState();
	}

	private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (RootGrid is null || _isPopupAnimationPlaying)
		{
			return;
		}

		var isPressedScaleEnabled = IsPressedScaleEnabled();
		var isSwipeDismissEnabled = CanStartSwipeDismiss();
		if (!isPressedScaleEnabled && !isSwipeDismissEnabled)
		{
			return;
		}

		StopSwipeDismissRestoreAnimation();
		RootGrid.CapturePointer(e.Pointer);

		if (isSwipeDismissEnabled)
		{
			StartSwipeDismiss(e);
		}

		if (isPressedScaleEnabled)
		{
			_isPressAnimationActive = true;
			AnimatePressedScale(GetResolvedPressedScale(), TimeSpan.FromMilliseconds(110));
		}
	}

	private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (UpdateSwipeDismiss(e))
		{
			e.Handled = true;
		}
	}

	private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		var dismissedBySwipe = CompleteSwipeDismiss(e);

		if (RootGrid is not null)
		{
			RootGrid.ReleasePointerCapture(e.Pointer);
		}

		if (!dismissedBySwipe)
		{
			RestorePressedScale();
		}
	}

	private void RootGrid_PointerCanceled(object sender, PointerRoutedEventArgs e)
	{
		CancelSwipeDismiss();
		RestorePressedScale();
	}

	private void RootGrid_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		CancelSwipeDismiss();
		RestorePressedScale();
	}

	private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (!_isSwipeDismissDragging)
		{
			RestorePressedScale();
		}
	}

	private void SwipeDismissRestoreAnimation_Completed(object? sender, EventArgs e)
	{
		if (sender is not CompositionAnimationOperation animation || !ReferenceEquals(_swipeDismissRestoreAnimation, animation))
		{
			return;
		}

		animation.Completed -= SwipeDismissRestoreAnimation_Completed;
		_swipeDismissRestoreAnimation = null;
		animation.Dispose();
		if (!_disposed)
		{
			SetOpenTransform();
			RestartAutoCloseTimer();
		}
	}

	private void PressScaleAnimation_Completed(object? sender, EventArgs e)
	{
		if (sender is not CompositionAnimationOperation animation || !ReferenceEquals(_pressScaleAnimation, animation))
		{
			return;
		}

		animation.Completed -= PressScaleAnimation_Completed;
		_pressScaleAnimation = null;
		animation.Dispose();
		if (!_disposed)
		{
			SetRootScale(_pressScaleTargetScale, _pressScaleTargetScale);
		}
	}

	private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateRootScaleCenterPoint();
	}

	private void EnvironmentTimer_Tick(object? sender, object e)
	{
		if (!_disposed && RefreshPlacement(apply: false))
		{
			InvalidatePlacement();
		}
	}

	private void HostWindow_Inactivated(object? sender, EventArgs e)
	{
		if (IsLightDismissEnabled)
		{
			Hide();
		}
	}

	private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
	{
		if (_disposed)
		{
			return;
		}

#if (UWP || WASDK) && !HAS_UNO
		_systemAnimationsEnabled = _uiSettings.AnimationsEnabled;
#endif
		Presenter.UpdateFlyoutTheme(forceBackdropUpdate: true);
	}

#if !HAS_UNO
	private void OnTaskbarOverflowFlyoutClosed()
	{
		_taskbarOverflowFlyoutMonitor = null;
		InvalidatePlacement();
	}

	private void HostWindow_DisplayAreaChanged(object? sender, EventArgs e)
	{
		InvalidatePlacement();
	}
#endif

#if (UWP || WASDK) && !HAS_UNO
	private void UISettings_AnimationsEnabledChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
	{
		void UpdateSystemAnimationsEnabled()
		{
			if (!_disposed)
			{
				_systemAnimationsEnabled = sender.AnimationsEnabled;
			}
		}

#if UWP
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateSystemAnimationsEnabled);
#elif WASDK
		DispatcherQueue.TryEnqueue(UpdateSystemAnimationsEnabled);
#endif
	}
#endif
}
