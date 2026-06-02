// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
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
    /// Call <see cref="Destroy"/> when the icon should
    /// be removed.
    /// </remarks>
    public unsafe class SystemTrayIcon
    {
        private const uint WM_UNIQUE_MESSAGE = 2048U;

        private readonly static string TrayIconWindowClassName = $"SystemTrayIconClass_{Guid.NewGuid():B}";

        private readonly uint _taskbarRestartMessageId;
        private readonly WNDPROC _wndProc;
        private readonly HWND _hWnd = default;

        private bool _created;
        private HICON _currentHIcon = default;
        private bool _iconFromFile;

        private string? _IconPath;

        /// <summary>
        /// Gets or sets the path to the icon file.
        /// </summary>
        /// <value>The path to an icon file loaded with the Win32 <c>LoadImage</c> API.</value>
        /// <remarks>
        /// Setting this property updates the existing tray icon immediately.
        /// Setting this property clears <see cref="Icon"/>.
        /// </remarks>
        public string? IconPath
        {
            get => _IconPath;
            set
            {
                _IconPath = value;
                _Icon = default;
                _iconFromFile = true;
                Show();
            }
        }

        private nint _Icon;

        /// <summary>
        /// Gets or sets the native icon handle.
        /// </summary>
        /// <value>A caller-owned icon handle. The handle is not destroyed by <see cref="SystemTrayIcon"/>.</value>
        /// <remarks>
        /// Setting this property updates the existing tray icon immediately.
        /// Setting this property clears <see cref="IconPath"/>.
        /// The caller must ensure the handle remains valid for the lifetime of the tray icon.
        /// </remarks>
        public nint Icon
        {
            get => _Icon;
            set
            {
                _Icon = value;
                _IconPath = null;
                _iconFromFile = false;
                Show();
            }
        }

        private string _Tooltip;

        /// <summary>
        /// Gets or sets the tooltip text shown for the tray icon.
        /// </summary>
        /// <value>The tray icon tooltip text. Text longer than the shell limit is truncated.</value>
        /// <remarks>
        /// Setting this property updates the existing tray icon immediately.
        /// </remarks>
        public string Tooltip
        {
            get => _Tooltip;
            set
            {
                _Tooltip = value;
                Show();
            }
        }

        private bool _IsVisible;

        /// <summary>
        /// Gets or sets whether the tray icon is visible.
        /// </summary>
        /// <value><see langword="true"/> to show the tray icon; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// Setting this property updates the existing tray icon immediately.
        /// </remarks>
        public bool IsVisible
        {
            get => _IsVisible;
            set
            {
                _IsVisible = value;
                Show();
            }
        }

        /// <summary>
        /// Gets the stable identifier used for the tray icon.
        /// </summary>
        /// <value>The GUID used by the shell to identify this notification icon.</value>
        public Guid Id { get; }

        /// <summary>
        /// Occurs when the tray icon window is destroyed.
        /// </summary>
        /// <remarks>
        /// The event is raised after <see cref="Destroy"/> has been called from the hidden tray icon
        /// window's destroy message.
        /// </remarks>
        public event EventHandler? IconDestroyed;

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
        /// Initializes a new instance of <see cref="SystemTrayIcon"/>.
        /// </summary>
        /// <param name="iconPath">The path to the icon file.</param>
        /// <param name="tooltip">The tooltip text.</param>
        /// <param name="id">The stable identifier for the tray icon.</param>
        /// <param name="isVisible">Whether the tray icon is initially visible.</param>
        /// <remarks>
        /// Construction creates the hidden window that receives notification icon callbacks. Call
        /// <see cref="Show"/> to add the icon to the shell notification area.
        /// </remarks>
        public SystemTrayIcon(string iconPath, string tooltip, Guid id, bool isVisible = true)
        {
            _taskbarRestartMessageId = PInvoke.RegisterWindowMessage((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "TaskbarCreated".GetPinnableReference())));
            _wndProc = new(WndProc);

            _IconPath = iconPath;
            _iconFromFile = true;
            _Tooltip = tooltip;
            Id = id;
            _IsVisible = isVisible;

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
        /// Initializes a new instance of <see cref="SystemTrayIcon"/> with a native icon handle.
        /// </summary>
        /// <param name="hIcon">A caller-owned icon handle. The handle is not destroyed by <see cref="SystemTrayIcon"/>.</param>
        /// <param name="tooltip">The tooltip text.</param>
        /// <param name="id">The stable identifier for the tray icon.</param>
        /// <param name="isVisible">Whether the tray icon is initially visible.</param>
        /// <remarks>
        /// Construction creates the hidden window that receives notification icon callbacks. Call
        /// <see cref="Show"/> to add the icon to the shell notification area.
        /// The caller must ensure the handle remains valid for the lifetime of the tray icon.
        /// </remarks>
        public SystemTrayIcon(nint hIcon, string tooltip, Guid id, bool isVisible = true)
        {
            _taskbarRestartMessageId = PInvoke.RegisterWindowMessage((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "TaskbarCreated".GetPinnableReference())));
            _wndProc = new(WndProc);

            _Icon = hIcon;
            _iconFromFile = false;
            _Tooltip = tooltip;
            Id = id;
            _IsVisible = isVisible;

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
        /// Displays the notification icon in the system tray, creating it if necessary or updating its appearance and tooltip if it already exists.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the icon cannot be loaded from <see cref="IconPath"/> or <see cref="Icon"/> is not a valid icon handle.
        /// </exception>
        public void Show()
        {
            HICON hIcon;
            if (_iconFromFile)
            {
                hIcon = (HICON)(void*)PInvoke.LoadImage(
                    HINSTANCE.Null,
                    (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in IconPath!.GetPinnableReference())),
                    GDI_IMAGE_TYPE.IMAGE_ICON,
                    cx: 0, cy: 0,
                    IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);
                if (hIcon.IsNull)
                    throw new ArgumentOutOfRangeException(nameof(IconPath), $"LoadImage failed with 0x{Marshal.GetLastWin32Error():X8}");
            }
            else
            {
                hIcon = (HICON)(void*)Icon;
                if (hIcon.IsNull)
                    throw new ArgumentOutOfRangeException(nameof(Icon), "Icon handle is null.");

                ICONINFO iconInfo = default;
                if (!PInvoke.GetIconInfo(hIcon, &iconInfo))
                    throw new ArgumentOutOfRangeException(nameof(Icon), $"GetIconInfo failed with 0x{Marshal.GetLastWin32Error():X8}");

                if ((nint)iconInfo.hbmColor != HBITMAP.Null)
                    PInvoke.DeleteObject(iconInfo.hbmColor);
                if ((nint)iconInfo.hbmMask != HBITMAP.Null)
                    PInvoke.DeleteObject(iconInfo.hbmMask);
            }

            NOTIFYICONDATAW data = default;
            data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
            data.hWnd = _hWnd;
            data.uCallbackMessage = WM_UNIQUE_MESSAGE;
            data.hIcon = hIcon;
            data.guidItem = Id;
            data.dwState = IsVisible ? 0U : NOTIFY_ICON_STATE.NIS_HIDDEN;
            data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_STATE | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;

            // Properly copy the tooltip string into the __char_128 struct
            if (!string.IsNullOrEmpty(Tooltip))
            {
                var tipSpan = Tooltip.AsSpan();
                int len = Math.Min(tipSpan.Length, 127); // __char_128 can hold up to 127 chars + null terminator
                var destSpan = new Span<char>(data.szTip.Value, len);
                tipSpan.Slice(0, len).CopyTo(destSpan);
                data.szTip.Value[len] = '\0';
            }
            else
            {
                data.szTip.Value[0] = '\0';
            }

            bool updated = false;
            if (_created)
            {
                updated = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, &data);
                if (!updated)
                    _created = false;
            }

            if (!_created)
            {
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);
                updated = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &data);
                if (updated)
                {
                    data.Anonymous.uVersion = 4u;
                    PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, &data);

                    _created = true;
                }
            }

            if (updated)
            {
                DestroyCurrentIcon();
                _currentHIcon = hIcon;
            }
            else if (_iconFromFile)
            {
                PInvoke.DestroyIcon(hIcon);
            }
        }

        /// <summary>
        /// Removes the associated notification icon from the system tray.
        /// </summary>
        /// <remarks>
        /// This method is safe to call multiple times.
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

            DestroyCurrentIcon();
        }

        private void DestroyCurrentIcon()
        {
            if (_currentHIcon.IsNull || !_iconFromFile)
                return;

            PInvoke.DestroyIcon(_currentHIcon);
            _currentHIcon = default;
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
                                        LeftClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));

                                    break;
                                }
                            case PInvoke.WM_RBUTTONUP:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                        RightClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));

                                    break;
                                }
                            case PInvoke.WM_LBUTTONDBLCLK:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                        LeftDoubleClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));

                                    break;
                                }
                            case PInvoke.WM_RBUTTONDBLCLK:
                                {
                                    PInvoke.SetForegroundWindow(hWnd);
                                    var point = GetCenterPointOfTrayIcon(hWnd);
                                    if (!point.IsEmpty)
                                        RightDoubleClicked?.Invoke(this, new MouseEventReceivedEventArgs(point));

                                    break;
                                }
                        }

                        break;
                    }
                case PInvoke.WM_DESTROY:
                    {
                        Destroy();
                        IconDestroyed?.Invoke(this, EventArgs.Empty);

                        break;
                    }
                default:
                    {
                        // Taskbar was restarted, recreate the icon
                        if (uMsg == _taskbarRestartMessageId)
                        {
                            Destroy();
                            Show();
                        }

                        return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                    }
            }
            return default;
        }
    }
}
