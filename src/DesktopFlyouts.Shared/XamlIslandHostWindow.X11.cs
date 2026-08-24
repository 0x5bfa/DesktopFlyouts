#if HAS_UNO
// X11 implementation of XamlIslandHostWindow.
// Uses Microsoft.UI.Xaml.Window for XAML rendering and raw X11 P/Invoke
// for window property manipulation (borderless, override_redirect, positioning).

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.NativeElementHosting;
using static DesktopFlyouts.X11PInvoke;

namespace DesktopFlyouts;

internal partial class XamlIslandHostWindow : IDisposable
{
    private readonly Window _window;
    private readonly nint _display, _x11Window;
    private readonly nint _netWmStateAtom, _netWmStateSkipTaskbarAtom,
        _netWmStateSkipPagerAtom, _netWmStateAboveAtom, _netWmWindowTypeAtom,
        _netWmWindowTypeDockAtom, _motifWmHintsAtom, _netActiveWindowAtom,
        _netWmOpacityAtom;
    private bool _disposed;
    private bool _focused;
    private bool _focusMonitoring;
    private nint _previousActiveWindow;
    private readonly DispatcherTimer _focusTimer;
    private DesktopFlyoutActivationMode _activationMode = DesktopFlyoutActivationMode.Activate;

    internal object? DesktopWindowXamlSource { get; private set; }

    internal Rect WindowSize
    {
        get
        {
            if (_display is 0 || _x11Window is 0)
                return default;

            var attrs = new XWindowAttributes();
            if (XGetWindowAttributes(_display, _x11Window, ref attrs) is 0)
                return default;

            var rootWindow = XDefaultRootWindow(_display);
            if (rootWindow is not 0 &&
                XTranslateCoordinates(
                    _display,
                    _x11Window,
                    rootWindow,
                    0,
                    0,
                    out var rootX,
                    out var rootY,
                    out _) is not 0)
                return new Rect(rootX, rootY, attrs.Width, attrs.Height);

            return new Rect(attrs.X, attrs.Y, attrs.Width, attrs.Height);
        }
    }

    internal double XamlIslandRasterizationScale
    {
        get => 1.0D;
    }

    private EventHandler? _windowInactivated;
    private bool _windowVisible;

    internal event EventHandler? WindowInactivated
    {
        add
        {
            _windowInactivated += value;
            if (_windowVisible)
                StartFocusMonitoring();
        }
        remove
        {
            _windowInactivated -= value;
            if (_windowInactivated is null)
                StopFocusMonitoring();
        }
    }
    internal event EventHandler? SystemSettingsChanged;

    private readonly List<nint> managedWindows;

    internal XamlIslandHostWindow()
    {
        ThrowHelper.ThrowIfNotLinux();

        _window = new TransparentWindow();
        _window.Title = "DesktopFlyoutHost";

        var nativeWindow = Uno.UI.Xaml.WindowHelper.GetNativeWindow(_window) as X11NativeWindow
            ?? throw new InvalidOperationException("Uno did not create an X11 native window.");
        if ((_display = XOpenDisplay(0)) is 0)
            throw new InvalidOperationException("Failed to open X11 display.");

        _x11Window = nativeWindow.WindowId;

        managedWindows = new(2) { _x11Window };
        if (TryGetTopWindow(_display, _x11Window, out var topWindow))
            managedWindows.Add(topWindow);

        // Cache atoms.
        _netWmStateAtom = XInternAtom(_display, "_NET_WM_STATE", false);
        _netWmStateSkipTaskbarAtom = XInternAtom(_display, "_NET_WM_STATE_SKIP_TASKBAR", false);
        _netWmStateSkipPagerAtom = XInternAtom(_display, "_NET_WM_STATE_SKIP_PAGER", false);
        _netWmStateAboveAtom = XInternAtom(_display, "_NET_WM_STATE_ABOVE", false);
        _netWmWindowTypeAtom = XInternAtom(_display, "_NET_WM_WINDOW_TYPE", false);
        _netWmWindowTypeDockAtom = XInternAtom(_display, "_NET_WM_WINDOW_TYPE_DOCK", false);
        _motifWmHintsAtom = XInternAtom(_display, "_MOTIF_WM_HINTS", false);
        _netActiveWindowAtom = XInternAtom(_display, "_NET_ACTIVE_WINDOW", false);
        _netWmOpacityAtom = XInternAtom(_display, "_NET_WM_WINDOW_OPACITY", false);

        // Configure as a borderless utility window.
        ConfigureWindow();

        // Set up a DesktopWindowXamlSource-like object for null-check compatibility.
        // On X11, the Window itself handles XAML rendering, so we provide a non-null marker.
        DesktopWindowXamlSource = new object();

        // Subscribe to window events.
        _window.Closed += OnWindowClosed;
        _window.Activated += OnWindowActivated;
        GeneralHelpers.SystemSettingsChanged += GeneralHelpers_SystemSettingsChanged;

        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _focusTimer.Tick += FocusTimer_Tick;

        // Listen for property changes on the root window (theme changes, etc.).
        var rootWindow = XDefaultRootWindow(_display);
        if (rootWindow is not 0)
        {
            XSelectInput(_display, rootWindow, (nint)(PropertyChangeMask | StructureNotifyMask));
        }
    }

