using Microsoft.UI.Xaml;

namespace DesktopFlyouts_WinUI_IntegrationTests;

public sealed partial class IntegrationTestWindow : Window
{
    public IntegrationTestWindow()
    {
        InitializeComponent();
    }

    public void SetStatus(string text) => StatusText.Text = text;
}
