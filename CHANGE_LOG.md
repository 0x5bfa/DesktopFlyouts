# Changelog

Release sections follow the repository's version tags. Changes after the latest tagged release
belong in **Unreleased** until the next release is tagged.

## Unreleased

### Fixed

- Generate WinRT collection interfaces for direct flyout islands and the sample data source.
  This prevents startup ArgumentException (0x80070057) when assigning ItemsSource with dynamic
  code support disabled, as in the samples' AOT configuration.
- Reserve 12-DIP gaps between islands and 12-DIP outer insets in the islands panel so the
  shadows have room without adding extra gaps. Expand the scroll clip into the host's shadow
  allowance, keep the presenter at zero padding and margin, and reconnect WinUI backdrop
  targets after the flyout host becomes visible.
- Use the XAML island window's DPI when mapping flyout regions between physical pixels and DIPs,
  including fractional display scales.

### Breaking changes

- Disable scrolling of the root island collection. Content outside the visible viewport is clipped;
  use a `ScrollViewer` inside an island for scrollable content.
- Make `DesktopFlyout` a nonvisual `DependencyObject, IDisposable` for v3. Keep Show, Hide,
  navigation, state, activation, dismissal, and disposal on that type. The visual presenter is
  now internal and created by the flyout. Remove external presenter construction and styles.
  Existing derived flyout class names and XAML roots can stay, but move Resources to application
  resources or content elements; use explicit binding sources or compiled bindings on the flyout.
- Make `DesktopFlyoutIsland` a nonvisual content/allocation definition. Move inherited Width,
  Height, padding, and content appearance settings to its contained UI, or use IslandWidth and
  IslandHeight for allocation. Remove TemplateSettings, Background, border/corner/shadow styling,
  frame templates, outer margin, BackdropKind, PressedScale, MenuFlyout, and IsBackdropEnabled
  configuration from flyouts. The library owns all outer chrome and uses system-adaptive Acrylic
  or a platform fallback. Content templates may still customize the interior.
- Replace FlyoutWidth/FlyoutHeight with the flyout's own Width/Height DPs (double, NaN = Auto),
  including Min/Max dimensions. Replace an overall star size with an Auto dimension and the
  corresponding HorizontalAlignment/VerticalAlignment set to Stretch. Child island stars no
  longer implicitly stretch Auto parents. Requested values and bindings survive viewport clipping.
- Widen Islands to IList<object>; IslandsSource now connects to an enumerable without copying.
  Move arbitrary data display into IslandTemplate or IslandTemplateSelector, or use a definition's
  ContentTemplate. Direct Islands edits throw while IslandsSource is set; clear the source to
  restore direct items. Background, frame, and container-style overrides have no public API.
- Bound the actual frame to the available work area while preserving requested dimensions and
  bindings, and give scrollable island content a finite measurement budget.
- Use a 12-DIP screen-edge gap and a separate 12-DIP shadow margin. Remove
  flyout Margin/PlacementMargin overrides. Move padding intended for content inside its UI tree.
  Construction is host-free; the first show with a resolvable position creates the native host.

- Remove `DesktopFlyout.Show(System.Drawing.Point)`. Use
  `flyout.Show(DesktopFlyoutPosition.ForAnchor(bounds, side))` for an explicit screen anchor,
  or `flyout.Show(DesktopFlyoutPosition.ForTrayIcon(trayIcon))` for a tray icon.
- Remove parameterless `DesktopFlyout.Show()`. Every flyout show request must specify a
  `DesktopFlyoutPosition`. To retain bottom-right placement on the primary monitor, use
  `flyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight))`.
- Remove `DesktopFlyoutPosition.ForTaskbarAnchor(Rectangle)`. Use `ForTrayIcon` to follow a live
  tray icon, or `ForAnchor` for fixed coordinates with an explicit side.
- Remove the obsolete `DesktopFlyoutPosition.InWorkArea(DesktopFlyoutPlacementMode, ...)` overload
  and `DesktopFlyoutPlacementMode`. Use `InWorkArea(DesktopFlyoutWorkAreaPosition, ...)` and replace
  each old enum member with the same named member of `DesktopFlyoutWorkAreaPosition`; their numeric
  values differ, so do not migrate by casting.

### Added

- Add UWP Composition Acrylic and wallpaper-based Mica primitives on Windows 11+. Desktop
  flyouts use Acrylic internally; standalone Mica uses the blurred desktop wallpaper with
  luminosity and tint, without Acrylic noise.
- Add the WinUI sample's settings screen and the flyout examples to the UWP sample. Marshal
  settings and tray operations to the XAML thread, serialize example replacement, and release
  flyout/menu hosts before closing the settings window. Add a data-template example to all three
  samples and keep chrome configuration internal.

- Consolidate the UWP sample executable, MSIX manifest, and assets in
  `samples/DesktopFlyouts.SampleApp.Uwp`; remove the separate tray host, packaging project,
  and secondary application/protocol demo.

- Add `DesktopFlyoutPosition.ForTrayIcon(SystemTrayIcon)` with live icon-bound resolution.
- Follow monitor work-area, taskbar, registered appbar, and visible auto-hide bar changes while a
  flyout is open. Windows display/work-area/DPI messages and WinEvents coalesce layout updates;
  a 250 ms sampler while opening or visible covers missed shell notifications and icon movement.
- Add portable geometry tests and WinUI lifecycle, backdrop, live-placement, and native-event tests.
  Consolidate `DesktopFlyouts.Windows.IntegrationTests` into `DesktopFlyouts.WinUI.IntegrationTests`.

### Fixed

- Measure the whole visual flyout using standard XAML dimension constraints and reuse that
  layout for placement, the native viewport, and transition distances. Update open layouts for
  root sizing, alignment, collection changes, and island allocation changes, retaining
  updates made during opening. Auto islands receive finite work-area constraints so bounded
  scroll content can establish a viewport. Update all three sample flavors to the nonvisual
  owner and fixed chrome.
- Stop applying WinUI 2 Mica to `Window.Current` for UWP XAML-island flyouts. Keep flyout
  material inside library-owned island containers. Replace shared in-app Acrylic resource
  aliases with per-island Composition effects and enable
  host-backdrop sampling on the Win32 host, avoiding standard AcrylicBrush's activation fallback.
  Add the Win2D UWP dependency, respect transparency/high contrast/energy saver settings, release
  effect resources on disconnection. Use Acrylic for consistent desktop flyout chrome.
  Older Windows versions and unavailable backdrop sources use a solid fallback.
- Stop transition and gesture storyboards before disposing a flyout host so their completion
  callbacks cannot run against a torn-down XAML island.
- Keep flyout opening and backdrop cleanup on the owning UI thread. Reuse system backdrops across
  open/close cycles and dispose detached `ContentExternalBackdropLink` instances on that thread,
  preventing owning-thread errors during rapid toggles and garbage collection. Cancel queued
  opening work when the flyout is disposed.
- Keep monitor-sized host geometry stable during content resizing.
- Place tray flyouts against the full notification-area overflow window, align with the clicked
  icon's row or column, avoid an extra transparent gap, and reposition when the overflow closes.
- Keep an auto-hide taskbar revealed while its tray-anchored flyout is visible.
- Apply activation mode before showing the host and activate visible islands when requested.
- Respond to system theme changes at runtime.

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
