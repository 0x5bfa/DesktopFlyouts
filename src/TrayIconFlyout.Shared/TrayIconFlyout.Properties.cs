// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#elif WASDK
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace U5BFA.Libraries
{
	public partial class TrayIconFlyout
	{
		private readonly ObservableCollection<TrayIconFlyoutIsland> _islands = [];
		public IList<TrayIconFlyoutIsland> Islands => _islands;

		[GeneratedDependencyProperty]
		public partial object? IslandsSource { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsBackdropEnabled { get; set; }

		public static readonly DependencyProperty FlyoutWidthProperty =
			DependencyProperty.Register(nameof(FlyoutWidth), typeof(GridLength), typeof(TrayIconFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

		public GridLength FlyoutWidth
		{
			get => (GridLength)GetValue(FlyoutWidthProperty);
			set => SetValue(FlyoutWidthProperty, value);
		}

		public static readonly DependencyProperty FlyoutHeightProperty =
			DependencyProperty.Register(nameof(FlyoutHeight), typeof(GridLength), typeof(TrayIconFlyout), new PropertyMetadata(GridLength.Auto, OnFlyoutSizePropertyChanged));

		public GridLength FlyoutHeight
		{
			get => (GridLength)GetValue(FlyoutHeightProperty);
			set => SetValue(FlyoutHeightProperty, value);
		}

		[GeneratedDependencyProperty(DefaultValue = TrayIconFlyoutPopupDirection.Vertical)]
		public partial TrayIconFlyoutPopupDirection PopupDirection { get; set; }

		[GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
		public partial Orientation IslandsOrientation { get; set; }

		[GeneratedDependencyProperty(DefaultValue = FlyoutPlacementMode.BottomEdgeAlignedRight)]
		public partial FlyoutPlacementMode Placement { get; set; }

		[GeneratedDependencyProperty]
		public partial MenuFlyout? MenuFlyout { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsTransitionAnimationEnabled { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool HideOnLostFocus { get; set; }

		[GeneratedDependencyProperty(DefaultValue = BackdropKind.Acrylic)]
		public partial BackdropKind BackdropKind { get; set; }

		private static void OnFlyoutSizePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			if (dependencyObject is TrayIconFlyout flyout)
				flyout.OnFlyoutSizeChanged();
		}

		partial void OnIslandsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is not IEnumerable<TrayIconFlyoutIsland> newIslands)
				return;

			Islands.Clear();

			foreach (var island in newIslands)
				Islands.Add(island);

			UpdateIslands();
		}

        partial void OnIslandsOrientationPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((Orientation)e.NewValue == (Orientation)e.OldValue)
                return;

            UpdateIslands();
			UpdateOpenFlyoutLayout();
        }

		partial void OnIsBackdropEnabledPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue == (bool)e.OldValue)
				return;

#if WASDK
			UpdateBackdropManager(true);
#endif
		}

		partial void OnBackdropKindPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if ((BackdropKind)e.NewValue == (BackdropKind)e.OldValue)
				return;

#if WASDK
			UpdateBackdropManager(true);
#endif
		}
	}
}
