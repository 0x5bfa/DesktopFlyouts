// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Graphics;
using FoundationPoint = Windows.Foundation.Point;
using FoundationSize = Windows.Foundation.Size;

#if (UWP || WASDK) && !HAS_UNO
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
#endif

#if WASDK && !HAS_UNO
using Microsoft.UI.Composition.SystemBackdrops;
#endif

#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#endif

namespace DesktopFlyouts;

/// <summary>
/// Displays a desktop flyout in an independent XAML island window.
/// </summary>
/// <remarks>
/// Use <see cref="DesktopFlyout"/> when you need a lightweight desktop surface that can be opened
/// from a tray icon or from application code. Add one or more <see cref="DesktopFlyoutIsland"/>
/// instances to define the visible sections, then call <see cref="Show(DesktopFlyoutPosition)"/>
/// to display it. Dispose the instance when it is no longer used
/// so the underlying host window and XAML island can be released.
/// </remarks>
[ContentProperty(Name = nameof(Islands))]
public partial class DesktopFlyout : Control, IDisposable
{
	private const string PART_RootGrid = "PART_RootGrid";
	private const string PART_IslandsItemsControl = "PART_IslandsItemsControl";
	private const double SwipeDismissDragStartThreshold = 4.0D;
	private const double SwipeDismissAxisDominanceRatio = 1.2D;

	private readonly XamlIslandHostWindow? _host;
#if !HAS_UNO
	// Keep the native viewport monitor-sized; only content and the input region change.
	private readonly Canvas _hostCanvas;
	private readonly Grid _hostContainer;
	private Rectangle _hostBounds;
	private TaskbarFlyoutVisibilityLease? _taskbarFlyoutVisibilityLease;
	private TaskbarOverflowFlyoutMonitor? _taskbarOverflowFlyoutMonitor;
	private TaskbarLayoutMonitor? _taskbarLayoutMonitor;
	private List<Windows.Win32.Foundation.HWND> _barWindows = [];
#endif
#if (UWP || WASDK) && !HAS_UNO
	private readonly UISettings _uiSettings;
	private bool _systemAnimationsEnabled;
	private bool _isAnimationsEnabledChangedSubscribed;
#endif
	private bool _isPopupAnimationPlaying;
	private bool _hideAfterOpen;
	private bool _showAfterClose;
	private DesktopFlyoutPosition? _showAfterClosePosition;
	private bool _isPressAnimationActive;
	private DesktopFlyoutPosition? _position;
	private DesktopFlyoutPosition? _requestedPosition;
	private Rectangle? _lastTrayIconBounds;
	private Rectangle _monitorReferenceBounds;
	private double _lastLayoutScale;
	private bool _placementUpdatePending;
	private DispatcherTimer? _environmentTimer;
	private Rectangle _activeWorkArea;
	private DesktopFlyoutPopupDirection _activePopupDirection = DesktopFlyoutPopupDirection.BottomToTop;
	private DispatcherTimer? _autoCloseTimer;
	private DispatcherTimer? _restoreActivationTimer;
	private Storyboard? _transitionStoryboard;
	private Storyboard? _pressScaleStoryboard;
	private Storyboard? _swipeDismissRestoreStoryboard;
	private FoundationPoint _swipeDismissStartPoint;
	private double _pressScaleTargetScale = 1.0D;
	private uint _swipeDismissPointerId;
	private int _restoreActivationTickCount;
	private readonly List<(Control Control, bool IsTabStop)> _suppressedTabStopStates = [];
	private readonly List<(FrameworkElement Element, bool AllowFocusOnInteraction)> _suppressedInteractionFocusStates = [];
	private bool _isSwipeDismissTracking;
	private bool _isSwipeDismissDragging;
	private bool _isFocusManagerGettingFocusSubscribed;
	private bool _isUpdatingOpenFlyoutLayout;
	private bool _isOpenFlyoutLayoutUpdateQueued;
	private bool _disposed;

	private Grid? RootGrid;
	private ItemsControl? IslandsItemsControl;

	/// <summary>
	/// Initializes a new instance of <see cref="DesktopFlyout"/>.
	/// </summary>
	/// <remarks>
	/// The constructor creates the hidden desktop host window used to display the flyout.
	/// </remarks>
	public DesktopFlyout()
	{
		DefaultStyleKey = typeof(DesktopFlyout);

		_host = new XamlIslandHostWindow();
#if !HAS_UNO
		_hostCanvas = new Canvas();
		_hostContainer = new Grid();
		_hostContainer.Children.Add(this);
		_hostCanvas.Children.Add(_hostContainer);
		_host.SetContent(_hostCanvas);
#else
		_host.SetContent(this);
#endif
		_ = _host.UpdateWindowVisibility(false);
#if (UWP || WASDK) && !HAS_UNO
		_uiSettings = new UISettings();
		_systemAnimationsEnabled = _uiSettings.AnimationsEnabled;
		if (ApiInformation.IsEventPresent("Windows.UI.ViewManagement.UISettings", "AnimationsEnabledChanged"))
		{
			_uiSettings.AnimationsEnabledChanged += UISettings_AnimationsEnabledChanged;
			_isAnimationsEnabledChangedSubscribed = true;
		}
#endif
#if HAS_UNO
		if (HideOnLostFocus)
			_host.WindowInactivated += HostWindow_Inactivated;
#else
		_host.WindowInactivated += HostWindow_Inactivated;
#endif
		_host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
#if !HAS_UNO
		_host.DisplayAreaChanged += HostWindow_DisplayAreaChanged;
#endif
	}

