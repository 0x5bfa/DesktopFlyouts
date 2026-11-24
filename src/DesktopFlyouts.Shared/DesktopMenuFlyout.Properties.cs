// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if WINDOWS
using CommunityToolkit.WinUI;

namespace DesktopFlyouts;

public partial class DesktopMenuFlyout
{
	/// <summary>
	/// Gets whether the menu flyout is currently open.
	/// </summary>
	/// <value><see langword="true"/> while the hosted menu is open; otherwise, <see langword="false"/>.</value>
	[GeneratedDependencyProperty]
	public partial bool IsOpen { get; private set; }
}
#endif
