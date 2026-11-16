// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

// Copyright (c) Files Community.

#if !HAS_UNO

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32;

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
#endif
