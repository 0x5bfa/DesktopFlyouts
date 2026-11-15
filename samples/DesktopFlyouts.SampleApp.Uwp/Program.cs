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
#pragma warning disable CA1806 // Do not ignore method results
			XamlIslandApplication.Start(_ => new App());
#pragma warning restore CA1806 // Do not ignore method results
		}
	}
}
