// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts.WinUI.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class TaskbarLayoutMonitorTests
{
	[UITestMethod]
	public async Task Move_hide_and_destroy_are_coalesced_and_deferred()
	{
		var window = await CreateTestWindow();
		var hwnd = GetHandle(window);
		var pending = new Queue<Action>();
		var changes = 0;
		using var monitor = TaskbarLayoutMonitor.TryCreate(new[] { hwnd }, action => { pending.Enqueue(action); return true; }, () => changes++);
		Assert.IsNotNull(monitor);
		try
		{
			Move(hwnd, -31900);
			PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			await WaitUntil(() => pending.Count > 0);
			Assert.AreEqual(0, changes);
			Assert.AreEqual(1, pending.Count);
			pending.Dequeue()();
			Assert.AreEqual(1, changes);

			window.Close();
			await WaitUntil(() => pending.Count > 0);
			pending.Dequeue()();
			Assert.AreEqual(2, changes);
		}
		finally
		{
			monitor.Dispose();
			if (PInvoke.IsWindow(hwnd))
			{
				window.Close();
			}
		}
	}

	[UITestMethod]
	public async Task Other_windows_are_ignored_and_a_replacement_can_be_tracked()
	{
		var first = await CreateTestWindow();
		var second = await CreateTestWindow();
		var pending = new Queue<Action>();
		using var monitor = TaskbarLayoutMonitor.TryCreate(new[] { GetHandle(first) }, action => { pending.Enqueue(action); return true; }, () => { });
		Assert.IsNotNull(monitor);
		try
		{
			Move(GetHandle(second), -31900);
			await Task.Delay(80);
			Assert.AreEqual(0, pending.Count);
			monitor.SetWindows(new[] { GetHandle(second) });
			Move(GetHandle(second), -31800);
			await WaitUntil(() => pending.Count == 1);
		}
		finally
		{
			monitor.Dispose();
			first.Close();
			second.Close();
		}
	}

	[UITestMethod]
	public async Task Disposal_cancels_an_already_queued_notification_and_rejected_dispatch_unhooks()
	{
		var window = await CreateTestWindow();
		var pending = new Queue<Action>();
		var changes = 0;
		var monitor = TaskbarLayoutMonitor.TryCreate(new[] { GetHandle(window) }, action => { pending.Enqueue(action); return true; }, () => changes++);
		Assert.IsNotNull(monitor);
		try
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Move(GetHandle(window), -31900);
			await WaitUntil(() => pending.Count == 1);
			monitor.Dispose();
			pending.Dequeue()();
			Assert.AreEqual(0, changes);

			var attempts = 0;
			using var rejected = TaskbarLayoutMonitor.TryCreate(new[] { GetHandle(window) }, _ => { attempts++; return false; }, () => Assert.Fail("Rejected notification ran."));
			Assert.IsNotNull(rejected);
			Move(GetHandle(window), -31800);
			await WaitUntil(() => attempts == 1);
			Move(GetHandle(window), -31700);
			await Task.Delay(80);
			Assert.AreEqual(1, attempts);
		}
		finally
		{
			monitor.Dispose();
			window.Close();
		}
	}

	private static async Task<Window> CreateTestWindow()
	{
		var window = new Window { Content = new Grid() };
		window.Activate();
		Move(GetHandle(window), -32000);
		await Task.Delay(40);
		return window;
	}

    private static HWND GetHandle(Window window)
    {
        return (HWND)WinRT.Interop.WindowNative.GetWindowHandle(window);
    }

    private static void Move(HWND hwnd, int x)
	{
		Assert.IsTrue(PInvoke.SetWindowPos(hwnd, HWND.Null, x, -32000, 240, 80,
			SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE));
	}

	private static async Task WaitUntil(Func<bool> predicate)
	{
		var timeout = Stopwatch.StartNew();
		while (!predicate())
		{
			Assert.IsTrue(timeout.Elapsed < TimeSpan.FromSeconds(3), "Timed out waiting for a native window event.");
			await Task.Delay(10);
		}
	}
}
