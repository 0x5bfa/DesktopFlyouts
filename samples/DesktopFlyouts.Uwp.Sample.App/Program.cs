// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using DesktopFlyouts.Shared;

namespace DesktopFlyouts
{
	public class Program
	{
		[STAThread]
		static void Main()
		{
			XamlIslandApplication.Start(_ => new App());
		}
	}
}
