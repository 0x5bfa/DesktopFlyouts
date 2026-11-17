// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DesktopFlyouts.SampleApp.Shared;

namespace DesktopFlyouts;

internal sealed class TrayIconManager
{
	internal static TrayIconManager Default { get; } = new();

	internal SystemTrayIcon? SystemTrayIcon { get; private set; }
	internal event EventHandler<string>? OperationFailed;

	private SampleFlyoutWindow<DesktopFlyout>? _flyout;
	private SampleFlyoutWindow<DesktopMenuFlyout>? _menu;
	private DesktopFlyoutSampleKind _selectedExample;
	private Task _operations = Task.CompletedTask;
	private bool _closing;

	private TrayIconManager() { }

	internal void Initialize(SystemTrayIcon trayIcon)
	{
		SystemTrayIcon = trayIcon;
		trayIcon.LeftClicked += TrayIcon_LeftClicked;
		trayIcon.RightClicked += TrayIcon_RightClicked;
		trayIcon.Show();

		QueueOperation(async () => _menu = await SampleFlyoutWindow<DesktopMenuFlyout>.CreateAsync(() => new MainDesktopMenuFlyout()));
	}

	internal void SwitchFlyout(DesktopFlyoutSampleKind example, FlyoutSettings settings)
	{
		QueueOperation(async () =>
		{
			if (_flyout is not null && _selectedExample == example)
			{
				if (example is DesktopFlyoutSampleKind.Customizable)
				{
					await _flyout.InvokeAsync(settings.Apply);
				}

				return;
			}

			var old = _flyout;
			_flyout = null;
			if (old is not null)
			{
				await old.DisposeAsync();
			}

			_flyout = await SampleFlyoutWindow<DesktopFlyout>.CreateAsync(() =>
			{
				var flyout = CreateFlyout(example);
				if (example is DesktopFlyoutSampleKind.Customizable)
				{
					settings.Apply(flyout);
				}

				return flyout;
			});
			_selectedExample = example;
		});
	}

	internal void UpdateSettings(FlyoutSettings settings)
	{
		QueueOperation(async () =>
		{
			if (_flyout is not null && _selectedExample is DesktopFlyoutSampleKind.Customizable)
			{
				await _flyout.InvokeAsync(settings.Apply);
			}
		});
	}

	internal void ToggleFlyout(DesktopFlyoutPosition position)
	{
		QueueOperation(async () =>
		{
			if (_flyout is not null)
			{
				await _flyout.InvokeAsync(flyout => Toggle(flyout, position));
			}
		});
	}

    internal bool TryPreTranslateMessage(nint message)
    {
        return (_flyout?.TryPreTranslateMessage(message) ?? false) ||
        (_menu?.TryPreTranslateMessage(message) ?? false);
    }

    private static void Toggle(DesktopFlyout flyout, DesktopFlyoutPosition position)
	{
		if (flyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
		{
			flyout.Hide();
		}
		else
		{
			flyout.Show(position);
		}
	}

	private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
	{
		var position = SystemTrayIcon?.TryGetBounds(out _) == true
			? DesktopFlyoutPosition.ForTrayIcon(SystemTrayIcon)
			: DesktopFlyoutPosition.ForAnchor(
				new System.Drawing.Rectangle(e.Point, System.Drawing.Size.Empty), DesktopFlyoutAnchorSide.Above);
		ToggleFlyout(position);
	}

	private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
	{
		var point = new System.Drawing.Point(e.Point.X, e.Point.Y - 32);
		QueueOperation(async () =>
		{
			if (_menu is not null)
			{
				await _menu.InvokeAsync(menu =>
				{
					if (menu.IsOpen)
					{
						menu.Hide();
					}

					menu.Show(point);
				});
			}
		});
	}

	private void QueueOperation(Func<Task> operation)
	{
		if (!_closing)
		{
			_operations = RunOperationAsync(_operations, operation);
		}
	}

	private async Task RunOperationAsync(Task previous, Func<Task> operation)
	{
		await previous;
		if (_closing)
		{
			return;
		}

		try
		{
			await operation();
		}
		catch (Exception exception)
		{
			OperationFailed?.Invoke(this, exception.Message);
		}
	}

	internal async Task ShutdownAsync()
	{
		_closing = true;
		if (SystemTrayIcon is not null)
		{
			SystemTrayIcon.LeftClicked -= TrayIcon_LeftClicked;
			SystemTrayIcon.RightClicked -= TrayIcon_RightClicked;
			SystemTrayIcon.Dispose();
			SystemTrayIcon = null;
		}

		await _operations;
		try
		{
			if (_flyout is not null)
			{
				await _flyout.DisposeAsync();
			}
		}
		finally
		{
			if (_menu is not null)
			{
				await _menu.DisposeAsync();
			}

			_flyout = null;
			_menu = null;
		}
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
		return content;	}
}
