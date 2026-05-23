// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP
using Windows.UI.Xaml;
#elif WASDK
using Microsoft.UI.Xaml;
#endif

namespace U5BFA.Libraries
{
    /// <summary>
    /// Provides calculated values that can be referenced from a <see cref="DesktopFlyoutIsland"/> template.
    /// </summary>
    /// <remarks>
    /// This follows the same pattern as platform template settings classes such as
    /// <c>NavigationViewTemplateSettings</c>. It is intended for control templates, not for general app logic.
    /// </remarks>
    public class DesktopFlyoutIslandTemplateSettings : DependencyObject
    {
        /// <summary>
        /// Identifies the <see cref="BackdropCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackdropCornerRadiusProperty =
            DependencyProperty.Register(nameof(BackdropCornerRadius), typeof(CornerRadius), typeof(DesktopFlyoutIslandTemplateSettings), new PropertyMetadata(default(CornerRadius)));

        /// <summary>
        /// Gets the corner radius used by backdrop elements inside the island template.
        /// </summary>
        /// <value>The owner island's corner radius reduced for its inner backdrop surface.</value>
        public CornerRadius BackdropCornerRadius
        {
            get => (CornerRadius)GetValue(BackdropCornerRadiusProperty);
            internal set => SetValue(BackdropCornerRadiusProperty, value);
        }
    }
}
