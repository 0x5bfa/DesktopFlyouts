# Getting started

Install the package that matches your XAML stack.

Windows App SDK / WinUI 3:

```console
dotnet add package DesktopFlyouts.WinUI
```

UWP / WinUI 2:

```console
dotnet add package DesktopFlyouts.Uwp
```

Uno Platform on Linux / X11:

```console
dotnet add package DesktopFlyouts.Uno
```

Add the DesktopFlyouts namespace to XAML.

```xml
xmlns:desktop="using:DesktopFlyouts"
```

Use `using DesktopFlyouts;` in C# files that reference the controls and position types.

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
    if (_flyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
        _flyout.Hide();
    else
        _flyout.Show();
}
```

`Show()` opens at the bottom-right of the primary monitor's work area. Use `TransitionState` when
toggling so a click during opening also requests a close. Create and operate the flyout on the UI
thread, and call `Dispose` on that thread when it is no longer needed.

```csharp
_flyout.Dispose();
```

## Show from a tray icon

Pass a `SystemTrayIcon` to `ForTrayIcon` to follow its current screen bounds. If icon bounds are
unavailable, use the click position with an explicit anchor side.

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (sender is SystemTrayIcon trayIcon && trayIcon.TryGetBounds(out _))
    {
        _flyout.Show(DesktopFlyoutPosition.ForTrayIcon(trayIcon));
    }
    else
    {
        var pointBounds = new System.Drawing.Rectangle((int)e.Point.X, (int)e.Point.Y, 0, 0);
        _flyout.Show(DesktopFlyoutPosition.ForAnchor(pointBounds, DesktopFlyoutAnchorSide.Above));
    }
}
```

`ForTrayIcon` follows the icon's current bounds, detects the taskbar edge, and opens toward the work area. If the
platform cannot report taskbar bounds, it estimates the side from the nearest monitor edge.

## Show from a screen anchor

`ForAnchor` accepts a `System.Drawing.Rectangle` in physical screen pixels and requires the side
where the flyout should appear. Use it for buttons or other UI elements whose screen bounds are
available.

```csharp
var anchor = new System.Drawing.Rectangle(x, y, width, height);
_flyout.Show(DesktopFlyoutPosition.ForAnchor(anchor, DesktopFlyoutAnchorSide.Above));
```

See [position requests](desktop-flyout.md#position-requests) for alignment, monitor selection, and
transition directions.

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
