// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using XamlHostingKit;
using WinRT.Interop;

namespace DesktopFlyouts
{
    internal sealed unsafe class MainWindow : IDisposable
    {
        private readonly XamlWindow _window = XamlWindow.Current;
        private readonly HWND _hwnd;
        private bool _disposed;

        internal MainWindow()
        {
            _window.Title = "DesktopFlyouts sample app (UWP)";
            _window.Styles = WindowStyles.OverlappedWindow;
            _window.Resize(600, 900);
            _window.Content = new RootView();

            _hwnd = (HWND)WindowNative.GetWindowHandle(_window);
            if (!PInvoke.SetWindowSubclass(_hwnd, &WindowSubclass, 1, 0))
                throw new InvalidOperationException("Could not install the sample window close handler.");
        }

        internal void Show() => _window.Show();

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static LRESULT WindowSubclass(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam, nuint id, nuint data)
        {
            if (message == PInvoke.WM_CLOSE)
            {
                // Keep the main dispatcher alive until the other XAML threads have released their controls.
                App.ExitApplication();
                return (LRESULT)0;
            }

            return PInvoke.DefSubclassProc(hwnd, message, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            PInvoke.RemoveWindowSubclass(_hwnd, &WindowSubclass, 1);
            _window.Content = null;
            PInvoke.DestroyWindow(_hwnd);
        }
    }
}