	/// <inheritdoc/>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		RootGrid?.GettingFocus -= RootGrid_GettingFocus;
		RootGrid?.PointerPressed -= RootGrid_PointerPressed;
		RootGrid?.PointerMoved -= RootGrid_PointerMoved;
		RootGrid?.PointerReleased -= RootGrid_PointerReleased;
		RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
		RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
		RootGrid?.PointerExited -= RootGrid_PointerExited;
		if (IslandsItemsControl is not null)
			IslandsItemsControl.ItemsSource = null;

		RootGrid = GetTemplateChild(PART_RootGrid) as Grid
			?? throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(DesktopFlyout)}'s style.");
		IslandsItemsControl = GetTemplateChild(PART_IslandsItemsControl) as ItemsControl
			?? throw new MissingFieldException($"Could not find {PART_IslandsItemsControl} in the given {nameof(DesktopFlyout)}'s style.");
		IslandsItemsControl.ItemsSource = Islands;

		EnsureRootTransform();

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

		UpdateIslands();
	}

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

		if (_disposed || _host?.DesktopWindowXamlSource is null || RootGrid is null)
		{
			return;
		}

		if (_isPopupAnimationPlaying)
		{
			if (TransitionState is DesktopFlyoutTransitionState.Opening)
				_hideAfterOpen = false;
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
			return;

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
			_host.PreserveActivationState();

		_host.SetActivationMode(ActivationMode);
#if !HAS_UNO
		UpdateHostBounds(shouldActivateOnOpen);
		PrepareHostContainerLayout();
#else
		_host.Maximize(_activeWorkArea, shouldActivateOnOpen);
#endif

#if UWP
		_ = RootGrid.Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, async () =>
#elif WASDK
		RootGrid.DispatcherQueue.TryEnqueue(async () =>
#endif
		{
			if (_disposed)
				return;

			RefreshPlacement();
#if !HAS_UNO
			UpdateHostBounds(false);
			PrepareHostContainerLayout();
#endif
			ResetResolvedFlyoutSize();
			UpdateLayout();
			await Task.Delay(1);
			if (_disposed)
				return;

			ApplyResolvedFlyoutSize();

			UpdateLayout();
			await Task.Delay(1);
			if (_disposed)
				return;

			UpdateFlyoutTheme();
			UpdateFocusSuppression();
			_activePopupDirection = UpdateFlyoutRegion(shouldActivateOnOpen);

			// Ensure to hide first
			SetClosedTransform(_activePopupDirection);

			UpdateLayout();
			await Task.Delay(1);
			if (_disposed)
				return;

			await _host.UpdateWindowVisibility(true, shouldActivateOnOpen);
			if (_disposed)
				return;

			if (!shouldActivateOnOpen)
				RestartRestoreActivationTimer();

			if (AreTransitionAnimationsEnabled)
			{
				var storyboard = GetOpenStoryboard(_activePopupDirection);
				_transitionStoryboard = storyboard;
				storyboard.Completed += OpenAnimationStoryboard_Completed;
				storyboard.Begin();
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

#if WASDK && !HAS_UNO
	internal SystemBackdrop? CreateIslandSystemBackdrop(ElementTheme islandTheme, SystemBackdrop? currentBackdrop)
	{
		if (BackdropKind is DesktopFlyoutBackdropKind.None)
			return null;

		var useLightTheme = islandTheme switch
		{
			ElementTheme.Light => true,
			ElementTheme.Dark => false,
			_ => GeneralHelpers.IsTaskbarLight(),
		};

		var useMica = BackdropKind is DesktopFlyoutBackdropKind.Mica;
		if (currentBackdrop is DesktopFlyoutSystemBackdrop backdrop
			&& backdrop.UseLightTheme == useLightTheme
			&& (useMica ? backdrop is DesktopFlyoutMicaBackdrop : backdrop is DesktopFlyoutAcrylicBackdrop))
		{
			return backdrop;
		}

		return BackdropKind switch
		{
			DesktopFlyoutBackdropKind.Mica => new DesktopFlyoutMicaBackdrop(useLightTheme),
			_ => new DesktopFlyoutAcrylicBackdrop(useLightTheme),
		};
	}
#endif

	private void Hide(bool closeFromCurrentTransform)
	{
		StopAutoCloseTimer();
		StopRestoreActivationTimer();
		StopSwipeDismissRestoreStoryboard();
		ResetSwipeDismissTracking();

		if (_disposed || RootGrid is null)
			return;

		if (_isPopupAnimationPlaying)
		{
			if (TransitionState is DesktopFlyoutTransitionState.Opening)
				_hideAfterOpen = true;
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
			SetOpenTransform();

		if (AreTransitionAnimationsEnabled)
		{
			var storyboard = GetCloseStoryboard(_activePopupDirection, closeFromCurrentTransform);
			_transitionStoryboard = storyboard;
			storyboard.Completed += CloseAnimationStoryboard_Completed;
			storyboard.Begin();
		}
		else
		{
			CompleteClose();
		}
	}

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

	private void UpdateFlyoutTheme(bool forceBackdropUpdate = false)
	{
		var requestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
		RequestedTheme = requestedTheme;

		foreach (var island in Islands)
			island.RequestedTheme = requestedTheme;

		UpdateIslandBackdrops(forceBackdropUpdate);
	}

	private void UpdateIslandBackdrops(bool forceUpdate = false)
	{
#if WASDK && !HAS_UNO
		foreach (var island in Islands)
			island.UpdateOwnerBackdrop(forceUpdate);
#elif UWP
		foreach (var island in Islands)
			island.UpdateOwnerBackdrop(forceUpdate);
#endif
	}

#if UWP
	internal bool IsHostBackdropBrushEnabled => _host?.IsHostBackdropBrushEnabled == true;
#endif

	private void UpdateIslands()
	{
		if (IslandsItemsControl is null)
			return;

		for (var index = 0; index < Islands.Count; index++)
		{
			if (Islands[index] is DesktopFlyoutIsland island)
				island.SetOwner(this);
		}

		IslandsItemsControl.InvalidateMeasure();
	}

	private DesktopFlyoutPopupDirection UpdateFlyoutRegion(bool activateHost = false)
	{
		if (_host?.DesktopWindowXamlSource is null || IslandsItemsControl is null)
			return _activePopupDirection;

#if !HAS_UNO
		UpdateHostBounds(activateHost);
#endif
		var scale = _host.XamlIslandRasterizationScale;
		var flyoutWidth = GetCurrentFlyoutWidth();
		var flyoutHeight = GetCurrentFlyoutHeight();
		var scaledFlyoutSize = new FoundationSize(flyoutWidth * scale, flyoutHeight * scale);
		var scaledMargin = GetScaledMargin(Margin, scale);
		var frameWidth = scaledFlyoutSize.Width + scaledMargin.Left + scaledMargin.Right;
		var frameHeight = scaledFlyoutSize.Height + scaledMargin.Top + scaledMargin.Bottom;
		var workArea = _activeWorkArea;
		var hostWidth = workArea.Width;
		var hostHeight = workArea.Height;
		var regionWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(frameWidth, hostWidth)));
		var regionHeight = Math.Max(1, (int)Math.Ceiling(Math.Min(frameHeight, hostHeight)));
		var resolvedPosition = DesktopFlyoutPositionResolver.Resolve(
			_position ?? DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight),
			workArea,
			new Size(regionWidth, regionHeight));
		var bounds = resolvedPosition.Bounds;

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
#endif

		return resolvedPosition.TransitionDirection;
	}

	internal void OnIslandSizeChanged()
	{
		UpdateIslands();
		UpdateOpenFlyoutLayout();
	}

	internal void OnIslandVisibilityChanged()
	{
		UpdateIslands();
		UpdateOpenFlyoutLayout();
	}

	internal void OnIslandLayoutChanged()
	{
		UpdateOpenFlyoutLayout();
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
			return;

		CompositionTarget.Rendering -= OpenFlyoutLayout_Rendering;
		_isOpenFlyoutLayoutUpdateQueued = false;
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
			ApplyResolvedFlyoutSize();
			MeasureRootGridForAvailableFlyoutSize();
			UpdateLayout();
			_activePopupDirection = UpdateFlyoutRegion();
			SetOpenTransform();
		}
		finally
		{
			_isUpdatingOpenFlyoutLayout = false;
		}

		if (_placementUpdatePending)
			UpdateOpenFlyoutLayout();
	}

	private void ResetResolvedFlyoutSize()
	{
		if (RootGrid is null)
			return;

		RootGrid.Width = double.NaN;
		RootGrid.Height = double.NaN;
	}

	private void ApplyResolvedFlyoutSize()
	{
		if (RootGrid is null || _host is null)
			return;

		var (availableWidth, availableHeight) = GetAvailableFlyoutSizeInDips();
		RootGrid.Width = ResolveFlyoutLength(FlyoutWidth, availableWidth, HasStarIslandWidth());
		RootGrid.Height = ResolveFlyoutLength(FlyoutHeight, availableHeight, HasStarIslandHeight());
	}

	private void MeasureRootGridForAvailableFlyoutSize()
	{
		if (RootGrid is null)
			return;

		var (availableWidth, availableHeight) = GetAvailableFlyoutSizeInDips();
		RootGrid.Measure(new(availableWidth, availableHeight));
	}

	private (double Width, double Height) GetAvailableFlyoutSizeInDips()
	{
		if (_host is null)
			return (0, 0);

		var scale = _host.XamlIslandRasterizationScale;
		var workArea = _activeWorkArea;
		var availableWidth = (workArea.Width / scale) - Margin.Left - Margin.Right;
		var availableHeight = (workArea.Height / scale) - Margin.Top - Margin.Bottom;

		return (Math.Max(0, availableWidth), Math.Max(0, availableHeight));
	}

	private bool HasStarIslandWidth()
	{
		foreach (var item in Islands)
		{
			if (item is DesktopFlyoutIsland island && IsIslandVisible(island) && island.IslandWidth.IsStar)
				return true;
		}

		return false;
	}

	private bool HasStarIslandHeight()
	{
		foreach (var item in Islands)
		{
			if (item is DesktopFlyoutIsland island && IsIslandVisible(island) && island.IslandHeight.IsStar)
				return true;
		}

		return false;
	}

	private static bool IsIslandVisible(DesktopFlyoutIsland island)
	{
		return island.Visibility is Visibility.Visible;
	}

	private void OpenAnimationStoryboard_Completed(object? sender, object e)
	{
		if (sender is not Storyboard storyboard || !ReferenceEquals(_transitionStoryboard, storyboard))
			return;

		storyboard.Completed -= OpenAnimationStoryboard_Completed;
		_transitionStoryboard = null;
		storyboard.Stop();
		if (_disposed)
			return;

		CompleteOpen();
	}

	private void CloseAnimationStoryboard_Completed(object? sender, object e)
	{
		if (sender is not Storyboard storyboard || !ReferenceEquals(_transitionStoryboard, storyboard))
			return;

		storyboard.Completed -= CloseAnimationStoryboard_Completed;
		_transitionStoryboard = null;
		storyboard.Stop();
		if (_disposed)
			return;

		CompleteClose();
	}

	private void StopTransitionStoryboard()
	{
		if (_transitionStoryboard is null)
			return;

		var storyboard = _transitionStoryboard;
		_transitionStoryboard = null;
		storyboard.Completed -= OpenAnimationStoryboard_Completed;
		storyboard.Completed -= CloseAnimationStoryboard_Completed;
		storyboard.Stop();
	}

	private void CompleteOpen()
	{
		StopSwipeDismissRestoreStoryboard();
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
			PrepareInitialFocus();
		else
			RestartRestoreActivationTimer();
		RestartAutoCloseTimer();
		SetTransitionState(DesktopFlyoutTransitionState.Open);
		if (_placementUpdatePending)
			UpdateOpenFlyoutLayout();
	}

	private void CompleteClose()
	{
		CancelOpenFlyoutLayoutUpdate();
		StopEnvironmentMonitoring();
		_requestedPosition = null;
		_lastTrayIconBounds = null;
		_placementUpdatePending = false;
		StopAutoCloseTimer();
		StopSwipeDismissRestoreStoryboard();
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
			QueueShowAfterClose();
	}

	private void PrepareInitialFocus()
	{
		if (_host is null)
			return;

		IsTabStop = true;
		_host.NavigateFocus(XamlSourceFocusNavigationReason.Programmatic);

		// Keep the first real child unfocused until the user presses Tab.
		Focus(FocusState.Programmatic);
	}

	private void RestartAutoCloseTimer()
	{
		StopAutoCloseTimer();

		if (_disposed || !IsOpen || AutoCloseDelay <= TimeSpan.Zero)
			return;

		_autoCloseTimer = new() { Interval = AutoCloseDelay };
		_autoCloseTimer.Tick += AutoCloseTimer_Tick;
		_autoCloseTimer.Start();
	}

	private void StopAutoCloseTimer()
	{
		if (_autoCloseTimer is null)
			return;

		_autoCloseTimer.Stop();
		_autoCloseTimer.Tick -= AutoCloseTimer_Tick;
		_autoCloseTimer = null;
	}

	private void RestartRestoreActivationTimer()
	{
		StopRestoreActivationTimer();

		if (_disposed || ShouldActivateOnOpen())
			return;

		_restoreActivationTickCount = 0;
		_host?.RestoreActivationState();
		_restoreActivationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
		_restoreActivationTimer.Tick += RestoreActivationTimer_Tick;
		_restoreActivationTimer.Start();
	}

	private void StopRestoreActivationTimer()
	{
		if (_restoreActivationTimer is null)
			return;

		_restoreActivationTimer.Stop();
		_restoreActivationTimer.Tick -= RestoreActivationTimer_Tick;
		_restoreActivationTimer = null;
		_restoreActivationTickCount = 0;
	}

	private void AutoCloseTimer_Tick(object? sender, object e)
	{
		StopAutoCloseTimer();

		if (IsOpen && !_isPopupAnimationPlaying)
			Hide();
	}

	private void RestoreActivationTimer_Tick(object? sender, object e)
	{
		_host?.RestoreActivationState();
		_restoreActivationTickCount++;

		if (_restoreActivationTickCount >= 8)
			StopRestoreActivationTimer();
	}

	private void RootGrid_GettingFocus(UIElement sender, GettingFocusEventArgs args)
	{
		if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate)
			return;

		args.Cancel = true;
		args.Handled = true;
		_host?.RestoreActivationState();
	}

	private void FocusManager_GettingFocus(object? sender, GettingFocusEventArgs args)
	{
		if (ActivationMode is not DesktopFlyoutActivationMode.NeverActivate || args.NewFocusedElement is not DependencyObject newFocusedElement)
			return;

		if (!IsFlyoutElement(newFocusedElement))
			return;

		args.Cancel = true;
		args.Handled = true;
		_host?.RestoreActivationState();
	}

	private bool IsFlyoutElement(DependencyObject element)
	{
		var rootGrid = RootGrid;
		if (rootGrid is null)
			return false;

		if (ReferenceEquals(element, this) || ReferenceEquals(element, rootGrid))
			return true;

		var current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, this) || ReferenceEquals(current, rootGrid))
				return true;

			current = VisualTreeHelper.GetParent(current);
		}

		return element is FrameworkElement frameworkElement &&
			rootGrid.XamlRoot is not null &&
			ReferenceEquals(frameworkElement.XamlRoot, rootGrid.XamlRoot);
	}

	private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (RootGrid is null || _isPopupAnimationPlaying)
			return;

		var isPressedScaleEnabled = IsPressedScaleEnabled();
		var isSwipeDismissEnabled = CanStartSwipeDismiss();
		if (!isPressedScaleEnabled && !isSwipeDismissEnabled)
			return;

		StopSwipeDismissRestoreStoryboard();
		RootGrid.CapturePointer(e.Pointer);

		if (isSwipeDismissEnabled)
			StartSwipeDismiss(e);

		if (isPressedScaleEnabled)
		{
			_isPressAnimationActive = true;
			AnimatePressedScale(GetResolvedPressedScale(), TimeSpan.FromMilliseconds(110));
		}
	}

	private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (UpdateSwipeDismiss(e))
			e.Handled = true;
	}

	private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		var dismissedBySwipe = CompleteSwipeDismiss(e);

		if (RootGrid is not null)
			RootGrid.ReleasePointerCapture(e.Pointer);

		if (!dismissedBySwipe)
			RestorePressedScale();
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
			RestorePressedScale();
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
			return;

		_swipeDismissPointerId = e.Pointer.PointerId;
		_swipeDismissStartPoint = e.GetCurrentPoint(RootGrid).Position;
		_isSwipeDismissTracking = true;
		_isSwipeDismissDragging = false;
	}

	private bool UpdateSwipeDismiss(PointerRoutedEventArgs e)
	{
		if (!_isSwipeDismissTracking || RootGrid is null || e.Pointer.PointerId != _swipeDismissPointerId)
			return false;

		var currentPoint = e.GetCurrentPoint(RootGrid).Position;
		var deltaX = currentPoint.X - _swipeDismissStartPoint.X;
		var deltaY = currentPoint.Y - _swipeDismissStartPoint.Y;

		if (!TryGetSwipeDismissTranslation(deltaX, deltaY, out var translateX, out var translateY))
			return false;

		if (!_isSwipeDismissDragging)
		{
			_isSwipeDismissDragging = true;
			StopAutoCloseTimer();
			RestorePressedScale();
		}

		var transform = GetRootTransform();
		if (transform is null)
			return false;

		transform.TranslateX = translateX;
		transform.TranslateY = translateY;
		return true;
	}

	private bool CompleteSwipeDismiss(PointerRoutedEventArgs e)
	{
		if (!_isSwipeDismissTracking || e.Pointer.PointerId != _swipeDismissPointerId)
			return false;

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
			AnimateSwipeDismissRestore();

		return false;
	}

	private void CancelSwipeDismiss()
	{
		var shouldRestore = _isSwipeDismissDragging;
		ResetSwipeDismissTracking();

		if (shouldRestore)
			AnimateSwipeDismissRestore();
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
				return false;

			translateX = Math.Sign(closedX) * Math.Min(primaryDistance, Math.Abs(closedX));
			return true;
		}

		var closedY = GetClosedYOffset(_activePopupDirection);
		if (closedY == 0)
			return false;

		var verticalPrimaryDistance = Math.Max(0, Math.Sign(closedY) * deltaY);
		if (!CanStartSwipeDismissDrag(verticalPrimaryDistance, Math.Abs(deltaX)))
			return false;

		translateY = Math.Sign(closedY) * Math.Min(verticalPrimaryDistance, Math.Abs(closedY));
		return true;
	}

	private bool CanStartSwipeDismissDrag(double primaryDistance, double secondaryDistance)
	{
		if (_isSwipeDismissDragging)
			return true;

		if (primaryDistance < SwipeDismissDragStartThreshold)
			return false;

		return primaryDistance >= secondaryDistance * SwipeDismissAxisDominanceRatio;
	}

	private double GetSwipeDismissDistance()
	{
		var transform = GetRootTransform();
		if (transform is null)
			return 0;

		var closedX = GetClosedXOffset(_activePopupDirection);
		if (closedX != 0)
			return Math.Max(0, Math.Sign(closedX) * transform.TranslateX);

		var closedY = GetClosedYOffset(_activePopupDirection);
		return closedY == 0
			? 0
			: Math.Max(0, Math.Sign(closedY) * transform.TranslateY);
	}

	private double GetSwipeDismissMaxDistance()
	{
		var closedX = GetClosedXOffset(_activePopupDirection);
		if (closedX != 0)
			return Math.Abs(closedX);

		return Math.Abs(GetClosedYOffset(_activePopupDirection));
	}

	private double GetResolvedSwipeDismissThreshold()
	{
		if (double.IsNaN(SwipeDismissThreshold) || double.IsInfinity(SwipeDismissThreshold))
			return Math.Min(80.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));

		return Clamp(SwipeDismissThreshold, 1.0D, Math.Max(1.0D, GetSwipeDismissMaxDistance()));
	}

	private void AnimateSwipeDismissRestore()
	{
		StopSwipeDismissRestoreStoryboard();

		if (!AreTransitionAnimationsEnabled)
		{
			SetOpenTransform();
			RestartAutoCloseTimer();
			return;
		}

		_swipeDismissRestoreStoryboard = GetOpenStoryboard(_activePopupDirection, true);
		_swipeDismissRestoreStoryboard.Completed += SwipeDismissRestoreStoryboard_Completed;
		_swipeDismissRestoreStoryboard.Begin();
	}

	private void StopSwipeDismissRestoreStoryboard()
	{
		if (_swipeDismissRestoreStoryboard is null)
			return;

		_swipeDismissRestoreStoryboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
		_swipeDismissRestoreStoryboard.Stop();
		_swipeDismissRestoreStoryboard = null;
	}

	private void SwipeDismissRestoreStoryboard_Completed(object? sender, object e)
	{
		if (sender is not Storyboard storyboard || !ReferenceEquals(_swipeDismissRestoreStoryboard, storyboard))
			return;

		storyboard.Completed -= SwipeDismissRestoreStoryboard_Completed;
		_swipeDismissRestoreStoryboard = null;
		storyboard.Stop();
		if (_disposed)
			return;

		SetOpenTransform();
		RestartAutoCloseTimer();
	}

	private void RestorePressedScale()
	{
		if (!_isPressAnimationActive)
			return;

		_isPressAnimationActive = false;
		AnimatePressedScale(1.0D, TimeSpan.FromMilliseconds(240));
	}

	private void ResetPressedScale()
	{
		_isPressAnimationActive = false;
		StopPressedScaleStoryboard();
		_pressScaleStoryboard = null;
		_pressScaleTargetScale = 1.0D;

		var transform = GetRootTransform();
		if (transform is null)
			return;

		transform.ScaleX = 1.0D;
		transform.ScaleY = 1.0D;
	}

	private void AnimatePressedScale(double scale, TimeSpan duration)
	{
		var transform = GetRootTransform();
		if (transform is null)
			return;

		if (!AreSystemAnimationsEnabled)
		{
			StopPressedScaleStoryboard();
			transform.ScaleX = scale;
			transform.ScaleY = scale;
			_pressScaleTargetScale = scale;
			return;
		}

		var currentScaleX = transform.ScaleX;
		var currentScaleY = transform.ScaleY;

		StopPressedScaleStoryboard();
		transform.ScaleX = currentScaleX;
		transform.ScaleY = currentScaleY;
		_pressScaleTargetScale = scale;
		_pressScaleStoryboard = TransitionHelpers.GetPressedScaleTransitionStoryboard(
			transform,
			currentScaleX,
			currentScaleY,
			scale,
			duration);
		_pressScaleStoryboard.Completed += PressScaleStoryboard_Completed;
		_pressScaleStoryboard.Begin();
	}

	private void StopPressedScaleStoryboard()
	{
		if (_pressScaleStoryboard is null)
			return;

		_pressScaleStoryboard.Completed -= PressScaleStoryboard_Completed;
		_pressScaleStoryboard.Stop();
		_pressScaleStoryboard = null;
	}

	private void PressScaleStoryboard_Completed(object? sender, object e)
	{
		if (!ReferenceEquals(sender, _pressScaleStoryboard))
			return;

		StopPressedScaleStoryboard();

		var transform = GetRootTransform();
		if (transform is null)
			return;

		transform.ScaleX = _pressScaleTargetScale;
		transform.ScaleY = _pressScaleTargetScale;
	}

	private bool IsPressedScaleEnabled()
	{
		return AreSystemAnimationsEnabled && Math.Abs(GetResolvedPressedScale() - 1.0D) > 0.001D;
	}

	private double GetResolvedPressedScale()
	{
		if (double.IsNaN(PressedScale) || double.IsInfinity(PressedScale))
			return 1.0D;

		return Clamp(PressedScale, 0.1D, 2.0D);
	}

	private void UpdateFocusSuppression()
	{
		if (ActivationMode is DesktopFlyoutActivationMode.NeverActivate)
			SuppressFocus();
		else
			RestoreFocusSuppression();
	}

	private void SuppressFocus()
	{
		if (RootGrid is null)
			return;

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
			SuppressFocus(VisualTreeHelper.GetChild(element, index));
	}

	private void RestoreFocusSuppression()
	{
		foreach (var (element, allowFocusOnInteraction) in _suppressedInteractionFocusStates)
			element.AllowFocusOnInteraction = allowFocusOnInteraction;

		foreach (var (control, isTabStop) in _suppressedTabStopStates)
			control.IsTabStop = isTabStop;

		_suppressedInteractionFocusStates.Clear();
		_suppressedTabStopStates.Clear();
	}

	private Storyboard GetOpenStoryboard(DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
	{
		var transform = GetRootTransform() ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

		return IsVerticalDirection(popupDirection)
			? TransitionHelpers.GetWindows11BottomToTopTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : GetClosedYOffset(popupDirection), 0)
			: TransitionHelpers.GetWindows11RightToLeftTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : GetClosedXOffset(popupDirection), 0);
	}

	private Storyboard GetCloseStoryboard(DesktopFlyoutPopupDirection popupDirection, bool fromCurrentTransform = false)
	{
		var transform = GetRootTransform() ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

		return IsVerticalDirection(popupDirection)
			? TransitionHelpers.GetWindows11TopToBottomTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateY : 0, GetClosedYOffset(popupDirection))
			: TransitionHelpers.GetWindows11LeftToRightTransitionStoryboard(transform, fromCurrentTransform ? transform.TranslateX : 0, GetClosedXOffset(popupDirection));
	}

	private void SetOpenTransform()
	{
		var transform = GetRootTransform();
		if (transform is null)
			return;

		transform.TranslateX = 0;
		transform.TranslateY = 0;
	}

	private void SetClosedTransform(DesktopFlyoutPopupDirection popupDirection)
	{
		var transform = GetRootTransform();
		if (transform is null)
			return;

		transform.TranslateX = GetClosedXOffset(popupDirection);
		transform.TranslateY = GetClosedYOffset(popupDirection);
	}

	private CompositeTransform? GetRootTransform()
	{
		if (RootGrid is null)
			return null;

		return EnsureRootTransform();
	}

	private CompositeTransform EnsureRootTransform()
	{
		var rootGrid = RootGrid ?? throw new InvalidOperationException($"{PART_RootGrid} is not initialized.");

		if (rootGrid.RenderTransform is CompositeTransform compositeTransform)
			return compositeTransform;

		var transform = new CompositeTransform()
		{
			ScaleX = 1.0D,
			ScaleY = 1.0D,
		};

		if (rootGrid.RenderTransform is TranslateTransform translateTransform)
		{
			transform.TranslateX = translateTransform.X;
			transform.TranslateY = translateTransform.Y;
		}

		rootGrid.RenderTransform = transform;
		return transform;
	}

	private int GetClosedXOffset(DesktopFlyoutPopupDirection popupDirection)
	{
		return popupDirection switch
		{
			DesktopFlyoutPopupDirection.LeftToRight => -(int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Left),
			DesktopFlyoutPopupDirection.RightToLeft => (int)Math.Ceiling(GetCurrentFlyoutWidth() + Margin.Right),
			_ => 0,
		};
	}

	private int GetClosedYOffset(DesktopFlyoutPopupDirection popupDirection)
	{
		return popupDirection switch
		{
			DesktopFlyoutPopupDirection.TopToBottom => -(int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Top),
			DesktopFlyoutPopupDirection.BottomToTop => (int)Math.Ceiling(GetCurrentFlyoutHeight() + Margin.Bottom),
			_ => 0,
		};
	}

	private double GetCurrentFlyoutWidth()
	{
		return RootGrid is null
			? DesiredSize.Width
			: ResolveCurrentFlyoutLength(RootGrid.Width, RootGrid.DesiredSize.Width, RootGrid.ActualWidth, DesiredSize.Width);
	}

	private double GetCurrentFlyoutHeight()
	{
		return RootGrid is null
			? DesiredSize.Height
			: ResolveCurrentFlyoutLength(RootGrid.Height, RootGrid.DesiredSize.Height, RootGrid.ActualHeight, DesiredSize.Height);
	}

	private static double ResolveCurrentFlyoutLength(double explicitLength, double desiredLength, double actualLength, double fallbackLength)
	{
		if (!double.IsNaN(explicitLength) && !double.IsInfinity(explicitLength))
			return explicitLength;

		if (desiredLength > 0)
			return desiredLength;

		if (actualLength > 0)
			return actualLength;

		return fallbackLength;
	}

	private static double ResolveFlyoutLength(GridLength length, double availableLength, bool stretchWhenAuto)
	{
		if (length.IsAuto)
			return stretchWhenAuto ? availableLength : double.NaN;

		if (length.IsStar)
			return availableLength;

		return Clamp(length.Value, 0, availableLength);
	}

	private static Thickness GetScaledMargin(Thickness margin, double scale)
	{
		return new(
			margin.Left * scale,
			margin.Top * scale,
			margin.Right * scale,
			margin.Bottom * scale);
	}

	private static bool IsVerticalDirection(DesktopFlyoutPopupDirection popupDirection)
	{
		return popupDirection is DesktopFlyoutPopupDirection.BottomToTop or DesktopFlyoutPopupDirection.TopToBottom;
	}

	private bool ShouldActivateOnOpen()
	{
		return ActivationMode is DesktopFlyoutActivationMode.Activate;
	}

	private static double Clamp(double value, double min, double max)
	{
		return Math.Min(Math.Max(value, min), Math.Max(min, max));
	}

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

	private bool AreTransitionAnimationsEnabled => IsTransitionAnimationEnabled && AreSystemAnimationsEnabled;

	private void SetTransitionState(DesktopFlyoutTransitionState transitionState)
	{
		if (TransitionState == transitionState)
			return;

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
			return;

		var position = _showAfterClosePosition!;
		_showAfterClose = false;
		_showAfterClosePosition = null;
		Show(position);
	}

