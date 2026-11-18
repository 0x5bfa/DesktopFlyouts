// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace DesktopFlyouts.WinUI.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class DesktopFlyoutLifecycleTests
{
	[UITestMethod]
	public async Task Repeated_open_close_reuses_connected_backdrops_on_the_UI_thread()
	{
		using var errors = new WrongThreadErrors();
		using var flyout = CreateFlyout();
		var ownerThread = Environment.CurrentManagedThreadId;
		flyout.TransitionStateChanged += (_, _) =>
		{
			Assert.AreEqual(ownerThread, Environment.CurrentManagedThreadId);
			Assert.IsTrue(flyout.DispatcherQueue.HasThreadAccess);
		};

		flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
		var island = (Control)GetPresenter(flyout).ContainerFromIndex(0);
		var backdrop = FindBackdropElement(island)?.SystemBackdrop;
		Assert.IsNotNull(backdrop);
		Assert.IsTrue(island.IsLoaded, "Exercise a live XAML island, not just backdrop construction.");
		var element = FindBackdropElement(island);
		Assert.IsNotNull(element);
		Assert.AreSame(backdrop, element.SystemBackdrop);

		for (var cycle = 0; cycle < 25; cycle++)
		{
			flyout.Hide();
			await WaitForState(flyout, DesktopFlyoutTransitionState.Closed);
			flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
			await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
			Assert.AreSame(backdrop, FindBackdropElement(island)?.SystemBackdrop, $"Backdrop changed on open {cycle + 2}.");
			Assert.AreSame(backdrop, element.SystemBackdrop);

			// Exercise wrapper finalization while the backdrop is still connected.
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		flyout.Dispose();
		Assert.IsNull(FindBackdropElement(island)?.SystemBackdrop);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		errors.AssertNone();
	}

	[UITestMethod]
	public async Task Library_chrome_is_not_exposed_as_consumer_styles()
	{
		using var flyout = CreateFlyout();
		flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
		Assert.IsFalse(typeof(FrameworkElement).IsAssignableFrom(typeof(DesktopFlyout)));
		foreach (var name in new[] { "Style", "Template", "Background", "BorderBrush", "Margin", "FlyoutPresenterStyle", "IslandContainerStyle", "BackdropKind" })
			Assert.IsNull(typeof(DesktopFlyout).GetProperty(name));
		var presenter = GetPresenter(flyout);
		Assert.IsFalse(presenter.GetType().IsPublic);
		Assert.IsFalse(presenter.ContainerFromIndex(0).GetType().IsPublic);
	}

	[UITestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task Hide_during_opening_completes_closed_and_can_open_again(bool animationsEnabled)
	{
		using var flyout = CreateFlyout();
		flyout.IsTransitionAnimationEnabled = animationsEnabled;
		for (var cycle = 0; cycle < 10; cycle++)
		{
			flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
			Assert.AreEqual(DesktopFlyoutTransitionState.Opening, flyout.TransitionState);
			flyout.Hide();
			await WaitForState(flyout, DesktopFlyoutTransitionState.Closed);
			Assert.IsFalse(flyout.IsOpen);
		}

		flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
		Assert.IsTrue(flyout.IsOpen);
	}

	[UITestMethod]
	public async Task Dispose_cancels_an_open_that_is_already_queued()
	{
		using var errors = new WrongThreadErrors();
		using var flyout = CreateFlyout();
		flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
		Assert.AreEqual(DesktopFlyoutTransitionState.Opening, flyout.TransitionState);
		flyout.Dispose();

		// Let the queued callback and any layout-delay continuations run.
		await Task.Delay(250);
		Assert.AreEqual(DesktopFlyoutTransitionState.Closed, flyout.TransitionState);
		Assert.IsFalse(flyout.IsOpen);
		Assert.AreEqual(0, GetPresenter(flyout).Items.Count);
		errors.AssertNone();
	}

	private static DesktopFlyout CreateFlyout()
	{
		var flyout = new DesktopFlyout
		{
			Width = 160, Height = 80,
			IsTransitionAnimationEnabled = false,
			IsLightDismissEnabled = false,
			ActivationMode = DesktopFlyoutActivationMode.NeverActivate,
		};
		flyout.Islands.Add(new DesktopFlyoutIsland { Content = new TextBlock { Text = "Lifecycle regression test" } });
		return flyout;
	}

	private static ItemsControl GetPresenter(DesktopFlyout flyout)
		=> (ItemsControl)typeof(DesktopFlyout).GetProperty("Presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flyout)!;

	[UITestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task Live_tray_bounds_move_the_open_flyout_without_reopening_and_stop_sampling_when_closed(bool animationsEnabled)
	{
		using var flyout = CreateFlyout();
		flyout.IsTransitionAnimationEnabled = animationsEnabled;
		flyout.Width = 160;
		var area = Microsoft.UI.Windowing.DisplayArea.Primary.WorkArea;
		Rectangle? bounds = new Rectangle(area.X + area.Width / 3, area.Y + area.Height / 3, 24, 24);
		var reads = 0;
		var request = CreateTrayRequest(() => { reads++; return bounds; });
		var openings = 0;
		flyout.TransitionStateChanged += (_, _) => { if (flyout.TransitionState is DesktopFlyoutTransitionState.Opening) { openings++; } };
		flyout.Show(request);
		await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
		var container = (FrameworkElement)VisualTreeHelper.GetParent(GetPresenter(flyout));
		var previousX = Canvas.GetLeft(container);
		var previousY = Canvas.GetTop(container);
		var backdrop = FindBackdropElement(GetPresenter(flyout))?.SystemBackdrop;

		bounds = new Rectangle(area.X + area.Width * 2 / 3, area.Y + area.Height * 2 / 3, 24, 24);
		var timeout = Stopwatch.StartNew();
		while (Math.Abs(Canvas.GetLeft(container) - previousX) + Math.Abs(Canvas.GetTop(container) - previousY) < 10)
		{
			Assert.IsTrue(timeout.Elapsed < TimeSpan.FromSeconds(3), "The live anchor did not update the actual host layout.");
			await Task.Delay(10);
		}

		var updatedX = Canvas.GetLeft(container);
		var updatedY = Canvas.GetTop(container);
		Assert.AreEqual(1, openings, "Repositioning must not restart the opening transition.");
		Assert.AreSame(backdrop, FindBackdropElement(GetPresenter(flyout))?.SystemBackdrop);
		bounds = null;
		await Task.Delay(400);
		Assert.AreEqual(updatedX, Canvas.GetLeft(container), 0.01, "Temporarily missing bounds must keep the last anchor.");
		Assert.AreEqual(updatedY, Canvas.GetTop(container), 0.01);

		flyout.Hide();
		await WaitForState(flyout, DesktopFlyoutTransitionState.Closed);
		var readsWhenClosed = reads;
		await Task.Delay(400);
		Assert.AreEqual(readsWhenClosed, reads, "Closed flyouts must stop reading shell geometry.");
	}

	[UITestMethod]
	public async Task Unavailable_tray_bounds_do_not_open_or_start_monitoring()
	{
		using var flyout = CreateFlyout();
		var reads = 0;
		flyout.Show(CreateTrayRequest(() => { reads++; return null; }));
		await Task.Delay(400);
		Assert.AreEqual(DesktopFlyoutTransitionState.Closed, flyout.TransitionState);
		Assert.AreEqual(1, reads);
	}

	private static DesktopFlyoutPosition CreateTrayRequest(Func<Rectangle?> readBounds)
	{
		// Replace only the shell bounds source. The actual library, XAML host, dispatcher,
		// sampler, transitions, and backdrop lifecycle all run normally.
		var factory = typeof(DesktopFlyoutPosition).GetMethod("FromTrayIconBounds", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(factory);
		return (DesktopFlyoutPosition)factory.Invoke(null, new object[] { readBounds })!;
	}

	private static async Task WaitForState(DesktopFlyout flyout, DesktopFlyoutTransitionState expected)
	{
		var timeout = Stopwatch.StartNew();
		while (flyout.TransitionState != expected)
		{
			Assert.IsLessThan(TimeSpan.FromSeconds(5), timeout.Elapsed, $"Timed out waiting for {expected}; actual state: {flyout.TransitionState}.");
			await Task.Delay(10);
		}
	}

	private static SystemBackdropElement? FindBackdropElement(DependencyObject root)
	{
		if (root is SystemBackdropElement element)
		{
			return element;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			if (FindBackdropElement(VisualTreeHelper.GetChild(root, i)) is { } child)
			{
				return child;
			}
		}

		return null;
	}

	private sealed class WrongThreadErrors : IDisposable
	{
		private readonly ConcurrentQueue<string> _errors = new();

		internal WrongThreadErrors()
		{
			AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
		}

		private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs args)
		{
			if (args.Exception.HResult == unchecked((int)0x8001010E))
			{
				_errors.Enqueue($"Thread {Environment.CurrentManagedThreadId}: {args.Exception}");
			}
		}

        internal void AssertNone()
        {
            Assert.IsTrue(_errors.IsEmpty, string.Join(Environment.NewLine, _errors));
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }
    }
}
