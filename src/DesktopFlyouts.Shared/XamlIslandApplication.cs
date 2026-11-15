#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP
using Windows.UI.Xaml;
#endif

namespace DesktopFlyouts.Shared
{
#if UWP
    /// <summary>
    /// Provides the UWP XAML application used by XAML island hosting.
    /// </summary>
    /// <remarks>
    /// This type is used by the UWP package to initialize XAML island hosting infrastructure.
    /// Application code usually does not need to create it directly.
    /// </remarks>
    public partial class XamlIslandApplication : Application
    {
    }
#endif
}
#endif
