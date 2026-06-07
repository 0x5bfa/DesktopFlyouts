// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

#if UWP
using System.Runtime.InteropServices.Marshalling;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using Windows.Win32.System.WinRT.Xaml;
using WinRT;
#elif WASDK
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
#endif

namespace DesktopFlyouts
{
    internal unsafe partial class XamlIslandHostWindow : IDisposable
    {
        private const string WindowClassNamePrefix = "DesktopFlyoutHostClass";
        private const string WindowName = "DesktopFlyoutHostWindow";
        private const int HTTRANSPARENT = -1;
        private const int HTCAPTION = 2;
        private static readonly object s_cbtHookTargetsLock = new();
        private static readonly Dictionary<uint, List<XamlIslandHostWindow>> s_cbtHookTargetsByThread = [];

        private readonly WNDPROC _wndProc;
        private readonly WNDPROC _xamlWndProc;
        private readonly string _windowClassName = $"{WindowClassNamePrefix}.{Guid.NewGuid():N}";

        private HWND _xamlHwnd = default;
        private HHOOK _cbtHook = default;
        private uint _cbtHookThreadId;
        private HWND _preservedForegroundHWnd = default;
        private HWND _preservedActiveHWnd = default;
        private HWND _preservedFocusHWnd = default;
        private Border? _contentRoot;
        private readonly Dictionary<nint, nint> _subclassedXamlWndProcs = [];
        private readonly List<RectInt32> _dragRegionRects = [];
        private Thickness _contentMargin = default;
        private bool _disposed;
        private DesktopFlyoutActivationMode _activationMode = DesktopFlyoutActivationMode.Activate;
        private DesktopFlyoutDragMode _dragMode = DesktopFlyoutDragMode.None;

#if UWP
        private HWND _coreHwnd = default;
        private CoreWindow? _coreWindow = null;

        private IDesktopWindowXamlSourceNative2 _pdwxsn2 = null!;
#elif WASDK
        private InputNonClientPointerSource? _nonClientPointerSource;
#endif

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        internal HWND HWnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        internal DesktopWindowXamlSource? DesktopWindowXamlSource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        internal Rect WindowSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                RECT rect;
                PInvoke.GetWindowRect(HWnd, &rect);
                return new(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        internal Rect ContentWindowSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var windowSize = WindowSize;
                var contentRect = GetScaledContentRect();
                return new(
                    windowSize.X + contentRect.X,
                    windowSize.Y + contentRect.Y,
                    contentRect.Width,
                    contentRect.Height);
            }
        }

        internal double XamlIslandRasterizationScale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if UWP
                return 1.0D;
#else
                return DesktopWindowXamlSource?.SiteBridge.SiteView.RasterizationScale ?? 1.0D;
#endif
            }
        }

        internal event EventHandler? WindowInactivated;
#if WASDK
        internal event EventHandler? NativeMoveSizeStarted;
        internal event EventHandler? NativeMoveSizeEnded;
