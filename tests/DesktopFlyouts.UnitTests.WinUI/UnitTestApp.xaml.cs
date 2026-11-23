// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace DesktopFlyouts.UnitTests.WinUI;

public partial class UnitTestApp : Application
{
	private Window? _window;

	public UnitTestApp()
	{
		InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		_window = new UnitTestAppWindow();
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
