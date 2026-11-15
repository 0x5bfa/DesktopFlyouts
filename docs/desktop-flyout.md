# DesktopFlyout

`DesktopFlyout` displays a panel-style flyout in an independent desktop XAML island window.

Use it when you need a small desktop surface that can be shown from a tray icon or from normal application code.

## Basic usage

```xml
<desktop:DesktopFlyout
    x:Class="MyApp.MainDesktopFlyout"
    FlyoutWidth="360"
    ActivationMode="NoActivateOnOpen">

    <desktop:DesktopFlyoutIsland IslandHeight="300">
        <Grid Padding="16">
            <TextBlock Text="Content" />
        </Grid>
    </desktop:DesktopFlyoutIsland>

</desktop:DesktopFlyout>
```

```csharp
var flyout = new MainDesktopFlyout();
flyout.Show();
```

`Show()` opens at the bottom-right of the primary monitor's work area. Pass a
`DesktopFlyoutPosition` to `Show` to choose another position.

## Islands

The flyout content is the `Islands` collection. Each island is a `DesktopFlyoutIsland`.

```xml
<desktop:DesktopFlyout IslandsOrientation="Horizontal">
    <desktop:DesktopFlyoutIsland IslandWidth="320" />
    <desktop:DesktopFlyoutIsland IslandWidth="240" />
</desktop:DesktopFlyout>
```

Use `IslandsOrientation="Vertical"` for stacked sections and `Horizontal` for side-by-side sections. `IslandHeight` is used for vertical layout. `IslandWidth` is used for horizontal layout.

## Position requests

Pass a position request to `Show`. The flyout is measured first, then positioned in physical screen
pixels and clamped to the selected monitor's work area. Explicit anchor sides remain fixed. Tray
placement is resolved again when the icon, taskbar, or monitor layout changes.

Position is supplied per `Show` call.

For a regular UI anchor, pass its screen-space bounds and the side where the flyout should appear.
The default alignment is centered. The transition direction defaults to the direction implied by
the chosen side.

```csharp
var anchor = new Rectangle(x, y, width, height);
flyout.Show(DesktopFlyoutPosition.ForAnchor(anchor, DesktopFlyoutAnchorSide.Above));
```

For a tray icon, pass the icon itself to `ForTrayIcon`:

```csharp
flyout.Show(DesktopFlyoutPosition.ForTrayIcon(trayIcon));
```

`ForTrayIcon` queries the icon's latest bounds, uses a taskbar edge reported by the platform, and opens toward the
work area. If detection fails, it infers a side from the nearest monitor edge; if monitor bounds are
also unavailable, it uses `Above`. Windows shell taskbars and X11 panels that publish EWMH strut
properties can be detected. Third-party bars that expose no supported geometry are not guaranteed
to be detected. The nearest-edge fallback is a separate estimate, not a detection result.

The icon must have shell-reported bounds when first shown; otherwise the show request is ignored.
A temporarily unavailable icon keeps
its last known anchor while the flyout is open. The caller continues to own the icon.

Open flyouts follow changes to their monitor's work area, including registered appbars. On Windows,
the visible portion of a revealed auto-hide taskbar or registered auto-hide appbar is also excluded.
Area already reserved by the work area is not subtracted twice. Tray requests refresh the icon bounds;
`InWorkArea` requests retain the monitor chosen when shown, even if the pointer moves elsewhere.
Explicit `ForAnchor` coordinates remain fixed and are clamped into the current work area.

Layout changes during opening are applied after the transition. Repositioning an open flyout does
not restart its animation or activate its window.

On Windows, an icon in an open notification-area overflow flyout uses that flyout's full window
bounds, including all rows or columns, to choose the contact edge. Alignment along the other axis
stays on the tray icon. The flyout meets the overflow host without adding an extra transparent gap
and is sized to the available space on that side. When the overflow closes, it moves
back to the taskbar edge with its usual margins.

Use `DesktopFlyoutAnchorSide.Above`, `Below`, `Left`, or `Right` for an explicit side. Use
`DesktopFlyoutAnchorAlignment.Start`, `Center`, or `End` to align the flyout along the anchor.

For a fixed work-area position, use `DesktopFlyoutWorkAreaPosition` and choose whether to use the
primary monitor or the monitor under the cursor.

```csharp
flyout.Show(DesktopFlyoutPosition.InWorkArea(
    DesktopFlyoutWorkAreaPosition.BottomRight,
    DesktopFlyoutMonitorSelection.Cursor));
```

The transition follows the resolved placement by default. Pass a `DesktopFlyoutPopupDirection` to
the request factory only when the transition should intentionally differ from the placement. For
example, `BottomRight` normally enters from below; pass `RightToLeft` to make the same placement
enter from the right. `Auto` remains available as a transition direction, but there is no automatic
position or anchor-side request.

```csharp
flyout.Show(DesktopFlyoutPosition.InWorkArea(
    DesktopFlyoutWorkAreaPosition.BottomRight,
    DesktopFlyoutMonitorSelection.Cursor,
    DesktopFlyoutPopupDirection.RightToLeft));
```

## Sizing

Use `FlyoutWidth` and `FlyoutHeight` to request a flyout size.

```xml
<desktop:DesktopFlyout
    FlyoutWidth="420"
    FlyoutHeight="Auto" />
```

Both properties are `GridLength` values. `Auto` uses desired size. Star sizing stretches to the available work-area size after subtracting `Margin`.

## Closing behavior

`HideOnLostFocus` defaults to `True`. Set it to `False` for flyouts that should remain open after the user returns to the app or another window.

```xml
<desktop:DesktopFlyout HideOnLostFocus="False" />
```

`TransitionState` reports `Closed`, `Opening`, `Open`, or `Closing`; `TransitionStateChanged` fires
when the state changes. A `Hide` during opening is applied after opening completes. A newer `Show`
during opening cancels that deferred close. A `Show` during closing is replayed after the flyout
finishes closing, while a newer `Hide` cancels that replay. Controllers that track their own flyout
state should use `TransitionStateChanged` to stay synchronized with native focus-loss dismissal and
forward requests during transitions. Avoid buffering the same request in both the controller and
the flyout.

`AutoCloseDelay` starts after the flyout has opened.

```csharp
flyout.AutoCloseDelay = TimeSpan.FromSeconds(4);
```

Use `TimeSpan.Zero` or a negative value to disable automatic close.

## Transitions

`IsTransitionAnimationEnabled` defaults to `True`. On Windows, transitions also respect the system
animation setting. Set this property to `False` to open and close without transition animations.

## Interaction

`PressedScale` controls the pointer-pressed scale effect. The default is `1.0`, which disables the effect.

```xml
<desktop:DesktopFlyout PressedScale="0.92" />
```

`IsSwipeToDismissEnabled` enables swipe-to-dismiss in the opposite direction of the active popup direction.

```xml
<desktop:DesktopFlyout
    IsSwipeToDismissEnabled="True"
    SwipeDismissThreshold="80" />
```

## Backdrop

The Windows App SDK package can apply backdrops to flyout islands.

```xml
<desktop:DesktopFlyout
    IsBackdropEnabled="True"
    BackdropKind="DesktopAcrylic" />
```

`BackdropKind` can be `DesktopAcrylic` or `Mica`. UWP builds keep the same API surface, but do not create a Windows App SDK system backdrop.

## Lifetime

`DesktopFlyout` owns a desktop host window. Create the flyout once, reuse it, and call `Show`, `Hide`,
and `Dispose` on its owning UI thread. Call `Dispose` when the flyout is no longer needed.

```csharp
flyout.Dispose();
```
