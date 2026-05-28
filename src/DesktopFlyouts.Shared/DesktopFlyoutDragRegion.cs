// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
#endif

namespace DesktopFlyouts
{
    /// <summary>
    /// Marks an area inside a <see cref="DesktopFlyout"/> that can move the native flyout window.
    /// </summary>
    /// <remarks>
    /// The element only contributes bounds for native hit-testing. It does not handle XAML pointer
    /// input or capture pointers.
    /// </remarks>
    public partial class DesktopFlyoutDragRegion : ContentControl
    {
        private DesktopFlyout? _owner;

        /// <summary>
        /// Initializes a new instance of <see cref="DesktopFlyoutDragRegion"/>.
        /// </summary>
        public DesktopFlyoutDragRegion()
        {
            DefaultStyleKey = typeof(DesktopFlyoutDragRegion);
            IsTabStop = false;

            Loaded += DesktopFlyoutDragRegion_Loaded;
            Unloaded += DesktopFlyoutDragRegion_Unloaded;
            PointerPressed += DesktopFlyoutDragRegion_PointerPressed;
            SizeChanged += DesktopFlyoutDragRegion_SizeChanged;
            LayoutUpdated += DesktopFlyoutDragRegion_LayoutUpdated;
        }

        private void DesktopFlyoutDragRegion_Loaded(object sender, RoutedEventArgs e)
        {
            _owner?.UnregisterDragRegion(this);
            _owner = FindOwnerFlyout();
            _owner?.RegisterDragRegion(this);
        }

        private void DesktopFlyoutDragRegion_Unloaded(object sender, RoutedEventArgs e)
        {
            _owner?.UnregisterDragRegion(this);
            _owner = null;
        }

        private void DesktopFlyoutDragRegion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
#if UWP
            _owner?.BeginDragMove(this, e);
#endif
        }

        private void DesktopFlyoutDragRegion_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _owner?.OnDragRegionChanged();
        }

        private void DesktopFlyoutDragRegion_LayoutUpdated(object? sender, object e)
        {
            _owner?.OnDragRegionChanged();
        }

        private DesktopFlyout? FindOwnerFlyout()
        {
            DependencyObject? current = this;
            while (current is not null)
            {
                if (current is DesktopFlyout flyout)
                    return flyout;

                if (current is DesktopFlyoutIsland island)
                    return island.Owner;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
