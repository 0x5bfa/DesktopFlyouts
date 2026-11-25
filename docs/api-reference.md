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

The WinUI package requires Windows 10 version 1809 (build 17763) or later. The UWP package
requires Windows 10 version 1903 (build 18362) or later. Both packages support Windows x86,
x64, and ARM64 through RID-specific implementations.
The Uno assembly is AnyCPU; its supported Linux architectures depend on the Uno/native runtime.

XAML types such as `Control`, `GridLength`, `MenuFlyout`, and `CornerRadius` come from the target's
XAML framework: `Microsoft.UI.Xaml` for WinUI and Uno, or `Windows.UI.Xaml` for UWP.
`XamlSourceFocusNavigationReason` comes from that framework's `Hosting` namespace.

- `Point` and `Rectangle` in positioning signatures mean `System.Drawing.Point` and
  `System.Drawing.Rectangle`, using physical screen pixels, including negative monitor origins.
- XAML sizes, margins, panel spacing, and swipe thresholds use device-independent pixels (DIPs).
- **DP** marks a dependency property. Each has a public static identifier named
  `<PropertyName>Property`, usable with XAML binding and dependency-property APIs. A property
  marked read-only has no public CLR setter.
- Inherited XAML members and their Uno-generated equivalents are not exhaustively listed.
  Menu flyouts inherit `Items` and `ItemsSource`. Desktop flyouts and island definitions are
  nonvisual and have only their explicitly declared configuration. SDK-generated bootstrap types such as Uno's
  `GlobalStaticResources` and `WindowExtensions` are outside this library API reference.
- Construct, show, hide, and dispose flyouts on their owning UI thread. Tray events may require dispatching.

## DesktopFlyout

```csharp
public partial class DesktopFlyout : DependencyObject, IDisposable
```

A nonvisual controller with `Islands` as its XAML content property. `DesktopFlyout()` initializes
the definition and private visual presenter without creating a native window. The first show
with a resolvable position creates the host. Operate and dispose it on its UI thread.

### Properties

All settable properties below are DPs. The collection reference itself is read-only.

| Property | Type | Default | Behavior |
| --- | --- | --- | --- |
| `Islands` | `IList<object>` | Empty | Direct definitions, UI elements, or data items. Mutation throws while IslandsSource is set. |
| `IslandsSource` | `object?` | `null` | Enumerable item source, connected directly. Collection notifications update generated containers. Null restores direct Islands. Invalid non-enumerables throw ArgumentException. |
| `IslandTemplate` | `DataTemplate?` | `null` | Content template for generated island containers. |
| `IslandTemplateSelector` | `DataTemplateSelector?` | `null` | Selects item content templates using the standard XAML template-selection rules. |
| `IslandsOrientation` | `Orientation` | `Vertical` | Main direction of island allocation. |
| `Width`, `Height` | `double` | `NaN` | Requested dimensions in DIPs. NaN means Auto. Negative or infinite values throw ArgumentOutOfRangeException. |
| `MinWidth`, `MinHeight` | `double` | `0` | Nonnegative finite minimum dimensions. |
| `MaxWidth`, `MaxHeight` | `double` | `PositiveInfinity` | Nonnegative maximum dimensions; positive infinity removes the limit. |
| `HorizontalAlignment`, `VerticalAlignment` | Corresponding XAML enum | `Left`, `Top` | Sizing alignment. Stretch fills an automatic dimension subject to its min/max constraints. Explicit dimensions take precedence. |

On Windows with dynamic code support disabled, managed `IslandsSource` collections must expose
generated WinRT collection interfaces. Mark a partial collection class with
`[WinRT.GeneratedWinRTExposedType]`. Custom `DataTemplateSelector` subclasses and their containing
types must also be partial for WinRT source generation.

Requested values and bindings are preserved when the viewport is constrained. Minimum constraints
take precedence over maximum constraints, with the available work area as the hard presentation
limit. The root island collection does not scroll, so content beyond the visible viewport is clipped.
Rendering and input geometry share the measured layout. Automatic parent dimensions do not become
stretched merely because a child island uses stars.

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `IsOpen` | `bool` | `false` | Read-only, DP | Becomes `true` after opening completes and `false` after closing completes. Use `TransitionState` to distinguish transitions. |
| `TransitionState` | `DesktopFlyoutTransitionState` | `Closed` | Read-only | Current lifecycle phase. |
| `IsLightDismissEnabled` | `bool` | `true` | Get/set, DP | Enables light dismissal when the host window is deactivated. |
| `ActivationMode` | `DesktopFlyoutActivationMode` | `Activate` | Get/set, DP | Controls foreground activation and keyboard focus. |
| `AutoCloseDelay` | `TimeSpan` | `TimeSpan.Zero` | Get/set, DP | Automatic close delay after opening. Zero or negative disables it. Changing the value while open restarts the timer. |
| `IsTransitionAnimationEnabled` | `bool` | `true` | Get/set, DP | Enables opening and closing animations. Windows hosts also respect the system animation setting. |
| `IsSwipeToDismissEnabled` | `bool` | `false` | Get/set, DP | Enables dismissal by swiping opposite the active popup direction. |
| `SwipeDismissThreshold` | `double` | `80.0` | Get/set, DP | Required distance in DIPs, constrained by the available dismiss distance. NaN or infinity uses a distance-based default. |

