// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace U5BFA.Libraries
{
	/// <summary>
	/// Provides functionality for displaying and managing a system tray icon in the taskbar.
	/// </summary>
	public unsafe class SystemTrayIcon
	{
		private const uint WM_UNIQUE_MESSAGE = 2048U;

		private readonly static string TrayIconWindowClassName = $"SystemTrayIconClass_{Guid.NewGuid():B}";

		private readonly uint _taskbarRestartMessageId;
		private readonly WNDPROC _wndProc;
		private readonly HWND _hWnd = default;

		private bool _created;
		private HICON _currentHIcon = default;

		private string _IconPath;
		public string IconPath
		{
			get => _IconPath;
			set
			{
				_IconPath = value;
				Show();
			}
		}

		private string _Tooltip;
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
		public bool IsVisible
		{
			get => _IsVisible;
			set
			{
				_IsVisible = value;
				Show();
			}
		}

		public Guid Id { get; }

		public event EventHandler? IconDestroyed;
		public event EventHandler<MouseEventReceivedEventArgs>? LeftClicked;
		public event EventHandler<MouseEventReceivedEventArgs>? RightClicked;

		/// <summary>
		/// Initializes a new instance of <see cref="SystemTrayIcon"/>.
		/// </summary>
		public SystemTrayIcon(string iconPath, string tooltip, Guid id, bool isVisible = true)
		{
			_taskbarRestartMessageId = PInvoke.RegisterWindowMessage((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "TaskbarCreated".GetPinnableReference())));
			_wndProc = new(WndProc);

			_IconPath = iconPath;
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
		public void Show()
		{
			HICON hIcon = (HICON)(void*)PInvoke.LoadImage(
				HINSTANCE.Null, (PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in IconPath.GetPinnableReference())),
				GDI_IMAGE_TYPE.IMAGE_ICON, cx: 0, cy: 0, IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);
			if (hIcon.IsNull)
				return;

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
			else
			{
				PInvoke.DestroyIcon(hIcon);
			}
		}

		/// <summary>
		/// Removes the associated notification icon from the system tray.
		/// </summary>
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
			if (_currentHIcon.IsNull)
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
