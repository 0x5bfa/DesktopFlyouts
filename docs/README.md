# DesktopFlyouts usage guide

DesktopFlyouts provides desktop-hosted XAML flyouts for lightweight panels, context menus, and
optional tray icon integration.

- [Getting started](#getting-started)
- [DesktopFlyout](#desktopflyout)
- [Position requests](#position-requests)
- [Focus and activation](#focus-and-activation)
- [DesktopMenuFlyout](#desktopmenuflyout)
- [SystemTrayIcon](#systemtrayicon)
- [Lifetime and threading](#lifetime-and-threading)
- [API reference](api-reference.md)

Release history and migration notes are in the [changelog](../CHANGE_LOG.md).

## Getting started

Install the package that matches your XAML stack:

| Package | Target |
| --- | --- |
| `DesktopFlyouts.WinUI` | Windows App SDK / WinUI 3 desktop XAML island hosting |
| `DesktopFlyouts.Uwp` | UWP / WinUI 2 desktop XAML island hosting |
| `DesktopFlyouts.Uno` | Uno Platform on Linux / X11 |

Choose one of these commands:

```console
dotnet add package DesktopFlyouts.WinUI
```

```console
dotnet add package DesktopFlyouts.Uwp
```

```console
dotnet add package DesktopFlyouts.Uno
```

Import `DesktopFlyouts` in C# and use `xmlns:desktop="using:DesktopFlyouts"` in XAML.

Define a flyout with one or more islands:

```xml
<desktop:DesktopFlyout
    x:Class="MyApp.MainDesktopFlyout"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:desktop="using:DesktopFlyouts"
    Width="360">

    <desktop:DesktopFlyoutIsland IslandHeight="300">
        <Grid Padding="16">
            <TextBlock Text="Hello from a desktop flyout" />
        </Grid>
    </desktop:DesktopFlyoutIsland>
</desktop:DesktopFlyout>
```

Initialize its XAML in the code-behind:

```csharp
using DesktopFlyouts;

namespace MyApp;

public sealed partial class MainDesktopFlyout : DesktopFlyout
{
    public MainDesktopFlyout() => InitializeComponent();
}
```

Create the flyout once on the UI thread and reuse it:

```csharp
private readonly MainDesktopFlyout _flyout = new()
{
    ActivationMode = DesktopFlyoutActivationMode.NoActivateOnOpen,
    IsLightDismissEnabled = false,
};

private void ToggleFlyout()
{
    if (_flyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
        _flyout.Hide();
    else
        _flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
}
```

`Show` requires a position request. This example opens at the bottom-right of the primary monitor's
work area. Use `TransitionState` for toggling so a click during opening also requests a close.

### Start a UWP desktop host

The UWP package hosts content with `DesktopWindowXamlSource`. Disable the generated XAML entry
point in the executable project:

```xml
<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>
```

Start System XAML and the application from an STA entry point:

```csharp
using System;
using Windows.Win32;
using Windows.Win32.System.WinRT;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
        _ = new App();
    }
}
```

In the `Application` constructor, initialize the XAML manager and create flyouts on that thread.
Each flyout creates its `DesktopWindowXamlSource` on first display; flyouts and menus can
share the application thread:

```csharp
WindowsXamlManager.InitializeForCurrentThread();
SynchronizationContext.SetSynchronizationContext(
    new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

var flyout = new MainDesktopFlyout();
var menu = new MainDesktopMenuFlyout();
```

Before dispatching each native message, give it to the flyouts so their islands can process
keyboard input:

```csharp
MSG message;
while (PInvoke.GetMessage(&message, HWND.Null, 0U, 0U))
{
    if (!flyout.TryPreTranslateMessage((nint)(&message)) &&
        !menu.TryPreTranslateMessage((nint)(&message)))
    {
        PInvoke.TranslateMessage(&message);
        PInvoke.DispatchMessage(&message);
    }
}
```

Dispatch later operations to the owning XAML thread when calling from another thread. The UWP
sample combines the executable, MSIX manifest, and tray assets in
`samples/DesktopFlyouts.SampleApp.Uwp`.

## DesktopFlyout

`DesktopFlyout : DependencyObject, IDisposable` is a nonvisual flyout definition and controller.
It owns the items, opening and closing, placement, activation, and native host lifetime. The
library creates its private `ItemsControl` presenter and island containers; applications do not
construct, style, or replace those visuals. Construction creates no native window.

### Islands and data templates

`Islands` is the default XAML content collection and accepts island definitions, UI elements, or
data items. A `DesktopFlyoutIsland` is a nonvisual definition of one section's content, visibility,
and allocation. Every item is displayed inside a library-owned container:

```xml
<desktop:DesktopFlyout Width="360">
    <desktop:DesktopFlyoutIsland IslandHeight="300">
        <Grid Padding="16">
            <TextBlock Text="Content inside a consistent frame" />
        </Grid>
    </desktop:DesktopFlyoutIsland>
</desktop:DesktopFlyout>
```

Use `IslandsOrientation="Vertical"` for stacked sections and `Horizontal` for side-by-side sections.
`IslandHeight` controls allocation in vertical layouts; `IslandWidth` controls it in horizontal
layouts. These are `GridLength` values: a number for DIPs, `Auto` for desired size, or weighted
stars such as `*` and `2*` for sharing the assigned space. Stars do not implicitly stretch an
automatically sized parent. Set a definition's `Visibility="Collapsed"` to remove its layout space.

For external data, set `IslandsSource` to an enumerable collection and supply `IslandTemplate`
or `IslandTemplateSelector`. Each generated container displays the data through the selected
template. A definition's own `ContentTemplate` takes precedence for that definition's content.
The templates define content only; do not put another `DesktopFlyoutIsland` at a DataTemplate root.

```xml
<desktop:DesktopFlyout Width="360" IslandsSource="{x:Bind ViewModel.Sections}">
    <desktop:DesktopFlyout.IslandTemplate>
        <DataTemplate x:DataType="local:Section">
            <Grid Padding="16">
                <TextBlock Text="{x:Bind Title}" />
            </Grid>
        </DataTemplate>
    </desktop:DesktopFlyout.IslandTemplate>
</desktop:DesktopFlyout>
```

Here `ViewModel.Sections` and `Section.Title` belong to the application. The Data templates example
in each sample includes a complete compiled-binding example. Sources are connected directly;
supported collection-change notifications update the visible items without a snapshot copy.
Direct `Islands` mutation is prohibited while `IslandsSource` is non-null. Setting it to null
restores the original direct collection. Use explicit binding sources or `x:Bind` for properties
on the nonvisual flyout; it does not inherit a `DataContext` or expose `Resources`.

### Overall dimensions

Set the flyout's `Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight`, and `MaxHeight` DPs directly:

```xml
<desktop:DesktopFlyout Width="420" MaxHeight="640" />
```

Dimensions use DIPs. Omit `Width` or `Height` in XAML for `Auto`; it is `double.NaN` in C#.
Defaults follow content, with
`HorizontalAlignment="Left"` and `VerticalAlignment="Top"`. These alignment properties control
sizing, while `Show(position)` controls screen placement. An automatic dimension with the
corresponding alignment set to `Stretch` fills available space subject to min/max constraints:

```xml
<desktop:DesktopFlyout Width="336" VerticalAlignment="Stretch" />
```

An explicit dimension takes precedence over stretching. Requested dimensions and bindings are
never overwritten during placement. The visible frame fits the work area even if a requested
or minimum dimension exceeds it. The root island collection does not scroll; content outside the
visible viewport is clipped. Use a bounded `ScrollViewer` inside an island for scrollable content.
Changes to dimensions, templates, items, visibility, and allocation update an open flyout without
reopening it. Changes during opening are applied after the transition.

### Consistent outer appearance

The library owns the background material, 1-DIP border, 8-DIP corner radius, shadow, 12-DIP island
spacing and outer inset, a 12-DIP shadow margin, and a 12-DIP visible screen-edge gap. Presenter
styles, container styles, custom frame templates, backdrop selection,
and outer margin overrides are not part of the public flyout API.

Applications control the content inside each frame, including its padding and colors. Content is
clipped to the frame's interior. The outer chrome uses a private resource dictionary and fixed
policy rather than application-overridable frame resources. Windows hosts use Desktop Acrylic
with system theme and accessibility behavior; UWP uses its Composition implementation and
solid fallbacks. Uno uses the library's theme-appropriate solid fallback.

### Closing and transitions

`IsLightDismissEnabled` defaults to true and closes the flyout when its host window is deactivated.
`ActivationMode`, `AutoCloseDelay`,
`IsTransitionAnimationEnabled`, `IsSwipeToDismissEnabled`, and `SwipeDismissThreshold` configure
behavior on `DesktopFlyout` itself.

`TransitionState` reports Closed, Opening, Open, or Closing. `TransitionStateChanged` fires when
it changes. `IsOpen` becomes true after opening completes and false after closing completes.

- `Hide` during opening defers closing; a newer `Show` cancels that deferred close.
- `Show` during closing queues reopening; a newer `Hide` cancels it.

Windows hosts also respect the system animation setting. `AutoCloseDelay` starts after opening;
zero or negative values disable it. Changing it while open restarts the timer. Swipe thresholds
use DIPs. Construct, operate, and dispose the flyout on its owning UI thread.

## Position requests

Pass a `DesktopFlyoutPosition` to each `Show` call. Anchor rectangles and menu points use physical
screen pixels; XAML sizes, margins, and swipe distances use DIPs. Flyouts are measured and clamped
to the selected monitor's work area.

### Work-area placement

Choose a `DesktopFlyoutWorkAreaPosition` and either the primary monitor or the monitor under the
cursor. The selected monitor is retained while the flyout is open, even if the pointer moves.

```csharp
_flyout.Show(DesktopFlyoutPosition.InWorkArea(
    DesktopFlyoutWorkAreaPosition.BottomRight,
    DesktopFlyoutMonitorSelection.Cursor));
```

### Explicit screen anchors

Use `ForAnchor` for a button or another UI element whose screen bounds are available. The side is
required; alignment defaults to `Center`.

```csharp
var anchor = new System.Drawing.Rectangle(x, y, width, height);
_flyout.Show(DesktopFlyoutPosition.ForAnchor(
    anchor,
    DesktopFlyoutAnchorSide.Above,
    DesktopFlyoutAnchorAlignment.Center));
```

Sides are `Above`, `Below`, `Left`, and `Right`; alignments are `Start`, `Center`, and `End` along the
perpendicular axis. Explicit anchor coordinates remain fixed while open. Placement is clamped to
the current work area without changing the requested side.

### Live tray anchors

Use `ForTrayIcon` to follow a tray icon's current bounds:

```csharp
_flyout.Show(DesktopFlyoutPosition.ForTrayIcon(trayIcon));
```

The icon must have shell-reported bounds when first shown, or the show request is ignored. A
temporary lookup failure while open retains the last known anchor. The caller continues to own
the tray icon.

Placement uses a detected taskbar edge and opens toward the work area. If edge detection fails,
it estimates a side from the nearest monitor edge; if monitor bounds are also unavailable, it uses
`Above`. Supported geometry includes Windows shell taskbars and X11 panels publishing EWMH struts;
third-party bars without supported geometry may not be detected. X11 StatusNotifierItem does not
provide icon bounds, so use the click-point fallback shown under [SystemTrayIcon](#systemtrayicon).

Open flyouts follow work-area changes, including registered appbars. On Windows, the visible
portion of a revealed auto-hide taskbar or registered auto-hide appbar is excluded as well. Space
already reserved by the work area is not subtracted twice. Layout changes during opening are
applied after the transition; repositioning does not restart animations or activate the host.

For icons in the Windows notification-area overflow, placement uses the full overflow window to
choose the contact edge and aligns along the other axis with the icon's row or column. The flyout
meets the overflow host without an extra transparent gap and fits the available space on that side.
When the overflow closes, the flyout returns to the taskbar edge with its usual margins.

### Transition direction

The transition follows placement by default. Override it when the desired animation differs from
placement. For example, `BottomRight` normally enters from below; `RightToLeft` enters from the right:

```csharp
_flyout.Show(DesktopFlyoutPosition.InWorkArea(
    DesktopFlyoutWorkAreaPosition.BottomRight,
    DesktopFlyoutMonitorSelection.Cursor,
    DesktopFlyoutPopupDirection.RightToLeft));
```

`Auto` resolves the transition direction; position and anchor side are specified explicitly.

## Focus and activation

`DesktopFlyout.ActivationMode` controls foreground activation and keyboard focus:

| Mode | Behavior |
| --- | --- |
| `Activate` | Activates the flyout when it opens. This is the default; use it for immediate keyboard interaction. |
| `NoActivateOnOpen` | Restores the previous foreground window after opening. The flyout may still be activated by later user interaction. |
| `NeverActivate` | Prevents activation and focus. Use it for passive UI. |

```csharp
_flyout.ActivationMode = DesktopFlyoutActivationMode.NeverActivate;
_flyout.IsLightDismissEnabled = false;
```

`NavigateFocus` moves focus into the hosted XAML island unless `NeverActivate` is selected, which
suppresses focusable states in the content. `IsLightDismissEnabled="True"` closes the flyout when
its host window is deactivated; set it to `False` for a persistent panel.

### UWP message dispatch

Call `TryPreTranslateMessage(nint message)` for each flyout before translating and dispatching a
native message. It forwards the `MSG` to `DesktopWindowXamlSource` and returns `true` when the
island handled it.

## DesktopMenuFlyout

`DesktopMenuFlyout` hosts a XAML `MenuFlyout` at a physical screen point. Use it for tray context
menus or other menus independent of an application window.

```xml
<desktop:DesktopMenuFlyout
    x:Class="MyApp.MainDesktopMenuFlyout"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:desktop="using:DesktopFlyouts">
    <MenuFlyoutItem Text="Settings" />
    <MenuFlyoutSeparator />
    <MenuFlyoutItem Text="Exit" />
</desktop:DesktopMenuFlyout>
```

Initialize this class with `InitializeComponent()` in a code-behind constructor, as for the panel
flyout, and reuse the instance:

```csharp
private readonly MainDesktopMenuFlyout _menu = new();

private void TrayIcon_RightClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (_menu.IsOpen)
        _menu.Hide();

    _menu.Show(e.Point);
}
```

`Items` accepts `MenuFlyoutItem`, `MenuFlyoutSubItem`, `MenuFlyoutSeparator`, or custom types derived
from `MenuFlyoutItemBase`. The hosted menu is rebuilt when its items change. `IsOpen` is `true`
while the menu is open and becomes `false` on dismissal or `Hide()`.

`Show(System.Drawing.Point)` uses physical screen pixels. For an offset, adjust the point:

```csharp
_menu.Show(new System.Drawing.Point(e.Point.X, e.Point.Y - 32));
```

## SystemTrayIcon

`SystemTrayIcon` is optional; flyout controls also work without a tray icon. Windows uses the Win32
notification area; Uno on Linux uses D-Bus StatusNotifierItem.

### Construction and events

Construction prepares resources but does not display the icon. Subscribe to events, then call
`Show()` to register it:

```csharp
var trayIcon = new SystemTrayIcon(
    iconPath: "Assets/AppIcon.ico",
    tooltip: "My app",
    id: new Guid("ad9ceac0-742a-48d3-a4aa-ef6dbe9f2f7d"));

trayIcon.LeftClicked += TrayIcon_LeftClicked;
trayIcon.RightClicked += TrayIcon_RightClicked;
trayIcon.Show();
```

Windows also accepts an `nint hIcon` instead of a path. The handle is copied internally, so the
caller retains ownership of the original. Uno additionally accepts a string identifier. See
[platform-specific constructors and events](api-reference.md#systemtrayicon).

Windows click events carry the icon center in physical screen pixels. X11 events carry the point
provided by the StatusNotifierItem activation request. Check bounds before choosing a live anchor:

```csharp
private void TrayIcon_LeftClicked(object? sender, MouseEventReceivedEventArgs e)
{
    if (sender is SystemTrayIcon trayIcon && trayIcon.TryGetBounds(out _))
    {
        _flyout.Show(DesktopFlyoutPosition.ForTrayIcon(trayIcon));
    }
    else
    {
        var pointBounds = new System.Drawing.Rectangle(e.Point.X, e.Point.Y, 0, 0);
        _flyout.Show(DesktopFlyoutPosition.ForAnchor(pointBounds, DesktopFlyoutAnchorSide.Above));
    }
}
```

`TryGetBounds` returns `false` and an empty rectangle if geometry is unavailable. It always returns
`false` on X11, where the protocol provides no icon rectangle. Use an explicit side appropriate to
your UI when falling back to the event point. Tray callbacks must reach the flyout's UI thread;
dispatch them there if they arrive on another thread.

### Visibility and updates

`IsVisible` initially returns `false`. `Show()` makes the icon visible and registers it if needed.
Changing `Tooltip` updates an existing icon. On Windows, setting `IsVisible` before `Show()` only
records the value; on Uno, setting it to `true` calls `Show()` and `false` calls `Destroy()`.

```csharp
trayIcon.Tooltip = "New tooltip";
trayIcon.IsVisible = false;
trayIcon.IsVisible = true;
```

`IconPath` is read-only. Replace the icon with `SetIcon(string)` or, on Windows, `SetIcon(nint)`:

```csharp
trayIcon.SetIcon("Assets/AlternateIcon.ico");
```

Windows paths must identify a file loadable by Win32 `LoadImage`. A missing file throws
`FileNotFoundException`; a file that cannot be loaded as an icon throws `ArgumentOutOfRangeException`.

## Lifetime and threading

Create and operate flyouts on their owning UI thread, including `Show`, `Hide`, and `Dispose`.
Reuse each flyout while needed. Dispose flyouts and menus to release their hosts, generated
containers, content attachments, event subscriptions, and native resources:

```csharp
_flyout.Dispose();
_menu.Dispose();
```

Use `trayIcon.Destroy()` to remove the icon while retaining an instance that can be shown again.
It resets `IsVisible` to `false`. When finished, unsubscribe application event handlers and call
`trayIcon.Dispose()`. On Windows this removes the icon, destroys the hidden callback window, and
releases its owned icon handle. On Uno it removes the registration and disposes D-Bus resources.
