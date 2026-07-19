#if HAS_UNO
// X11 implementation of XamlIslandHostWindow.
// Uses Microsoft.UI.Xaml.Window for XAML rendering and raw X11 P/Invoke
// for window property manipulation (borderless, override_redirect, positioning).

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private DesktopFlyoutActivationMode _activationMode = DesktopFlyoutActivationMode.Activate;

    internal object? DesktopWindowXamlSource { get; private set; }

    internal Rect WindowSize
    {
        get
        {
            if (_display is 0 || _x11Window is 0)
                return default;

            var attrs = new XWindowAttributes();
            XGetWindowAttributes(_display, _x11Window, ref attrs);
            return new Rect(0, 0, attrs.Width, attrs.Height);
        }
    }

    internal double XamlIslandRasterizationScale
    {
        get => 1.0D;
    }

    internal event EventHandler? WindowInactivated;
    internal event EventHandler? SystemSettingsChanged;

    private readonly List<nint> managedWindows;

    internal XamlIslandHostWindow()
    {
        ThrowHelper.ThrowIfNotLinux();

        _window = new TransparentWindow();
        _window.Title = "DesktopFlyoutHost";

        var nativeWindow = (X11NativeWindow)Uno.UI.Xaml.WindowHelper.GetNativeWindow(_window);
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

        // Set override_redirect on BOTH windows so the WM doesn't reposition or decorate them.
        // Uno creates two windows (RootX11Window + TopX11Window child) and maps both in ShowCore().
        // Both need override_redirect to prevent KWin from grabbing and repositioning.
        foreach (var window in managedWindows)
            SetOverrideRedirect(window, true);

        // Remove window decorations by setting Motif WM hints (no title, no resize, no close).
        var motifHints = new byte[5 * 4]; // 5 x uint32
        BitConverter.TryWriteBytes(new Span<byte>(motifHints, 0, 4), 2); // MWM_HINTS_DECORATIONS
        BitConverter.TryWriteBytes(new Span<byte>(motifHints, 4, 4), 0); // no decorations
        BitConverter.TryWriteBytes(new Span<byte>(motifHints, 8, 4), 0); // functions
        BitConverter.TryWriteBytes(new Span<byte>(motifHints, 12, 4), 0); // input_mode
        BitConverter.TryWriteBytes(new Span<byte>(motifHints, 16, 4), 0); // status
        XChangeProperty(_display, _x11Window, _motifWmHintsAtom,
            _motifWmHintsAtom, 32, PropertyMode.Replace, motifHints, 5);

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
        var stateAtoms = new int[]
        {
            (int)_netWmStateSkipTaskbarAtom,
            (int)_netWmStateSkipPagerAtom,
            (int)_netWmStateAboveAtom,
        };
        var stateBytes = new byte[stateAtoms.Length * 4];
        Buffer.BlockCopy(stateAtoms, 0, stateBytes, 0, stateBytes.Length);
        XChangeProperty(_display, window, _netWmStateAtom, XA_ATOM, 32, PropertyMode.Replace, stateBytes, stateAtoms.Length);
    }

    private void SetNetWmWindowType(nint window)
    {
        var typeAtomArr = new int[] { (int)_netWmWindowTypeDockAtom };
        var typeBytes = new byte[4];
        Buffer.BlockCopy(typeAtomArr, 0, typeBytes, 0, 4);
        XChangeProperty(_display, window, _netWmWindowTypeAtom, XA_ATOM, 32, PropertyMode.Replace, typeBytes, 1);
    }

    internal void SetContent(object content)
    {
        _window.Content = content as UIElement;
    }

    internal void PreserveActivationState()
    {
        // On X11, activation state preservation is handled at the window manager level.
        // This is a no-op for now; the flyout window will activate normally.
    }

    internal void RestoreActivationState()
    {
        // On X11, restore activation by raising the window.
        if (_display is 0 || _x11Window is 0)
            return;

        XRaiseWindow(_display, _x11Window);
        XFlush(_display);
    }

    internal void MoveAndResize(RectInt32 rect, bool activate = true)
    {
        if (_display is 0 || _x11Window is 0)
            return;

        // Use AppWindow.Resize to notify the Skia rendering pipeline of the new size.
        _window.AppWindow.Resize(new SizeInt32 { Width = rect.Width, Height = rect.Height });

        // Use raw X11 for positioning to avoid Uno's pipeline re-reading the WM-overridden position.
        XMoveWindow(_display, _x11Window, rect.X, rect.Y);
        XSync(_display, false);

        if (activate)
        {
            // Send _NET_ACTIVE_WINDOW message to request activation.
            var xclient = new XEvent
            {
                type = ClientMessage,
                xclient = new XClientMessageEvent
                {
                    type = ClientMessage,
                    display = _display,
                    window = _x11Window,
                    message_type = _netActiveWindowAtom,
                    format = 32,
                    ptr1 = (nint)2, // _NET_WM_STATE_REQUEST
                    ptr2 = 0,
                    ptr3 = 0,
                }
            };
            var rootWindow = XDefaultRootWindow(_display);
            XSendEvent(_display, rootWindow, false,
                (nint)(StructureNotifyMask | FocusChangeMask), ref xclient);
        }

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

        if (activate)
        {
            XRaiseWindow(_display, _x11Window);
        }

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

            // Re-apply override_redirect immediately before map on BOTH windows.
            // Uno's ShowCore() maps both windows; KWin grabs TopX11Window if it lacks override_redirect.
            foreach (var window in managedWindows)
                SetOverrideRedirect(window, true);

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

            if (activate)
            {
                XRaiseWindow(_display, _x11Window);
            }
            XSync(_display, false);

            // Restore opacity after Skia has presented at least one frame.
            // Task.Delay avoids needing CompositionTarget.Rendering (which has thread issues).
            var opacityTask = RestoreOpacityAfterDelay();
            XFlush(_display);
            return opacityTask;
        }
        else
        {
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

        // For NeverActivate, we set override_redirect so the WM doesn't manage focus.
        foreach (var window in managedWindows)
            SetOverrideRedirect(window, activationMode == DesktopFlyoutActivationMode.NeverActivate);
        XFlush(_display);
    }

    internal bool NavigateFocus(object reason)
    {
        // On X11, focus navigation is handled by the window manager.
        // Attempt to set input focus to the flyout window.
        if (_display is 0 || _x11Window is 0)
            return false;

        XSetInputFocus(_display, _x11Window, 1 /* RevertToParent */, 0 /* CurrentTime */);
        XFlush(_display);
        return true;
    }

    private void SetWindowOpacity(nint window, uint opacity)
    {
        var data = BitConverter.GetBytes(opacity);
        XChangeProperty(_display, window, _netWmOpacityAtom, 6 /* XA_CARDINAL */, 32,
            PropertyMode.Replace, data, 1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _window.Closed -= OnWindowClosed;
        _window.Activated -= OnWindowActivated;

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

    private void OnWindowClosed(object? sender, WindowEventArgs args)
    {
        WindowInactivated?.Invoke(this, EventArgs.Empty);
    }

    private void OnWindowActivated(object? sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is Windows.UI.Core.CoreWindowActivationState.Deactivated)
        {
            WindowInactivated?.Invoke(this, EventArgs.Empty);
        }
    }
}

#endif