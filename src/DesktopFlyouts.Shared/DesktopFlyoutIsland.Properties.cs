// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace DesktopFlyouts
{
    public partial class DesktopFlyoutIsland : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="IslandWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IslandWidthProperty =
            DependencyProperty.Register(nameof(IslandWidth), typeof(GridLength), typeof(DesktopFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

        /// <summary>
        /// Gets or sets the island width.
        /// </summary>
        /// <value>The width used when the owner flyout arranges islands horizontally. The default is <see cref="GridLength.Auto"/>.</value>
        public GridLength IslandWidth
        {
            get => (GridLength)GetValue(IslandWidthProperty);
            set => SetValue(IslandWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IslandHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IslandHeightProperty =
            DependencyProperty.Register(nameof(IslandHeight), typeof(GridLength), typeof(DesktopFlyoutIsland), new PropertyMetadata(GridLength.Auto, OnIslandSizePropertyChanged));

        /// <summary>
        /// Gets or sets the island height.
        /// </summary>
        /// <value>The height used when the owner flyout arranges islands vertically. The default is <see cref="GridLength.Auto"/>.</value>
        public GridLength IslandHeight
        {
            get => (GridLength)GetValue(IslandHeightProperty);
            set => SetValue(IslandHeightProperty, value);
        }
    }
}
