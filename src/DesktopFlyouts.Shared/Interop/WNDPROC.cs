// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Copyright (c) Files Community.

#if !HAS_UNO

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate LRESULT WNDPROC([In] HWND hWnd, [In] uint uMsg, [In] WPARAM wParam, [In] LPARAM lParam);
#endif