#endif
        internal event EventHandler? SystemSettingsChanged;
        internal event EventHandler<XamlSourceFocusNavigationRequest>? TakeFocusRequested;

        internal XamlIslandHostWindow()
        {
            _wndProc = new(WndProc);
            _xamlWndProc = new(XamlWndProc);

            WNDCLASSW wndClass = default;
            wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
            wndClass.hInstance = PInvoke.GetModuleHandle(null);
            wndClass.lpszClassName = (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _windowClassName.GetPinnableReference()));
            PInvoke.RegisterClass(&wndClass);

            HWnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST,
                (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _windowClassName.GetPinnableReference())),
                (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in WindowName.GetPinnableReference())),
                WINDOW_STYLE.WS_POPUP, 0, 0, 0, 0, HWND.Null, HMENU.Null, wndClass.hInstance, null);

            InitializeDesktopWindowXamlSource();
        }

        internal void SetContent(UIElement content)
        {
            if (_disposed)
                return;

            _contentRoot ??= new();
            _contentRoot.Padding = _contentMargin;
            _contentRoot.Child = content;
            DesktopWindowXamlSource!.Content = _contentRoot;
            ApplyActivationModeToWindows();
        }

        internal void SetContentMargin(Thickness margin)
        {
            if (_disposed)
                return;

            _contentMargin = NormalizeThickness(margin);
            if (_contentRoot is not null)
                _contentRoot.Padding = _contentMargin;

            ApplyActivationModeToWindows();
            ApplyNativeDragRegions();
        }

        internal void BeginNativeDragMove()
        {
            if (_disposed || HWnd.IsNull)
                return;

#if UWP
            ReleaseCapture();
            PInvoke.SendMessage(HWnd, PInvoke.WM_NCLBUTTONDOWN, (WPARAM)HTCAPTION, default);
#endif
        }

        internal void PreserveActivationState()
        {
            if (_disposed)
                return;

            _preservedForegroundHWnd = PInvoke.GetForegroundWindow();
            _preservedActiveHWnd = PInvoke.GetActiveWindow();
            _preservedFocusHWnd = PInvoke.GetFocus();
        }

        internal void RestoreActivationState()
        {
            if (_disposed)
                return;

            if (!_preservedForegroundHWnd.IsNull)
                PInvoke.SetForegroundWindow(_preservedForegroundHWnd);

            if (!_preservedActiveHWnd.IsNull)
                PInvoke.SetActiveWindow(_preservedActiveHWnd);

            if (!_preservedFocusHWnd.IsNull)
                PInvoke.SetFocus(_preservedFocusHWnd);
        }

        internal void MoveAndResize(RectInt32 rect, bool activate = true)
        {
            if (_disposed)
                return;

            var flags = activate ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            PInvoke.SetWindowPos(HWnd, HWND.HWND_TOP, rect.X, rect.Y, rect.Width, rect.Height, flags);
            PInvoke.SetWindowPos(_xamlHwnd, HWND.HWND_TOP, 0, 0, rect.Width, rect.Height, flags);
            ApplyNativeDragRegions();
        }

        internal void Maximize(System.Drawing.Rectangle workArea, bool activate = true)
        {
            if (_disposed)
                return;

            var flags = activate ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            PInvoke.SetWindowPos(HWnd, HWND.HWND_TOP, workArea.X, workArea.Y, workArea.Width, workArea.Height, flags);
            PInvoke.SetWindowPos(_xamlHwnd, HWND.HWND_TOP, 0, 0, workArea.Width, workArea.Height, flags);
            ApplyNativeDragRegions();
        }

        internal void SetHWndRectRegion(RectInt32 rect)
        {
            if (_disposed)
                return;

            SetWindowRectRegion(HWnd, rect);
            SetWindowRectRegion(_xamlHwnd, rect);
        }

        private static void SetWindowRectRegion(HWND hWnd, RectInt32 rect)
        {
            HRGN region = PInvoke.CreateRectRgn(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
            if (region.IsNull)
                return;

            if (PInvoke.SetWindowRgn(hWnd, region, false) == 0)
                PInvoke.DeleteObject(region);
        }

        internal void UpdateWindowVisibility(bool isVisible, bool activate = true)
        {
            if (_disposed)
                return;

            var command = isVisible
                ? activate ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE
                : SHOW_WINDOW_CMD.SW_HIDE;

            PInvoke.ShowWindow(HWnd, command);
            if (isVisible && activate)
                PInvoke.SetForegroundWindow(HWnd);
#if UWP
            PInvoke.ShowWindow(_xamlHwnd, command);
#else
            if (isVisible)
            {
                if (activate)
                    DesktopWindowXamlSource?.SiteBridge.Show();
                else
                    PInvoke.ShowWindow(_xamlHwnd, command);
            }
            else
            {
                DesktopWindowXamlSource?.SiteBridge.Hide();
            }
#endif

            if (isVisible)
                ApplyActivationModeToWindows();
        }

        internal void SetActivationMode(DesktopFlyoutActivationMode activationMode)
        {
            if (_disposed)
                return;

            _activationMode = activationMode;
            ApplyActivationModeToWindows();
        }

        internal void SetDragMode(DesktopFlyoutDragMode dragMode)
        {
            if (_disposed)
                return;

            _dragMode = dragMode;
            ApplyNativeDragRegions();
        }

        internal void SetDragRegions(IReadOnlyList<RectInt32> dragRegionRects)
        {
            if (_disposed)
                return;

            _dragRegionRects.Clear();
            foreach (var rect in dragRegionRects)
                _dragRegionRects.Add(rect);

            ApplyNativeDragRegions();
        }

        private RectInt32 GetScaledContentRect()
        {
            RECT clientRect;
            if (!PInvoke.GetClientRect(HWnd, &clientRect))
                return default;

            var scale = XamlIslandRasterizationScale;
            var marginLeft = Math.Max(0, (int)Math.Floor(_contentMargin.Left * scale));
            var marginTop = Math.Max(0, (int)Math.Floor(_contentMargin.Top * scale));
            var marginRight = Math.Max(0, (int)Math.Ceiling(_contentMargin.Right * scale));
            var marginBottom = Math.Max(0, (int)Math.Ceiling(_contentMargin.Bottom * scale));
            var width = Math.Max(0, clientRect.Width - marginLeft - marginRight);
            var height = Math.Max(0, clientRect.Height - marginTop - marginBottom);

            return new(marginLeft, marginTop, width, height);
        }

        private static Thickness NormalizeThickness(Thickness thickness)
        {
            return new(
                GetFiniteOrZero(thickness.Left),
                GetFiniteOrZero(thickness.Top),
                GetFiniteOrZero(thickness.Right),
                GetFiniteOrZero(thickness.Bottom));
        }

        private static double GetFiniteOrZero(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;
        }

        private void ApplyNativeDragRegions()
        {
#if WASDK
            if (_nonClientPointerSource is null)
                return;

            _nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
            var dragRegions = GetNativeDragRegions();
            if (dragRegions.Length > 0)
                _nonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, dragRegions);
#endif
        }

