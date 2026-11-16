namespace DesktopFlyouts;

public enum MouseScrollOrientation
{
	Vertical,
	Horizontal
}

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