Background, border, corner radius, shadow, outer margin, presenter/container styles, and templates
are not public configuration points. Library chrome uses a private dictionary, system theme and
accessibility behavior, 12-DIP island spacing, a 12-DIP visible screen-edge gap, and a separately
reserved 12-DIP shadow margin outside the presenter. The islands panel adds no outer inset.
There are no inherited FrameworkElement dimensions, DataContext, Resources, or Style on this type.

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
public partial class DesktopFlyoutIsland : DependencyObject
```

A nonvisual section definition with `Content` as its XAML content property. The library creates
and owns the actual visual container. A definition can be attached to only one live container
at a time; it can be reused once its previous flyout has detached it.

| Property | Type | Default | Behavior |
| --- | --- | --- | --- |
| `Content` | `object?` | `null` | UI element or data displayed inside the fixed frame. DP. |
| `ContentTemplate` | `DataTemplate?` | `null` | Definition-specific content template, taking precedence over the flyout template. DP. |
| `Width` | `GridLength` | `Auto` | Horizontal main-axis allocation: pixels, Auto, or weighted stars. DP. |
| `Height` | `GridLength` | `Auto` | Vertical main-axis allocation: pixels, Auto, or weighted stars. DP. |
| `Visibility` | `Visibility` | `Visible` | Collapsed sections consume no space. DP. |

Definition changes update a connected container and its layout. Arbitrary data items in Islands
or IslandsSource also receive generated containers; use a definition when explicit allocation or
visibility is needed. Templates customize content only. This type has no Background, BorderBrush,
CornerRadius, Margin, Style, Template, or TemplateSettings properties.

## DesktopFlyoutPosition

```csharp
public sealed partial class DesktopFlyoutPosition
```

A position request passed to `DesktopFlyout.Show`. Create it through a factory; it has no
public constructor or public mutable properties. Factory calls describe placement; resolution happens
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

### FromTrayIconBounds

```csharp
public static DesktopFlyoutPosition FromTrayIconBounds(Func<Rectangle?> getBounds);
```

Use this factory for a tray icon registered by your app or another library. The flyout calls
`getBounds` on its owning UI thread to retrieve the current icon rectangle in physical screen
pixels. Return `null` when bounds are unavailable. Throws `ArgumentNullException` if `getBounds`
is `null`.

The request uses the same live placement rules as `ForTrayIcon`. The first lookup must return
a rectangle with positive width and height for `Show` to open the flyout. Later lookup failures
retain the last valid anchor while the flyout is open.

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
public partial class DesktopMenuFlyout : DependencyObject, IDisposable
```

Hosts a XAML menu in an independent desktop island window. `DesktopMenuFlyout()` creates the
hidden host. Its XAML content property is `Items`. The host owns its visual layout and follows
the system theme. Submenus are supported in WinUI and UWP.

| Property | Type | Default | Access / DP | Behavior |
| --- | --- | --- | --- | --- |
| `Items` | `IList<MenuFlyoutItemBase>` | Empty | Get-only, mutable collection | Items displayed in the hosted menu. |
| `IsOpen` | `bool` | `false` | Read-only, DP | Whether the hosted menu is open. On Windows this follows the underlying XAML menu. |

```csharp
public void Show(Point point);
public void Hide();
public void Dispose();
```

| Method | Parameters and behavior |
| --- | --- |
| `Show(point)` | Opens the hosted menu at a physical screen point using the current items. On Windows, a request made while the menu closes is handled after close, and empty menus are skipped. Calls after disposal are ignored. |
| `Hide()` | Hides both the menu and host and resets `IsOpen`. Calls after disposal are ignored. |
| `Dispose()` | Closes the menu, clears `Items`, and releases its host. Repeated calls are safe. |

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

`DesktopAcrylic = 0`, `Mica = 1`, `None = 2`. This enum remains available to the standalone
UWP Composition primitives. Desktop flyouts select their Acrylic or solid fallback internally;
neither the flyout nor an island definition exposes a backdrop-selection property.

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
| `Spacing` | `double` | `0.0` | Get/set, DP | Standalone panel spacing. Hosted flyouts always use the library-owned 12-DIP spacing. |

### XamlIslandApplication

**UWP only; namespace `DesktopFlyouts.Shared`.**
`public partial class XamlIslandApplication : Application` is the UWP application type used by
XAML island hosting. It has a public parameterless constructor; applications normally use their
own `Application` subclass.

### TransparentWindow

**Uno only.** `public partial class TransparentWindow : Window` initializes a transparent window
and keeps its internal root background transparent. It exposes `TransparentWindow()` and otherwise
inherits its public members from the Uno XAML `Window` class. It is used by the Linux host.
