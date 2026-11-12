# Getting started

Install the package that matches your XAML stack.

```console
dotnet add package 0x5BFA.DesktopFlyouts.WinUI --prerelease
```

```console
dotnet add package 0x5BFA.DesktopFlyouts.Uwp --prerelease
```

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
    HideOnLostFocus="False">

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
        _flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
}
```

Call `Dispose` when the flyout is no longer needed.

```csharp
_flyout.Dispose();
```

## Show from a screen anchor

`ForAnchor` accepts a physical-pixel screen rectangle and requires the side where the flyout should
appear. This works with tray icons, buttons, and other UI elements when their screen bounds are
available.

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (sender is SystemTrayIcon trayIcon && trayIcon.TryGetBounds(out var bounds))
    {
        _flyout.Show(DesktopFlyoutPosition.ForTaskbarAnchor(bounds));
    }
    else
    {
        var pointBounds = new System.Drawing.Rectangle((int)e.Point.X, (int)e.Point.Y, 0, 0);
        _flyout.Show(DesktopFlyoutPosition.ForAnchor(pointBounds, DesktopFlyoutAnchorSide.Above));
    }
}
```

`ForTaskbarAnchor` detects the taskbar edge when supported and opens toward the work area. If the
platform cannot report taskbar bounds, it estimates the side from the nearest monitor edge. To anchor
to a normal UI element, pass its physical screen bounds to `ForAnchor` with an explicit side.

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
