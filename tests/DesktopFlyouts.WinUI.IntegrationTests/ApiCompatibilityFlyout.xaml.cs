using DesktopFlyouts;

namespace DesktopFlyouts.WinUI.IntegrationTests;

public sealed partial class ApiCompatibilityFlyout : DesktopFlyout
{
    public ApiCompatibilityFlyout()
    {
        InitializeComponent();
    }

    public string BoundLabel => BoundLabelText.Text;
}
