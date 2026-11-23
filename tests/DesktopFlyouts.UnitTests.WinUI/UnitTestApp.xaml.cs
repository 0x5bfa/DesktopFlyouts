// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace DesktopFlyouts.UnitTests.WinUI;

public partial class UnitTestApp : Application
{
	private Window? _window;

	public UnitTestApp()
	{
		InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		UnitTestClient.CreateDefaultUI();
		_window = new UnitTestAppWindow();
		_window.Activate();
		_window.DispatcherQueue.EnsureSystemDispatcherQueue();
		UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;
		UnitTestClient.Run(Environment.CommandLine);
	}
}
