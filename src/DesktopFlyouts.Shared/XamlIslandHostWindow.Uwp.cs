#if UWP
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using XamlHostingKit;

namespace DesktopFlyouts
{
    internal sealed class CoreDispatcherSynchronizationContext : SynchronizationContext
    {
        private readonly CoreDispatcher _dispatcher;

        internal CoreDispatcherSynchronizationContext(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => callback(state));
        }

        public override void Send(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);

            if (!_dispatcher.HasThreadAccess)
                throw new NotSupportedException("Cross-thread synchronous dispatch is not supported.");

            callback(state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return new CoreDispatcherSynchronizationContext(_dispatcher);
        }
    }

    internal unsafe partial class XamlIslandHostWindow : IDisposable
    {
        private static readonly object s_cbtHookTargetsLock = new();
        private static readonly Dictionary<uint, List<XamlIslandHostWindow>> s_cbtHookTargetsByThread = [];
        private static readonly object s_claimedWindowsLock = new();
        private static readonly HashSet<nint> s_claimedWindows = [];

        private readonly WNDPROC _xamlWndProc;
        private readonly UISettings _uiSettings = new();
        private readonly Dictionary<nint, nint> _subclassedXamlWndProcs = [];

        private XamlWindow? _xamlWindow;
        private CoreWindow? _coreWindow;
        private UIElement? _content;
        private Grid? _contentRoot;
        private HWND _xamlHwnd = default;
        private HHOOK _cbtHook = default;
        private uint _cbtHookThreadId;
        private HWND _preservedForegroundHWnd = default;
        private HWND _preservedActiveHWnd = default;
        private HWND _preservedFocusHWnd = default;
        private bool _disposed;
        private DesktopFlyoutActivationMode _activationMode = DesktopFlyoutActivationMode.Activate;

        internal HWND HWnd { get; private set; }

        internal bool IsInitialized => _xamlWindow is not null;

        internal Rect WindowSize
        {
            get
            {
                RECT rect;
                PInvoke.GetWindowRect(HWnd, &rect);
                return new(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        internal double XamlIslandRasterizationScale
        {
            get
            {
                try
                {
                    return DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                }
                catch
                {
                    return 1.0D;
                }
            }
        }

        internal event EventHandler? WindowInactivated;
        internal event EventHandler? SystemSettingsChanged;

        internal XamlIslandHostWindow()
        {
            _xamlWndProc = new(XamlWndProc);

            _xamlWindow = XamlWindow.Current;
            if (_xamlWindow is null)
            {
                throw new InvalidOperationException(
                    "UWP DesktopFlyouts must be created inside a XamlHostingKit XamlWindow. " +
                    "Start the app with XamlIslandApplication.Start and create additional flyouts " +
                    "inside XamlIslandApplication.CreateWindow callbacks.");
            }

            if (_xamlWindow.Content is not null)
            {
                throw new InvalidOperationException(
                    "A XamlHostingKit XamlWindow can host only one DesktopFlyout or DesktopMenuFlyout instance.");
            }

            _xamlWindow.Title = "DesktopFlyoutHostWindow";
            _xamlWindow.Styles = WindowStyles.Popup;
            _xamlWindow.ExtendedStyles =
                WindowExtendedStyles.NoRedirectionBitmap |
                WindowExtendedStyles.ToolWindow |
                WindowExtendedStyles.Topmost;

            HWnd = (HWND)unchecked((nint)_xamlWindow.WindowHandle.Value);
            if (HWnd.IsNull)
                throw new InvalidOperationException("XamlHostingKit did not provide a host window handle.");

            lock (s_claimedWindowsLock)
            {
                if (!s_claimedWindows.Add((nint)HWnd.Value))
                {
                    throw new InvalidOperationException(
                        "A XamlHostingKit XamlWindow can host only one DesktopFlyout or DesktopMenuFlyout instance.");
                }
            }

            try
            {
                _coreWindow = _xamlWindow.CoreWindow;
                SynchronizationContext.SetSynchronizationContext(
                    new CoreDispatcherSynchronizationContext(_coreWindow.Dispatcher));
                InitializeCoreWindowHandle();

                _coreWindow.Activated += CoreWindow_Activated;
                _uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;

                ApplyActivationModeToWindows();
            }
            catch
            {
                lock (s_claimedWindowsLock)
                    s_claimedWindows.Remove((nint)HWnd.Value);

                throw;
            }
        }

        internal void SetContent(UIElement content)
        {
            if (_disposed || _xamlWindow is null)
                return;

            _content = content;
            var dispatcher = _coreWindow?.Dispatcher;
            if (dispatcher is null)
                throw new InvalidOperationException("XamlHostingKit did not provide a CoreDispatcher.");

            // A derived control calls InitializeComponent after its base DesktopFlyout constructor.
            // Attach it on the next dispatcher turn so XAML parsing completes before Window.Content
            // starts loading templates and resources.
            _ = dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (!_disposed && _xamlWindow is not null && ReferenceEquals(_content, content))
                {
                    _contentRoot = new Grid();
                    var desktopFlyoutsResources = new DesktopFlyoutResources();
                    _contentRoot.Resources.MergedDictionaries.Add(desktopFlyoutsResources);

                    if (content is Control control)
                    {
                        control.Style = content switch
                        {
                            DesktopFlyout => desktopFlyoutsResources.FlyoutStyle,
                            DesktopMenuFlyout => desktopFlyoutsResources.MenuFlyoutStyle,
                            _ => throw new InvalidOperationException($"Unsupported flyout type: {content.GetType().Name}."),
                        };
                    }

                    _contentRoot.Children.Add(content);
                    _xamlWindow.Content = _contentRoot;

                    var wasVisible = PInvoke.IsWindowVisible(HWnd);
                    if (!wasVisible)
                    {
                        PInvoke.SetWindowPos(
                            HWnd,
                            HWND.Null,
                            -32000,
                            -32000,
                            1,
                            1,
                            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                        PInvoke.ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
                    }

                    if (content is Control templatedControl)
                    {
                        templatedControl.ApplyTemplate();
                        templatedControl.UpdateLayout();
                    }

                    if (!wasVisible)
                        PInvoke.ShowWindow(HWnd, SHOW_WINDOW_CMD.SW_HIDE);

                    ApplyActivationModeToWindows();
                }
            });
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
        }

        internal void Maximize(System.Drawing.Rectangle workArea, bool activate = true)
        {
            if (_disposed)
                return;

            var flags = activate ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            PInvoke.SetWindowPos(HWnd, HWND.HWND_TOP, workArea.X, workArea.Y, workArea.Width, workArea.Height, flags);
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
            if (hWnd.IsNull)
                return;

            HRGN region = PInvoke.CreateRectRgn(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
            if (region.IsNull)
                return;

            if (PInvoke.SetWindowRgn(hWnd, region, false) == 0)
                PInvoke.DeleteObject(region);
        }

        internal ValueTask UpdateWindowVisibility(bool isVisible, bool activate = true)
        {
            if (_disposed)
                return default;

            var command = isVisible
                ? activate ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE
                : SHOW_WINDOW_CMD.SW_HIDE;

            PInvoke.ShowWindow(HWnd, command);

            if (isVisible)
                ApplyActivationModeToWindows();

            return default;
        }

        internal void SetActivationMode(DesktopFlyoutActivationMode activationMode)
        {
            if (_disposed)
                return;

            _activationMode = activationMode;
            ApplyActivationModeToWindows();
        }

        internal bool NavigateFocus(XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic)
        {
            if (_disposed || _xamlHwnd.IsNull || _activationMode is DesktopFlyoutActivationMode.NeverActivate)
                return false;

            PInvoke.SetFocus(_xamlHwnd);
            return _content is Control control && control.Focus(FocusState.Programmatic);
        }

        public bool TryPreTranslateMessage(MSG* msg)
        {
            // XamlHostingKit owns the CoreWindow message loop and performs XAML message translation.
            return false;
        }

        private void InitializeCoreWindowHandle()
        {
            if (_coreWindow is null)
                throw new InvalidOperationException("XamlHostingKit did not provide a CoreWindow.");

            void* ppv;
            ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr)->QueryInterface(
                (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_ICoreWindowInterop)), &ppv);

            var wrappers = new StrategyBasedComWrappers();
            var interop = (ICoreWindowInterop)wrappers.GetOrCreateObjectForComInstance(
                (nint)ppv,
                CreateObjectFlags.None);
            interop.get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd));
        }

        private void ApplyActivationModeToWindows()
        {
            var neverActivate = _activationMode is DesktopFlyoutActivationMode.NeverActivate;
            UpdateCbtHook(neverActivate);
            if (neverActivate)
                RefreshXamlWindowSubclasses();

            SetNoActivateStyle(HWnd, neverActivate);
            SetNoActivateStyle(_xamlHwnd, neverActivate);

            foreach (var hWnd in _subclassedXamlWndProcs.Keys)
                SetNoActivateStyle((HWND)hWnd, neverActivate);

            if (!neverActivate)
                UnsubclassXamlWindows();
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

        private void RefreshXamlWindowSubclasses()
        {
            if (_xamlHwnd.IsNull)
                return;

            SubclassXamlWindow(_xamlHwnd);
            SubclassChildWindows(HWnd);
            SubclassChildWindows(_xamlHwnd);
        }

        private void SubclassChildWindows(HWND parentHWnd)
        {
            for (var childHWnd = PInvoke.GetWindow(parentHWnd, GET_WINDOW_CMD.GW_CHILD);
                !childHWnd.IsNull;
                childHWnd = PInvoke.GetWindow(childHWnd, GET_WINDOW_CMD.GW_HWNDNEXT))
            {
                SubclassXamlWindow(childHWnd);
                SubclassChildWindows(childHWnd);
            }
        }

        private void SubclassXamlWindow(HWND hWnd)
        {
            if (hWnd.IsNull || _subclassedXamlWndProcs.ContainsKey((nint)hWnd.Value))
                return;

            var previousWndProc = (nint)PInvoke.SetWindowLongPtr(
                hWnd,
                WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_xamlWndProc));

            _subclassedXamlWndProcs[(nint)hWnd.Value] = previousWndProc;
        }

        private void UnsubclassXamlWindows()
        {
            foreach (var item in _subclassedXamlWndProcs)
            {
                var hWnd = (HWND)item.Key;
                if (!hWnd.IsNull)
                    PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, item.Value);
            }

            _subclassedXamlWndProcs.Clear();
        }

        private LRESULT XamlWndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case PInvoke.WM_MOUSEACTIVATE:
                    if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                    {
                        RestoreActivationState();
                        return (LRESULT)(int)PInvoke.MA_NOACTIVATE;
                    }
                    break;
                case PInvoke.WM_SETFOCUS:
                    if (_activationMode is DesktopFlyoutActivationMode.NeverActivate)
                    {
                        RestoreActivationState();
                        return (LRESULT)0;
                    }
                    break;
            }

            return CallPreviousXamlWndProc(hWnd, uMsg, wParam, lParam);
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

        private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            if (!_disposed && args.WindowActivationState is CoreWindowActivationState.Deactivated)
                WindowInactivated?.Invoke(this, EventArgs.Empty);
        }

        private void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            var dispatcher = _coreWindow?.Dispatcher;
            if (_disposed || dispatcher is null)
                return;

            _ = dispatcher.TryRunAsync(
                CoreDispatcherPriority.Normal,
                () => SystemSettingsChanged?.Invoke(this, EventArgs.Empty));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            RemoveCbtHook();
            UnsubclassXamlWindows();

            _uiSettings.ColorValuesChanged -= UISettings_ColorValuesChanged;
            if (_coreWindow is not null)
                _coreWindow.Activated -= CoreWindow_Activated;

            if (_xamlWindow is not null)
                _xamlWindow.Content = null;

            _contentRoot?.Children.Clear();
            _contentRoot = null;
            _content = null;

            lock (s_claimedWindowsLock)
                s_claimedWindows.Remove((nint)HWnd.Value);

            if (!HWnd.IsNull)
                PInvoke.DestroyWindow(HWnd);

            _xamlWindow = null;
            _coreWindow = null;
            HWnd = default;
            _xamlHwnd = default;

            GC.SuppressFinalize(this);
        }
    }
}
#endif
