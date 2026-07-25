using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DesktopFlyouts;

internal partial class RootViewModel : ObservableObject
{
    private bool _isModifiable = true;
    private string? _iconPath;
    private string? _tooltipText;
    private bool _isBackdropEnabled;
    private bool _hideOnLostFocus;
    private bool _isSwipeToDismissEnabled;
    private double _swipeDismissThresholdValue = 80D;
    private int _selectedActivationModeIndex;
    private double _autoCloseDelaySecondsValue;
    private int _selectedPopupDirectionIndex;
    private int _selectedFlyoutPlacementIndex;
    private int _selectedFlyoutExampleIndex;
    private double _flyoutWidthValue = 360D;
    private double _flyoutHeightValue = double.NaN;
    private int _selectedSystemBackdropIndex;

    internal bool IsModifiable
    {
        get => _isModifiable;
        set => SetProperty(ref _isModifiable, value);
    }

    internal string? IconPath
    {
        get => _iconPath;
        set
        {
            if (SetProperty(ref _iconPath, value))
                OnIconPathChanged(value);
        }
    }

    internal string? TooltipText
    {
        get => _tooltipText;
        set
        {
            if (SetProperty(ref _tooltipText, value))
                OnTooltipTextChanged(value);
        }
    }

    internal bool IsBackdropEnabled
    {
        get => _isBackdropEnabled;
        set
        {
            if (SetProperty(ref _isBackdropEnabled, value))
                OnIsBackdropEnabledChanged(value);
        }
    }

    internal bool HideOnLostFocus
    {
        get => _hideOnLostFocus;
        set
        {
            if (SetProperty(ref _hideOnLostFocus, value))
                OnHideOnLostFocusChanged(value);
        }
    }

    internal bool IsSwipeToDismissEnabled
    {
        get => _isSwipeToDismissEnabled;
        set
        {
            if (SetProperty(ref _isSwipeToDismissEnabled, value))
                OnIsSwipeToDismissEnabledChanged(value);
        }
    }

    internal double SwipeDismissThresholdValue
    {
        get => _swipeDismissThresholdValue;
        set
        {
            if (SetProperty(ref _swipeDismissThresholdValue, value))
                OnSwipeDismissThresholdValueChanged(value);
        }
    }

    internal int SelectedActivationModeIndex
    {
        get => _selectedActivationModeIndex;
        set
        {
            if (SetProperty(ref _selectedActivationModeIndex, value))
                OnSelectedActivationModeIndexChanged(value);
        }
    }

    internal double AutoCloseDelaySecondsValue
    {
        get => _autoCloseDelaySecondsValue;
        set
        {
            if (SetProperty(ref _autoCloseDelaySecondsValue, value))
                OnAutoCloseDelaySecondsValueChanged(value);
        }
    }

    internal int SelectedPopupDirectionIndex
    {
        get => _selectedPopupDirectionIndex;
        set
        {
            if (SetProperty(ref _selectedPopupDirectionIndex, value))
                OnSelectedPopupDirectionIndexChanged(value);
        }
    }

    internal int SelectedFlyoutPlacementIndex
    {
        get => _selectedFlyoutPlacementIndex;
        set
        {
            if (SetProperty(ref _selectedFlyoutPlacementIndex, value))
                OnSelectedFlyoutPlacementIndexChanged(value);
        }
    }

    internal int SelectedFlyoutExampleIndex
    {
        get => _selectedFlyoutExampleIndex;
        set
        {
            if (SetProperty(ref _selectedFlyoutExampleIndex, value))
                OnSelectedFlyoutExampleIndexChanged(value);
        }
    }

    internal double FlyoutWidthValue
    {
        get => _flyoutWidthValue;
        set
        {
            if (SetProperty(ref _flyoutWidthValue, value))
                OnFlyoutWidthValueChanged(value);
        }
    }

    internal double FlyoutHeightValue
    {
        get => _flyoutHeightValue;
        set
        {
            if (SetProperty(ref _flyoutHeightValue, value))
                OnFlyoutHeightValueChanged(value);
        }
    }

    internal int SelectedSystemBackdropIndex
    {
        get => _selectedSystemBackdropIndex;
        set
        {
            if (SetProperty(ref _selectedSystemBackdropIndex, value))
                OnSelectedSystemBackdropIndexChanged(value);
        }
    }

    public Dictionary<DesktopFlyoutPopupDirection, string> PopupDirections { get; private set; } = [];
    public Dictionary<DesktopFlyoutPlacementMode, string> FlyoutPlacements { get; private set; } = [];
    public Dictionary<DesktopFlyoutActivationMode, string> ActivationModes { get; private set; } = [];
    public Dictionary<DesktopFlyoutSampleKind, string> FlyoutExamples { get; private set; } = [];
    public Dictionary<DesktopFlyoutBackdropKind, string> SystemBackdrops { get; private set; } = [];

    public ICommand ToggleFlyoutOpenCommand { get; }

