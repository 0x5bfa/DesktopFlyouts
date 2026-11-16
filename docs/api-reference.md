# DesktopFlyouts API reference

This reference describes the public library APIs in the current source tree. For installation and
examples, see the [usage guide](README.md). Release history is in the [changelog](../CHANGE_LOG.md).

- [Conventions and availability](#conventions-and-availability)
- [DesktopFlyout](#desktopflyout)
- [DesktopFlyoutIsland](#desktopflyoutisland)
- [DesktopFlyoutPosition](#desktopflyoutposition)
- [DesktopMenuFlyout](#desktopmenuflyout)
- [SystemTrayIcon](#systemtrayicon)
- [Event arguments](#event-arguments)
- [Enumerations](#enumerations)
- [Template and hosting types](#template-and-hosting-types)

## Conventions and availability

Types are in `DesktopFlyouts` unless another namespace is specified. **WinUI** means
`DesktopFlyouts.WinUI`, **UWP** means `DesktopFlyouts.Uwp`, and **Uno** means `DesktopFlyouts.Uno`
on Linux / X11. Members are available in all three unless marked otherwise.

XAML types such as `Control`, `GridLength`, `MenuFlyout`, and `CornerRadius` come from the target's
XAML framework: `Microsoft.UI.Xaml` for WinUI and Uno, or `Windows.UI.Xaml` for UWP.
`XamlSourceFocusNavigationReason` comes from that framework's `Hosting` namespace.

- `Point` and `Rectangle` in positioning signatures mean `System.Drawing.Point` and
  `System.Drawing.Rectangle`, using physical screen pixels, including negative monitor origins.
- XAML sizes, margins, panel spacing, and swipe thresholds use device-independent pixels (DIPs).
- **DP** marks a dependency property. Each has a public static identifier named
  `<PropertyName>Property`, usable with XAML binding and dependency-property APIs. A property
  marked read-only has no public CLR setter.
- Inherited XAML members and their Uno-generated equivalents are not exhaustively listed. For
  example, islands inherit `Content`, `Visibility`, `Margin`, and `CornerRadius`; menu flyouts
  inherit `Items` and `ItemsSource`. SDK-generated bootstrap types such as Uno's
  `GlobalStaticResources` and `WindowExtensions` are outside this library API reference.
- Construct, show, hide, and dispose flyout controls on their owning UI thread. Tray events may
  require dispatching to that thread before operating a flyout.

## DesktopFlyout

```csharp
public partial class DesktopFlyout : Control, IDisposable
```

Hosts one or more islands in an independent desktop window. The XAML content property is `Islands`.

### Constructor

`DesktopFlyout()` creates the hidden desktop host and initializes the control. Retain and reuse
the instance until `Dispose()` is called.

### Properties

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `Islands` | `IList<DesktopFlyoutIsland>` | Empty collection | Read-only collection reference | Add, remove, or reorder the displayed sections. Also accepts child islands in XAML. |
| `IslandsSource` | `object?` | `null` | Get/set, DP | Accepts `IEnumerable<DesktopFlyoutIsland>`. Assigning a valid source clears and repopulates `Islands`; incompatible values, including `null`, are ignored. This assignment copies the sequence. |
| `FlyoutWidth` | `GridLength` | `Auto` | Get/set, DP | Requested width. Star sizing fills available work-area width after margins. |
| `FlyoutHeight` | `GridLength` | `Auto` | Get/set, DP | Requested height. Star sizing fills available work-area height after margins. |
| `IslandsOrientation` | `Orientation` | `Vertical` | Get/set, DP | Stacks islands vertically or horizontally. |
| `IsOpen` | `bool` | `false` | Read-only, DP | Becomes `true` after opening completes and `false` after closing completes. Use `TransitionState` to distinguish transitions. |
| `TransitionState` | `DesktopFlyoutTransitionState` | `Closed` | Read-only | Current lifecycle phase. |
| `HideOnLostFocus` | `bool` | `true` | Get/set, DP | Closes the flyout when its host is deactivated. |
| `ActivationMode` | `DesktopFlyoutActivationMode` | `Activate` | Get/set, DP | Controls foreground activation and keyboard focus. |
| `AutoCloseDelay` | `TimeSpan` | `TimeSpan.Zero` | Get/set, DP | Automatic close delay after opening. Zero or negative disables it. Changing the value while open restarts the timer. |
| `IsTransitionAnimationEnabled` | `bool` | `true` | Get/set, DP | Enables opening and closing animations. Windows hosts also respect the system animation setting. |
| `PressedScale` | `double` | `1.0` | Get/set, DP | Effective press scale is clamped to `0.1` through `2.0`. `1.0`, NaN, or infinity disables the effect. |
| `IsSwipeToDismissEnabled` | `bool` | `false` | Get/set, DP | Enables dismissal by swiping opposite the active popup direction. |
| `SwipeDismissThreshold` | `double` | `80.0` | Get/set, DP | Required distance in DIPs, constrained by the available dismiss distance. NaN or infinity uses a distance-based default. |
| `BackdropKind` | `DesktopFlyoutBackdropKind` | `DesktopAcrylic` | Get/set, DP | Chooses Desktop Acrylic, Mica, or no library-owned backdrop. UWP uses per-island Composition effects, with Mica sampling the blurred desktop wallpaper. |
| `MenuFlyout` | `MenuFlyout?` | `null` | Get/set, DP | Optional reference for application code. The control does not show this menu automatically. |

### Methods

```csharp
public void Show(DesktopFlyoutPosition position);
public void Hide();
public void NavigateFocus(
    XamlSourceFocusNavigationReason reason = XamlSourceFocusNavigationReason.Programmatic);
public void Dispose();
```

| Method | Parameters and behavior |
| --- | --- |
| `Show(position)` | Resolves a required position request and begins opening. `position == null` throws `ArgumentNullException`. A live tray request with no initial icon bounds is ignored. Opening completes asynchronously; observe `TransitionStateChanged`. Calls after disposal are ignored. |
| `Hide()` | Begins closing. During opening, defers closing until opening completes. During closing, cancels a queued reopen. Calls after disposal are ignored. |
| `NavigateFocus(reason)` | Requests focus navigation into the hosted island using the supplied platform navigation reason. Focus is suppressed in `NeverActivate` mode. |
| `Dispose()` | Releases the host, islands' owned backdrop resources, timers, and environment monitoring. Cancels queued opening work. Repeated calls are safe; do not reuse the disposed instance. |

A `Show` during opening cancels a deferred close. A `Show` during closing queues reopening with
that position after the close transition completes; another `Show` replaces the pending position.

**UWP only:**

```csharp
public bool TryPreTranslateMessage(nint message);
```

Forwards a native `MSG` pointer to the hosted `DesktopWindowXamlSource` before dispatch. Returns
`true` when the island handled the message.

### Events

```csharp
public event EventHandler? TransitionStateChanged;
```

Raised whenever `TransitionState` changes. Read the new value from the flyout. Use this event to
observe opening, closing, and native focus-loss dismissal on the owning UI thread.

## DesktopFlyoutIsland

```csharp
public partial class DesktopFlyoutIsland : ContentControl
```

A content section owned by a `DesktopFlyout`. `DesktopFlyoutIsland()` initializes the default
island style. Populate the inherited `Content` property in code or XAML.

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `IslandWidth` | `GridLength` | `Auto` | Get/set, DP | Requested size along a horizontal island layout. Supports pixel, auto, and star sizing. |
| `IslandHeight` | `GridLength` | `Auto` | Get/set, DP | Requested size along a vertical island layout. Supports pixel, auto, and star sizing. |
| `TemplateSettings` | `DesktopFlyoutIslandTemplateSettings` | Per-island instance | Read-only | Calculated values for control templates. |

Runtime changes to island size, content layout, or inherited `Visibility` update the owning
flyout's layout. A collapsed island does not occupy layout space.

## DesktopFlyoutPosition

```csharp
public sealed partial class DesktopFlyoutPosition
```

A position request passed to `DesktopFlyout.Show`. Create it through a factory; it has no public
constructor or public mutable properties. Factory calls describe placement; resolution happens
when the flyout is shown. Open flyouts follow work-area changes without restarting their animations
or activating their host windows.

### ForAnchor

```csharp
public static DesktopFlyoutPosition ForAnchor(
    Rectangle anchor,
    DesktopFlyoutAnchorSide side,
    DesktopFlyoutAnchorAlignment alignment = DesktopFlyoutAnchorAlignment.Center,
    DesktopFlyoutPopupDirection transitionDirection = DesktopFlyoutPopupDirection.Auto);
```

| Parameter | Meaning |
| --- | --- |
| `anchor` | Fixed screen rectangle in physical pixels. Zero width or height permits a point-like anchor. |
| `side` | Required side on which the flyout appears. Work-area clamping does not flip this side. |
| `alignment` | Alignment along the perpendicular axis: horizontal for `Above`/`Below`, vertical for `Left`/`Right`. |
| `transitionDirection` | Animation direction. `Auto` derives it from the resolved placement. |

Returns a request anchored to that rectangle and its monitor. The rectangle remains fixed while
open; changes to the work area can adjust the clamped flyout position. Throws
`ArgumentOutOfRangeException` for a negative anchor width or height, an invalid `side`, or an invalid
`alignment`.

### ForTrayIcon

```csharp
public static DesktopFlyoutPosition ForTrayIcon(SystemTrayIcon trayIcon);
```

`trayIcon` is the live icon whose bounds should be queried. Throws `ArgumentNullException` if it
is `null`. The request retains a reference to the icon but does not take ownership or dispose it.

At the first `Show`, a failed bounds lookup causes the request to be ignored. While open, the
flyout refreshes icon bounds and retains the last known anchor during temporary lookup failures.
Taskbar or notification-area overflow geometry determines the contact side. Without a detected
edge, placement estimates the nearest monitor edge, then falls back to `Above` if monitor bounds
are unavailable. The flyout follows icon movement and desktop layout changes.

Uno's StatusNotifierItem implementation does not provide icon bounds, so this factory cannot
produce a usable initial anchor there. Use `ForAnchor` with the tray event's screen point instead.

### InWorkArea

```csharp
public static DesktopFlyoutPosition InWorkArea(
    DesktopFlyoutWorkAreaPosition position,
    DesktopFlyoutMonitorSelection monitor = DesktopFlyoutMonitorSelection.Primary,
    DesktopFlyoutPopupDirection transitionDirection = DesktopFlyoutPopupDirection.Auto);
```

| Parameter | Meaning |
| --- | --- |
| `position` | Required fixed position within the monitor work area. |
| `monitor` | Primary monitor or the monitor under the cursor when shown. |
| `transitionDirection` | Animation direction; `Auto` follows placement. |

Returns a work-area placement request. The selected monitor is retained while the flyout is open,
and its work-area changes update the flyout layout. Moving the cursor alone does not select a new
monitor while open.

## DesktopMenuFlyout

```csharp
public partial class DesktopMenuFlyout : ItemsControl, IDisposable
```

Hosts a XAML menu in an independent desktop island window. `DesktopMenuFlyout()` creates the
hidden host. Its XAML content property is the inherited `Items` collection; items must derive from
`MenuFlyoutItemBase`, including `MenuFlyoutItem`, `MenuFlyoutSubItem`, and `MenuFlyoutSeparator`.

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `IsOpen` | `bool` | `false` | Read-only, DP | Whether the hosted menu is open. Resets on dismissal or `Hide()`. |

```csharp
public void Show(Point point);
public void Hide();
public void Dispose();
```

| Method | Parameters and behavior |
| --- | --- |
| `Show(point)` | Opens the hosted menu at a physical screen point. Uses the current menu items; calls after disposal are ignored. |
| `Hide()` | Hides both the menu and host and resets `IsOpen`. Calls after disposal are ignored. |
| `Dispose()` | Closes the menu and releases its host. Repeated calls are safe. |

**UWP only:** `bool TryPreTranslateMessage(nint message)` forwards a native `MSG` pointer to the
hosted `DesktopWindowXamlSource` before dispatch and reports whether the island handled it.

## SystemTrayIcon

```csharp
public class SystemTrayIcon : IDisposable
```

Provides Win32 notification-area integration on Windows and D-Bus StatusNotifierItem integration
on Uno / Linux. Construction prepares resources; `Show()` requests shell registration.

### Constructors

```csharp
// Windows and Uno
public SystemTrayIcon(string iconPath, string tooltip, Guid id);

// Windows only
public SystemTrayIcon(nint hIcon, string tooltip, Guid id);

// Uno only
public SystemTrayIcon(string iconPath, string tooltip, string id);
```

| Parameter | Meaning |
| --- | --- |
| `iconPath` | Icon file path. Windows loads it with `LoadImage`; Uno renders the file into the StatusNotifierItem pixmap. |
| `hIcon` | Windows `HICON` to copy. The library owns the copy; the caller retains the original. |
| `tooltip` | Initial tooltip text. Windows truncates text to the shell field limit. |
| `id` | Stable icon identifier. Windows uses a GUID; Uno converts the GUID overload to a string. |

On Windows a missing file throws `FileNotFoundException`; a file that `LoadImage` cannot load
throws `ArgumentOutOfRangeException`. Failure to copy an `HICON` throws `Win32Exception`. Uno icon
loading can throw `FileNotFoundException` when a file cannot be found or loaded. Uno requires Linux.

### Properties

| Property | Windows | Uno | Behavior |
| --- | --- | --- | --- |
| `IconPath` | `string?`, read-only | `string`, read-only | Current path. Windows returns `null` for an icon supplied by handle. |
| `Tooltip` | `string`, get/set | `string`, get/set | Initial constructor value; updates an existing icon. |
| `IsVisible` | `bool`, get/set | `bool`, get/set | Initially `false`. Windows records visibility until `Show()` first creates the icon, then updates it. Uno setters call `Show()` or `Destroy()`. |
| `Id` | `Guid`, read-only | `string`, read-only | Identifier supplied to the constructor, converted to a string by Uno's GUID overload. |

These are ordinary CLR properties, not dependency properties. `IsVisible` reflects the requested
visibility state; it does not guarantee where a desktop shell displays the icon.

### Methods

```csharp
public void Show();
public void Destroy();
public bool TryGetBounds(out Rectangle bounds);
public void SetIcon(string iconPath);
public void Dispose();

// Windows only
public void SetIcon(nint hIcon);
```

| Method | Return / behavior |
| --- | --- |
| `Show()` | Sets `IsVisible` to `true`, registers the icon if necessary, and synchronizes its state. Uno registration can complete after D-Bus initialization. |
| `Destroy()` | Removes the shell registration and sets `IsVisible` to `false`. Can be called repeatedly on a live instance; `Show()` can register it again. Does not dispose the instance. |
| `TryGetBounds(out bounds)` | On Windows, returns `true` with the physical screen rectangle only if the visible, registered icon has valid shell geometry. Otherwise returns `false` with `Rectangle.Empty`. Uno always returns `false` and an empty rectangle. |
| `SetIcon(iconPath)` | Replaces the owned icon from a file and updates it. Windows uses the same file checks as the path constructor. |
| `SetIcon(hIcon)` | Windows only. Copies the supplied handle and replaces the owned icon, resetting `IconPath` to `null`. Throws `Win32Exception` if copying fails. |
| `Dispose()` | Removes the icon and releases owned resources. Windows destroys the hidden callback window and owned icon handle; Uno disposes D-Bus connections and watches. Repeated calls are safe. |

On Windows, `Show`, `SetIcon`, and the `Tooltip`/`IsVisible` setters throw `ObjectDisposedException`
after disposal. `TryGetBounds` returns `false`. Dispose explicitly on the icon's owning thread
rather than depending on finalization. Windows subclasses can override
`protected virtual void Dispose(bool disposing)` and should call the base implementation.

### Events

| Event | Type | Availability | Meaning |
| --- | --- | --- | --- |
| `LeftClicked` | `EventHandler<MouseEventReceivedEventArgs>?` | All | Left click / activation request. |
| `RightClicked` | `EventHandler<MouseEventReceivedEventArgs>?` | All | Right click / context-menu request. |
| `LeftDoubleClicked` | `EventHandler<MouseEventReceivedEventArgs>?` | Windows | Left double-click. |
| `RightDoubleClicked` | `EventHandler<MouseEventReceivedEventArgs>?` | Windows | Right double-click. |
| `MiddleClicked` | `EventHandler<MouseEventReceivedEventArgs>?` | Uno | Secondary activation request. |
| `Scrolled` | `EventHandler<MouseScrollEventReceivedEventArgs>?` | Uno | Scroll delta and orientation from StatusNotifierItem. |

Windows click events are raised only when a nonempty icon-center point can be obtained. Uno uses
the activation coordinates supplied by the desktop shell, which do not establish an icon rectangle.

## Event arguments

### MouseEventReceivedEventArgs

`public class MouseEventReceivedEventArgs : EventArgs` has a read-only `System.Drawing.Point Point`
property in physical screen pixels. Windows supplies the tray icon center; Uno supplies activation
coordinates. Instances are created by the library; there is no public constructor.

### MouseScrollEventReceivedEventArgs

**Uno only.** `public class MouseScrollEventReceivedEventArgs : EventArgs` exposes read-only
`int Delta` and `MouseScrollOrientation Orientation` properties. The delta is passed through from
the StatusNotifierItem scroll request. There is no public constructor.

## Enumerations

### DesktopFlyoutWorkAreaPosition

| Value | Numeric value | Position |
| --- | --- | --- |
| `TopLeft` | 0 | Top-left corner. |
| `TopCenter` | 1 | Centered along the top edge. |
| `TopRight` | 2 | Top-right corner. |
| `LeftCenter` | 3 | Centered along the left edge. |
| `RightCenter` | 4 | Centered along the right edge. |
| `BottomLeft` | 5 | Bottom-left corner. |
| `BottomCenter` | 6 | Centered along the bottom edge. |
| `BottomRight` | 7 | Bottom-right corner. |

### DesktopFlyoutAnchorSide

`Above = 0`, `Below = 1`, `Left = 2`, `Right = 3`: the side of an explicit anchor on which the flyout
is placed.

### DesktopFlyoutAnchorAlignment

`Start = 0`, `Center = 1`, `End = 2`: alignment on the axis perpendicular to the anchor side.
`Start` means left for `Above`/`Below` and top for `Left`/`Right`; `End` means right or bottom.

### DesktopFlyoutMonitorSelection

`Primary = 0` uses the primary monitor. `Cursor = 1` selects the monitor under the cursor when the
request is shown.

### DesktopFlyoutPopupDirection

| Value | Numeric value | Animation |
| --- | --- | --- |
| `BottomToTop` | 0 | Enters upward from the bottom edge. |
| `TopToBottom` | 1 | Enters downward from the top edge. |
| `Vertical` | 2 | Chooses a vertical direction from the resolved region. |
| `LeftToRight` | 3 | Enters rightward from the left edge. |
| `RightToLeft` | 4 | Enters leftward from the right edge. |
| `Horizontal` | 5 | Chooses a horizontal direction from the resolved region. |
| `Auto` | 6 | Derives the transition from placement. |

### DesktopFlyoutActivationMode

| Value | Numeric value | Behavior |
| --- | --- | --- |
| `Activate` | 0 | Activates and focuses the flyout on opening. |
| `NoActivateOnOpen` | 1 | Restores the previous foreground window after opening; later interaction may activate the flyout. |
| `NeverActivate` | 2 | Suppresses activation and keyboard focus. |

### DesktopFlyoutBackdropKind

`DesktopAcrylic = 0` selects the Desktop Acrylic system backdrop on WinUI. On Windows 11 and later,
UWP enables `DWMWA_USE_HOSTBACKDROPBRUSH` on the Win32 host and uses a per-island Composition
host-backdrop effect with luminosity, tint, and noise. The UWP package depends on `Win2D.uwp`
for the effect graph. Accent tint follows taskbar color prevalence. The effect stays active
when the host loses activation, including `NoActivateOnOpen` and `NeverActivate`.
UWP uses a solid fallback on older Windows versions, when transparency is disabled, in high
contrast or energy saver mode, or when Composition effects are unavailable. Effect policy and
device changes are monitored while each brush is connected; resources are released on disconnection.

`Mica = 1` selects base Mica on WinUI. On Windows 11 and later, UWP uses
`Compositor.TryCreateBlurredWallpaperBackdropBrush()` with a Win2D luminosity and tint effect
on each island. It samples the desktop wallpaper, not other windows, and adds no Acrylic noise.
It shares Acrylic's theme/accent handling, activation-independent lifetime, and effect policy
fallbacks. If the wallpaper API is absent or returns no brush, Mica uses a solid fallback.
Both UWP kinds use the system window color in high contrast
and preserve locally set island `Background` values.

`None = 2` leaves the island without a library-owned backdrop and preserves locally set island
`Background` values. Uno does not apply these Windows materials.

### DesktopFlyoutTransitionState

`Closed = 0`, `Opening = 1`, `Open = 2`, `Closing = 3`. `Closed` and `Open` mean no transition is
running; the other states indicate an in-progress transition.

### MouseScrollOrientation

**Uno only.** `Vertical = 0`, `Horizontal = 1` describe the scroll axis.

## Template and hosting types

### DesktopFlyoutIslandsPanel

`public partial class DesktopFlyoutIslandsPanel : Panel` arranges islands inside a flyout template.
It has a public parameterless constructor.

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `Orientation` | `Orientation` | `Vertical` | Get/set, DP | Fallback orientation outside a `DesktopFlyout`; an owning flyout supplies its island orientation. |
| `Spacing` | `double` | `0.0` | Get/set, DP | Spacing in DIPs between visible islands. |

### DesktopFlyoutIslandTemplateSettings

`public partial class DesktopFlyoutIslandTemplateSettings : DependencyObject` supplies calculated
template values through `DesktopFlyoutIsland.TemplateSettings`. It has a public parameterless
constructor; applications normally use the instance provided by the island.

The following properties are **WinUI only**. They are publicly readable DPs with internal setters:

| Property | Type | Behavior |
| --- | --- | --- |
| `BackdropCornerRadius` | `CornerRadius` | Owner corner radius adjusted for the inner backdrop surface. |
| `SystemBackdrop` | `SystemBackdrop?` | Library-owned backdrop for this island, or `null` when disconnected. Do not dispose or replace it from application code. |

### XamlIslandApplication

**UWP only; namespace `DesktopFlyouts.Shared`.**
`public partial class XamlIslandApplication : Application` is the UWP application type used by
XAML island hosting. It has a public parameterless constructor; applications normally use their
own `Application` subclass.

### TransparentWindow

**Uno only.** `public partial class TransparentWindow : Window` initializes a transparent window
and keeps its internal root background transparent. It exposes `TransparentWindow()` and otherwise
inherits its public members from the Uno XAML `Window` class. It is used by the Linux host.
