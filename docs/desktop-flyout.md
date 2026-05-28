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
Each island is hosted in its own `DesktopWindowXamlSource`, so stacked and freeform layouts are
computed by `DesktopFlyout` but rendered as separate native island windows.

```xml
<desktop:DesktopFlyout IslandsOrientation="Horizontal">
    <desktop:DesktopFlyoutIsland IslandWidth="320" />
    <desktop:DesktopFlyoutIsland IslandWidth="240" />
</desktop:DesktopFlyout>
```

Use `IslandsOrientation="Vertical"` for stacked sections and `Horizontal` for side-by-side sections. `IslandHeight` is used for vertical layout. `IslandWidth` is used for horizontal layout.

Use `IslandLayoutMode="Freeform"` when islands should be placed on a canvas instead of the stack grid. Freeform layout uses each island's `CanvasLeft` and `CanvasTop` values.

```xml
<desktop:DesktopFlyout
    FlyoutWidth="620"
    FlyoutHeight="340"
    IslandLayoutMode="Freeform">
    <desktop:DesktopFlyoutIsland
        CanvasLeft="0"
        CanvasTop="0"
        IslandWidth="188"
        IslandHeight="250" />
    <desktop:DesktopFlyoutIsland
        CanvasLeft="210"
        CanvasTop="42"
        IslandWidth="188"
        IslandHeight="250" />
</desktop:DesktopFlyout>
```

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

`DragMode` lets the user move the native island window. `Full` starts native movement from the
whole island surface.

```xml
<desktop:DesktopFlyout DragMode="Full" />
```

Use `DragMode="Region"` with `DesktopFlyoutDragRegion` to limit native window movement to specific
visual areas. The region only starts the native move; it does not run a XAML pointer drag loop.

```xml
<desktop:DesktopFlyout DragMode="Region">
    <desktop:DesktopFlyoutIsland>
        <Grid RowSpacing="12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <desktop:DesktopFlyoutDragRegion>
                <Grid Height="48">
                    <TextBlock Text="Drag here" />
                </Grid>
            </desktop:DesktopFlyoutDragRegion>

            <Button Grid.Row="1" Content="Interactive content" />
        </Grid>
    </desktop:DesktopFlyoutIsland>
</desktop:DesktopFlyout>
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
    BackdropKind="Acrylic" />
```

`BackdropKind` can be `Acrylic` or `Mica`. UWP builds keep the same API surface, but do not create a Windows App SDK system backdrop.

## Lifetime

`DesktopFlyout` owns a desktop host window. Create the flyout once, reuse it, and call `Dispose` when it is no longer needed.

```csharp
flyout.Dispose();
```
