DesktopFlyouts is a WinUI library for showing desktop flyouts from tray icons or programmatically.

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
