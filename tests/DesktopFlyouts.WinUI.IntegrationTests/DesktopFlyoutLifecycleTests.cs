// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    [DataRow(DesktopFlyoutBackdropKind.DesktopAcrylic)]
    [DataRow(DesktopFlyoutBackdropKind.Mica)]
    public async Task Repeated_open_close_reuses_connected_backdrops_on_the_UI_thread(DesktopFlyoutBackdropKind kind)
    {
        using var errors = new WrongThreadErrors();
        using var flyout = CreateFlyout(kind);
        var island = flyout.Islands[0];
        var ownerThread = Environment.CurrentManagedThreadId;
        flyout.TransitionStateChanged += (_, _) =>
        {
            Assert.AreEqual(ownerThread, Environment.CurrentManagedThreadId);
            Assert.IsTrue(flyout.DispatcherQueue.HasThreadAccess);
        };

        flyout.Show();
        await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
        var backdrop = island.TemplateSettings.SystemBackdrop;
        Assert.IsNotNull(backdrop);
        Assert.IsTrue(island.IsLoaded, "Exercise a live XAML island, not just backdrop construction.");
        var element = FindBackdropElement(island);
        Assert.IsNotNull(element);
        Assert.AreSame(backdrop, element.SystemBackdrop);

        for (var cycle = 0; cycle < 25; cycle++)
        {
            flyout.Hide();
            await WaitForState(flyout, DesktopFlyoutTransitionState.Closed);
            flyout.Show();
            await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
            Assert.AreSame(backdrop, island.TemplateSettings.SystemBackdrop, $"Backdrop changed on open {cycle + 2}.");
            Assert.AreSame(backdrop, element.SystemBackdrop);

            // Exercise wrapper finalization while the backdrop is still connected.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        flyout.Dispose();
        Assert.IsNull(island.TemplateSettings.SystemBackdrop);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        errors.AssertNone();
    }

    [UITestMethod]
    public async Task Backdrop_settings_changes_replace_or_disconnect_the_visible_backdrop()
    {
        using var flyout = CreateFlyout(DesktopFlyoutBackdropKind.DesktopAcrylic);
        flyout.Show();
        await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
        var island = flyout.Islands[0];
        var element = FindBackdropElement(island);
        Assert.IsNotNull(element);
        var acrylic = island.TemplateSettings.SystemBackdrop;
        Assert.IsNotNull(acrylic);

        flyout.BackdropKind = DesktopFlyoutBackdropKind.Mica;
        var mica = island.TemplateSettings.SystemBackdrop;
        Assert.IsNotNull(mica);
        Assert.AreNotSame(acrylic, mica);
        Assert.AreSame(mica, element.SystemBackdrop);

        flyout.IsBackdropEnabled = false;
        Assert.IsNull(island.TemplateSettings.SystemBackdrop);
        Assert.IsNull(element.SystemBackdrop);

        flyout.IsBackdropEnabled = true;
        var reenabled = island.TemplateSettings.SystemBackdrop;
        Assert.IsNotNull(reenabled);
        Assert.AreNotSame(mica, reenabled);
        Assert.AreSame(reenabled, element.SystemBackdrop);
    }

    [UITestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Hide_during_opening_completes_closed_and_can_open_again(bool animationsEnabled)
    {
        using var flyout = CreateFlyout(DesktopFlyoutBackdropKind.DesktopAcrylic);
        flyout.IsTransitionAnimationEnabled = animationsEnabled;
        for (var cycle = 0; cycle < 10; cycle++)
        {
            flyout.Show();
            Assert.AreEqual(DesktopFlyoutTransitionState.Opening, flyout.TransitionState);
            flyout.Hide();
            await WaitForState(flyout, DesktopFlyoutTransitionState.Closed);
            Assert.IsFalse(flyout.IsOpen);
        }

        flyout.Show();
        await WaitForState(flyout, DesktopFlyoutTransitionState.Open);
        Assert.IsTrue(flyout.IsOpen);
    }

    [UITestMethod]
    public async Task Dispose_cancels_an_open_that_is_already_queued()
    {
        using var errors = new WrongThreadErrors();
        using var flyout = CreateFlyout(DesktopFlyoutBackdropKind.DesktopAcrylic);
        flyout.Show();
        Assert.AreEqual(DesktopFlyoutTransitionState.Opening, flyout.TransitionState);
        flyout.Dispose();

        // Let the queued callback and any layout-delay continuations run.
        await Task.Delay(250);
        Assert.AreEqual(DesktopFlyoutTransitionState.Closed, flyout.TransitionState);
        Assert.IsFalse(flyout.IsOpen);
        Assert.IsNull(flyout.Islands[0].TemplateSettings.SystemBackdrop);
        errors.AssertNone();
    }

    private static DesktopFlyout CreateFlyout(DesktopFlyoutBackdropKind kind)
    {
        var flyout = new DesktopFlyout
        {
            BackdropKind = kind,
            IsBackdropEnabled = true,
            IsTransitionAnimationEnabled = false,
            HideOnLostFocus = false,
            ActivationMode = DesktopFlyoutActivationMode.NeverActivate,
        };
        flyout.Islands.Add(new DesktopFlyoutIsland
        {
            Width = 160,
            Height = 80,
            Content = new TextBlock { Text = "Lifecycle regression test" },
        });
        Assert.IsTrue(flyout.ApplyTemplate());
        return flyout;
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
            return element;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindBackdropElement(VisualTreeHelper.GetChild(root, i)) is { } child)
                return child;
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
                _errors.Enqueue($"Thread {Environment.CurrentManagedThreadId}: {args.Exception}");
        }

        internal void AssertNone() => Assert.IsTrue(_errors.IsEmpty, string.Join(Environment.NewLine, _errors));

        public void Dispose() => AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
    }
}
