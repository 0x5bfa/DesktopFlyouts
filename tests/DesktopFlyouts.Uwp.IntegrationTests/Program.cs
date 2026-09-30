using DesktopFlyoutsIntegration;
using System.Windows.Forms;

namespace DesktopFlyouts_Uwp_IntegrationTests;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new IntegrationTestForm());
        Environment.ExitCode = IntegrationTestForm.ExitCode;
    }
}

internal sealed class IntegrationTestForm : Form
{
    public static int ExitCode { get; private set; } = 1;

    public IntegrationTestForm()
    {
        Text = "DesktopFlyouts UWP integration tests";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 480;
        Height = 160;
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Text = "Running DesktopFlyouts UWP runtime checks…",
        });
        Shown += RunScenario;
    }

    private async void RunScenario(object? sender, EventArgs args)
    {
        try
        {
            await RuntimeScenario.RunBasicChecks(Handle);
            ExitCode = 0;
            Console.WriteLine("UWP runtime integration checks passed.");
        }
        catch (Exception exception)
        {
            ExitCode = 1;
            Console.Error.WriteLine(exception);
        }
        finally
        {
            Close();
        }
    }
}
