// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace DesktopFlyouts.WinUI.IntegrationTests;

public partial class App : Application
{
	private Window? _window;

	public App()
	{
		InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		_window = new Window { Title = "DesktopFlyouts WinUI integration tests", Content = new Grid() };
		_window.Activate();
		_window.DispatcherQueue.EnsureSystemDispatcherQueue();
		UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;

		try
		{
			Environment.ExitCode = await MicrosoftTestingPlatformApplication.RunAsync(Environment.GetCommandLineArgs()[1..]);
		}
		finally
		{
			_window.Close();
			Exit();
		}
	}
}
