// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using WinRT;

namespace DesktopFlyouts;

internal abstract partial class DesktopFlyoutSystemBackdrop : SystemBackdrop
{
	private readonly SystemBackdropTheme _theme;
	private readonly Dictionary<ICompositionSupportsSystemBackdrop, TargetState> _targets = [];

	internal bool UseLightTheme => _theme is SystemBackdropTheme.Light;
	internal bool HasConnectedController => _targets.Values.Any(static state => state.Controller is not null);
	internal event Action? ControllerConnectionChanged;

	protected DesktopFlyoutSystemBackdrop(bool useLightTheme)
	{
		_theme = useLightTheme ? SystemBackdropTheme.Light : SystemBackdropTheme.Dark;
	}

	protected abstract ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration);

	internal void ClearFrom(SystemBackdropElement element)
	{
		var targets = new List<ICompositionSupportsSystemBackdrop>(_targets.Keys);
		element.SystemBackdrop = null;

		// SystemBackdropElement has now detached its links. Close them on the UI thread
		// before the last managed reference can be released by the finalizer thread.
		foreach (var target in targets)
		{
			if (!_targets.ContainsKey(target))
			{
				target.As<IDisposable>().Dispose();
			}
		}
	}

	protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		base.OnTargetConnected(target, xamlRoot);

		if (_targets.ContainsKey(target))
		{
			return;
		}

		var configuration = CreateConfiguration(target, xamlRoot);
		// Keep the link available for UI-thread cleanup even if the material is unsupported.
		var state = new TargetState(null, configuration);
		_targets.Add(target, state);
		ISystemBackdropControllerWithTargets? controller = null;
		try
		{
			controller = TryCreateController(configuration);
			if (controller is null)
			{
				return;
			}

			controller.SetSystemBackdropConfiguration(configuration);
			if (!controller.AddSystemBackdropTarget(target)
				|| !_targets.TryGetValue(target, out var currentState) || !ReferenceEquals(state, currentState))
			{
				return;
			}

			_targets[target] = state with { Controller = controller };
			controller = null;
		}
		catch (COMException exception)
		{
			Debug.WriteLine($"Could not connect the desktop flyout backdrop: 0x{exception.HResult:X8}.");
			return;
		}
		finally
		{
			(controller as IDisposable)?.Dispose();
		}

		ControllerConnectionChanged?.Invoke();
	}

	protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
	{
		base.OnTargetDisconnected(target);

		if (!_targets.Remove(target, out var state) || state.Controller is not { } controller)
		{
			return;
		}

		try
		{
			controller.RemoveSystemBackdropTarget(target);
		}
		finally
		{
			(controller as IDisposable)?.Dispose();
			ControllerConnectionChanged?.Invoke();
		}
	}

	protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);

		if (!_targets.TryGetValue(target, out var state))
		{
			return;
		}

		ApplyConfiguration(state.Configuration, target, xamlRoot);
		state.Controller?.SetSystemBackdropConfiguration(state.Configuration);
	}

	private SystemBackdropConfiguration CreateConfiguration(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		var configuration = new SystemBackdropConfiguration();
		ApplyConfiguration(configuration, target, xamlRoot);

		return configuration;
	}

	private void ApplyConfiguration(SystemBackdropConfiguration configuration, ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		var defaultConfiguration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);

		configuration.Theme = _theme;
		configuration.IsHighContrast = defaultConfiguration.IsHighContrast;
		configuration.IsInputActive = true;
	}

	private sealed record TargetState(ISystemBackdropControllerWithTargets? Controller, SystemBackdropConfiguration Configuration);
}
