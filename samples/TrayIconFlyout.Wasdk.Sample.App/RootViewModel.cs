// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace U5BFA.Libraries
{
	internal partial class RootViewModel : ObservableObject
	{
		[ObservableProperty()]
		internal partial string? IconPath { get; set; }

		[ObservableProperty()]
		internal partial string? TooltipText { get; set; }

		[ObservableProperty]
		internal partial bool IsBackdropEnabled { get; set; }

		[ObservableProperty]
		internal partial bool HideOnLostFocus { get; set; }

		[ObservableProperty]
		internal partial int SelectedPopupDirectionIndex { get; set; }

		[ObservableProperty]
		internal partial int SelectedFlyoutPlacementIndex { get; set; }

		[ObservableProperty]
		internal partial int SelectedBackdropIndex { get; set; }

		public Dictionary<TrayIconFlyoutPopupDirection, string> PopupDirections { get; private set; } = [];
		public Dictionary<FlyoutPlacementMode, string> FlyoutPlacements { get; private set; } = [];
		public Dictionary<BackdropKind, string> Backdrops { get; private set; } = [];

		internal RootViewModel()
		{
			IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
			TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

			IsBackdropEnabled = true;
			HideOnLostFocus = true;

			PopupDirections.Add(TrayIconFlyoutPopupDirection.Vertical, "Vertical");
			PopupDirections.Add(TrayIconFlyoutPopupDirection.BottomToTop, "Bottom to top");
			PopupDirections.Add(TrayIconFlyoutPopupDirection.TopToBottom, "Top to bottom");
			PopupDirections.Add(TrayIconFlyoutPopupDirection.Horizontal, "Horizontal");
			PopupDirections.Add(TrayIconFlyoutPopupDirection.LeftToRight, "Left to right");
			PopupDirections.Add(TrayIconFlyoutPopupDirection.RightToLeft, "Right to left");
			SelectedPopupDirectionIndex = 0;

			FlyoutPlacements.Add(FlyoutPlacementMode.TopEdgeAlignedLeft, "Top left");
			FlyoutPlacements.Add(FlyoutPlacementMode.TopEdgeAlignedRight, "Top right");
			FlyoutPlacements.Add(FlyoutPlacementMode.BottomEdgeAlignedLeft, "Bottom left");
			FlyoutPlacements.Add(FlyoutPlacementMode.BottomEdgeAlignedRight, "Bottom right");
			SelectedFlyoutPlacementIndex = 3;

			Backdrops.Add(BackdropKind.Acrylic, "Acrylic");
			Backdrops.Add(BackdropKind.Mica, "Mica");
			SelectedBackdropIndex = 0;
		}

		partial void OnIconPathChanged(string? value)
		{
			TrayIconManager.Default.SystemTrayIcon?.IconPath = value ?? string.Empty;
		}

		partial void OnTooltipTextChanged(string? value)
		{
			TrayIconManager.Default.SystemTrayIcon?.Tooltip = value ?? string.Empty;
		}

		partial void OnIsBackdropEnabledChanged(bool value)
		{
			TrayIconManager.Default.TrayIconFlyout?.IsBackdropEnabled = value;
		}

		partial void OnHideOnLostFocusChanged(bool value)
		{
			TrayIconManager.Default.TrayIconFlyout?.HideOnLostFocus = value;
		}

		partial void OnSelectedPopupDirectionIndexChanged(int value)
		{
			TrayIconManager.Default.TrayIconFlyout?.PopupDirection = PopupDirections.ElementAt(value).Key;
		}

		partial void OnSelectedFlyoutPlacementIndexChanged(int value)
		{
			TrayIconManager.Default.TrayIconFlyout?.Placement = FlyoutPlacements.ElementAt(value).Key;
		}

		partial void OnSelectedBackdropIndexChanged(int value)
		{
			TrayIconManager.Default.TrayIconFlyout?.BackdropKind = Backdrops.ElementAt(value).Key;
		}
	}
}
