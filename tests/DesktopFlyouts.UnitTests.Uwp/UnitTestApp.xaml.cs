// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DesktopFlyouts.UnitTests.Uwp;

public sealed partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		Window.Current.Content ??= new Frame();
		UnitTestClient.CreateDefaultUI();
		Window.Current.Activate();
		UnitTestClient.Run(args.Arguments);
	}
}
