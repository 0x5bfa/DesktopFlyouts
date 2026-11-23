// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Copyright (c) Files Community.

#if !HAS_UNO

global using static global::Windows.Win32.ManualDefinitions;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

internal static partial class ManualDefinitions
{
	// SetWindowLongPtrW is a header alias for SetWindowLongW on 32-bit Windows.
	public static nint SetWindowLongPointer(HWND window, WINDOW_LONG_PTR_INDEX index, nint value)
	{
		return nint.Size == 8
			? SetWindowLongPtr64(window, index, value)
			: PInvoke.SetWindowLong(window, index, unchecked((int)value));
	}

	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static partial nint SetWindowLongPtr64(HWND window, WINDOW_LONG_PTR_INDEX index, nint value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SUCCEEDED(HRESULT hr)
	{
		return hr >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool FAILED(HRESULT hr)
	{
		return hr < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort LOWORD(nint l)
	{
		return unchecked((ushort)(((nuint)(l)) & 0XFFFF));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort HIWORD(nint l)
	{
		return ((ushort)((((nuint)(l)) >> 16) & 0XFFFF));
	}
}
#endif
