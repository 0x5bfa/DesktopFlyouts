// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using DesktopFlyouts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts.WinUI.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed unsafe class TaskbarOverflowFlyoutMonitorTests
{
    [TestMethod]
    public void Hide_queues_one_deferred_completion()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            Assert.AreEqual(0, completions, "The close callback must be deferred through the dispatcher.");
            windows.PumpUntil(() => dispatch.Count > 0);
            Assert.AreEqual(0, completions);
            dispatch.Drain();
            Assert.AreEqual(1, completions);

            windows.PumpFor(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(1, completions, "A close must be reported once.");
        });
    }

    [TestMethod]
    public void Destroy_reports_close_once()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            PInvoke.DestroyWindow(overflow);
            windows.PumpUntil(() => dispatch.Count > 0);
            dispatch.Drain();
            windows.PumpFor(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(1, completions);
        });
    }

    [TestMethod]
    public void Hide_then_destroy_before_dispatch_still_reports_once()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            PInvoke.DestroyWindow(overflow);
            windows.PumpUntil(() => dispatch.Count > 0);
            Assert.AreEqual(0, completions);
            Assert.AreEqual(1, dispatch.Count);

            dispatch.Drain();
            windows.PumpFor(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(1, completions);
        });
    }

    [TestMethod]
    public void Missing_or_hidden_matching_window_is_not_discovered()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var bounds = windows.GetBounds(overflow);
            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            Assert.IsFalse(WindowHelpers.TryGetTaskbarOverflowWindowForBounds(bounds, out _));
            Assert.IsNull(TaskbarOverflowFlyoutMonitor.TryCreate(bounds, _ => true, () => Assert.Fail("Unexpected close.")));

            Assert.IsFalse(WindowHelpers.TryGetTaskbarOverflowWindowForBounds(new Rectangle(1000, 1000, 20, 20), out _));
            Assert.IsNull(TaskbarOverflowFlyoutMonitor.TryCreate(new Rectangle(1000, 1000, 20, 20), _ => true, () => Assert.Fail("Unexpected close.")));
        });
    }

    [TestMethod]
    public void Unrelated_client_child_and_show_events_are_ignored()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var unrelated = windows.CreateWindow(new Rectangle(-31000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            PInvoke.NotifyWinEvent(PInvoke.EVENT_OBJECT_SHOW, overflow, (int)OBJECT_IDENTIFIER.OBJID_WINDOW, (int)PInvoke.CHILDID_SELF);
            PInvoke.NotifyWinEvent(PInvoke.EVENT_OBJECT_HIDE, overflow, (int)OBJECT_IDENTIFIER.OBJID_CLIENT, (int)PInvoke.CHILDID_SELF);
            PInvoke.NotifyWinEvent(PInvoke.EVENT_OBJECT_HIDE, overflow, (int)OBJECT_IDENTIFIER.OBJID_WINDOW, 1);
            PInvoke.NotifyWinEvent(PInvoke.EVENT_OBJECT_HIDE, unrelated, (int)OBJECT_IDENTIFIER.OBJID_WINDOW, (int)PInvoke.CHILDID_SELF);
            windows.PumpFor(TimeSpan.FromMilliseconds(150));

            Assert.AreEqual(0, completions);
            Assert.AreEqual(0, dispatch.Count);
        });
    }

    [TestMethod]
    public void Disposing_after_completion_is_queued_cancels_callback()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            windows.PumpUntil(() => dispatch.Count == 1);
            Assert.AreEqual(0, completions);

            monitor.Dispose();
            dispatch.Drain();
            Assert.AreEqual(0, completions);
        });
    }

    [TestMethod]
    public void Native_callback_remains_rooted_across_collection()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => completions++);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            windows.PumpUntil(() => dispatch.Count > 0);
            dispatch.Drain();

            Assert.AreEqual(1, completions);
        });
    }

    [TestMethod]
    public void Bounds_include_full_current_window_size_for_multiple_rows()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 234, 74));
            var dispatch = new DeferredDispatcher();
            using var monitor = windows.CreateMonitor(overflow, dispatch, () => Assert.Fail("Unexpected close."));

            Assert.IsTrue(monitor.TryGetBounds(out var first));
            Assert.AreEqual(new Size(234, 74), first.Size);

            windows.Resize(overflow, 234, 154);
            Assert.IsTrue(monitor.TryGetBounds(out var twoRows));
            Assert.AreEqual(new Size(234, 154), twoRows.Size);

            windows.Resize(overflow, 154, 234);
            Assert.IsTrue(monitor.TryGetBounds(out var twoColumns));
            Assert.AreEqual(new Size(154, 234), twoColumns.Size);

            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            Assert.IsFalse(monitor.TryGetBounds(out _));
        });
    }

    [TestMethod]
    public void Rejecting_dispatch_disposes_monitor_and_unhooks()
    {
        RunOnSta(windows =>
        {
            var overflow = windows.CreateWindow(new Rectangle(-32000, -32000, 240, 80));
            var completions = 0;
            using var monitor = windows.CreateMonitor(overflow, _ => false, () => completions++);

            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            windows.PumpFor(TimeSpan.FromMilliseconds(150));
            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_SHOWNA);
            PInvoke.ShowWindow(overflow, SHOW_WINDOW_CMD.SW_HIDE);
            windows.PumpFor(TimeSpan.FromMilliseconds(150));

            Assert.AreEqual(0, completions);
            Assert.IsFalse(monitor.TryGetBounds(out _));
        });
    }

    private static void RunOnSta(Action<TestWindows> test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using (var windows = new TestWindows())
                {
                    test(windows);
                    windows.PumpFor(TimeSpan.FromMilliseconds(50));
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(5)), "The STA test thread did not finish in time.");
        if (failure is not null)
            Assert.Fail($"STA integration test failed: {failure}");
    }

    private sealed class DeferredDispatcher
    {
        private readonly Queue<Action> _pending = new();

        internal int Count => _pending.Count;

        internal bool TryEnqueue(Action callback)
        {
            _pending.Enqueue(callback);
            return true;
        }

        internal void Drain()
        {
            while (_pending.TryDequeue(out var callback))
                callback();
        }
    }

    private sealed class TestWindows : IDisposable
    {
        private const string OverflowWindowClass = "TopLevelWindowForOverflowXamlIsland";
        private readonly HINSTANCE _instance;
        private readonly List<HWND> _windows = new();
        private readonly ushort _classAtom;

        internal TestWindows()
        {
            _instance = PInvoke.GetModuleHandle(default);
            fixed (char* className = OverflowWindowClass)
            {
                WNDCLASSEXW windowClass = new()
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    lpfnWndProc = &WindowProcedure,
                    hInstance = _instance,
                    lpszClassName = (PCWSTR)className,
                };
                _classAtom = PInvoke.RegisterClassEx(&windowClass);
            }

            Assert.AreNotEqual((ushort)0, _classAtom, "Could not register the test-owned overflow window class.");
        }

        internal HWND CreateWindow(Rectangle bounds)
        {
            HWND window;
            fixed (char* className = OverflowWindowClass)
            {
                window = PInvoke.CreateWindowEx(
                    WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE,
                    (PCWSTR)className,
                    default,
                    WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE,
                    bounds.Left,
                    bounds.Top,
                    bounds.Width,
                    bounds.Height,
                    HWND.Null,
                    default,
                    _instance,
                    null);
            }

            Assert.IsFalse(window.IsNull, "Could not create test-owned window.");
            _windows.Add(window);
            return window;
        }

        internal TaskbarOverflowFlyoutMonitor CreateMonitor(HWND window, DeferredDispatcher dispatcher, Action onClosed) =>
            CreateMonitor(window, dispatcher.TryEnqueue, onClosed);

        internal TaskbarOverflowFlyoutMonitor CreateMonitor(HWND window, Func<Action, bool> tryDispatch, Action onClosed)
        {
            var bounds = GetBounds(window);
            var monitor = TaskbarOverflowFlyoutMonitor.TryCreate(bounds, tryDispatch, onClosed);
            Assert.IsNotNull(monitor, "The production overflow discovery code did not find the test window or hook registration failed.");
            return monitor!;
        }

        internal void Resize(HWND window, int width, int height)
        {
            Assert.IsTrue(PInvoke.SetWindowPos(
                window,
                HWND.Null,
                0,
                0,
                width,
                height,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE));
        }

        internal void PumpUntil(Func<bool> predicate)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!predicate() && stopwatch.Elapsed < TimeSpan.FromSeconds(2))
                PumpOnce();
            Assert.IsTrue(predicate(), "Timed out while waiting for a WinEvent callback.");
        }

        internal void PumpFor(TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
                PumpOnce();
        }

        private void PumpOnce()
        {
            MSG message;
            while (PInvoke.PeekMessage(&message, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
            {
                PInvoke.TranslateMessage(&message);
                PInvoke.DispatchMessage(&message);
            }

            Thread.Sleep(2);
        }

        internal Rectangle GetBounds(HWND window)
        {
            RECT rect;
            Assert.IsTrue(PInvoke.GetWindowRect(window, &rect));
            return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        }

        public void Dispose()
        {
            foreach (var window in _windows)
            {
                if (PInvoke.IsWindow(window))
                    PInvoke.DestroyWindow(window);
            }

            PumpFor(TimeSpan.FromMilliseconds(30));
            fixed (char* className = OverflowWindowClass)
                PInvoke.UnregisterClass((PCWSTR)className, _instance);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT WindowProcedure(HWND window, uint message, WPARAM wParam, LPARAM lParam) =>
        PInvoke.DefWindowProc(window, message, wParam, lParam);
}
