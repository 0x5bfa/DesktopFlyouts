// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if !WINDOWS
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using FoundationSize = Windows.Foundation.Size;
using Windows.Graphics;
using Microsoft.UI.Xaml.Markup;

namespace DesktopFlyouts;

/// <summary>
/// Displays a menu flyout in an independent XAML island window.
/// </summary>
/// <remarks>
/// Use <see cref="DesktopMenuFlyout"/> when a context menu must be opened at a physical screen
/// point, such as a tray icon click. Add <see cref="MenuFlyoutItemBase"/> items as children and
/// call the point-based show overload to display the menu.
/// Submenus are not supported in this implementation.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
public partial class DesktopMenuFlyout : DependencyObject, IDisposable
{
	private readonly XamlIslandHostWindow? _host;
	private readonly Grid _menuTarget = new() { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
	private readonly ObservableCollection<MenuFlyoutItemBase> _items = new();
	private readonly List<MenuFlyoutItem> _subscribedItems = new();
	private readonly MenuFlyoutPresenter _presenter;
	private bool _disposed;
	private bool _isShowPending;

	/// <summary>Gets the items displayed in the menu.</summary>
	public IList<MenuFlyoutItemBase> Items => _items;

	/// <summary>
	/// Initializes a new instance of <see cref="DesktopMenuFlyout"/>.
	/// </summary>
	/// <remarks>
	/// The constructor creates the hidden desktop host window used to display the menu.
	/// </remarks>
	public DesktopMenuFlyout()
	{
		_presenter = new MenuFlyoutPresenter { ItemsSource = _items };
		_host = new XamlIslandHostWindow();
		_host.SetContent(_menuTarget);
		_ = _host.UpdateWindowVisibility(false);
		_host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
		_host.WindowInactivated += HostWindow_Inactivated;
		_items.CollectionChanged += Items_CollectionChanged;
	}

	private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (IsOpen)
		{
			UnsubscribeItemClicks();
			SubscribeItemClicks();
		}
	}

	private void SubscribeItemClicks()
	{
		foreach (var item in Items)
		{
			CollectClickableItems(item);
		}
	}

	private void CollectClickableItems(MenuFlyoutItemBase item)
	{
		if (item is MenuFlyoutItem mfi)
		{
			// avoid double subscribing in case it happens
			mfi.Click -= OnMenuItemClicked;
			mfi.Click += OnMenuItemClicked;
			_subscribedItems.Add(mfi);
		}

		if (item is MenuFlyoutSubItem mfsi)
		{
			foreach (var child in mfsi.Items)
			{
				CollectClickableItems(child);
			}
		}
		else if (item is SplitMenuFlyoutItem smfi)
		{
			foreach (var child in smfi.Items)
			{
				CollectClickableItems(child);
			}
		}
	}

	private void UnsubscribeItemClicks()
	{
		foreach (var mfi in _subscribedItems)
		{
			mfi.Click -= OnMenuItemClicked;
		}

		_subscribedItems.Clear();
	}

	private void OnMenuItemClicked(object sender, RoutedEventArgs e)
	{
		Hide();
	}

	/// <summary>
	/// Opens the menu flyout at the specified screen point.
	/// </summary>
	/// <param name="point">The physical screen pixel where the menu host should be positioned.</param>
	/// <remarks>
	/// Uses the current <see cref="Items"/> collection. Submenus are not supported in this implementation.
	/// </remarks>
	public void Show(Point point)
	{
		if (_disposed)
		{
			return;
		}

		// If a previous show is still pending its deferred layout, hide first.
		if (_isShowPending || IsOpen)
		{
			HideImmediate();
		}

		SubscribeItemClicks();

		UpdateFlyoutTheme();

		// Add the presenter to the visual tree.
		_menuTarget.Children.Clear();
		_menuTarget.Children.Add(_presenter);

		// Get the work area for positioning.
		var workArea = WindowHelpers.GetFlyoutWorkAreaRect(new Point(point.X, point.Y));

		// Step 1: Place the host window at the target position with a generous initial size
		// so that XAML can compute layout for the MenuFlyoutPresenter.
		var initRegion = new RectInt32()
		{
			X = point.X,
			Y = point.Y - workArea.Height / 2,
			Width = workArea.Width / 2,
			Height = workArea.Height / 2
		};

		_host?.MoveAndResize(initRegion);
		_host?.SetHWndRectRegion(new RectInt32() { Width = initRegion.Width, Height = initRegion.Height });
		_ = _host?.UpdateWindowVisibility(true);

		IsOpen = true;
		_isShowPending = true;

		// Step 2: After the window is mapped and XAML has had a layout pass,
		// measure the presenter and resize to the correct size.
		var showPoint = new Point(point.X, point.Y);
		var dispatcherQueue = _menuTarget.DispatcherQueue;
		_ = Task.Run(() => dispatcherQueue.TryEnqueue(() => FinalizeShow(showPoint)));
	}

