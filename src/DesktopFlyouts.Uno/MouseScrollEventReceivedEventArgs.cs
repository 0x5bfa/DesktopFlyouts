// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace DesktopFlyouts;

public class MouseScrollEventReceivedEventArgs : EventArgs
{
	public int Delta { get; }
	public MouseScrollOrientation Orientation { get; }

	internal MouseScrollEventReceivedEventArgs(int delta, MouseScrollOrientation orientation)
	{
		Delta = delta;
		Orientation = orientation;
	}
}
