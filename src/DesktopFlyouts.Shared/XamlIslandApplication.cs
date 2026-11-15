#if !HAS_UNO
// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP
using System;
using Windows.UI.Xaml;
using XamlHostingKit;
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
        /// <summary>
        /// Starts System XAML through XamlHostingKit and runs its desktop message loop.
        /// </summary>
        /// <param name="initializationCallback">Creates the application's <see cref="Application"/> instance.</param>
        public new static void Start(ApplicationInitializationCallback initializationCallback)
        {
            ArgumentNullException.ThrowIfNull(initializationCallback);

            XamlApplication.Start(initializationCallback);
        }

        /// <summary>
        /// Creates another XamlHostingKit window and invokes a callback on its XAML thread.
        /// </summary>
        /// <param name="initializationCallback">
        /// Creates one <see cref="DesktopFlyout"/> or <see cref="DesktopMenuFlyout"/> for the new window.
        /// </param>
        /// <remarks>
        /// System XAML permits one top-level XAML window per thread. Create each additional
        /// DesktopFlyouts host inside a separate callback supplied to this method.
        /// </remarks>
        public static void CreateWindow(ApplicationInitializationCallback initializationCallback)
        {
            ArgumentNullException.ThrowIfNull(initializationCallback);
            XamlApplication.CreateWindow(new WindowCreationOptions(), initializationCallback);
        }
    }
#endif
}
#endif
