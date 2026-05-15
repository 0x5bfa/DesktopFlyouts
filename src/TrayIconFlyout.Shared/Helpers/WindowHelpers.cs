// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace U5BFA.Libraries
{
	internal unsafe static class WindowHelpers
	{
		internal static Point GetBottomRightCornerPoint()
		{
			RECT rect;
			PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rect, 0);

			Point point = default;
			point.X = rect.right;
			point.Y = rect.bottom;

			return point;
		}
	}
}
