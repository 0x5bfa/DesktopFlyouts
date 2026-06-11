// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DesktopFlyouts
{
    internal partial class RootViewModel : ObservableObject
    {
        [ObservableProperty]
        internal partial string? IconPath { get; set; }

        [ObservableProperty]
        internal partial string? TooltipText { get; set; }

        [ObservableProperty]
        internal partial int SelectedPopupDirectionIndex { get; set; } = 5;

        [ObservableProperty]
        internal partial int SelectedButtonPopupPositionIndex { get; set; } = 5;

        [ObservableProperty]
        internal partial double ButtonFlyoutWidthValue { get; set; } = 360D;

        [ObservableProperty]
        internal partial double ButtonFlyoutHeightValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial bool IsSwipeToDismissEnabled { get; set; } = true;

        [ObservableProperty]
        internal partial double SwipeDismissThresholdValue { get; set; } = 80D;

        [ObservableProperty]
        internal partial double PressedScaleValue { get; set; } = 0.9D;

        [ObservableProperty]
        internal partial int SelectedActivationModeIndex { get; set; } = 2;

        [ObservableProperty]
        internal partial int SelectedIndicatorPopupPositionIndex { get; set; } = 4;

        [ObservableProperty]
        internal partial double IndicatorFlyoutWidthValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial double IndicatorFlyoutHeightValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial double AutoCloseDelaySecondsValue { get; set; } = 1.5D;

        [ObservableProperty]
        internal partial bool HideOnLostFocus { get; set; } = true;

        [ObservableProperty]
        internal partial int SelectedDraggableIslandLayoutModeIndex { get; set; } = 1;

        [ObservableProperty]
        internal partial bool IsDraggableFlyoutSizeEnabled { get; set; }

        [ObservableProperty]
        internal partial int SelectedDraggableDragModeIndex { get; set; } = 2;

        [ObservableProperty]
        internal partial int SelectedDraggablePopupPositionIndex { get; set; } = 4;

        [ObservableProperty]
        internal partial double DraggableFlyoutWidthValue { get; set; } = 620D;

        [ObservableProperty]
        internal partial double DraggableFlyoutHeightValue { get; set; } = 340D;

        [ObservableProperty]
        internal partial int SelectedSeverityPopupPositionIndex { get; set; } = 5;

        [ObservableProperty]
        internal partial double SeverityFlyoutWidthValue { get; set; } = 360D;

        [ObservableProperty]
        internal partial double SeverityFlyoutHeightValue { get; set; } = double.NaN;

        [ObservableProperty]
        internal partial bool IsBackdropEnabled { get; set; } = true;

        [ObservableProperty]
        internal partial int SelectedSystemBackdropIndex { get; set; } = 1;

        [ObservableProperty]
        internal partial int SelectedFlyoutExampleIndex { get; set; }

        [ObservableProperty]
        internal partial string SelectedFlyoutDescription { get; set; } = string.Empty;

        [ObservableProperty]
        internal partial Visibility ButtonSettingsVisibility { get; set; } = Visibility.Visible;

        [ObservableProperty]
        internal partial Visibility IndicatorSettingsVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        internal partial Visibility DraggableSettingsVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        internal partial Visibility SeveritySettingsVisibility { get; set; } = Visibility.Collapsed;

        [ObservableProperty]
        internal partial Visibility BackdropSettingsVisibility { get; set; } = Visibility.Collapsed;

        public Dictionary<DesktopFlyoutPopupDirection, string> PopupDirections { get; private set; } = [];
        public Dictionary<DesktopFlyoutPlacementMode, string> FlyoutPlacements { get; private set; } = [];
        public Dictionary<DesktopFlyoutActivationMode, string> ActivationModes { get; private set; } = [];
        public Dictionary<DesktopFlyoutIslandLayoutMode, string> IslandLayoutModes { get; private set; } = [];
        public Dictionary<DesktopFlyoutDragMode, string> DragModes { get; private set; } = [];
        public Dictionary<DesktopFlyoutSampleKind, string> FlyoutExamples { get; private set; } = [];
        public Dictionary<DesktopFlyoutBackdropKind, string> SystemBackdrops { get; private set; } = [];

        public ICommand ToggleFlyoutOpenCommand { get; }

        internal RootViewModel()
        {
            IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
            TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

            ActivationModes.Add(DesktopFlyoutActivationMode.Activate, "Activate");
            ActivationModes.Add(DesktopFlyoutActivationMode.NoActivateOnOpen, "No activate on open");
            ActivationModes.Add(DesktopFlyoutActivationMode.NeverActivate, "Never activate");

            FlyoutExamples.Add(DesktopFlyoutSampleKind.Button, "Button");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.IndicatorStyle, "Indicator");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.Draggable, "Draggable");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.NotificationCenterStyle, "Notification Center");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.StartMenuStyle, "Start Menu");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.StickySmallStyle, "Sticky Small");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.WidgetStyle, "Widget");
            FlyoutExamples.Add(DesktopFlyoutSampleKind.Severity, "Severity");

            PopupDirections.Add(DesktopFlyoutPopupDirection.Vertical, "Vertical");
            PopupDirections.Add(DesktopFlyoutPopupDirection.BottomToTop, "Bottom to top");
            PopupDirections.Add(DesktopFlyoutPopupDirection.TopToBottom, "Top to bottom");
            PopupDirections.Add(DesktopFlyoutPopupDirection.Horizontal, "Horizontal");
            PopupDirections.Add(DesktopFlyoutPopupDirection.LeftToRight, "Left to right");
            PopupDirections.Add(DesktopFlyoutPopupDirection.RightToLeft, "Right to left");

            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopLeft, "Top left");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopCenter, "Top center");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopRight, "Top right");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomLeft, "Bottom left");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomCenter, "Bottom center");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomRight, "Bottom right");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.LeftCenter, "Left center");
            FlyoutPlacements.Add(DesktopFlyoutPlacementMode.RightCenter, "Right center");

            SystemBackdrops.Add(DesktopFlyoutBackdropKind.Mica, "Mica");
            SystemBackdrops.Add(DesktopFlyoutBackdropKind.DesktopAcrylic, "Desktop Acrylic");

            IslandLayoutModes.Add(DesktopFlyoutIslandLayoutMode.Stack, "Stack");
            IslandLayoutModes.Add(DesktopFlyoutIslandLayoutMode.Freeform, "Freeform");

            DragModes.Add(DesktopFlyoutDragMode.None, "None");
            DragModes.Add(DesktopFlyoutDragMode.Full, "Full");
            DragModes.Add(DesktopFlyoutDragMode.Region, "Region");

            ToggleFlyoutOpenCommand = new RelayCommand(ExecuteToggleFlyoutOpenCommand);

            UpdateSettingsVisibility(DesktopFlyoutSampleKind.Button);
            UpdateSelectedFlyoutDescription(DesktopFlyoutSampleKind.Button);
            ApplySelectedFlyoutSettings();
        }

        partial void OnIconPathChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.SetIcon(value ?? string.Empty);
        }

        partial void OnTooltipTextChanged(string? value)
        {
            TrayIconManager.Default.SystemTrayIcon?.Tooltip = value ?? string.Empty;
        }

        partial void OnSelectedPopupDirectionIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout && TryGetValue(PopupDirections, value, out var popupDirection))
                flyout.PopupDirection = popupDirection;
        }

        partial void OnSelectedButtonPopupPositionIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                ApplyPlacement(flyout, value);
        }

        partial void OnButtonFlyoutWidthValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                flyout.FlyoutWidth = ToGridLength(value);
        }

        partial void OnButtonFlyoutHeightValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                flyout.FlyoutHeight = ToGridLength(value);
        }

        partial void OnIsSwipeToDismissEnabledChanged(bool value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                flyout.IsSwipeToDismissEnabled = value;
        }

        partial void OnSwipeDismissThresholdValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                flyout.SwipeDismissThreshold = value;
        }

        partial void OnPressedScaleValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is ButtonFlyout flyout)
                flyout.PressedScale = value;
        }

        partial void OnSelectedActivationModeIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout && TryGetValue(ActivationModes, value, out var activationMode))
                flyout.ActivationMode = activationMode;
        }

        partial void OnSelectedIndicatorPopupPositionIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout)
                ApplyPlacement(flyout, value);
        }

        partial void OnIndicatorFlyoutWidthValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout)
                flyout.FlyoutWidth = ToGridLength(value);
        }

        partial void OnIndicatorFlyoutHeightValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout)
                flyout.FlyoutHeight = ToGridLength(value);
        }

        partial void OnAutoCloseDelaySecondsValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout)
                flyout.AutoCloseDelay = ToAutoCloseDelay(value);
        }

        partial void OnHideOnLostFocusChanged(bool value)
        {
            if (TrayIconManager.Default.DesktopFlyout is IndicatorStyleFlyout flyout)
                flyout.HideOnLostFocus = value;
        }

        partial void OnSelectedDraggableIslandLayoutModeIndexChanged(int value)
        {
            if (!TryGetValue(IslandLayoutModes, value, out var layoutMode))
                return;

            IsDraggableFlyoutSizeEnabled = layoutMode is DesktopFlyoutIslandLayoutMode.Stack;
            if (TrayIconManager.Default.DesktopFlyout is DraggableFlyout flyout)
            {
                flyout.IslandLayoutMode = layoutMode;
                ApplyDraggableFlyoutSize(flyout);
            }
        }

        partial void OnSelectedDraggableDragModeIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is DraggableFlyout flyout && TryGetValue(DragModes, value, out var dragMode))
                flyout.DragMode = dragMode;
        }

        partial void OnSelectedDraggablePopupPositionIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is DraggableFlyout flyout)
                ApplyPlacement(flyout, value);
        }

        partial void OnDraggableFlyoutWidthValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is DraggableFlyout flyout)
                ApplyDraggableFlyoutSize(flyout);
        }

        partial void OnDraggableFlyoutHeightValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is DraggableFlyout flyout)
                ApplyDraggableFlyoutSize(flyout);
        }

        partial void OnSelectedSeverityPopupPositionIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is SeverityFlyout flyout)
                ApplyPlacement(flyout, value);
        }

        partial void OnSeverityFlyoutWidthValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is SeverityFlyout flyout)
                flyout.FlyoutWidth = ToGridLength(value);
        }

        partial void OnSeverityFlyoutHeightValueChanged(double value)
        {
            if (TrayIconManager.Default.DesktopFlyout is SeverityFlyout flyout)
                flyout.FlyoutHeight = ToGridLength(value);
        }

        partial void OnIsBackdropEnabledChanged(bool value)
        {
            if (TrayIconManager.Default.DesktopFlyout is WidgetStyleFlyout flyout)
                flyout.IsBackdropEnabled = value;
        }

        partial void OnSelectedSystemBackdropIndexChanged(int value)
        {
            if (TrayIconManager.Default.DesktopFlyout is WidgetStyleFlyout flyout && TryGetValue(SystemBackdrops, value, out var backdropKind))
                flyout.BackdropKind = backdropKind;
        }

        partial void OnSelectedFlyoutExampleIndexChanged(int value)
        {
            if (!TryGetValue(FlyoutExamples, value, out var flyoutKind))
                return;

            TrayIconManager.Default.SwitchFlyout(flyoutKind);
            UpdateSettingsVisibility(flyoutKind);
            UpdateSelectedFlyoutDescription(flyoutKind);
            ApplySelectedFlyoutSettings();
        }

        private void ApplySelectedFlyoutSettings()
        {
            switch (TrayIconManager.Default.DesktopFlyout)
            {
                case ButtonFlyout flyout:
                    ApplyButtonFlyoutSettings(flyout);
                    break;
                case IndicatorStyleFlyout flyout:
                    ApplyIndicatorFlyoutSettings(flyout);
                    break;
                case DraggableFlyout flyout:
                    ApplyDraggableFlyoutSettings(flyout);
                    break;
                case SeverityFlyout flyout:
                    ApplySeverityFlyoutSettings(flyout);
                    break;
                case WidgetStyleFlyout flyout:
                    ApplyWidgetFlyoutSettings(flyout);
                    break;
            }
        }

        private void ApplyButtonFlyoutSettings(ButtonFlyout flyout)
        {
            if (TryGetValue(PopupDirections, SelectedPopupDirectionIndex, out var popupDirection))
                flyout.PopupDirection = popupDirection;

            ApplyPlacement(flyout, SelectedButtonPopupPositionIndex);
            ApplyFlyoutSize(flyout, ButtonFlyoutWidthValue, ButtonFlyoutHeightValue);
            flyout.IsSwipeToDismissEnabled = IsSwipeToDismissEnabled;
            flyout.SwipeDismissThreshold = SwipeDismissThresholdValue;
            flyout.PressedScale = PressedScaleValue;
        }

        private void ApplyIndicatorFlyoutSettings(IndicatorStyleFlyout flyout)
        {
            if (TryGetValue(ActivationModes, SelectedActivationModeIndex, out var activationMode))
                flyout.ActivationMode = activationMode;

            ApplyPlacement(flyout, SelectedIndicatorPopupPositionIndex);
            ApplyFlyoutSize(flyout, IndicatorFlyoutWidthValue, IndicatorFlyoutHeightValue);
            flyout.AutoCloseDelay = ToAutoCloseDelay(AutoCloseDelaySecondsValue);
            flyout.HideOnLostFocus = HideOnLostFocus;
        }

        private void ApplyDraggableFlyoutSettings(DraggableFlyout flyout)
        {
            ApplyPlacement(flyout, SelectedDraggablePopupPositionIndex);
            if (TryGetValue(IslandLayoutModes, SelectedDraggableIslandLayoutModeIndex, out var layoutMode))
            {
                IsDraggableFlyoutSizeEnabled = layoutMode is DesktopFlyoutIslandLayoutMode.Stack;
                flyout.IslandLayoutMode = layoutMode;
            }

            ApplyDraggableFlyoutSize(flyout);
            if (TryGetValue(DragModes, SelectedDraggableDragModeIndex, out var dragMode))
                flyout.DragMode = dragMode;
        }

        private void ApplyDraggableFlyoutSize(DraggableFlyout flyout)
        {
            if (flyout.IslandLayoutMode is DesktopFlyoutIslandLayoutMode.Freeform)
                return;

            ApplyFlyoutSize(flyout, DraggableFlyoutWidthValue, DraggableFlyoutHeightValue);
        }

        private void ApplySeverityFlyoutSettings(SeverityFlyout flyout)
        {
            ApplyPlacement(flyout, SelectedSeverityPopupPositionIndex);
            ApplyFlyoutSize(flyout, SeverityFlyoutWidthValue, SeverityFlyoutHeightValue);
        }

        private void ApplyWidgetFlyoutSettings(WidgetStyleFlyout flyout)
        {
            if (TryGetValue(SystemBackdrops, SelectedSystemBackdropIndex, out var backdropKind))
                flyout.BackdropKind = backdropKind;

            flyout.IsBackdropEnabled = IsBackdropEnabled;
        }

        private void ApplyPlacement(DesktopFlyout flyout, int placementIndex)
        {
            if (TryGetValue(FlyoutPlacements, placementIndex, out var placement))
                flyout.Placement = placement;
        }

        private static void ApplyFlyoutSize(DesktopFlyout flyout, double width, double height)
        {
            flyout.FlyoutWidth = ToGridLength(width);
            flyout.FlyoutHeight = ToGridLength(height);
        }

        private void UpdateSettingsVisibility(DesktopFlyoutSampleKind flyoutKind)
        {
            ButtonSettingsVisibility = flyoutKind is DesktopFlyoutSampleKind.Button
                ? Visibility.Visible
                : Visibility.Collapsed;
            IndicatorSettingsVisibility = flyoutKind is DesktopFlyoutSampleKind.IndicatorStyle
                ? Visibility.Visible
                : Visibility.Collapsed;
            DraggableSettingsVisibility = flyoutKind is DesktopFlyoutSampleKind.Draggable
                ? Visibility.Visible
                : Visibility.Collapsed;
            SeveritySettingsVisibility = flyoutKind is DesktopFlyoutSampleKind.Severity
                ? Visibility.Visible
                : Visibility.Collapsed;
            BackdropSettingsVisibility = flyoutKind is DesktopFlyoutSampleKind.WidgetStyle
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void UpdateSelectedFlyoutDescription(DesktopFlyoutSampleKind flyoutKind)
        {
            SelectedFlyoutDescription = flyoutKind switch
            {
                DesktopFlyoutSampleKind.Button => "Demonstrates swipe-to-dismiss, swipe threshold, pressed scale, popup direction, position, and size.",
                DesktopFlyoutSampleKind.IndicatorStyle => "Demonstrates activation behavior, hide-on-focus-loss behavior, auto close, position, and size.",
                DesktopFlyoutSampleKind.Draggable => "Demonstrates native window dragging with full-surface and region modes.",
                DesktopFlyoutSampleKind.NotificationCenterStyle => "Shows a full-height notification-center style layout.",
                DesktopFlyoutSampleKind.StartMenuStyle => "Shows a multi-island Start menu style layout.",
                DesktopFlyoutSampleKind.StickySmallStyle => "Shows a compact flyout opened near the tray icon point.",
                DesktopFlyoutSampleKind.WidgetStyle => "Demonstrates island backdrop enablement and backdrop material selection.",
                DesktopFlyoutSampleKind.Severity => "Shows semantic island styling with configurable position and size.",
                _ => string.Empty,
            };
        }

        private static bool TryGetValue<TKey>(Dictionary<TKey, string> items, int index, out TKey key)
            where TKey : notnull
        {
            if (index < 0 || index >= items.Count)
            {
                key = default!;
                return false;
            }

            key = items.ElementAt(index).Key;
            return true;
        }

        private static GridLength ToGridLength(double value)
        {
            return double.IsNaN(value) || value <= 0
                ? GridLength.Auto
                : new(value);
        }

        private static TimeSpan ToAutoCloseDelay(double value)
        {
            return double.IsNaN(value) || value <= 0
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(value);
        }

        private void ExecuteToggleFlyoutOpenCommand()
        {
            TrayIconManager.Default.ToggleFlyout();
        }
    }
}
