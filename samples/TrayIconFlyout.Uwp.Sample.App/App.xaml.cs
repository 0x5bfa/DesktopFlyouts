// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinRT;

namespace U5BFA.Libraries
{
	public sealed partial class App : Application
	{
		private bool _isTrayHostLaunchRequested;

		public App()
		{
			InitializeComponent();

			Suspending += OnSuspending;
		}

		protected override async void OnLaunched(LaunchActivatedEventArgs args)
		{
			InitializeWindow(args.PreviousExecutionState, args.Arguments);
			await LaunchTrayHostAsync();
		}

		protected override void OnActivated(IActivatedEventArgs args)
		{
			object? navigationParameter = null;
			if (args.Kind is ActivationKind.Protocol)
			{
				navigationParameter = args.As<ProtocolActivatedEventArgs>().Data;
			}

			InitializeWindow(args.PreviousExecutionState, navigationParameter);
		}

		private void InitializeWindow(ApplicationExecutionState previousExecutionState, object? navigationParameter)
		{
			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active.
			if (Window.Current.Content is not Frame rootFrame)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();
				rootFrame.NavigationFailed += OnNavigationFailed;

				if (previousExecutionState == ApplicationExecutionState.Terminated)
				{
					// TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			if (rootFrame.Content == null)
			{
				CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true; 
				
				rootFrame.Navigate(typeof(MainPage), navigationParameter);
			}

			// Ensure the current window is active
			Window.Current.Activate();
		}

		private async System.Threading.Tasks.Task LaunchTrayHostAsync()
		{
			if (_isTrayHostLaunchRequested)
				return;

			_isTrayHostLaunchRequested = true;

			try
			{
				await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to launch tray host process: {ex}");
			}
		}

		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception($"Failed to load page '{e.SourcePageType.FullName}'.");
		}

		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

			// TODO: Save application state and stop any background activity
			deferral.Complete();
		}
	}
}
