# Getting started

Install the package that matches your XAML stack.

```console
dotnet add package 0x5BFA.DesktopFlyouts.WinUI --prerelease
```

```console
dotnet add package 0x5BFA.DesktopFlyouts.Uwp --prerelease
```

### Start a UWP desktop host

The UWP package uses XamlHostingKit to own System XAML initialization and the desktop message loop.
Disable the generated XAML entry point and start the application through `XamlIslandApplication`:

```xml
<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>
```

```csharp
using DesktopFlyouts.Shared;

[STAThread]
static void Main()
{
    XamlIslandApplication.Start(_ => new App());
}
```

Create the first `DesktopFlyout` or `DesktopMenuFlyout` on the main XAML thread. System XAML
allows one top-level window per thread, so create every additional flyout in its own callback:

```csharp
private DesktopFlyout? _flyout;
private DesktopMenuFlyout? _menu;

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    _flyout = new MainDesktopFlyout();

    XamlIslandApplication.CreateWindow(_ =>
    {
        _menu = new MainDesktopMenuFlyout();
    });
}
```

XamlHostingKit dispatches each callback on the matching XAML thread. Dispatch later menu or
flyout operations back to the instance's `Dispatcher` when the caller runs on another thread.

Add the DesktopFlyouts namespace to XAML.

```xml
xmlns:desktop="using:U5BFA.Libraries"
```

## Create a flyout

Define a `DesktopFlyout` with one or more `DesktopFlyoutIsland` sections.

```xml
<desktop:DesktopFlyout
    x:Class="MyApp.MainDesktopFlyout"
    FlyoutWidth="360"
    ActivationMode="NoActivateOnOpen"
    HideOnLostFocus="False"
    Placement="BottomRight"
    PopupDirection="Vertical">

    <desktop:DesktopFlyoutIsland IslandHeight="300">
        <Grid Padding="16">
            <TextBlock Text="Hello from a desktop flyout" />
        </Grid>
    </desktop:DesktopFlyoutIsland>

</desktop:DesktopFlyout>
```

Create the flyout once and keep it alive while the app needs it.

```csharp
private readonly MainDesktopFlyout _flyout = new();

private void ToggleFlyout()
{
    if (_flyout.IsOpen)
        _flyout.Hide();
    else
        _flyout.Show();
}
```

Call `Dispose` when the flyout is no longer needed.

```csharp
_flyout.Dispose();
```

## Show from a screen point

Use `Show(Point)` when you already have a physical screen coordinate, such as the center of a tray icon.

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    _flyout.Show(e.Point);
}
```

`Show(Point)` treats the point as the desired bottom-center point of the flyout for that open operation only. Later calls to `Show()` use the configured `Placement`.

## Add a tray menu

Use `DesktopMenuFlyout` for context-menu behavior.

```xml
<desktop:DesktopMenuFlyout x:Class="MyApp.MainDesktopMenuFlyout">
    <MenuFlyoutItem Text="Settings" />
    <MenuFlyoutSeparator />
    <MenuFlyoutItem Text="Exit" />
</desktop:DesktopMenuFlyout>
```

```csharp
private readonly MainDesktopMenuFlyout _menu = new();

private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (_menu.IsOpen)
        _menu.Hide();

    _menu.Show(e.Point);
}
```
