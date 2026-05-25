# C++/WinRT Port Progress

Updated: 2026-05-25

## Goal

Port the WinUI implementation in `src/DesktopFlyouts.Shared` to native C++/WinRT in
`src-cpp/DesktopFlyouts.Shared`, while keeping the public WinRT surface usable from
the C# Windows App SDK sample through C#/WinRT projection.

## Status

| Area | Native implementation | State | Notes |
| --- | --- | --- | --- |
| Public WinRT API | `DesktopFlyouts.idl` | In progress | `DesktopFlyout.Islands` exposes `IVector<T>` and its dependency property for XAML content collection projection; runtime validation remains. |
| `SystemTrayIcon` | `SystemTrayIcon.cpp`, `SystemTrayIcon.h` | Reviewed | Notification icon behavior matches the C# path; the native window callback now contains exceptions before crossing `WNDPROC`. |
| XAML island host | `XamlIslandHostWindow.cpp`, `XamlIslandHostWindow.h` | In progress | Host-window path exists. `NeverActivate` now protects XAML child windows and thread activation attempts; validation is pending. |
| `DesktopFlyout` | `DesktopFlyout.cpp`, `DesktopFlyout.h` | In progress | The typed XAML content collection is now stored as a dependency-property value, following WinUI `MenuBar`/`MenuBarItem`; focus/backdrop parity remains. |
| `DesktopFlyoutIsland` | `DesktopFlyoutIsland.cpp`, `DesktopFlyoutIsland.h` | In progress | `TemplateSettings` binding metadata is now declared for the backdrop template path; behavior validation remains. |
| `DesktopMenuFlyout` | `DesktopMenuFlyout.cpp`, `DesktopMenuFlyout.h` | In progress | Native implementation uses show-time menu rebuilding and avoids opening for an empty `Items` collection. |
| Helpers and template settings | `FlyoutHelpers.*`, `DesktopFlyoutIslandTemplateSettings.*`, `MouseEventReceivedEventArgs.*` | Needs review | Native files exist; C# behavior comparison remains. |
| Resources and packaging | Windows App SDK/project files and XAML resources | Needs validation | C#/WinRT consumption, component activation, and XAML resource loading need end-to-end verification. |

## Current Pass

`XamlIslandHostWindow` was extended for `DesktopFlyoutActivationMode::NeverActivate`:

- applies the no-activate behavior to the host, XAML island, and discovered child HWNDs;
- subclasses island HWNDs to reject mouse activation and focus changes while in `NeverActivate`;
- installs a UI-thread CBT hook to block activation/focus of owned flyout windows created below the island;
- removes hooks and subclasses when activation mode changes or the host is closed.

`SystemTrayIcon` was validated against the C# implementation:

- preserved the existing native notification icon, tooltip, visibility, restart, and click-event behavior;
- added an exception boundary around instance dispatch from the native window procedure, since `Show()` and WinRT event handlers may fail and must not unwind through `WNDPROC`.

`DesktopFlyout` property/content handling was reviewed:

- confirmed that `FlyoutWidth`, `FlyoutHeight`, and the typed `Islands` content property are declared in IDL and registered in native code;
- changed `IslandsSource` synchronization to accept `IIterable<DesktopFlyoutIsland>`, so projected C# enumerable collections are not limited to `IObservableVector<DesktopFlyoutIsland>`.
- changed the public `Islands` getter from `IObservableVector<DesktopFlyoutIsland>` to `IVector<DesktopFlyoutIsland>` so C#/WinRT projects the XAML content collection as `IList<DesktopFlyoutIsland>`, matching the C# implementation; the native backing vector remains observable.
- added `IslandsProperty` and moved the observable vector from a native field into the dependency-property value; the getter and native collection consumers retrieve the vector through `Islands()`, matching the WinUI `MenuBar`/`MenuBarItem` content collection pattern.
- added `Microsoft.UI.Xaml.Data.Bindable` metadata to `DesktopFlyoutIsland` and `DesktopFlyoutIslandTemplateSettings`, because the island control template resolves `TemplateSettings.BackdropCornerRadius` and `TemplateSettings.SystemBackdrop` using `{Binding}`.

## Known Issues And Risks

- No build or runtime verification has been performed for the current native edits.
- The `DesktopFlyoutIsland` XAML content-add failure has a targeted projection fix, but it still requires runtime validation.
- The `SystemBackdropElement.CornerRadius` binding-assignment failure has a targeted bindable-metadata fix, but it still requires runtime validation after regenerating metadata.
- `NeverActivate` should be tested with interactive XAML content and nested popups/menu flyouts after native activation and projection issues are cleared.
- `DesktopFlyout` still needs parity work for C# focus suppression and inactive-backdrop behavior.

## Next Review Order

1. Audit `DesktopFlyoutIsland` and its template settings against the C# source.
2. Complete `DesktopFlyout` focus suppression and backdrop parity.
3. Verify C#/WinRT projection packaging and XAML resource/runtime activation layout.
4. Run the Windows App SDK sample and test activation, focus, menu, and dismissal behavior when builds are requested.
