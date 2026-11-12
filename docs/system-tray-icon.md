# SystemTrayIcon

`SystemTrayIcon` is an optional helper for creating a Win32 notification icon and receiving click events.

The flyout controls do not require a tray icon. Use this type only when your app needs tray integration.

Construction prepares the hidden callback window and icon resources, but does not add the icon to the shell. Call `Show()` to create the tray icon.

## Basic usage

```csharp
var trayIcon = new SystemTrayIcon(
    iconPath: "Assets/AppIcon.ico",
    tooltip: "My app",
    id: new Guid("00000000-0000-0000-0000-000000000000"));

trayIcon.LeftClicked += TrayIcon_LeftClicked;
trayIcon.RightClicked += TrayIcon_RightClicked;
trayIcon.Show();
```

## Construction

`SystemTrayIcon` currently provides two constructors:

```csharp
var trayIconFromPath = new SystemTrayIcon(
    iconPath: "Assets/AppIcon.ico",
    tooltip: "My app",
    id: new Guid("00000000-0000-0000-0000-000000000000"));

var trayIconFromHandle = new SystemTrayIcon(
    hIcon: existingIconHandle,
    tooltip: "My app",
    id: new Guid("00000000-0000-0000-0000-000000000000"));
```

When the icon is created from a handle, the handle is copied internally.

## Icon bounds and click points

`LeftClicked` and `RightClicked` provide the center point of the tray icon in physical screen pixels.
Use `TryGetBounds` when the full icon rectangle is needed by `ForTaskbarAnchor`:

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (sender is SystemTrayIcon trayIcon && trayIcon.TryGetBounds(out var bounds))
    {
        flyout.Show(DesktopFlyoutPosition.ForTaskbarAnchor(bounds));
    }
    else
    {
        var pointBounds = new System.Drawing.Rectangle((int)e.Point.X, (int)e.Point.Y, 0, 0);
        flyout.Show(DesktopFlyoutPosition.ForAnchor(pointBounds, DesktopFlyoutAnchorSide.Above));
    }
}

private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
{
    menu.Show(e.Point);
}
```

`TryGetBounds` returns `false` and an empty rectangle when the icon is unavailable or the platform
cannot provide its bounds. The X11 StatusNotifierItem implementation does not expose icon geometry;
its click event can still provide a screen point for an explicitly sided `ForAnchor` request.

## Visibility and updates

`IsVisible` defaults to `false`.

Calling `Show()` sets `IsVisible = true` and creates the tray icon if needed.

After the tray icon has been created in the shell, changing `Tooltip` or `IsVisible` updates it immediately. If the tray icon has not been created yet, those values are only recorded and will be applied the next time `Show()` is called.

```csharp
trayIcon.Tooltip = "New tooltip";
trayIcon.IsVisible = false;
trayIcon.IsVisible = true;
```

`Destroy()` removes the tray icon from the shell and sets `IsVisible` back to `false`, but it does not dispose the `SystemTrayIcon` object.

## Changing the icon

`IconPath` is read-only. To replace the icon, use `SetIcon(string)` or `SetIcon(nint)`.

```csharp
trayIcon.SetIcon("Assets/AlternateIcon.ico");
trayIcon.SetIcon(existingIconHandle);
```

When `SetIcon(string)` is used, the path must point to an icon file that can be loaded by the Win32 `LoadImage` API. The method throws `ArgumentOutOfRangeException` when the icon cannot be loaded.

## Cleanup

Call `Destroy()` when the tray icon should be removed from the shell but the `SystemTrayIcon` instance will still be kept.

```csharp
trayIcon.Destroy();
```

Call `Dispose()` explicitly when the instance is no longer needed.

```csharp
trayIcon.Dispose();
```

`Dispose()` removes the tray icon if it is still present, destroys the hidden callback window, and releases the owned icon handle.

Unsubscribe event handlers when the owning object is disposed.
