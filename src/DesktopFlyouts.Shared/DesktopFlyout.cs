// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

#if UWP
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
#endif

namespace DesktopFlyouts;

/// <summary>
/// Displays islands in an independent desktop window with library-owned chrome.
/// </summary>
/// <remarks>
/// Construct and operate the flyout on its UI thread. The native host is created on the
/// first successful show request. Dispose the flyout to release its host and connected content.
/// </remarks>
[ContentProperty(Name = nameof(Islands))]
public partial class DesktopFlyout : DependencyObject, IDisposable
{
	private const string PART_RootGrid = "PART_RootGrid";

	private XamlIslandHostWindow? _host;
#if !HAS_UNO
	// Keep the native viewport monitor-sized; only content and the input region change.
	private readonly Canvas _hostCanvas;
#endif
#if (UWP || WASDK) && !HAS_UNO
	private readonly UISettings _uiSettings;
	private bool _systemAnimationsEnabled;
	private bool _isAnimationsEnabledChangedSubscribed;
#endif
	private bool _disposed;

	private readonly DesktopFlyoutLayoutPanel _hostContainer;
	private readonly DesktopFlyoutIslandCollection _islands;
	private Grid? RootGrid;

	internal DesktopFlyoutPresenter Presenter { get; }

	/// <summary>
	/// Gets the current lifecycle phase of the flyout.
	/// </summary>
	/// <value>The current opening, open, closing, or closed state.</value>
	/// <remarks>
	/// Unlike <see cref="IsOpen"/>, this property also distinguishes transitions in progress.
	/// </remarks>
	public DesktopFlyoutTransitionState TransitionState { get; private set; } = DesktopFlyoutTransitionState.Closed;

	/// <summary>Occurs when the flyout lifecycle state changes.</summary>
	public event EventHandler? TransitionStateChanged;

	/// <summary>Gets the directly supplied island definitions, UI elements, or data items.</summary>
	/// <remarks>Mutation is prohibited while IslandsSource is non-null.</remarks>
	public IList<object> Islands => _islands;

#if UWP
	internal bool IsHostBackdropBrushEnabled => _host?.IsHostBackdropBrushEnabled == true;
#endif

	/// <summary>Initializes a flyout without creating a native window.</summary>
	public DesktopFlyout()
	{
		_islands = new DesktopFlyoutIslandCollection(this);
		Presenter = new DesktopFlyoutPresenter(this);
		_hostContainer = new DesktopFlyoutLayoutPanel(Presenter);

#if !HAS_UNO
		_hostCanvas = new Canvas();
		_hostCanvas.Children.Add(_hostContainer);

#endif
		Presenter.TemplateChanged += Presenter_TemplateChanged;
		Presenter.LayoutInvalidated += Presenter_LayoutInvalidated;

#if (UWP || WASDK) && !HAS_UNO
		_uiSettings = new UISettings();
		_systemAnimationsEnabled = _uiSettings.AnimationsEnabled;
		if (ApiInformation.IsEventPresent("Windows.UI.ViewManagement.UISettings", "AnimationsEnabledChanged"))
		{
			_uiSettings.AnimationsEnabledChanged += UISettings_AnimationsEnabledChanged;
			_isAnimationsEnabledChangedSubscribed = true;
		}
#endif
	}

	private void EnsureHost()
	{
		if (_host is not null)
		{
			return;
		}

		_hostContainer.AttachContent();
		try
		{
			_host = new XamlIslandHostWindow();
#if !HAS_UNO
			_host.SetContent(_hostCanvas);
#else
			_host.SetContent(_hostContainer);
#endif
		}
		catch
		{
			_host?.Dispose();
			_host = null;
			_hostContainer.DetachContent();
			throw;
		}

		_ = _host.UpdateWindowVisibility(false);
		_host.WindowInactivated += HostWindow_Inactivated;
		_host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
#if !HAS_UNO
		_host.DisplayAreaChanged += HostWindow_DisplayAreaChanged;
#endif

		Presenter.ApplyTemplate();
		Presenter_TemplateChanged(Presenter, EventArgs.Empty);
		Presenter.UpdateFlyoutTheme(forceBackdropUpdate: true);
	}

	private void DetachRootHandlers()
	{
		RootGrid?.GettingFocus -= RootGrid_GettingFocus;
		RootGrid?.SizeChanged -= RootGrid_SizeChanged;
		RootGrid?.PointerPressed -= RootGrid_PointerPressed;
		RootGrid?.PointerMoved -= RootGrid_PointerMoved;
		RootGrid?.PointerReleased -= RootGrid_PointerReleased;
		RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
		RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
		RootGrid?.PointerExited -= RootGrid_PointerExited;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		CancelOpenFlyoutLayoutUpdate();
		StopTransitionAnimation();
		StopSwipeDismissRestoreAnimation();
		ResetSwipeDismissTracking();
		ResetPressedScale();
		SetOpenTransform();
		_isPopupAnimationPlaying = false;
		_hideAfterOpen = false;
		StopEnvironmentMonitoring();
		_requestedPosition = null;
		StopAutoCloseTimer();
		StopRestoreActivationTimer();
#if !HAS_UNO
		ReleaseTaskbarFlyoutVisibilityLease();
#endif
		RestoreFocusSuppression();

		_host?.WindowInactivated -= HostWindow_Inactivated;
		_host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
#if !HAS_UNO
		_host?.DisplayAreaChanged -= HostWindow_DisplayAreaChanged;
#endif
#if (UWP || WASDK) && !HAS_UNO
		if (_isAnimationsEnabledChangedSubscribed)
		{
			_uiSettings.AnimationsEnabledChanged -= UISettings_AnimationsEnabledChanged;
		}
#endif
		DetachRootHandlers();
		Presenter.TemplateChanged -= Presenter_TemplateChanged;
		Presenter.LayoutInvalidated -= Presenter_LayoutInvalidated;
		Presenter.ReleasePresentationResources();
		_hostContainer.DetachContent();
		Presenter.ClearItems();

		if (_isFocusManagerGettingFocusSubscribed)
		{
			FocusManager.GettingFocus -= FocusManager_GettingFocus;
			_isFocusManagerGettingFocusSubscribed = false;
		}
		_host?.Dispose();
		_host = null;
		RootGrid = null;
		IsOpen = false;
		_showAfterClose = false;
		_showAfterClosePosition = null;
		SetTransitionState(DesktopFlyoutTransitionState.Closed);

		GC.SuppressFinalize(this);
	}
}
