// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Copyright (c) Files Community.

#if !HAS_UNO

global using static global::Windows.Win32.ManualDefinitions;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

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
#endif
