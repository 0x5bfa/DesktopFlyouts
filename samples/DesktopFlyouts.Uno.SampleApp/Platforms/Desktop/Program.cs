using Uno.UI.Hosting;

namespace DesktopFlyouts;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .Build();

        host.Run();
    }
}