    private void ConfigureWindow()
    {
        if (_display is 0 || _x11Window is 0)
            return;

        // NeverActivate windows bypass the window manager so it cannot assign focus.
        // Activate and NoActivateOnOpen remain WM-managed and differ only in whether
        // an activation request is sent when they are mapped.
        foreach (var window in managedWindows)
            SetOverrideRedirect(window, X11WindowActivation.UsesOverrideRedirect(_activationMode));

        // Remove window decorations by setting Motif WM hints (no title, no resize, no close).
        ReadOnlySpan<nuint> motifHints = [
            2, // MWM_HINTS_DECORATIONS
            0, // functions
            0, // decorations
            0, // input_mode
            0, // status
        ];
        XChangeProperty32(
            _display,
            _x11Window,
            _motifWmHintsAtom,
            _motifWmHintsAtom,
            PropertyMode.Replace,
            motifHints);

        // Set EWMH hints on BOTH windows — task managers may read either.
        foreach (var wnd in managedWindows)
        {
            SetNetWmState(wnd);
            SetNetWmWindowType(wnd);
        }

        XFlush(_display);
    }

    private void SetNetWmState(nint window)
    {
        ReadOnlySpan<nuint> stateAtoms = [
            (nuint)_netWmStateSkipTaskbarAtom,
            (nuint)_netWmStateSkipPagerAtom,
            (nuint)_netWmStateAboveAtom,
        ];
        XChangeProperty32(
            _display,
            window,
            _netWmStateAtom,
            XA_ATOM,
            PropertyMode.Replace,
            stateAtoms);
    }

    private void SetNetWmWindowType(nint window)
    {
        ReadOnlySpan<nuint> typeAtoms = [(nuint)_netWmWindowTypeDockAtom];
        XChangeProperty32(
            _display,
            window,
            _netWmWindowTypeAtom,
            XA_ATOM,
            PropertyMode.Replace,
            typeAtoms);
    }

    internal void SetContent(object content)
    {
        _window.Content = content as UIElement;
    }

    internal void PreserveActivationState()
    {
        _previousActiveWindow = TryGetActiveWindow(out var activeWindow) &&
            !managedWindows.Contains(activeWindow)
            ? activeWindow
            : 0;
    }

    internal void RestoreActivationState()
    {
        if (_display is 0 || _previousActiveWindow is 0)
            return;

        if (!TryGetActiveWindow(out var activeWindow) || activeWindow != _previousActiveWindow)
            RequestActivation(_previousActiveWindow);
    }