#if WASDK
        private RectInt32[] GetNativeDragRegions()
        {
            if (_dragMode is DesktopFlyoutDragMode.None)
                return [];

            if (_dragMode is DesktopFlyoutDragMode.Full)
            {
                var contentRect = GetScaledContentRect();
                if (contentRect.Width <= 0 || contentRect.Height <= 0)
                    return [];

                return [contentRect];
            }

            List<RectInt32> dragRegions = [];
            foreach (var rect in _dragRegionRects)
            {
                if (rect.Width > 0 && rect.Height > 0)
                    dragRegions.Add(rect);
            }

            return [.. dragRegions];
        }
#endif

        private void ApplyActivationModeToWindows()
        {
            var neverActivate = _activationMode is DesktopFlyoutActivationMode.NeverActivate;
            var needsXamlIslandSubclass = neverActivate || HasContentMargin();
            UpdateCbtHook(neverActivate);
            if (needsXamlIslandSubclass)
                RefreshXamlIslandWindowSubclasses();

            SetNoActivateStyle(HWnd, neverActivate);

            foreach (var hWnd in _subclassedXamlWndProcs.Keys)
                SetNoActivateStyle((HWND)hWnd, neverActivate);

            if (!needsXamlIslandSubclass)
                UnsubclassXamlIslandWindows();
        }

        private void UpdateCbtHook(bool enabled)
        {
            if (enabled)
            {
                EnsureCbtHook();
                return;
            }

            RemoveCbtHook();
        }

        private void EnsureCbtHook()
        {
            if (_cbtHook != HHOOK.Null)
                return;

            _cbtHookThreadId = PInvoke.GetCurrentThreadId();
            _cbtHook = PInvoke.SetWindowsHookEx(
                WINDOWS_HOOK_ID.WH_CBT,
                &CbtHookProc,
                HINSTANCE.Null,
                _cbtHookThreadId);

            if (_cbtHook != HHOOK.Null)
                RegisterCbtHookTarget(_cbtHookThreadId);
        }

        private void RemoveCbtHook()
        {
            if (_cbtHook == HHOOK.Null)
                return;

            PInvoke.UnhookWindowsHookEx(_cbtHook);
            _cbtHook = default;
            UnregisterCbtHookTarget(_cbtHookThreadId);
            _cbtHookThreadId = 0;
        }

        private void RegisterCbtHookTarget(uint threadId)
        {
            lock (s_cbtHookTargetsLock)
            {
                if (!s_cbtHookTargetsByThread.TryGetValue(threadId, out var targets))
                {
                    targets = [];
                    s_cbtHookTargetsByThread[threadId] = targets;
                }

                if (!targets.Contains(this))
                    targets.Add(this);
            }
        }

        private void UnregisterCbtHookTarget(uint threadId)
        {
            if (threadId == 0)
                return;

            lock (s_cbtHookTargetsLock)
            {
                if (!s_cbtHookTargetsByThread.TryGetValue(threadId, out var targets))
                    return;

                targets.Remove(this);
                if (targets.Count == 0)
                    s_cbtHookTargetsByThread.Remove(threadId);
            }
        }

        private static XamlIslandHostWindow[] GetCbtHookTargets(uint threadId)
        {
            lock (s_cbtHookTargetsLock)
            {
                return s_cbtHookTargetsByThread.TryGetValue(threadId, out var targets)
                    ? [.. targets]
                    : [];
            }
        }

