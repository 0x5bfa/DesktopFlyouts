// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using WinRT;

namespace DesktopFlyouts;

internal abstract partial class DesktopFlyoutSystemBackdrop : SystemBackdrop
{
	private readonly SystemBackdropTheme _theme;
	private readonly Dictionary<ICompositionSupportsSystemBackdrop, TargetState> _targets = [];

	internal bool UseLightTheme => _theme is SystemBackdropTheme.Light;

	protected DesktopFlyoutSystemBackdrop(bool useLightTheme)
	{
		_theme = useLightTheme ? SystemBackdropTheme.Light : SystemBackdropTheme.Dark;
	}

	protected abstract ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration);

	internal void ClearFrom(DesktopFlyoutIslandTemplateSettings templateSettings)
	{
		var targets = new List<ICompositionSupportsSystemBackdrop>(_targets.Keys);
		templateSettings.SystemBackdrop = null;

		// SystemBackdropElement has now detached its links. Close them on the UI thread
		// before the last managed reference can be released by the finalizer thread.
		foreach (var target in targets)
		{
			if (!_targets.ContainsKey(target))
				target.As<IDisposable>().Dispose();
		}
	}

	protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		base.OnTargetConnected(target, xamlRoot);

		if (_targets.ContainsKey(target))
			return;

		var configuration = CreateConfiguration(target, xamlRoot);
		var controller = TryCreateController(configuration);
		// Keep the link available for UI-thread cleanup even if the material is unsupported.
		_targets[target] = new(controller, configuration);
		if (controller is null)
			return;

		controller.SetSystemBackdropConfiguration(configuration);
		controller.AddSystemBackdropTarget(target);
	}

	protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
	{
		base.OnTargetDisconnected(target);

		if (!_targets.Remove(target, out var state))
			return;

		state.Controller?.RemoveSystemBackdropTarget(target);

		if (state.Controller is IDisposable disposable)
			disposable.Dispose();
	}

	protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
		base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);

		if (!_targets.TryGetValue(target, out var state))
			return;

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

internal sealed partial class DesktopFlyoutAcrylicBackdrop : DesktopFlyoutSystemBackdrop
{
	internal DesktopFlyoutAcrylicBackdrop(bool useLightTheme)
		: base(useLightTheme)
	{
	}

	protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
	{
		return BackdropControllerHelpers.GetAcrylicController(configuration.Theme is SystemBackdropTheme.Light);
	}
}

internal sealed partial class DesktopFlyoutMicaBackdrop : DesktopFlyoutSystemBackdrop
{
	internal DesktopFlyoutMicaBackdrop(bool useLightTheme)
		: base(useLightTheme)
	{
	}

	protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
	{
		var controller = BackdropControllerHelpers.GetMicaController(configuration.Theme is SystemBackdropTheme.Light);
		if (controller is not null)
			controller.Kind = MicaKind.Base;

		return controller;
	}
}
