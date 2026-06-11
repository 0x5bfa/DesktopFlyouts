// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts
{
    /// <summary>
    /// Provides functionality for displaying and managing a system tray icon in the taskbar.
    /// </summary>
    /// <remarks>
    /// <see cref="SystemTrayIcon"/> wraps the Win32 notification icon APIs and raises click events
    /// with screen coordinates that can be passed to the point-based flyout and menu show methods.
    /// Call <see cref="Show"/> to add the icon to the shell notification area and
    /// <see cref="Destroy"/> to remove it. Call <see cref="Dispose()"/> explicitly when the object
    /// is no longer needed to release its owned resources.
    /// </remarks>
    public unsafe partial class SystemTrayIcon : IDisposable
    {
        private const uint WM_UNIQUE_MESSAGE = 2048U;

        private readonly static string TrayIconWindowClassName = $"SystemTrayIconClass_{Guid.NewGuid():B}";

        private readonly uint _taskbarRestartMessageId;
        private readonly WNDPROC _wndProc;
        private readonly HWND _hWnd;

        private bool _created;
        private bool _disposed;
        private HICON _hIcon;

        /// <summary>
        /// Gets the path to the current icon file, or <see langword="null"/> if the icon was set
        /// from an existing handle.
        /// </summary>
        /// <value>The path to an icon file loaded with the Win32 <c>LoadImage</c> API.</value>
        public string? IconPath { get; private set; }

        private string _Tooltip;

        /// <summary>
        /// Gets or sets the tooltip text shown for the tray icon.
        /// </summary>
        /// <value>The tray icon tooltip text. Text longer than the shell limit is truncated.</value>
        /// <remarks>
        /// If the tray icon has already been created in the shell, setting this property updates it
        /// immediately. Otherwise, the value is recorded and applied when <see cref="Show"/> is
        /// called.
        /// </remarks>
        public string Tooltip
        {
            get => _Tooltip;
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _Tooltip = value;
                Update();
            }
        }

        private bool _IsVisible;

        /// <summary>
        /// Gets or sets whether the tray icon is visible.
        /// </summary>
        /// <value><see langword="true"/> if the tray icon is visible; otherwise,
        /// <see langword="false"/>. The default is <see langword="false"/>.</value>
        /// <remarks>
        /// If the tray icon has already been created in the shell, setting this property updates
        /// the icon state immediately. Otherwise, the value is recorded and applied when
        /// <see cref="Show"/> is called.
        /// </remarks>
        public bool IsVisible
        {
            get => _IsVisible;
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _IsVisible = value;
                Update();
            }
        }

        /// <summary>
        /// Gets the stable identifier used for the tray icon.
        /// </summary>
        /// <value>The GUID used by the shell to identify this notification icon.</value>
        public Guid Id { get; }

        /// <summary>
        /// Occurs when the tray icon receives a left-click.
        /// </summary>
        /// <remarks>
        /// The event argument contains the center point of the tray icon in physical screen pixels.
        /// </remarks>
        public event EventHandler<MouseEventReceivedEventArgs>? LeftClicked;

        /// <summary>
        /// Occurs when the tray icon receives a right-click.
        /// </summary>
        /// <remarks>
        /// The event argument contains the center point of the tray icon in physical screen pixels.
        /// </remarks>
        public event EventHandler<MouseEventReceivedEventArgs>? RightClicked;

        /// <summary>
        /// Occurs when the tray icon receives a left double-click.
        /// </summary>
        /// <remarks>
        /// The event argument contains the center point of the tray icon in physical screen pixels.
        /// </remarks>
        public event EventHandler<MouseEventReceivedEventArgs>? LeftDoubleClicked;

        /// <summary>
        /// Occurs when the tray icon receives a right double-click.
        /// </summary>
        /// <remarks>
        /// The event argument contains the center point of the tray icon in physical screen pixels.
        /// </remarks>
        public event EventHandler<MouseEventReceivedEventArgs>? RightDoubleClicked;

        /// <summary>
        /// Initializes a new instance of <see cref="SystemTrayIcon"/> with an icon loaded from a
        /// file path.
        /// </summary>
        /// <param name="iconPath">The path to the icon file.</param>
        /// <param name="tooltip">The tooltip text.</param>
        /// <param name="id">The stable identifier for the tray icon.</param>
        /// <remarks>
        /// Construction prepares the icon resources and creates the hidden callback window. The
        /// icon is not added to the shell notification area until <see cref="Show"/> is called.
        /// </remarks>
        public SystemTrayIcon(string iconPath, string tooltip, Guid id)
        {
            _taskbarRestartMessageId = PInvoke.RegisterWindowMessage((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "TaskbarCreated".GetPinnableReference())));
            _wndProc = new(WndProc);

            IconPath = iconPath;
            _Tooltip = tooltip;
            Id = id;
            _hIcon = LoadIconFromPath(iconPath);

            WNDCLASSW wndClass = default;
            wndClass.style = WNDCLASS_STYLES.CS_DBLCLKS;
            wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
            wndClass.hInstance = PInvoke.GetModuleHandle(null);
            wndClass.lpszClassName = (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in TrayIconWindowClassName.GetPinnableReference()));
            PInvoke.RegisterClass(&wndClass);

            _hWnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_LEFT, (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in TrayIconWindowClassName.GetPinnableReference())),
                null, WINDOW_STYLE.WS_OVERLAPPED, X: 0, Y: 0, nWidth: 1, nHeight: 1, HWND.Null, HMENU.Null, HINSTANCE.Null, null);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SystemTrayIcon"/> with an existing icon handle.
        /// The handle is copied internally; the caller may destroy the original after construction.
        /// </summary>
        /// <param name="hIcon">An existing icon handle. The handle is copied, so the caller
        /// retains ownership of the original.</param>
        /// <param name="tooltip">The tooltip text.</param>
        /// <param name="id">The stable identifier for the tray icon.</param>
        /// <remarks>
        /// Construction prepares the icon resources and creates the hidden callback window. The
        /// icon is not added to the shell notification area until <see cref="Show"/> is called.
        /// </remarks>
        public SystemTrayIcon(nint hIcon, string tooltip, Guid id)
        {
            _taskbarRestartMessageId = PInvoke.RegisterWindowMessage((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "TaskbarCreated".GetPinnableReference())));
            _wndProc = new(WndProc);

            _Tooltip = tooltip;
            Id = id;

            _hIcon = PInvoke.CopyIcon((HICON)hIcon);
            if (_hIcon.IsNull)
            {
                throw new Win32Exception(Marshal.GetHRForLastWin32Error(), "Failed to copy icon.");
            }

            WNDCLASSW wndClass = default;
            wndClass.style = WNDCLASS_STYLES.CS_DBLCLKS;
            wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(_wndProc);
            wndClass.hInstance = PInvoke.GetModuleHandle(null);
            wndClass.lpszClassName = (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in TrayIconWindowClassName.GetPinnableReference()));
            PInvoke.RegisterClass(&wndClass);

            _hWnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_LEFT, (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in TrayIconWindowClassName.GetPinnableReference())),
                null, WINDOW_STYLE.WS_OVERLAPPED, X: 0, Y: 0, nWidth: 1, nHeight: 1, HWND.Null, HMENU.Null, HINSTANCE.Null, null);
        }

        /// <summary>
        /// Makes the tray icon visible and synchronizes its state to the shell.
        /// </summary>
        /// <remarks>
        /// If the tray icon has not yet been created in the shell, this method creates it. If it
        /// already exists, this method updates its current state.
        /// </remarks>
        public void Show()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _IsVisible = true;
            if (_created)
            {
                Update();
            }
            else
            {
                EnsureCreated();
            }
        }

        /// <summary>
        /// Replaces the current icon with one loaded from a file path.
        /// </summary>
        /// <param name="iconPath">The path to the icon file.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="iconPath"/> cannot be loaded as an icon file.
        /// </exception>
        public void SetIcon(string iconPath)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            HICON newIcon = LoadIconFromPath(iconPath);
            DeleteCurrentIcon();
            IconPath = iconPath;
            _hIcon = newIcon;
            Update();
        }

        /// <summary>
        /// Replaces the current icon with an existing icon handle. The handle is copied
        /// internally; the caller may destroy the original after this method returns.
        /// </summary>
        /// <param name="hIcon">An existing icon handle. The handle is copied, so the caller
        /// retains ownership of the original.</param>
        public void SetIcon(nint hIcon)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            HICON newIcon = PInvoke.CopyIcon((HICON)hIcon);
            if (newIcon.IsNull)
            {
                throw new Win32Exception(Marshal.GetHRForLastWin32Error(), "Failed to copy icon.");
            }
            DeleteCurrentIcon();
            IconPath = null;
            _hIcon = newIcon;
            Update();
        }

        /// <summary>
        /// Removes the associated notification icon from the system tray.
        /// </summary>
        /// <remarks>
        /// This method is safe to call multiple times. It does not dispose the
        /// <see cref="SystemTrayIcon"/> object; call <see cref="Dispose()"/> to release all native
        /// resources.
        /// </remarks>
        public void Destroy()
        {
            if (_created)
            {
                NOTIFYICONDATAW data = default;
                data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
                data.hWnd = _hWnd;
                data.guidItem = Id;
                data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;

                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);

                _created = false;
            }

            _IsVisible = false;
        }

        private void EnsureCreated()
        {
            if (!_created)
            {
                NOTIFYICONDATAW data = GetNotifyIconData();
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);
                if (PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &data))
                {
                    data.Anonymous.uVersion = 4u;
                    PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, &data);
                    _created = true;
                }
            }
        }

        private void Update()
        {
            if (!_created)
            {
                return;
            }
            NOTIFYICONDATAW data = GetNotifyIconData();
            if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, &data))
            {
                _created = false;
                if (IsVisible)
                {
                    EnsureCreated();
                }
            }
        }

        private NOTIFYICONDATAW GetNotifyIconData()
        {
            NOTIFYICONDATAW data = default;
            data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
            data.hWnd = _hWnd;
            data.uCallbackMessage = WM_UNIQUE_MESSAGE;
            data.hIcon = _hIcon;
            data.guidItem = Id;
            data.szTip = Tooltip ?? string.Empty;
            data.dwState = _IsVisible ? 0U : NOTIFY_ICON_STATE.NIS_HIDDEN;
            data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_STATE | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
            return data;
        }

        private static HICON LoadIconFromPath(string iconPath)
        {
            if (!File.Exists(iconPath))
            {
                throw new FileNotFoundException($"Icon file not found: {iconPath}", iconPath);
            }
            HICON hIcon = (HICON)(void*)PInvoke.LoadImage(
                HINSTANCE.Null,
                (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in iconPath.GetPinnableReference())),
                GDI_IMAGE_TYPE.IMAGE_ICON,
                cx: 0, cy: 0,
                IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);

            if (hIcon.IsNull)
            {
                throw new ArgumentOutOfRangeException(nameof(iconPath), $"LoadImage failed with 0x{Marshal.GetLastWin32Error():X8}");
            }

            return hIcon;
        }

        private void DeleteCurrentIcon()
        {
            if (_hIcon.IsNull)
            {
                return;
            }

            PInvoke.DestroyIcon(_hIcon);
            _hIcon = default;
        }

        private Point GetCenterPointOfTrayIcon(HWND hWnd)
        {
            NOTIFYICONIDENTIFIER nii = default;
            nii.cbSize = (uint)sizeof(NOTIFYICONIDENTIFIER);
            nii.hWnd = hWnd;
            nii.guidItem = Id;

            RECT rect = default;
            Point point = default;
            HRESULT hr = PInvoke.Shell_NotifyIconGetRect(&nii, &rect);
            if (SUCCEEDED(hr))
            {
                point.X = rect.right - (rect.Width / 2);
                point.Y = rect.bottom - (rect.Height / 2);
            }

            return point;
        }

        private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case WM_UNIQUE_MESSAGE:
                    {
                        switch ((uint)LOWORD(lParam.Value))
                        {
                            case PInvoke.WM_LBUTTONUP:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                    {
                                        LeftClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));
                                    }

                                    break;
                                }
                            case PInvoke.WM_RBUTTONUP:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                    {
                                        RightClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));
                                    }

                                    break;
                                }
                            case PInvoke.WM_LBUTTONDBLCLK:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                    {
                                        LeftDoubleClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));
                                    }

                                    break;
                                }
                            case PInvoke.WM_RBUTTONDBLCLK:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                    {
                                        RightDoubleClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                default:
                    {
                        if (uMsg == _taskbarRestartMessageId)
                        {
                            bool wasVisible = _IsVisible;
                            Destroy();
                            if (wasVisible)
                            {
                                Show();
                            }
                        }

                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                    }
            }
            return default;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Destroy();
                DeleteCurrentIcon();
                if (!_hWnd.IsNull)
                {
                    PInvoke.DestroyWindow(_hWnd);
                }
                _disposed = true;
            }
        }

        ~SystemTrayIcon()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Releases the resources held by this <see cref="SystemTrayIcon"/>.
        /// </summary>
        /// <remarks>
        /// Call this method explicitly when the instance is no longer needed. If the tray icon is
        /// still present in the shell, it is removed first. The hidden callback window is destroyed
        /// and any owned icon handle is released. After disposal, the instance should not be used
        /// again.
        /// </remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