#if UWP
        public bool TryPreTranslateMessage(MSG* msg)
        {
            BOOL result = false;

            _pdwxsn2.PreTranslateMessage(msg, &result);

            return result;
        }
#endif

        internal bool NavigateFocus(XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic)
        {
            if (_disposed || _xamlHwnd.IsNull || _activationMode is DesktopFlyoutActivationMode.NeverActivate)
                return false;

            PInvoke.SetFocus(_xamlHwnd);

            var result = DesktopWindowXamlSource?.NavigateFocus(new(reason));
            return result?.WasFocusMoved ?? false;
        }

        internal bool ContainsForegroundWindow()
        {
            return !_disposed && IsFlyoutWindow(PInvoke.GetForegroundWindow());
        }

        private static void SetNoActivateStyle(HWND hWnd, bool enabled)
        {
            if (hWnd.IsNull)
                return;

            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            exStyle = enabled
                ? exStyle | WINDOW_EX_STYLE.WS_EX_NOACTIVATE
                : exStyle & ~WINDOW_EX_STYLE.WS_EX_NOACTIVATE;

            PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);
            PInvoke.SetWindowPos(
                hWnd,
                HWND.Null,
                0,
                0,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
        }

        private void RefreshXamlIslandWindowSubclasses()
        {
            if (_xamlHwnd.IsNull)
                return;

            SubclassXamlIslandWindow(_xamlHwnd);
            SubclassChildWindows(HWnd);
            SubclassChildWindows(_xamlHwnd);
        }

        private void SubclassChildWindows(HWND parentHWnd)
        {
            for (var childHWnd = PInvoke.GetWindow(parentHWnd, GET_WINDOW_CMD.GW_CHILD);
                !childHWnd.IsNull;
                childHWnd = PInvoke.GetWindow(childHWnd, GET_WINDOW_CMD.GW_HWNDNEXT))
            {
                SubclassXamlIslandWindow(childHWnd);
                SubclassChildWindows(childHWnd);
            }
        }

        private void SubclassXamlIslandWindow(HWND hWnd)
        {
            if (hWnd.IsNull || _subclassedXamlWndProcs.ContainsKey((nint)hWnd.Value))
                return;

            var previousWndProc = (nint)PInvoke.SetWindowLongPtr(
                hWnd,
                WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_xamlWndProc));

            _subclassedXamlWndProcs[(nint)hWnd.Value] = previousWndProc;
        }

        private void UnsubclassXamlIslandWindows()
        {
            foreach (var item in _subclassedXamlWndProcs)
            {
                var hWnd = (HWND)item.Key;
                if (hWnd.IsNull)
                    continue;

                PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, item.Value);
            }

            _subclassedXamlWndProcs.Clear();
        }

        private void InitializeDesktopWindowXamlSource()
        {
            DesktopWindowXamlSource = new();
            DesktopWindowXamlSource.TakeFocusRequested += DesktopWindowXamlSource_TakeFocusRequested;

#if UWP
            // QI for IDesktopWindowXamlSourceNative2
            void* ppv;
            ((IUnknown*)((IWinRTObject)DesktopWindowXamlSource).NativeObject.ThisPtr)->QueryInterface(
                (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_IDesktopWindowXamlSourceNative2)), &ppv);

            var sbcw = new StrategyBasedComWrappers();
            _pdwxsn2 = (IDesktopWindowXamlSourceNative2)sbcw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.None);

            // For extra safety
            GC.KeepAlive(DesktopWindowXamlSource);

            // Set the base HWND and get the XAML island HWND
            _pdwxsn2.AttachToWindow(HWnd);
            _pdwxsn2.get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd));

            RECT wRect;
            PInvoke.GetClientRect(HWnd, &wRect);
            PInvoke.SetWindowPos(_xamlHwnd, HWND.Null, 0, 0, wRect.right - wRect.left, wRect.bottom - wRect.top, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

            var xst = Window.Current.As<IXamlSourceTransparency>();
            xst.set_IsBackgroundTransparent(true);

            // Get CoreWindow and its HWND
            _coreWindow = CoreWindow.GetForCurrentThread();
            ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr)->QueryInterface(
                (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_ICoreWindowInterop)), &ppv);

            var pcwi = (ICoreWindowInterop)sbcw.GetOrCreateObjectForComInstance((nint)ppv, CreateObjectFlags.None);
            pcwi.get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd));

