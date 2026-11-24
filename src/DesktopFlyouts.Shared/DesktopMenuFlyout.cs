// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if WINDOWS
using System;
using System.Drawing;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;


#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
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
public partial class DesktopMenuFlyout : ItemsControl, IDisposable
{
	private const string PART_MenuFlyoutTargetControl = "PART_MenuFlyoutTargetControl";

	private readonly XamlIslandHostWindow? _host;
	private MenuFlyout? _menuFlyout;
	private bool _disposed;
	private bool _menuPopupReported;

	private Border? MenuFlyoutTargetControl;

	/// <summary>
	/// Initializes a new instance of <see cref="DesktopMenuFlyout"/>.
	/// </summary>
	/// <remarks>
	/// The constructor creates the hidden desktop host window used to display the menu.
	/// </remarks>
	public DesktopMenuFlyout()
	{
		DefaultStyleKey = typeof(DesktopMenuFlyout);

		_host = new XamlIslandHostWindow();
		_host.SetContent(this);
		_ = _host.UpdateWindowVisibility(false);
		_host.SystemSettingsChanged += HostWindow_SystemSettingsChanged;
	}

	/// <inheritdoc/>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		MenuFlyoutTargetControl = GetTemplateChild(PART_MenuFlyoutTargetControl) as Border
			?? throw new MissingFieldException($"Could not find {PART_MenuFlyoutTargetControl} in the given {nameof(DesktopMenuFlyout)}'s style.");
	}

	/// <inheritdoc/>
	protected override void OnItemsChanged(object e)
	{
		base.OnItemsChanged(e);

		if (_disposed)
		{
			return;
		}

		if (_menuFlyout is null)
		{
			_menuFlyout = new MenuFlyout();
			_menuFlyout.Opened += MenuFlyout_Opened;
			_menuFlyout.Closed += MenuFlyout_Closed;
		}

		_menuFlyout.Items.Clear();

		foreach (var item in Items)
		{
			_menuFlyout.Items.Add((MenuFlyoutItemBase)item);
		}
	}

	/// <summary>
	/// Opens the menu flyout at the specified screen point.
	/// </summary>
	/// <param name="point">The physical screen pixel where the menu host should be positioned.</param>
	/// <remarks>
	/// The menu is built from the current <see cref="ItemsControl.Items"/> collection. Items must
	/// derive from <see cref="MenuFlyoutItemBase"/>.
	/// </remarks>
	public void Show(Point point)
	{
		if (_disposed || _menuFlyout is null)
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

			_menuFlyout.ShowAt(MenuFlyoutTargetControl);
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
			_menuFlyout?.Hide();
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
		if (_disposed || _menuPopupReported || _menuFlyout?.IsOpen != true || _host is null)
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
		RequestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
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
			if (_menuFlyout is not null)
			{
				_menuFlyout.Opened -= MenuFlyout_Opened;
				_menuFlyout.Closed -= MenuFlyout_Closed;
				if (IsOpen)
				{
					_menuFlyout.Hide();
				}

				_menuFlyout.Items.Clear();
				_menuFlyout = null;
			}
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
