// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
#endif

namespace U5BFA.Libraries
{
	[ContentProperty(Name = nameof(Items))]
	public partial class TrayIconMenuFlyout : ItemsControl, IDisposable
	{
		private const string PART_RootGrid = "PART_RootGrid";
		private const string PART_MenuFlyoutTargetControl = "PART_MenuFlyoutTargetControl";

		private readonly XamlIslandHostWindow? _host;
		private MenuFlyout? _menuFlyout;
		private bool _disposed;

		private Border? MenuFlyoutTargetControl;

		public bool IsOpen { get; private set; }

		public TrayIconMenuFlyout()
		{
			DefaultStyleKey = typeof(TrayIconMenuFlyout);

			_host = new XamlIslandHostWindow();
			_host.SetContent(this);
			_host.UpdateWindowVisibility(false);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (GetTemplateChild(PART_RootGrid) is not Grid)
				throw new MissingFieldException($"Could not find {PART_RootGrid} in the given {nameof(TrayIconMenuFlyout)}'s style.");
			MenuFlyoutTargetControl = GetTemplateChild(PART_MenuFlyoutTargetControl) as Border
				?? throw new MissingFieldException($"Could not find {PART_MenuFlyoutTargetControl} in the given {nameof(TrayIconMenuFlyout)}'s style.");
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (_disposed)
				return;

			if (_menuFlyout is null)
			{
				_menuFlyout = new MenuFlyout();
				_menuFlyout.Closed += MenuFlyout_Closed;
			}

			_menuFlyout.Items.Clear();

			foreach (var item in Items)
				_menuFlyout.Items.Add((MenuFlyoutItemBase)item);
		}

		public void Show(Point point)
		{
			if (_disposed || _menuFlyout is null)
				return;

			UpdateFlyoutTheme();

			_host?.MoveAndResize(new(point.X, point.Y, 0, 0));
			_host?.SetHWndRectRegion(new(0, 0, 1, 1));
			_host?.UpdateWindowVisibility(true);

			_menuFlyout.ShowAt(MenuFlyoutTargetControl);

			IsOpen = true;
		}

		public void Hide()
		{
			if (_disposed)
				return;

			_host?.UpdateWindowVisibility(false);

			_menuFlyout?.Hide();

			IsOpen = false;
		}

		private void MenuFlyout_Closed(object? sender, object e)
		{
			_host?.UpdateWindowVisibility(false);
			IsOpen = false;
		}

#if UWP
		public unsafe bool TryPreTranslateMessage(MSG* msg)
		{
			return _host?.TryPreTranslateMessage(msg) ?? false;
		}
#endif

		private void UpdateFlyoutTheme()
		{
			RequestedTheme = GeneralHelpers.IsTaskbarLight() ? ElementTheme.Light : ElementTheme.Dark;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (_menuFlyout is not null)
			{
				_menuFlyout.Closed -= MenuFlyout_Closed;
				if (IsOpen)
					_menuFlyout.Hide();

				_menuFlyout.Items.Clear();
				_menuFlyout = null;
			}

			_host?.Dispose();
			IsOpen = false;

			GC.SuppressFinalize(this);
		}
	}
}
