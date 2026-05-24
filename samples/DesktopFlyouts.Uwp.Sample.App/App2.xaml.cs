// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinRT;

namespace DesktopFlyouts
{
	public sealed partial class App2 : Application
	{
		public App2()
		{
			InitializeComponent();

			Suspending += OnSuspending;
		}

		protected override void OnActivated(IActivatedEventArgs args)
		{
			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active.
			if (Window.Current.Content is not Frame rootFrame)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();
				rootFrame.NavigationFailed += OnNavigationFailed;

				if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			if (rootFrame.Content == null)
			{
				CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true; 
				
				rootFrame.Navigate(typeof(MainPage), args.As<ProtocolActivatedEventArgs>().Data);
			}

			// Ensure the current window is active
			Window.Current.Activate();
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