#elif WASDK
            DesktopWindowXamlSource!.Initialize(Win32Interop.GetWindowIdFromWindow(HWnd));
            _xamlHwnd = (HWND)Win32Interop.GetWindowFromWindowId(DesktopWindowXamlSource.SiteBridge.WindowId);
            _nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(Win32Interop.GetWindowIdFromWindow(HWnd));
            _nonClientPointerSource.EnteringMoveSize += NonClientPointerSource_EnteringMoveSize;
            _nonClientPointerSource.ExitedMoveSize += NonClientPointerSource_ExitedMoveSize;
#endif

            ApplyActivationModeToWindows();
        }

#if WASDK
        private void NonClientPointerSource_EnteringMoveSize(InputNonClientPointerSource sender, EnteringMoveSizeEventArgs args)
        {
            NativeMoveSizeStarted?.Invoke(this, EventArgs.Empty);
        }

        private void NonClientPointerSource_ExitedMoveSize(InputNonClientPointerSource sender, ExitedMoveSizeEventArgs args)
        {
            ApplyNativeDragRegions();
            NativeMoveSizeEnded?.Invoke(this, EventArgs.Empty);
        }
#endif

        private void DesktopWindowXamlSource_TakeFocusRequested(DesktopWindowXamlSource sender, DesktopWindowXamlSourceTakeFocusRequestedEventArgs args)
        {
            TakeFocusRequested?.Invoke(this, args.Request);
        }

        private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case PInvoke.WM_NCHITTEST:
                    {
                        if (IsTransparentMarginHit(lParam))
                            return (LRESULT)HTTRANSPARENT;

                        if (IsNativeDragHit(lParam))
                            return (LRESULT)HTCAPTION;

                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                    }
