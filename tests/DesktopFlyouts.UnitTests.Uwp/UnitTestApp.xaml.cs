// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Testing.Extensions;
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

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		Window.Current.Content ??= new Frame();
		Window.Current.Activate();
		try
		{
			Environment.ExitCode = await MicrosoftTestingPlatformApplication.RunAsync(PackagedAppExtensions.GetTestApplicationArguments(args.Arguments));
		}
		finally
		{
			Exit();
		}
	}
}
