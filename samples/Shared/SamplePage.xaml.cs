#if UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif
using System.Globalization;

namespace DesktopFlyoutsSample;

public sealed partial class SamplePage : Page, IDisposable
{
    private FlyoutSample? _sample;

    public SamplePage()
    {
        InitializeComponent();
    }

    public void Initialize(long ownerWindow, Action close)
    {
        _sample = new FlyoutSample(ownerWindow, ReadSettings, text => StatusText.Text = text, close);
    }

    private FlyoutSettings ReadSettings() => new(
        Math.Clamp(FlyoutExampleComboBox.SelectedIndex, 0, 7),
        Math.Clamp(FlyoutPlacementComboBox.SelectedIndex, 0, 7),
        Math.Clamp(PopupDirectionComboBox.SelectedIndex, 0, 5),
        Math.Clamp(ActivationModeComboBox.SelectedIndex, 0, 2),
        Math.Clamp(BackdropKindComboBox.SelectedIndex, 0, 1),
        ReadNumber(FlyoutWidthNumberBox, 360, 0, 1400),
        ReadNumber(FlyoutHeightNumberBox, 0, 0, 1000),
        ReadNumber(SwipeDismissThresholdNumberBox, 80, 1, 2000),
        ReadNumber(AutoCloseDelayNumberBox, 0, 0, 60),
        BackdropCheckBox.IsChecked == true, HideOnLostFocusCheckBox.IsChecked == true,
        SwipeCheckBox.IsChecked == true, TransitionCheckBox.IsChecked == true);

#if UWP
    private static double ReadNumber(TextBox control, double fallback, double min, double max)
    {
        return double.TryParse(control.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
            && double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    }

    private void FlyoutSize_ValueChanged(object sender, TextChangedEventArgs args) => _sample?.ApplySettings();

    public bool TryPreTranslateMessage(nint message) => _sample?.TryPreTranslateMessage(message) == true;
#else
    private static double ReadNumber(NumberBox control, double fallback, double min, double max)
        => double.IsFinite(control.Value) ? Math.Clamp(control.Value, min, max) : fallback;

    private void FlyoutSize_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => _sample?.ApplySettings();
#endif

    private void ShowFlyout_Click(object sender, RoutedEventArgs args) => _sample?.Show();
    private void HideFlyout_Click(object sender, RoutedEventArgs args) => _sample?.Hide();
    private void ShowIslands_Click(object sender, RoutedEventArgs args) => _sample?.ShowIslands();
    private void ShowMenu_Click(object sender, RoutedEventArgs args) => _sample?.ShowMenu();
    private void ShowTray_Click(object sender, RoutedEventArgs args) => _sample?.ToggleTray();
    private void NavigateFocus_Click(object sender, RoutedEventArgs args) => _sample?.NavigateFocus();
    private void Settings_Click(object sender, RoutedEventArgs args) => _sample?.ApplySettings();
    private void Settings_SelectionChanged(object sender, SelectionChangedEventArgs args) => _sample?.ApplySettings();

    private void ShowAutoClose_Click(object sender, RoutedEventArgs args)
    {
#if UWP
        AutoCloseDelayNumberBox.Text = 0.7.ToString(CultureInfo.CurrentCulture);
#else
        AutoCloseDelayNumberBox.Value = 0.7;
#endif
        TransitionCheckBox.IsChecked = false;
        _sample?.Show();
        StatusText.Text = "Flyout auto-closes in 700 ms";
    }

    public void Dispose()
    {
        _sample?.Dispose();
        _sample = null;
    }
}