    internal void MoveAndResize(RectInt32 rect, bool activate = true)
    {
        if (_display is 0 || _x11Window is 0)
            return;

        // Ensure the window has a minimum size of 1x1 — X11 rejects 0-dimension windows with BadValue.
        var safeWidth = Math.Max(1, rect.Width);
        var safeHeight = Math.Max(1, rect.Height);

        // Use AppWindow.Resize to notify the Skia rendering pipeline of the new size.
        _window.AppWindow.Resize(new SizeInt32 { Width = safeWidth, Height = safeHeight });

        // Use raw X11 for positioning to avoid Uno's pipeline re-reading the WM-overridden position.
        XMoveWindow(_display, _x11Window, rect.X, rect.Y);
        XSync(_display, false);

        if (X11WindowActivation.ShouldRequestActivation(_activationMode, activate))
            RequestActivation(_x11Window);

        XSync(_display, false);
    }

    internal void Maximize(System.Drawing.Rectangle workArea, bool activate = true)
    {
        if (_display is 0 || _x11Window is 0)
        {
            return;
        }

        var x = workArea.X;
        var y = workArea.Y;
        var w = workArea.Width;
        var h = workArea.Height;

        if (w <= 0 || h <= 0)
        {
            // Maximize: workArea is zero-sized, falling back to screen dimensions
            var rootAttrs = new XWindowAttributes();
            XGetWindowAttributes(_display, XDefaultRootWindow(_display), ref rootAttrs);
            w = rootAttrs.Width;
            h = rootAttrs.Height;
            if (w <= 0 || h <= 0)
            {
                w = 1920; h = 1080;
            }
        }

        // Use Uno's Window API so the rendering pipeline is notified.
        _window.AppWindow.Resize(new SizeInt32 { Width = w, Height = h });
        XMoveWindow(_display, _x11Window, x, y);
        XSync(_display, false);

        XRaiseWindow(_display, _x11Window);
        if (X11WindowActivation.ShouldRequestActivation(_activationMode, activate))
            RequestActivation(_x11Window);

        XSync(_display, false);
    }

    internal void SetHWndRectRegion(RectInt32 rect)
    {
        if (_display is 0 || _x11Window is 0)
            return;

        // Create a rectangular region and apply it to the window shape.
        var region = XCreateRegion();
        if (region is 0)
            return;

        var xRect = new XRegionRectangle
        {
            x = (short)rect.X,
            y = (short)rect.Y,
            width = (ushort)rect.Width,
            height = (ushort)rect.Height
        };

        unsafe
        {
            XUnionRectWithRegion((XRegionRectangle*)Unsafe.AsPointer(ref xRect), region, region);
        }

        // ShapeInput = 2, ShapeSet = 0
        XShapeCombineRegion(_display, _x11Window, 2, 0, 0, region, 0);
        XDestroyRegion(region);
        XFlush(_display);
    }

    internal ValueTask UpdateWindowVisibility(bool isVisible, bool activate = true)
    {
        if (_display is 0 || _x11Window is 0)
        {
            return default;
        }

        _windowVisible = isVisible;

        if (isVisible)
        {
            // Set _NET_WM_WINDOW_OPACITY = 0 BEFORE mapping so the compositor
            // renders the window fully invisible during the black flash gap
            // (between XMapWindow and Skia's first presented frame).
            SetWindowOpacity(_x11Window, 0);
            foreach (var wnd in managedWindows)
                if (wnd != _x11Window)
                    SetWindowOpacity(wnd, 0);
            XFlush(_display);

            // Re-apply the activation policy immediately before Uno maps both its
            // root and rendering windows.
            foreach (var window in managedWindows)
                SetOverrideRedirect(window, X11WindowActivation.UsesOverrideRedirect(_activationMode));

            // Capture current position so we can re-apply it after map.
            var currentAttrs = new XWindowAttributes();
            XGetWindowAttributes(_display, _x11Window, ref currentAttrs);
            var savedX = currentAttrs.X;
            var savedY = currentAttrs.Y;

            // Map both RootX11Window and TopX11Window (child where Skia renders).
            foreach (var wnd in managedWindows)
                XMapWindow(_display, wnd);

            // Re-apply position after map — KWin overrides our position during XMapWindow.
            XMoveWindow(_display, _x11Window, savedX, savedY);

            // Re-apply EWMH properties after map — the WM may strip/override them.
            foreach (var wnd in managedWindows)
            {
                SetNetWmState(wnd);
                SetNetWmWindowType(wnd);
            }

            XRaiseWindow(_display, _x11Window);
            if (X11WindowActivation.ShouldRequestActivation(_activationMode, activate))
                RequestActivation(_x11Window);
            XSync(_display, false);

            // Restore opacity after Skia has presented at least one frame.
            // Task.Delay avoids needing CompositionTarget.Rendering (which has thread issues).
            var opacityTask = RestoreOpacityAfterDelay();
            XFlush(_display);

            // Start monitoring focus changes on our X11 window.
            StartFocusMonitoring();

            return opacityTask;
        }
        else
        {
            // Stop monitoring focus changes when hiding.
            StopFocusMonitoring();

            foreach (var wnd in managedWindows)
            {
                XUnmapWindow(_display, wnd);
            }

            XFlush(_display);
            return default;
        }
    }

