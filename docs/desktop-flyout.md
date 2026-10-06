# DesktopFlyout

`DesktopFlyout` displays a panel-style flyout in an independent desktop XAML island window.

Use it when you need a small desktop surface that can be shown from a tray icon or from normal application code.

## Basic usage

```xml
<desktop:DesktopFlyout
    x:Class="MyApp.MainDesktopFlyout"
    FlyoutWidth="360"
    Placement="BottomRight"
    PopupDirection="Vertical"
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

## Islands

The flyout content is the `Islands` collection. Each island is a `DesktopFlyoutIsland`.

```xml
<desktop:DesktopFlyout IslandsOrientation="Horizontal">
    <desktop:DesktopFlyoutIsland IslandWidth="320" />
    <desktop:DesktopFlyoutIsland IslandWidth="240" />
</desktop:DesktopFlyout>
```

Use `IslandsOrientation="Vertical"` for stacked sections and `Horizontal` for side-by-side sections. `IslandHeight` is used for vertical layout. `IslandWidth` is used for horizontal layout.

## Placement

`Show()` positions the flyout by `Placement`.

Available values:

- `BottomRight`
- `BottomLeft`
- `BottomCenter`
- `TopRight`
- `TopLeft`
- `TopCenter`
- `LeftCenter`
- `RightCenter`

`Show(Point)` bypasses `Placement` for that open operation and treats the point as the desired bottom-center point in physical screen pixels.

```csharp
flyout.Show(new Point(x, y));
```

## Popup direction

`PopupDirection` controls the transition direction.

- `Vertical`: resolves to `BottomToTop` in the bottom half of the work area, otherwise `TopToBottom`.
- `Horizontal`: resolves to `RightToLeft` in the right half of the work area, otherwise `LeftToRight`.
- Explicit values always use the requested direction.

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

`AutoCloseDelay` starts after the flyout has opened.

```csharp
flyout.AutoCloseDelay = TimeSpan.FromSeconds(4);
```

Use `TimeSpan.Zero` or a negative value to disable automatic close.

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

`DesktopFlyout` owns a desktop host window. Create the flyout once, reuse it, and call `Dispose` when it is no longer needed.

```csharp
flyout.Dispose();
```
