// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DesktopFlyouts.UnitTests.WinUI;

// Test-only APIs use the library's native types to avoid duplicate CsWin32 definitions.
internal static unsafe class NativeTestMethods
{
	internal const uint RemoveMessage = 0x0001;

	[DllImport("user32.dll", ExactSpelling = true)]
	internal static extern void NotifyWinEvent(uint eventType, HWND window, int objectId, int childId);

	[DllImport("user32.dll", ExactSpelling = true)]
	internal static extern int PeekMessageW(MSG* message, HWND window, uint min, uint max, uint remove);
}