    private bool _opacityRestoring;

    private async ValueTask RestoreOpacityAfterDelay()
    {
        if (_opacityRestoring)
            return;

        _opacityRestoring = true;
        try
        {
            // Wait long enough for Skia to render and present at least one frame.
            await Task.Delay(32); // ~2 frames at 60fps

            if (_disposed)
                return;

            SetWindowOpacity(_x11Window, 0xFFFFFFFF);
            foreach (var wnd in managedWindows)
                if (wnd != _x11Window)
                    SetWindowOpacity(wnd, 0xFFFFFFFF);
            XFlush(_display);
        }
        finally
        {
            _opacityRestoring = false;
        }
    }

    private static bool TryGetTopWindow(nint display, nint rootWindow, out nint topWindow)
    {
        topWindow = 0;
        if (XQueryTree(display, rootWindow, out _, out _, out var children, out var nchildren) == 0)
            return false;

        try
        {
            if (nchildren > 0 && children is not 0)
            {
                topWindow = Marshal.ReadIntPtr(children, 0);
                return true;
            }
        }
        finally
        {
            if (children is not 0)
                XFree(children);
        }
        return false;
    }

    internal void SetActivationMode(DesktopFlyoutActivationMode activationMode)
    {
        _activationMode = activationMode;

        if (_display is 0 || _x11Window is 0)
            return;

        foreach (var window in managedWindows)
            SetOverrideRedirect(window, X11WindowActivation.UsesOverrideRedirect(activationMode));
        XFlush(_display);
    }

    internal bool NavigateFocus(object reason)
    {
        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate ||
            _display is 0 || _x11Window is 0)
            return false;

