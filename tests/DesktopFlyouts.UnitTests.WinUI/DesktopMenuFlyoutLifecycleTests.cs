// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.Win32;

namespace DesktopFlyouts.UnitTests.WinUI;

[TestClass]
[DoNotParallelize]
public sealed class DesktopMenuFlyoutLifecycleTests
{
	[UITestMethod]
	public void Canceled_native_open_leaves_the_desktop_menu_and_host_closed()
	{
		using var flyout = new DesktopMenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		var nativeMenu = GetNativeMenu(flyout);
		var openingRaised = false;
		nativeMenu.Opening += (_, _) =>
		{
			openingRaised = true;
			nativeMenu.Hide();
		};

		flyout.Show(new Point(100, 100));

		Assert.IsTrue(openingRaised);
		Assert.IsFalse(nativeMenu.IsOpen);
		Assert.IsFalse(flyout.IsOpen);
		Assert.IsFalse(PInvoke.IsWindowVisible(GetHost(flyout).HWnd));
	}

	[UITestMethod]
	public async Task Show_after_hide_reopens_when_the_native_close_completes()
	{
		using var flyout = new DesktopMenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem { Text = "Item" });
		var nativeMenu = GetNativeMenu(flyout);
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		nativeMenu.Opened += (_, _) => opened.TrySetResult();

		flyout.Show(new Point(100, 100));
		await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.IsTrue(flyout.IsOpen);

		opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		flyout.Hide();
		flyout.Show(new Point(200, 200));
		await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.IsTrue(nativeMenu.IsOpen);
		Assert.IsTrue(flyout.IsOpen);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_menuFlyout")]
	private static extern ref MenuFlyout GetNativeMenu(DesktopMenuFlyout flyout);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_host")]
	private static extern ref XamlIslandHostWindow GetHost(DesktopMenuFlyout flyout);
}
