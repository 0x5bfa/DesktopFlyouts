namespace DesktopFlyouts_WinUI_IntegrationTests;

internal static class RuntimeScenarioRunner
{
    public static async Task RunAsync(IntegrationTestWindow window, nint ownerWindow)
    {
        Environment.ExitCode = 1;
        try
        {
            await DesktopFlyoutsIntegration.RuntimeScenario.RunBasicChecks(ownerWindow);
            Environment.ExitCode = 0;
            window.SetStatus("DesktopFlyouts WinUI runtime checks passed.");
            Console.WriteLine("WinUI runtime integration checks passed.");
        }
        catch (Exception exception)
        {
            window.SetStatus(exception.Message);
            Console.Error.WriteLine(exception);
        }
        finally
        {
            await Task.Delay(150);
            window.Close();
        }
    }
}