    internal RootViewModel()
    {
        IconPath = TrayIconManager.Default.SystemTrayIcon?.IconPath;
        TooltipText = TrayIconManager.Default.SystemTrayIcon?.Tooltip;

        IsBackdropEnabled = true;
        HideOnLostFocus = true;

        ActivationModes.Add(DesktopFlyoutActivationMode.Activate, "Activate");
        ActivationModes.Add(DesktopFlyoutActivationMode.NoActivateOnOpen, "No activate on open");
        ActivationModes.Add(DesktopFlyoutActivationMode.NeverActivate, "Never activate");
        SelectedActivationModeIndex = 0;

        FlyoutExamples.Add(DesktopFlyoutSampleKind.Customizable, "Default");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.Button, "Button");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.IndicatorStyle, "Indicator");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.NotificationCenterStyle, "Notification Center");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.StartMenuStyle, "Start Menu");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.StickySmallStyle, "Sticky small");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.WidgetStyle, "Widget");
        FlyoutExamples.Add(DesktopFlyoutSampleKind.Severity, "Severity");
        SelectedFlyoutExampleIndex = 0;

        PopupDirections.Add(DesktopFlyoutPopupDirection.Vertical, "Vertical");
        PopupDirections.Add(DesktopFlyoutPopupDirection.BottomToTop, "Bottom to top");
        PopupDirections.Add(DesktopFlyoutPopupDirection.TopToBottom, "Top to bottom");
        PopupDirections.Add(DesktopFlyoutPopupDirection.Horizontal, "Horizontal");
        PopupDirections.Add(DesktopFlyoutPopupDirection.LeftToRight, "Left to right");
        PopupDirections.Add(DesktopFlyoutPopupDirection.RightToLeft, "Right to left");
        SelectedPopupDirectionIndex = 0;

        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopLeft, "Top left");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopCenter, "Top center");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.TopRight, "Top right");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomLeft, "Bottom left");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomCenter, "Bottom center");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.BottomRight, "Bottom right");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.LeftCenter, "Left center");
        FlyoutPlacements.Add(DesktopFlyoutPlacementMode.RightCenter, "Right center");
        SelectedFlyoutPlacementIndex = 5;

        SystemBackdrops.Add(DesktopFlyoutBackdropKind.Mica, "Mica");
        SystemBackdrops.Add(DesktopFlyoutBackdropKind.DesktopAcrylic, "Desktop Acrylic");
        SelectedSystemBackdropIndex = 0;

        ToggleFlyoutOpenCommand = new RelayCommand(ExecuteToggleFlyoutOpenCommand);
    }

    private void OnIconPathChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && System.IO.File.Exists(value))
            TrayIconManager.Default.SystemTrayIcon?.SetIcon(value);
    }

    private void OnTooltipTextChanged(string? value)
    {
        TrayIconManager.Default.SystemTrayIcon?.Tooltip = value ?? string.Empty;
    }

    private void OnIsBackdropEnabledChanged(bool value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout?.IsBackdropEnabled = value;
    }

    private void OnHideOnLostFocusChanged(bool value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout?.HideOnLostFocus = value;
    }

    private void OnIsSwipeToDismissEnabledChanged(bool value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.IsSwipeToDismissEnabled = value;
    }

    private void OnSwipeDismissThresholdValueChanged(double value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.SwipeDismissThreshold = value;
    }

    private void OnAutoCloseDelaySecondsValueChanged(double value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.AutoCloseDelay = ToAutoCloseDelay(value);
    }

    private void OnSelectedActivationModeIndexChanged(int value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.ActivationMode = ActivationModes.ElementAt(value).Key;
    }

    private void OnSelectedPopupDirectionIndexChanged(int value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout?.PopupDirection = PopupDirections.ElementAt(value).Key;
    }

    private void OnSelectedFlyoutPlacementIndexChanged(int value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout?.Placement = FlyoutPlacements.ElementAt(value).Key;
    }

    private void OnSelectedFlyoutExampleIndexChanged(int value)
    {
        var flyoutKind = FlyoutExamples.ElementAt(value).Key;

        TrayIconManager.Default.SwitchFlyout(flyoutKind);
        if (flyoutKind is DesktopFlyoutSampleKind.Customizable)
        {
            IsModifiable = true;
            ApplyDefaultFlyoutSettings();
        }
        else
        {
            IsModifiable = false;
        }
    }

    private void OnSelectedSystemBackdropIndexChanged(int value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.BackdropKind = SystemBackdrops.ElementAt(value).Key;
    }

    private void OnFlyoutWidthValueChanged(double value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.FlyoutWidth = ToGridLength(value);
    }

    private void OnFlyoutHeightValueChanged(double value)
    {
        if (IsDefaultFlyoutSelected())
            TrayIconManager.Default.DesktopFlyout!.FlyoutHeight = ToGridLength(value);
    }

    private void ApplyDefaultFlyoutSettings()
    {
        if (!IsDefaultFlyoutSelected())
            return;

        var flyout = TrayIconManager.Default.DesktopFlyout!;
        flyout.BackdropKind = SystemBackdrops.ElementAt(SelectedSystemBackdropIndex).Key;
        flyout.FlyoutHeight = ToGridLength(FlyoutHeightValue);
        flyout.FlyoutWidth = ToGridLength(FlyoutWidthValue);
        flyout.AutoCloseDelay = ToAutoCloseDelay(AutoCloseDelaySecondsValue);
        flyout.ActivationMode = ActivationModes.ElementAt(SelectedActivationModeIndex).Key;
        flyout.HideOnLostFocus = HideOnLostFocus;
        flyout.IsSwipeToDismissEnabled = IsSwipeToDismissEnabled;
        flyout.SwipeDismissThreshold = SwipeDismissThresholdValue;
        flyout.IsBackdropEnabled = IsBackdropEnabled;
        flyout.Placement = FlyoutPlacements.ElementAt(SelectedFlyoutPlacementIndex).Key;
        flyout.PopupDirection = PopupDirections.ElementAt(SelectedPopupDirectionIndex).Key;
    }

    private static bool IsDefaultFlyoutSelected()
    {
        return TrayIconManager.Default.DesktopFlyout is CustomizableFlyout;
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