#if !HAS_UNO
	private void UpdateHostBounds(bool activate)
	{
		var bounds = WindowHelpers.TryGetMonitorScreenRect(_monitorReferenceBounds, out var monitorBounds)
			? monitorBounds
			: _activeWorkArea;
		if (_hostBounds == bounds)
			return;

		_host?.Maximize(bounds, activate);
		_hostBounds = bounds;
	}

	private void PrepareHostContainerLayout()
	{
		var scale = _host?.XamlIslandRasterizationScale ?? 1.0D;
		// Measure against the available area, not the previous visible container size.
		_hostContainer.Width = double.NaN;
		_hostContainer.Height = double.NaN;
		_hostContainer.MaxWidth = _activeWorkArea.Width / scale;
		_hostContainer.MaxHeight = _activeWorkArea.Height / scale;
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
			return;

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

	private void InvalidatePlacement()
	{
		if (_disposed || _requestedPosition is null)
			return;

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

	private void EnvironmentTimer_Tick(object? sender, object e)
	{
		if (!_disposed && RefreshPlacement(apply: false))
			InvalidatePlacement();
	}

	private bool RefreshPlacement(bool apply = true)
	{
		if (_disposed || _requestedPosition is null || _host?.DesktopWindowXamlSource is null)
			return false;

		if (!_requestedPosition.TryResolveTrayIcon(out var position))
		{
			if (_lastTrayIconBounds is not Rectangle lastBounds)
				return false;

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
			taskbarEdge = WindowHelpers.GetTaskbarEdge(taskbar, monitorBounds);

		if (_taskbarOverflowFlyoutMonitor?.TryGetBounds(out var overflow) == true && overflow.IntersectsWith(monitorBounds))
			overflowBounds = overflow;

		if (trayBounds is Rectangle trayIcon && overflowBounds is null && taskbarBounds is Rectangle bar)
		{
			var edgeAnchor = WindowHelpers.GetTaskbarEdgeAnchor(trayIcon, bar, monitorBounds, out _);
			position = DesktopFlyoutPosition.FromTaskbarBounds(edgeAnchor);
		}
#else
		workArea = WindowHelpers.GetFlyoutWorkAreaRect(position, _monitorReferenceBounds.Location);
		if (!WindowHelpers.TryGetMonitorScreenRect(reference, out monitorBounds))
			monitorBounds = workArea;
		if (trayBounds is Rectangle trayIcon && WindowHelpers.TryGetTaskbarInfoForBounds(trayIcon, out _, out var edge))
			taskbarEdge = edge;
#endif
		position = position.ResolveTaskbarAnchor(taskbarEdge, monitorBounds, overflowBounds);
#if !HAS_UNO
		if (trayBounds is Rectangle overflowIcon && overflowBounds is Rectangle overflowArea)
		{
			var margin = GetScaledMargin(Margin, _host.XamlIslandRasterizationScale);
			var adjacentMargin = position.AnchorSide switch
			{
				DesktopFlyoutAnchorSide.Above => margin.Bottom,
				DesktopFlyoutAnchorSide.Below => margin.Top,
				DesktopFlyoutAnchorSide.Left => margin.Right,
				_ => margin.Left,
			};
			var anchor = DesktopFlyoutPositionResolver.GetOverflowAnchor(overflowIcon, overflowArea,
				position.AnchorSide, (int)Math.Floor(Math.Max(0, adjacentMargin)));
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
			_position = position;
			_activeWorkArea = workArea;
			_monitorReferenceBounds = monitorBounds;
			_lastLayoutScale = scale;
			_placementUpdatePending = false;
			_lastTrayIconBounds = trayBounds;
#if !HAS_UNO
			if (trayBounds is Rectangle currentTray)
				EnsureTaskbarFlyoutVisibilityLease(currentTray);
#endif
		}

		return changed;
	}

	private void HostWindow_Inactivated(object? sender, EventArgs e)
	{
		if (HideOnLostFocus) Hide();
	}

	private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
	{
		if (_disposed)
			return;

#if (UWP || WASDK) && !HAS_UNO
		_systemAnimationsEnabled = _uiSettings.AnimationsEnabled;
#endif
		UpdateFlyoutTheme(forceBackdropUpdate: true);
	}

#if (UWP || WASDK) && !HAS_UNO
	private void UISettings_AnimationsEnabledChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
	{
		void UpdateSystemAnimationsEnabled()
		{
			if (!_disposed)
				_systemAnimationsEnabled = sender.AnimationsEnabled;
		}

#if UWP
		_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateSystemAnimationsEnabled);
#elif WASDK
		DispatcherQueue.TryEnqueue(UpdateSystemAnimationsEnabled);
#endif
	}
#endif

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		CancelOpenFlyoutLayoutUpdate();
		StopTransitionStoryboard();
		StopSwipeDismissRestoreStoryboard();
		ResetSwipeDismissTracking();
		ResetPressedScale();
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
			_uiSettings.AnimationsEnabledChanged -= UISettings_AnimationsEnabledChanged;
#endif
		RootGrid?.GettingFocus -= RootGrid_GettingFocus;
		RootGrid?.PointerPressed -= RootGrid_PointerPressed;
		RootGrid?.PointerMoved -= RootGrid_PointerMoved;
		RootGrid?.PointerReleased -= RootGrid_PointerReleased;
		RootGrid?.PointerCanceled -= RootGrid_PointerCanceled;
		RootGrid?.PointerCaptureLost -= RootGrid_PointerCaptureLost;
		RootGrid?.PointerExited -= RootGrid_PointerExited;
#if WASDK && !HAS_UNO
		foreach (var island in Islands)
			island.ClearOwnerBackdrop();
#endif
		if (IslandsItemsControl is not null)
			IslandsItemsControl.ItemsSource = null;

		if (_isFocusManagerGettingFocusSubscribed)
		{
			FocusManager.GettingFocus -= FocusManager_GettingFocus;
			_isFocusManagerGettingFocusSubscribed = false;
		}
		_host?.Dispose();
		IsOpen = false;
		_showAfterClose = false;
		_showAfterClosePosition = null;
		SetTransitionState(DesktopFlyoutTransitionState.Closed);

		GC.SuppressFinalize(this);
	}
}
