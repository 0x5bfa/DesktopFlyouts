// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if WINDOWS
using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Colors = Windows.UI.Colors;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Colors = Microsoft.UI.Colors;
#endif

namespace DesktopFlyouts;

/// <summary>
/// Displays a menu flyout in an independent XAML island window.
/// </summary>
/// <remarks>
/// Use <see cref="DesktopMenuFlyout"/> when a context menu must be opened at a physical screen
/// point, such as a tray icon click. Add <see cref="MenuFlyoutItemBase"/> items as children and
/// call the point-based show overload to display the menu.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
public partial class DesktopMenuFlyout : DependencyObject, IDisposable
{
	private readonly XamlIslandHostWindow? _host;
	private readonly Border _menuTarget = new() { /*Background = new SolidColorBrush(Colors.Transparent)*/ };
	private readonly MenuFlyout _menuFlyout = new();
	private bool _disposed;
	private bool _menuPopupReported;

	/// <summary>Gets the items displayed in the menu.</summary>
	public IList<MenuFlyoutItemBase> Items => _menuFlyout.Items;

	/// <summary>
	/// Initializes a new instance of <see cref="DesktopMenuFlyout"/>.
	/// </summary>
	/// <remarks>
	/// The constructor creates the hidden desktop host window used to display the menu.
	/// </remarks>
	public DesktopMenuFlyout()
	{
		_host = new XamlIslandHostWindow();
		_host.SetContent(_menuTarget);
		_ = _host.UpdateWindowVisibility(false);
		_host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
		_menuFlyout.Opened += MenuFlyout_Opened;
		_menuFlyout.Closed += MenuFlyout_Closed;
	}

	/// <summary>
	/// Opens the menu flyout at the specified screen point.
	/// </summary>
	/// <param name="point">The physical screen pixel where the menu host should be positioned.</param>
	/// <remarks>
	/// Uses the current <see cref="Items"/> collection. An empty menu is not shown.
	/// </remarks>
	public void Show(Point point)
	{
		if (_disposed || Items.Count == 0)
		{
			return;
		}

		UpdateFlyoutTheme();

		bool wasOpen = IsOpen;
		try
		{
			_host?.MoveAndResize(new RectInt32() { X = point.X, Y = point.Y }, activate: false);
			_host?.SetHWndRectRegion(new RectInt32() { Width = 1, Height = 1 });
			_ = _host?.UpdateWindowVisibility(true);

			_menuFlyout.ShowAt(_menuTarget);
			ReportMenuPopupOpened();

			IsOpen = true;
		}
		catch
		{
			if (!wasOpen)
			{
				HideMenuHost();
			}

			throw;
		}
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

		try
		{
			_menuFlyout.Hide();
		}
		finally
		{
			HideMenuHost();
		}
	}

	private void MenuFlyout_Opened(object? sender, object e)
	{
		ReportMenuPopupOpened();
	}

	private void MenuFlyout_Closed(object? sender, object e)
	{
		HideMenuHost();
	}

	private void ReportMenuPopupOpened()
	{
		if (_disposed || _menuPopupReported || !_menuFlyout.IsOpen || _host is null)
		{
			return;
		}

		// Explorer defers overflow light dismissal while a Win32 menu popup is open.
		_menuPopupReported = true;
		PInvoke.NotifyWinEvent(PInvoke.EVENT_SYSTEM_MENUPOPUPSTART, _host.HWnd, (int)OBJECT_IDENTIFIER.OBJID_WINDOW, (int)PInvoke.CHILDID_SELF);
	}

	private void ReportMenuPopupClosed()
	{
		if (!_menuPopupReported || _host is null)
		{
			return;
		}

		_menuPopupReported = false;
		PInvoke.NotifyWinEvent(PInvoke.EVENT_SYSTEM_MENUPOPUPEND, _host.HWnd, (int)OBJECT_IDENTIFIER.OBJID_WINDOW, (int)PInvoke.CHILDID_SELF);
	}

	private void HideMenuHost()
	{
		ReportMenuPopupClosed();
		_ = _host?.UpdateWindowVisibility(false);
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

	private void UpdateFlyoutTheme()
	{
		_menuTarget.RequestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		try
		{
			_menuFlyout.Opened -= MenuFlyout_Opened;
			_menuFlyout.Closed -= MenuFlyout_Closed;
			if (IsOpen)
			{
				_menuFlyout.Hide();
			}

			Items.Clear();
		}
		finally
		{
			ReportMenuPopupClosed();
			_host?.SystemSettingsChanged -= HostWindow_SystemSettingsChanged;
			_host?.Dispose();
			IsOpen = false;
		}

		GC.SuppressFinalize(this);
	}
}
#endif
