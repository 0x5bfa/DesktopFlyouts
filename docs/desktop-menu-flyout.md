# DesktopMenuFlyout

`DesktopMenuFlyout` displays a XAML `MenuFlyout` in an independent desktop XAML island window.

Use it for tray-icon context menus or other menus that need to open at physical screen coordinates.

## Basic usage

```xml
<desktop:DesktopMenuFlyout
    x:Class="MyApp.MainDesktopMenuFlyout">

    <MenuFlyoutItem Text="Settings" />
    <MenuFlyoutSeparator />
    <MenuFlyoutItem Text="Exit" />

</desktop:DesktopMenuFlyout>
```

```csharp
var menu = new MainDesktopMenuFlyout();
menu.Show(new System.Drawing.Point(x, y));
```

## Items

Add `MenuFlyoutItemBase` items as children:

- `MenuFlyoutItem`
- `MenuFlyoutSubItem`
- `MenuFlyoutSeparator`
- custom item types derived from `MenuFlyoutItemBase`

The control rebuilds the hosted menu from `Items` when the item collection changes.

## Coordinates

`Show(System.Drawing.Point)` expects a point in physical screen pixels. This matches the points raised by `SystemTrayIcon.LeftClicked` and `SystemTrayIcon.RightClicked`.

```csharp
private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (menu.IsOpen)
        menu.Hide();

    menu.Show(e.Point);
}
```

Adjust the point before calling `Show` if you want an offset.

```csharp
menu.Show(new System.Drawing.Point(e.Point.X, e.Point.Y - 32));
```

## State and lifetime

`IsOpen` is `true` while the hosted menu is open. It becomes `false` when the menu closes itself or when `Hide()` is called.

`DesktopMenuFlyout` owns a desktop host window. Create it once, reuse it, and call `Dispose` when it is no longer needed.
