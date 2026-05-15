#if WASDK
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
#endif

namespace U5BFA.Libraries
{
#if WASDK
	internal partial class PersistentAcrylicBackdrop : SystemBackdrop
	{
		private partial class ControllerEntry : IDisposable
		{
			public required ICompositionSupportsSystemBackdrop Target { get; init; }
			public required DesktopAcrylicController Controller { get; init; }

			public ControllerEntry()
			{
			}

			public void Initialize(SystemBackdropConfiguration configuration)
			{
				Controller.AddSystemBackdropTarget(Target);
				Controller.SetSystemBackdropConfiguration(configuration);
			}

			public void Dispose()
			{
				Controller.RemoveSystemBackdropTarget(Target);
				Controller.Dispose();
			}
		}


		private readonly HashSet<ControllerEntry> _controllers = [];

		protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
		{
			base.OnTargetConnected(target, xamlRoot);

			var controller = new DesktopAcrylicController();
			var configuration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);

			configuration.IsInputActive = true;

			var entry = new ControllerEntry() { Target = target, Controller = controller };
			entry.Initialize(configuration);
			_controllers.Add(entry);
		}

		protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
		{
			base.OnTargetDisconnected(target);

			var controller = _controllers.FirstOrDefault(x => x.Target == target);
			if (controller is null)
				return;

			controller.Dispose();
			_controllers.Remove(controller);
		}

		protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
		{
			base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);

			//var controller = _controllers.Where(x => x.Target == target).SingleOrDefault();
			//if (controller is null)
			//	return;

			//var configuration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);
			//configuration.IsInputActive = true;

			//controller.Controller.SetSystemBackdropConfiguration(configuration);
		}
	}
#endif
}