        XSetInputFocus(_display, _x11Window, 1 /* RevertToParent */, 0 /* CurrentTime */);
        XFlush(_display);
        return true;
    }

    private void SetWindowOpacity(nint window, uint opacity)
    {
        ReadOnlySpan<nuint> data = [opacity];
        XChangeProperty32(
            _display,
            window,
            _netWmOpacityAtom,
            (nint)XA_CARDINAL,
            PropertyMode.Replace,
            data);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        StopFocusMonitoring();
        _focusTimer.Stop();

        _window.Closed -= OnWindowClosed;
        _window.Activated -= OnWindowActivated;
        GeneralHelpers.SystemSettingsChanged -= GeneralHelpers_SystemSettingsChanged;

        if (_display is not 0 && _x11Window is not 0)
        {
            XUnmapWindow(_display, _x11Window);
            XFlush(_display);
        }
        if (_display is not 0)
        {
            XCloseDisplay(_display);
        }

        _window.Content = null;
        _window.Close();

        GC.SuppressFinalize(this);
    }
    private void SetOverrideRedirect(nint window, bool enabled)
    {
        var attrs = new XSetWindowAttributes
        {
            override_redirect = enabled ? 1 : 0
        };
        XChangeWindowAttributes(_display, window, CWOverrideRedirect, ref attrs);
    }

    private void RequestActivation(nint window)
    {
        if (window is 0)
            return;

        _ = TryGetActiveWindow(out var currentActiveWindow);
        var clientMessage = new XEvent
        {
            type = ClientMessage,
            xclient = new XClientMessageEvent
            {
                type = ClientMessage,
                display = _display,
                window = window,
                message_type = _netActiveWindowAtom,
                format = 32,
                ptr1 = 1, // normal application
                ptr2 = 0, // CurrentTime
                ptr3 = currentActiveWindow,
            }
        };
        var rootWindow = XDefaultRootWindow(_display);
        if (rootWindow is not 0)
        {
            XSendEvent(
                _display,
                rootWindow,
                false,
                (nint)(SubstructureRedirectMask | SubstructureNotifyMask),
                ref clientMessage);
            XFlush(_display);
        }
    }

    private bool TryGetActiveWindow(out nint activeWindow)
    {
        activeWindow = 0;
        var rootWindow = XDefaultRootWindow(_display);
        if (rootWindow is not 0)
        {
            var status = XGetWindowProperty(
                _display,
                (nuint)rootWindow,
                (nuint)_netActiveWindowAtom,
                0,
                1,
                false,
                0,
                out var actualType,
                out var actualFormat,
                out var itemCount,
                out _,
                out var property);

            if (status is 0 && actualType == XA_WINDOW && actualFormat is 32 &&
                itemCount > 0 && property is not 0)
            {
                try
                {
                    activeWindow = Marshal.ReadIntPtr(property);
                    return activeWindow is not 0;
                }
                finally
                {
                    XFree(property);
                }
            }

            if (property is not 0)
                XFree(property);
        }

        return XGetInputFocus(_display, out activeWindow, out _) is not 0 && activeWindow is not 0;
    }

    internal void StartFocusMonitoring()
    {
        if (_focusMonitoring || _disposed || _display is 0 || _x11Window is 0 || _windowInactivated is null)
            return;

        _focusMonitoring = true;

        // Read the actual active window from X11 instead of assuming focused.
        // For NoActivateOnOpen / NeverActivate flyouts the host never becomes
        // the active window, so assuming true would cause a spurious
        // WindowInactivated on the first timer tick.
        _focused = IsOurWindowActive();
        _focusTimer.Start();
    }

    private bool IsOurWindowActive()
    {
        return TryGetActiveWindow(out var activeWindow) && managedWindows.Contains(activeWindow);
    }

    internal void StopFocusMonitoring()
    {
        if (!_focusMonitoring)
            return;

        _focusMonitoring = false;
        try { _focusTimer.Stop(); } catch { }
    }

    private void FocusTimer_Tick(object? sender, object e)
    {
        if (_disposed || _display is 0)
        {
            StopFocusMonitoring();
            return;
        }

        try
        {
            if (!TryGetActiveWindow(out var activeWindow))
                return;

            var wasFocused = _focused;
            _focused = managedWindows.Contains(activeWindow);

            if (wasFocused && !_focused)
                _windowInactivated?.Invoke(this, EventArgs.Empty);
        }
        catch (ObjectDisposedException)
        {
            StopFocusMonitoring();
        }
        catch
        {
            StopFocusMonitoring();
        }
    }

    private void OnWindowClosed(object? sender, WindowEventArgs args)
    {
        _windowInactivated?.Invoke(this, EventArgs.Empty);
    }

    private void GeneralHelpers_SystemSettingsChanged(object? sender, EventArgs args)
    {
        if (_disposed)
            return;

        if (_window.DispatcherQueue.HasThreadAccess)
        {
            SystemSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _window.DispatcherQueue.TryEnqueue(() =>
            {
                if (!_disposed)
                    SystemSettingsChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    private void OnWindowActivated(object? sender, WindowActivatedEventArgs args)
    {
        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
            return;

        if (args.WindowActivationState is not Windows.UI.Core.CoreWindowActivationState.Deactivated)
        {
            _focused = true;
        }
        else if (_focused)
        {
            _focused = false;
            _windowInactivated?.Invoke(this, EventArgs.Empty);
        }
    }
}

#endif
