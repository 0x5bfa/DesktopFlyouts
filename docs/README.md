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
    FlyoutWidth="360"
    ActivationMode="NoActivateOnOpen"
    HideOnLostFocus="False">

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
private readonly MainDesktopFlyout _flyout = new();

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
Each flyout owns a `DesktopWindowXamlSource`; they can share the application thread:

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

`DesktopFlyout` displays a panel in an independent desktop XAML island window. Its content is the
`Islands` collection of `DesktopFlyoutIsland` controls.

### Islands and sizing

Use `IslandsOrientation="Vertical"` for stacked sections and `Horizontal` for side-by-side sections.
`IslandHeight` controls each island's size in vertical layouts; `IslandWidth` controls it in
horizontal layouts. An island's inherited `Visibility` can be changed at runtime.

```xml
<desktop:DesktopFlyout IslandsOrientation="Horizontal">
    <desktop:DesktopFlyoutIsland IslandWidth="320" />
    <desktop:DesktopFlyoutIsland IslandWidth="240" />
</desktop:DesktopFlyout>
```

Set `FlyoutWidth` and `FlyoutHeight` to request the overall size:

```xml
<desktop:DesktopFlyout FlyoutWidth="420" FlyoutHeight="Auto" />
```

Flyout and island size properties are `GridLength` values. Use a numeric value for DIPs, `Auto` for
desired size, or `*` to fill available space. Flyout star sizing uses the selected work area after
subtracting `Margin`. To populate islands from code or binding, use `Islands` or set `IslandsSource`
to an `IEnumerable<DesktopFlyoutIsland>`.

### Closing and transitions

`HideOnLostFocus` defaults to `True`. Set it to `False` when a flyout should remain open after the
user returns to another window.

`TransitionState` reports `Closed`, `Opening`, `Open`, or `Closing`. `TransitionStateChanged` fires
when the state changes. `IsOpen` becomes `true` after opening completes and becomes `false` after
closing completes.

- `Hide` during opening requests a close after opening completes. A newer `Show` during opening
  cancels that deferred close.
- `Show` during closing queues reopening. A newer `Hide` during closing cancels that request.

Controllers that track flyout state should subscribe to `TransitionStateChanged` so native
focus-loss dismissal is reflected in their state. Forward requests during transitions rather than
buffering the same request in both the controller and the flyout.

`IsTransitionAnimationEnabled` defaults to `True`. Windows hosts also respect the system animation
setting. Set the property to `False` to open and close without transition animations.

`AutoCloseDelay` starts after opening completes. Changing it while open restarts the timer. Zero
or a negative value disables automatic close:

```csharp
_flyout.AutoCloseDelay = TimeSpan.FromSeconds(4);
```

### Interaction and backdrop

`PressedScale` controls the press scale effect; its default of `1.0` disables the effect.
`IsSwipeToDismissEnabled` enables dismissal by swiping opposite the active popup direction.
`SwipeDismissThreshold` is measured in DIPs.

```xml
<desktop:DesktopFlyout
    PressedScale="0.92"
    IsSwipeToDismissEnabled="True"
    SwipeDismissThreshold="80" />
```

The Windows App SDK package applies Desktop Acrylic or Mica to islands:

```xml
<desktop:DesktopFlyout IsBackdropEnabled="True" BackdropKind="DesktopAcrylic" />
```

Use `BackdropKind="Mica"` for Mica. UWP retains these properties but does not create a Windows App
SDK system backdrop. See the [API reference](api-reference.md#desktopflyout) for defaults and
property behavior.

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

```xml
<desktop:DesktopFlyout ActivationMode="NeverActivate" HideOnLostFocus="False" />
```

`NavigateFocus` moves focus into the hosted XAML island unless `NeverActivate` is selected, which
suppresses focusable states in the content. `HideOnLostFocus="True"` closes the flyout when its host
is deactivated; set it to `False` for a persistent panel.

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

Create and operate flyout controls on their owning UI thread, including `Show`, `Hide`, and
`Dispose`. Create each flyout once and reuse it. Dispose flyouts when the application no longer
needs them to release their host windows and islands:

```csharp
_flyout.Dispose();
_menu.Dispose();
```

Use `trayIcon.Destroy()` to remove the icon while retaining an instance that can be shown again.
It resets `IsVisible` to `false`. When finished, unsubscribe application event handlers and call
`trayIcon.Dispose()`. On Windows this removes the icon, destroys the hidden callback window, and
releases its owned icon handle. On Uno it removes the registration and disposes D-Bus resources.
