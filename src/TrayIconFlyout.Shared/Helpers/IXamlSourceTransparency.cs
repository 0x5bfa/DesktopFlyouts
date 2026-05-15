// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Windows.UI.Xaml
{
	[GeneratedComInterface, Guid("06636C29-5A17-458D-8EA2-2422D997A922")]
	internal partial interface IXamlSourceTransparency // This is the same interface as IWindowPrivate
	{
		[PreserveSig()]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetIids(out int iidCount, out nint iids);

		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetRuntimeClassName(out HSTRING className);

		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetTrustLevel(out int trustLevel);

		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_IsBackgroundTransparent([MarshalAs(UnmanagedType.I1)] out bool pvalue);

		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT set_IsBackgroundTransparent([MarshalAs(UnmanagedType.I1)] bool value);
	}
}
