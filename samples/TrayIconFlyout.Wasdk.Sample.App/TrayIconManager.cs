// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

namespace U5BFA.Libraries
{
	internal partial class TrayIconManager : IDisposable
	{
		private static readonly Lazy<TrayIconManager> _default = new(() => new TrayIconManager());
		internal static TrayIconManager Default => _default.Value;

		internal SystemTrayIcon? SystemTrayIcon { get; set; }
		internal TrayIconFlyout? TrayIconFlyout { get; set; }
		internal TrayIconMenuFlyout? TrayIconMenuFlyout { get; set; }
		internal TrayIconFlyoutExample SelectedFlyoutExample { get; private set; }

		private bool _disposed;

		private TrayIconManager() { }

		internal void Initialize(SystemTrayIcon trayIcon)
		{
			TrayIconFlyout = CreateFlyout(SelectedFlyoutExample);
			TrayIconMenuFlyout = new MainTrayIconMeunFlyout();

			SystemTrayIcon = trayIcon;
			SystemTrayIcon.Show();
			SystemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
			SystemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
		}

		internal void SwitchFlyout(TrayIconFlyoutExample example)
		{
			if (_disposed || (TrayIconFlyout is not null && SelectedFlyoutExample == example))
				return;

			var oldFlyout = TrayIconFlyout;
			var newFlyout = CreateFlyout(example);

			TrayIconFlyout = newFlyout;
			SelectedFlyoutExample = example;
			oldFlyout?.Dispose();
		}

		private static TrayIconFlyout CreateFlyout(TrayIconFlyoutExample example)
		{
			return example switch
			{
				TrayIconFlyoutExample.StickySmall => new StickySmallTrayIconFlyout(),
				TrayIconFlyoutExample.StartMenuStyle => new StartMenuStyleTrayIconFlyout(),
				TrayIconFlyoutExample.WidgetStyle => new WidgetStyleTrayIconFlyout(),
				_ => new MainTrayIconFlyout(),
			};
		}

		private void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
		{
			if (TrayIconFlyout is null)
				return;

			if (TrayIconFlyout.IsOpen)
			{
				TrayIconFlyout.Hide();
			}
			else
			{
				if (TrayIconFlyout is StickySmallTrayIconFlyout)
				{
					TrayIconFlyout.Show(e.Point);

				}
				else
				{
					TrayIconFlyout.Show();
				}
			}
		}

		private void SystemTrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
		{
			if (TrayIconMenuFlyout is null)
				return;

			if (TrayIconMenuFlyout.IsOpen)
				TrayIconMenuFlyout.Hide();

			TrayIconMenuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			SystemTrayIcon?.LeftClicked -= SystemTrayIcon_LeftClicked;
			SystemTrayIcon?.RightClicked -= SystemTrayIcon_RightClicked;
			SystemTrayIcon?.Destroy();
			TrayIconFlyout?.Dispose();
			TrayIconMenuFlyout?.Dispose();

			SystemTrayIcon = null;
			TrayIconFlyout = null;
			TrayIconMenuFlyout = null;

			GC.SuppressFinalize(this);
		}
	}
}
