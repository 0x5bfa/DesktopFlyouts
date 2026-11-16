// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using Windows.Win32;
using Windows.Win32.System.WinRT;

namespace DesktopFlyouts;

	public class Program
	{
		[STAThread]
		static void Main()
		{
			PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
			_ = new App();
		}
	}
