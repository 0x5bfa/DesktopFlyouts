# Changelog

## Unreleased

### Changed

- Package WinUI and UWP implementations for Windows x86, x64, and ARM64 together, with
  architecture-neutral compile references. Ship Uno's managed library as AnyCPU.
- Build packages through `.nuget/DesktopFlyoutsPack.proj` and verify their architectures and sample consumers
  before publishing; ordinary library builds no longer produce partial packages.
- Organize tests into shared test sources and template-based WinUI and UWP test applications.
  Both applications reference their platform library directly, replacing product-source links;
  run them with Microsoft.Testing.Platform (MTP) through `MSTest.Sdk`. WinUI supports `dotnet test`;
  modern .NET 10 UWP uses the `InvokeTestingPlatform` MSBuild target for its AppContainer host.

### Fixed

- Use the 32-bit `SetWindowLongW` API when subclassing XAML island windows on x86.
- Reuse WinUI island backdrop instances across open/close cycles while reconnecting native
  targets and releasing them on the owning UI thread.
- Make WinUI test data sources and template selectors compatible with AOT marshalling, and wait
  for realized content and completed layout before asserting live collection and orientation changes.

## [3.0.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v3.0.0)

DesktopFlyouts 3.0 introduces a simpler flyout model with consistent appearance, data-bound
islands, and more reliable desktop integration across WinUI 3, UWP / WinUI 2, and Uno / X11.

### Highlights

- Display data-bound island collections with item templates and selectors, and update content,
  dimensions, orientation, and visibility while a flyout is open.
- Keep tray flyouts aligned as icons, monitors, DPI, work areas, and taskbars change. Improve
  notification-area overflow placement and keep auto-hide taskbars visible while a flyout is open.
- Use system-adaptive Acrylic with solid fallbacks that respect accessibility and transparency
  settings. Add standalone UWP Composition Acrylic and Mica effects for Windows 11+.
- Improve transitions, swipe gestures, runtime theme updates, and stability during rapid
  open/close cycles and disposal. Fix fractional-DPI sizing and extra outer island gaps.
- Support AOT island collections and expand the samples with settings, flyout, and data-template
  examples. Simplify the UWP sample into a single application.

### Breaking changes and migration

- **Nonvisual definitions:** `DesktopFlyout` and `DesktopFlyoutIsland` are now `DependencyObject`
  definitions rather than UI elements. Keep the flyout on its UI thread and dispose it when done.
  Move `Resources` and content appearance settings into application resources or the content UI;
  use explicit binding sources or `x:Bind` instead of an inherited `DataContext`.
- **Library-owned appearance:** presenter styles, custom frame/container templates, backdrop
  selection, outer margins, and `PressedScale` are no longer configurable. Customize the content
  inside each island. The library provides borders, corners, shadows, and 12-DIP island spacing.
- **Sizing:** replace `FlyoutWidth` / `FlyoutHeight` with `Width` / `Height` and their min/max
  properties. Use `double.NaN` for Auto, or Auto with the corresponding alignment set to `Stretch`
  to fill available space. Use `IslandWidth` / `IslandHeight` for island allocation and set content
  dimensions on the contained UI. Frames fit the work area without overwriting requested sizes.
- **Collections:** `Islands` is now `IList<object>`. Use `IslandTemplate` / `IslandTemplateSelector`
  for data items. `IslandsSource` connects directly to the source; clear it before editing
  `Islands`. Root island collections no longer scroll; put a bounded `ScrollViewer` inside an
  island when its content needs scrolling.
- **Dismissal:** rename `HideOnLostFocus` to `IsLightDismissEnabled` in C# and XAML. The default
  remains `true`.
- **Placement:** every `Show` call now requires a `DesktopFlyoutPosition`. Update removed APIs
  using the following replacements:

| Previous API | Replacement |
| --- | --- |
| `Show()` | `Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight))` |
| `Show(Point)` | `Show(DesktopFlyoutPosition.ForAnchor(bounds, side))` |
| `DesktopFlyoutPosition.ForTaskbarAnchor(Rectangle)` | `ForTrayIcon(trayIcon)` for a live tray anchor, or `ForAnchor(bounds, side)` for fixed bounds |
| `DesktopFlyoutPosition.InWorkArea(DesktopFlyoutPlacementMode, ...)` | Use the matching `DesktopFlyoutWorkAreaPosition` member; numeric values differ, so do not cast |

See the [usage guide](docs/README.md) and [API reference](docs/api-reference.md) for the current API.

## [2.2.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v2.2.0)

- Rename the Windows App SDK library project from `DesktopFlyouts.Wasdk` to
  `DesktopFlyouts.WinUI`, and standardize sample project names by platform.
- Update solution, build, and packaging references to the renamed projects. The WinUI NuGet
  package remains `DesktopFlyouts.WinUI`.

## [2.1.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v2.1.0)

- Align flyout content and backdrop themes with the system theme.
- Update dependencies.

## [2.0.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v2.0.0)

- Introduce `DesktopFlyoutPosition` requests for explicit anchors, taskbar anchors, and work-area
  positions, including side, alignment, monitor selection, and transition direction.
- Replace the `Placement` and `PopupDirection` XAML properties with position requests supplied to
  `Show`. Use `Show(DesktopFlyoutPosition.InWorkArea(position, monitor, transitionDirection))` for
  work-area placement, or `Show(DesktopFlyoutPosition.ForAnchor(bounds, side))` for a screen anchor.
- Expose transition state and handle repeated requests during animations: defer closing during
  opening, queue reopening during closing, and allow newer requests to cancel deferred work.
- Respect the Windows animation setting and marshal setting changes to the UI thread.
- Switch NuGet publishing to Trusted Signing.

## [1.6.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v1.6.0)

- Keep CsWin32-generated interop types internal to the library.
- Fix UWP `RectInt32` namespace resolution.

## [1.5.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v1.5.0)

- Add Uno Platform support on Linux / X11, including system tray integration.
- Replace the Windows App SDK metapackage with individual component package references.

## [1.4.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v1.4.0)

- Change the public namespace from `U5BFA.Libraries` to `DesktopFlyouts`. Update C# imports and
  XAML namespace declarations to `using DesktopFlyouts;` and `using:DesktopFlyouts` respectively.
- Add left and right double-click events to `SystemTrayIcon`, expand icon construction and
  replacement APIs, and improve icon resource cleanup and path validation.
- Support runtime island visibility changes with an `ItemsControl`-based layout.
- Fix placement on the anchor's monitor, top-anchored resizing and repositioning, and freeform
  island sizing. Handle press and swipe gestures per island.
- Clear connected backdrops before disposing islands.

## [1.2.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v1.2.0)

- Rename the project and controls from TrayIconFlyout to DesktopFlyouts. Rename NuGet packages
  to `DesktopFlyouts.WinUI` and `DesktopFlyouts.Uwp`.
- Add activation modes, `AutoCloseDelay`, `PressedScale`, swipe-to-dismiss settings, and
  left-center and right-center placement options.
- Improve keyboard focus navigation and the non-activating mode.
- Add system-backdrop hosting and accent color prevalence support.
- Fix runtime resizing and overlap with auto-hide taskbars.
- Adopt Central Package Management and expand the sample applications.

## [1.0.0](https://github.com/0x5bfa/DesktopFlyouts/releases/tag/v1.0.0)

- Initial tagged release, published under the TrayIconFlyout name for WinUI 3 and UWP / WinUI 2.
- Provide desktop-hosted panel flyouts, menu flyouts, and a system tray icon helper.
