// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;

namespace U5BFA.Libraries
{
	public class MouseEventReceivedEventArgs : EventArgs
	{
		public Point Point { get; }

		internal MouseEventReceivedEventArgs(Point point)
		{
			Point = point;
		}
	}
}
