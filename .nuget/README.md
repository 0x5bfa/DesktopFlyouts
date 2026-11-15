DesktopFlyouts is a WinUI library for showing desktop flyouts from tray icons or programmatically.

## Usage

### DesktopFlyout

```xml
<me:DesktopFlyout
    x:Class="..."
    xmlns:me="using:DesktopFlyouts"
    FlyoutWidth="360">

    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>
    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>

</me:DesktopFlyout>
```

```cs
if (_desktopFlyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
    _desktopFlyout.Hide();
else
    _desktopFlyout.Show();
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

- Getting started: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/getting-started.md
- DesktopFlyout: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/desktop-flyout.md
- DesktopMenuFlyout: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/desktop-menu-flyout.md
- SystemTrayIcon: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/system-tray-icon.md
- Focus and activation: https://github.com/0x5bfa/DesktopFlyouts/blob/main/docs/focus-and-activation.md
- Changelog: https://github.com/0x5bfa/DesktopFlyouts/blob/main/CHANGE_LOG.md

## License

Copyright (c) 0x5BFA. All rights reserved.