#if UWP
                case PInvoke.WM_SIZE:
                    {
                        var x = LOWORD(lParam);
                        var y = HIWORD(lParam);

                        if (_xamlHwnd != default)
                            PInvoke.SetWindowPos(_xamlHwnd, HWND.Null, 0, 0, x, y, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

                        if (_coreHwnd != default)
                            PInvoke.SendMessage(_coreHwnd, PInvoke.WM_SIZE, (WPARAM)(nuint)x, y);
                    }
                    break;
                case PInvoke.WM_DESTROY:
                    {
                        PInvoke.PostQuitMessage(0);
                    }
                    break;
#endif
                case PInvoke.WM_SETTINGCHANGE:
                case PInvoke.WM_THEMECHANGED:
                    {
#if UWP
                        // Process CoreWindow message
                        if (_coreHwnd != default)
                            PInvoke.SendMessage(_coreHwnd, uMsg, wParam, lParam);
#endif
                        SystemSettingsChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case PInvoke.WM_SETFOCUS:
                    {
                        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                        {
                            RestoreActivationState();
                            return (LRESULT)0;
                        }

#if UWP
                        if (_xamlHwnd != default)
                            PInvoke.SetFocus(_xamlHwnd);

                        return (LRESULT)0;
#else
                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
#endif
                    }
                case PInvoke.WM_ACTIVATE:
                    {
                        if (LOWORD((nint)(nuint)wParam) == PInvoke.WA_INACTIVE)
                            WindowInactivated?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case PInvoke.WM_MOUSEACTIVATE:
                    {
                        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                        {
                            RestoreActivationState();
                            return (LRESULT)(int)PInvoke.MA_NOACTIVATE;
                        }

                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                    }
                default:
                    {
                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                    }
            }

            return (LRESULT)0;
        }

        private LRESULT XamlWndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case PInvoke.WM_NCHITTEST:
                    {
                        if (IsTransparentMarginHit(lParam))
                            return (LRESULT)HTTRANSPARENT;

                        break;
                    }
                case PInvoke.WM_MOUSEACTIVATE:
                    {
                        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                        {
                            RestoreActivationState();
                            return (LRESULT)(int)PInvoke.MA_NOACTIVATE;
                        }

                        break;
                    }
                case PInvoke.WM_SETFOCUS:
                    {
                        if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                        {
                            RestoreActivationState();
                            return (LRESULT)0;
                        }

                        break;
                    }
            }

            return CallPreviousXamlWndProc(hWnd, uMsg, wParam, lParam);
        }

        private bool IsNativeDragHit(LPARAM lParam)
        {
            if (_dragMode is DesktopFlyoutDragMode.None)
                return false;

            if (!TryGetHostPoint(lParam, out var x, out var y))
                return false;

            if (_dragMode is DesktopFlyoutDragMode.Full)
                return IsPointInRect(x, y, GetScaledContentRect());

            foreach (var rect in _dragRegionRects)
            {
                if (IsPointInRect(x, y, rect))
                    return true;
            }

            return false;
        }

        private bool IsTransparentMarginHit(LPARAM lParam)
        {
            if (!HasContentMargin() || !TryGetHostPoint(lParam, out var x, out var y))
                return false;

            return !IsPointInRect(x, y, GetScaledContentRect());
        }

        private bool TryGetHostPoint(LPARAM lParam, out int x, out int y)
        {
            x = 0;
            y = 0;

            RECT hostRect;
            if (!PInvoke.GetWindowRect(HWnd, &hostRect))
                return false;

            x = GetSignedLoWord((nint)lParam) - hostRect.left;
            y = GetSignedHiWord((nint)lParam) - hostRect.top;
            return x >= 0 && y >= 0 && x < hostRect.Width && y < hostRect.Height;
        }

        private bool HasContentMargin()
        {
            return _contentMargin.Left > 0 ||
                _contentMargin.Top > 0 ||
                _contentMargin.Right > 0 ||
                _contentMargin.Bottom > 0;
        }

        private static bool IsPointInRect(int x, int y, RectInt32 rect)
        {
            return rect.Width > 0 &&
                rect.Height > 0 &&
                x >= rect.X &&
                x < rect.X + rect.Width &&
                y >= rect.Y &&
                y < rect.Y + rect.Height;
        }

        private static int GetSignedLoWord(nint value)
        {
            return unchecked((short)LOWORD(value));
        }

        private static int GetSignedHiWord(nint value)
        {
            return unchecked((short)HIWORD(value));
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static LRESULT CbtHookProc(int code, WPARAM wParam, LPARAM lParam)
        {
            if (code >= 0 && (code == PInvoke.HCBT_ACTIVATE || code == PInvoke.HCBT_SETFOCUS))
            {
                var targetHWnd = (HWND)(nint)(nuint)wParam;
                foreach (var target in GetCbtHookTargets(PInvoke.GetCurrentThreadId()))
                {
                    if (target._activationMode is DesktopFlyoutActivationMode.NeverActivate && target.IsFlyoutWindow(targetHWnd))
                    {
                        target.RestoreActivationState();
                        return (LRESULT)1;
                    }
                }
            }

            return PInvoke.CallNextHookEx(HHOOK.Null, code, wParam, lParam);
        }

        private bool IsFlyoutWindow(HWND hWnd)
        {
            if (hWnd.IsNull)
                return false;

            if (hWnd == HWnd || hWnd == _xamlHwnd)
                return true;

            if (_subclassedXamlWndProcs.ContainsKey((nint)hWnd.Value))
                return true;

            if (!HWnd.IsNull && PInvoke.IsChild(HWnd, hWnd))
                return true;

            if (!_xamlHwnd.IsNull && PInvoke.IsChild(_xamlHwnd, hWnd))
                return true;

            var rootHWnd = PInvoke.GetAncestor(hWnd, GET_ANCESTOR_FLAGS.GA_ROOT);
            if (rootHWnd == HWnd || rootHWnd == _xamlHwnd)
                return true;

            var rootOwnerHWnd = PInvoke.GetAncestor(hWnd, GET_ANCESTOR_FLAGS.GA_ROOTOWNER);
            return rootOwnerHWnd == HWnd || rootOwnerHWnd == _xamlHwnd;
        }

        private LRESULT CallPreviousXamlWndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            if (!_subclassedXamlWndProcs.TryGetValue((nint)hWnd.Value, out var previousWndProc) || previousWndProc == 0)
                return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);

            return PInvoke.CallWindowProc(
                (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)(void*)previousWndProc,
                hWnd,
                uMsg,
                wParam,
                lParam);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                RemoveCbtHook();
                UnsubclassXamlIslandWindows();
#if WASDK
                if (_nonClientPointerSource is not null)
                {
                    _nonClientPointerSource.EnteringMoveSize -= NonClientPointerSource_EnteringMoveSize;
                    _nonClientPointerSource.ExitedMoveSize -= NonClientPointerSource_ExitedMoveSize;
                    _nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
                }
#endif
                if (DesktopWindowXamlSource is not null)
                    DesktopWindowXamlSource.TakeFocusRequested -= DesktopWindowXamlSource_TakeFocusRequested;
                if (_contentRoot is not null)
                    _contentRoot.Child = null;
                if (DesktopWindowXamlSource is not null)
                    DesktopWindowXamlSource.Content = null;
                DesktopWindowXamlSource?.Dispose();
            }
            catch { }
            _contentRoot = null;
            DesktopWindowXamlSource = null;
#if WASDK
            _nonClientPointerSource = null;
#endif

            PInvoke.DestroyWindow(HWnd);
            PInvoke.UnregisterClass((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _windowClassName.GetPinnableReference())), PInvoke.GetModuleHandle(null));

            HWnd = default;
            _xamlHwnd = default;

            GC.SuppressFinalize(this);
        }
    }
}
