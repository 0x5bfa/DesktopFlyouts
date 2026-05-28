// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP
using Windows.UI.Xaml;
#elif WASDK
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

namespace DesktopFlyouts
{
    /// <summary>
    /// Provides calculated values that can be referenced from a <see cref="DesktopFlyoutIsland"/> template.
    /// </summary>
    /// <remarks>
    /// This follows the same pattern as platform template settings classes such as
    /// <c>NavigationViewTemplateSettings</c>. It is intended for control templates, not for general app logic.
    /// </remarks>
    public partial class DesktopFlyoutIslandTemplateSettings : DependencyObject
    {
        /// <summary>
        /// Identifies the <see cref="ShadowMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowMarginProperty =
            DependencyProperty.Register(nameof(ShadowMargin), typeof(Thickness), typeof(DesktopFlyoutIslandTemplateSettings), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Gets the transparent margin reserved for shadows around the island content.
        /// </summary>
        /// <value>The margin reserved outside the visible island surface.</value>
        public Thickness ShadowMargin
        {
            get => (Thickness)GetValue(ShadowMarginProperty);
            internal set => SetValue(ShadowMarginProperty, value);
        }

#if WASDK
        /// <summary>
        /// Gets the corner radius used by backdrop elements inside the island template.
        /// </summary>
        /// <value>The owner island's corner radius reduced for its inner backdrop surface.</value>
        [GeneratedDependencyProperty]
        public partial CornerRadius BackdropCornerRadius { get; internal set; }

        /// <summary>
        /// Gets the library-created system backdrop used by backdrop elements inside the island template.
        /// </summary>
        /// <value>The owning flyout's generated system backdrop for this island.</value>
        [GeneratedDependencyProperty]
        public partial SystemBackdrop? SystemBackdrop { get; internal set; }
#endif
    }
}
