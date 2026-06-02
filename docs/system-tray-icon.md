# SystemTrayIcon

`SystemTrayIcon` is an optional helper for creating a Win32 notification icon and receiving click events.

The flyout controls do not require a tray icon. Use this type only when your app needs tray integration.

## Basic usage (file path)

```csharp
var trayIcon = new SystemTrayIcon(
    iconPath: "Assets/AppIcon.ico",
    tooltip: "My app",
    id: new Guid("00000000-0000-0000-0000-000000000000"));

trayIcon.LeftClicked += TrayIcon_LeftClicked;
trayIcon.RightClicked += TrayIcon_RightClicked;
trayIcon.Show();
```

## Basic usage (native icon handle)

Use the `nint` constructor overload to pass a caller-owned icon handle directly. This avoids shipping a separate `.ico` file when the icon is already embedded in the process executable.

```csharp
HINSTANCE hInstance = PInvoke.GetModuleHandle(null);
nint hIcon = PInvoke.LoadIcon(hInstance, MAKEINTRESOURCE(32512)); // IDI_APPLICATION

var trayIcon = new SystemTrayIcon(
    hIcon: hIcon,
    tooltip: "My app",
    id: new Guid("00000000-0000-0000-0000-000000000000"));

trayIcon.Show();
```

The caller must ensure the handle remains valid for the lifetime of the tray icon. `SystemTrayIcon` does not destroy borrowed handles.

## Click points

`LeftClicked` and `RightClicked` provide the center point of the tray icon in physical screen pixels.

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    flyout.Show(e.Point);
}

private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
{
    menu.Show(e.Point);
}
```

## Updating the icon

Setting `IconPath`, `Icon`, `Tooltip`, or `IsVisible` updates the existing shell icon immediately.

`IconPath` loads from file and `Icon` accepts a native handle directly. The two properties are mutually exclusive — setting one clears the other.

```csharp
// Switch to a file-based icon
trayIcon.IconPath = "Assets/NewIcon.ico";

// Switch to a native handle
trayIcon.Icon = hIcon;

trayIcon.Tooltip = "New tooltip";
trayIcon.IsVisible = false;
trayIcon.IsVisible = true;
```

`IconPath` must point to an icon file that can be loaded by the Win32 `LoadImage` API. `Icon` must be a valid icon handle verified by `GetIconInfo`. `Show()` throws `ArgumentOutOfRangeException` when the icon is invalid.

## Cleanup

Call `Destroy()` when the tray icon should be removed.

```csharp
trayIcon.Destroy();
```

Unsubscribe event handlers when the owning object is disposed.