	private void FinalizeShow(Point point)
	{
		if (_disposed || !_isShowPending)
		{
			_isShowPending = false;
			return;
		}

		// Force a layout pass so the presenter calculates its desired size.
		_presenter.Measure(new FoundationSize(double.PositiveInfinity, double.PositiveInfinity));
		_presenter.Arrange(new Windows.Foundation.Rect(0, 0, _presenter.DesiredSize.Width, _presenter.DesiredSize.Height));

		// Get the desired size of the presenter.
		var presenterWidth = _presenter.ActualWidth > 0 ? _presenter.ActualWidth : _presenter.DesiredSize.Width;
		var presenterHeight = _presenter.ActualHeight > 0 ? _presenter.ActualHeight : _presenter.DesiredSize.Height;

		// Ensure minimum size.
		var regionWidth = Math.Max(1, (int)Math.Ceiling(presenterWidth));
		var regionHeight = Math.Max(1, (int)Math.Ceiling(presenterHeight));

		// Get the work area for positioning.
		var workArea = WindowHelpers.GetFlyoutWorkAreaRect(new Point(point.X, point.Y));

		// Calculate position: align to mouse X, place above the point (like a context menu).
		// 32 magic number is the offset between window bottom and the actual rendering - probably shadow
		var left = (double)point.X;
		var top = (double)point.Y - regionHeight + 32;

		// Clamp to work area.
		left = Clamp(left, workArea.Left, workArea.Right - regionWidth);
		top = Clamp(top, workArea.Top, workArea.Bottom - regionHeight);

		var region = new RectInt32()
		{
			X = (int)Math.Round((double)left),
			Y = (int)Math.Round((double)top),
			Width = regionWidth,
			Height = regionHeight
		};

		// Resize and reposition the host window to the correct size.
		_host?.MoveAndResize(region);
		_host?.SetHWndRectRegion(new RectInt32() { Width = regionWidth, Height = regionHeight });

		_isShowPending = false;
	}

	/// <summary>
	/// Closes the menu flyout.
	/// </summary>
	/// <remarks>
	/// This also hides the desktop host window.
	/// </remarks>
	public void Hide()
	{
		if (_disposed)
		{
			return;
		}

		_isShowPending = false;
		HideImmediate();
	}

	private void HideImmediate()
	{
		UnsubscribeItemClicks();

		_ = _host?.UpdateWindowVisibility(false);

		// Remove the presenter from the visual tree.
		_menuTarget.Children.Remove(_presenter);

		IsOpen = false;
	}

	private void HostWindow_SystemSettingsChanged(object? sender, EventArgs e)
	{
		if (_disposed)
		{
			return;
		}

		UpdateFlyoutTheme();
	}

	private void HostWindow_Inactivated(object? sender, EventArgs e)
	{
		if (_disposed || !IsOpen)
		{
			return;
		}

		Hide();
	}

	private void UpdateFlyoutTheme()
	{
		var isLight = GeneralHelpers.IsTaskbarLight();
		_menuTarget.RequestedTheme = isLight ? ElementTheme.Light : ElementTheme.Dark;
	}

	private static double Clamp(double value, double min, double max)
	{
		if (value < min)
		{
			return min;
		}

		if (value > max)
		{
			return max;
		}

		return value;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_isShowPending = false;

		// Remove the presenter from the visual tree.
		_menuTarget.Children.Remove(_presenter);
		_items.CollectionChanged -= Items_CollectionChanged;
		UnsubscribeItemClicks();
		_presenter.ItemsSource = null;
		_items.Clear();

		_host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
		_host?.WindowInactivated -= HostWindow_Inactivated;
		_host?.Dispose();
		IsOpen = false;

		GC.SuppressFinalize(this);
	}
}

#endif
