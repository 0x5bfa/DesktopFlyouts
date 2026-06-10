// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if WASDK
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace DesktopFlyouts
{
    internal abstract partial class DesktopFlyoutSystemBackdrop : SystemBackdrop
    {
        private readonly Dictionary<ICompositionSupportsSystemBackdrop, TargetState> _targets = [];

        protected abstract ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration);

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
        {
            base.OnTargetConnected(target, xamlRoot);

            if (_targets.ContainsKey(target))
                return;

            var configuration = CreateConfiguration(target, xamlRoot);
            var controller = TryCreateController(configuration);
            if (controller is null)
                return;

            controller.SetSystemBackdropConfiguration(configuration);
            controller.AddSystemBackdropTarget(target);

            _targets[target] = new(controller, configuration);
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
        {
            base.OnTargetDisconnected(target);

            if (!_targets.Remove(target, out var state))
                return;

            state.Controller.RemoveSystemBackdropTarget(target);

            if (state.Controller is IDisposable disposable)
                disposable.Dispose();
        }

        protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
        {
            base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);

            if (!_targets.TryGetValue(target, out var state))
                return;

            ApplyConfiguration(state.Configuration);
            state.Controller.SetSystemBackdropConfiguration(state.Configuration);
        }

        private SystemBackdropConfiguration CreateConfiguration(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
        {
            var configuration = new SystemBackdropConfiguration();
            ApplyConfiguration(configuration);

            return configuration;
        }

        private static void ApplyConfiguration(SystemBackdropConfiguration configuration)
        {
            configuration.IsInputActive = true;
        }

        private sealed record TargetState(ISystemBackdropControllerWithTargets Controller, SystemBackdropConfiguration Configuration);
    }

    internal sealed partial class DesktopFlyoutAcrylicBackdrop(bool useLightTheme) : DesktopFlyoutSystemBackdrop
    {
        protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
        {
            return BackdropControllerHelpers.GetAcrylicController(useLightTheme);
        }
    }

    internal sealed partial class DesktopFlyoutMicaBackdrop(bool useLightTheme) : DesktopFlyoutSystemBackdrop
    {
        protected override ISystemBackdropControllerWithTargets? TryCreateController(SystemBackdropConfiguration configuration)
        {
            var controller = BackdropControllerHelpers.GetMicaController(useLightTheme);
            controller?.Kind = MicaKind.Base;
            return controller;
        }
    }
}
#endif
