DesktopFlyouts is a WinUI library for showing desktop flyouts from tray icons or programmatically.

The packages target .NET 10. Newer .NET applications can use these assets when their platform
dependencies support that target. The Uno package is built against Uno Platform 6.6. Uno
applications targeting .NET 11 require the package to be rebuilt against Uno Platform 7.0.

The WinUI and UWP packages include Windows x86, x64, and ARM64 implementations. Match the
application's platform and runtime identifier; NuGet selects the appropriate implementation.
The Uno package contains architecture-neutral managed code for Linux / X11 hosts with supported
Uno/native dependencies.

## Usage

### DesktopFlyout

```xml
<me:DesktopFlyout
    x:Class="MyApp.MainDesktopFlyout"
    xmlns:me="using:DesktopFlyouts"
    Width="360">

    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>
    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>

</me:DesktopFlyout>
```

Create and retain the flyout on the UI thread, and dispose it when no longer needed:

```cs
private readonly MainDesktopFlyout _desktopFlyout = new();

if (_desktopFlyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
    _desktopFlyout.Hide();
else
    _desktopFlyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
```

### DesktopMenuFlyout

```xml
<me:DesktopMenuFlyout
    x:Class="..."
    xmlns:me="using:DesktopFlyouts">

    <MenuFlyoutSubItem Text="Settings">
        <MenuFlyoutSubItem.Icon>
            <FontIcon Glyph="..." />
        </MenuFlyoutSubItem.Icon>
        <MenuFlyoutSubItem.Items>
            <MenuFlyoutItem Text="Theme" />
            <MenuFlyoutItem Text="Language" />
            <MenuFlyoutItem Text="Privacy" />
        </MenuFlyoutSubItem.Items>
    </MenuFlyoutSubItem>
    <MenuFlyoutSeparator />
    <MenuFlyoutItem Text="Exit">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="..." />
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>

</me:DesktopMenuFlyout>
```

```cs
if (_desktopMenuFlyout.IsOpen)
    _desktopMenuFlyout.Hide();

_desktopMenuFlyout.Show(e.Point);
```

## Documentation

- Usage guide: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/README.md
- API reference: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/api-reference.md
- Changelog: https://github.com/0x5bfa/DesktopFlyouts/blob/main/CHANGE_LOG.md

## License

Copyright (c) 0x5BFA. All rights reserved.
