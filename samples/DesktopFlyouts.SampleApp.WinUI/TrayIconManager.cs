// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using DesktopFlyouts.SampleApp.Shared;
using Windows.Foundation;

namespace DesktopFlyouts;

internal partial class TrayIconManager : IDisposable
{
	private static readonly Lazy<TrayIconManager> _default = new(() => new TrayIconManager());
	internal static TrayIconManager Default => _default.Value;

	internal SystemTrayIcon? SystemTrayIcon { get; set; }
	internal DesktopFlyout? Flyout { get; set; }
	internal DesktopMenuFlyout? DesktopMenuFlyout { get; set; }
	internal DesktopFlyoutSampleKind SelectedFlyoutExample { get; private set; }

	private bool _disposed;

	private TrayIconManager() { }

	internal void Initialize(SystemTrayIcon trayIcon)
	{
		Flyout = CreateFlyout(SelectedFlyoutExample);
		DesktopMenuFlyout = new MainDesktopMenuFlyout();

		SystemTrayIcon = trayIcon;
		SystemTrayIcon.Show();
		SystemTrayIcon.LeftClicked += SystemTrayIcon_LeftClicked;
		SystemTrayIcon.RightClicked += SystemTrayIcon_RightClicked;
	}

	internal void SwitchFlyout(DesktopFlyoutSampleKind example)
	{
		if (_disposed || (Flyout is not null && SelectedFlyoutExample == example))
		{
			return;
		}

		var oldFlyout = Flyout;
		var newFlyout = CreateFlyout(example);

		Flyout = newFlyout;
		SelectedFlyoutExample = example;
		oldFlyout?.Dispose();
	}

	private static DesktopFlyout CreateFlyout(DesktopFlyoutSampleKind example)
	{
		DesktopFlyout content = example switch
		{
			DesktopFlyoutSampleKind.Button => new ButtonFlyout(),
			DesktopFlyoutSampleKind.IndicatorStyle => new IndicatorStyleFlyout(),
			DesktopFlyoutSampleKind.NotificationCenterStyle => new NotificationCenterStyleFlyout(),
			DesktopFlyoutSampleKind.StartMenuStyle => new StartMenuStyleFlyout(),
			DesktopFlyoutSampleKind.StickySmallStyle => new StickySmallFlyout(),
			DesktopFlyoutSampleKind.WidgetStyle => new WidgetStyleFlyout(),
			DesktopFlyoutSampleKind.Severity => new SeverityFlyout(),
			DesktopFlyoutSampleKind.DataTemplate => new DataTemplateFlyout(),
			_ => new CustomizableFlyout(),
		};

		content.HideOnLostFocus = example is not (DesktopFlyoutSampleKind.Button or DesktopFlyoutSampleKind.NotificationCenterStyle or DesktopFlyoutSampleKind.Severity);
		content.ActivationMode = example switch
		{
			DesktopFlyoutSampleKind.Button => DesktopFlyoutActivationMode.NoActivateOnOpen,
			DesktopFlyoutSampleKind.IndicatorStyle => DesktopFlyoutActivationMode.NeverActivate,
			_ => DesktopFlyoutActivationMode.Activate,
		};
		content.IsSwipeToDismissEnabled = example is DesktopFlyoutSampleKind.Button;
		content.AutoCloseDelay = example is DesktopFlyoutSampleKind.IndicatorStyle ? TimeSpan.FromSeconds(1.5) : TimeSpan.Zero;
		return content;
	}

	internal void ToggleFlyout(DesktopFlyoutPosition? position = null)
	{
		if (Flyout is null)
		{
			return;
		}

		if (Flyout.TransitionState is
			DesktopFlyoutTransitionState.Open or
			DesktopFlyoutTransitionState.Opening or
			DesktopFlyoutTransitionState.Closing)
		{
			Flyout.Hide();
		}
		else
		{
			Flyout.Show(position ?? DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		}
	}

	private void SystemTrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
	{
		if (Flyout is null)
		{
			return;
		}

		if (Flyout.TransitionState is
			DesktopFlyoutTransitionState.Open or
			DesktopFlyoutTransitionState.Opening or
			DesktopFlyoutTransitionState.Closing)
		{
			Flyout.Hide();
		}
		else if (SystemTrayIcon?.TryGetBounds(out _) == true)
		{
			Flyout.Show(DesktopFlyoutPosition.ForTrayIcon(SystemTrayIcon));
		}
		else
		{
			var anchor = new System.Drawing.Rectangle((int)e.Point.X, (int)e.Point.Y, 0, 0);
			Flyout.Show(DesktopFlyoutPosition.ForAnchor(anchor, DesktopFlyoutAnchorSide.Above));
		}
	}

	private void SystemTrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
	{
		if (DesktopMenuFlyout is null)
		{
			return;
		}

		if (DesktopMenuFlyout.IsOpen)
		{
			DesktopMenuFlyout.Hide();
		}

		DesktopMenuFlyout.Show(new(e.Point.X, e.Point.Y - 32));
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		SystemTrayIcon?.LeftClicked -= SystemTrayIcon_LeftClicked;
		SystemTrayIcon?.RightClicked -= SystemTrayIcon_RightClicked;
		SystemTrayIcon?.Dispose();
		Flyout?.Dispose();
		DesktopMenuFlyout?.Dispose();

		SystemTrayIcon = null;
		Flyout = null;
		DesktopMenuFlyout = null;

		GC.SuppressFinalize(this);
	}
}
