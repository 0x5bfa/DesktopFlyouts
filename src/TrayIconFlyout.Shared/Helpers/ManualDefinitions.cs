// Copyright (c) Files Community
// Licensed under the MIT License.

global using static global::Windows.Win32.ManualDefinitions;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	internal delegate LRESULT WNDPROC([In] HWND hWnd, [In] uint uMsg, [In] WPARAM wParam, [In] LPARAM lParam);

	internal static class ManualDefinitions
	{
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

	internal static partial class IID
	{
		internal static ref readonly Guid IID_IDesktopWindowXamlSourceNative2
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref MemoryMarshal.AsRef<Guid>([0xC7, 0xD8, 0xDC, 0xE3, 0x57, 0x30, 0x92, 0x46, 0x99, 0xC3, 0x7B, 0x77, 0x20, 0xAF, 0xDA, 0x31]);
		}

		internal static ref readonly Guid IID_ICoreWindowInterop
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref MemoryMarshal.AsRef<Guid>([0x29, 0x4A, 0xD6, 0x45, 0x3E, 0xA6, 0xB6, 0x4C, 0xB4, 0x98, 0x57, 0x81, 0xD2, 0x98, 0xCB, 0x4F]);
		}
	}
}
