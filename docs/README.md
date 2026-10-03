# DesktopFlyouts documentation

DesktopFlyouts is a Windows App SDK / WinUI 3 library implemented as a C++/WinRT native component.
Its public ABI is authored for IdlGen 2.0 and projected through the generated `winrt/DesktopFlyouts.h`.

## Start here

- [Getting started](getting-started.md): build the native component and use it from a C++/WinRT app.
- [DesktopFlyout](desktop-flyout.md): panel-style flyouts made from content and independent islands.
- [DesktopMenuFlyout](desktop-menu-flyout.md): context menus hosted at screen coordinates.
- [SystemTrayIcon](system-tray-icon.md): optional native tray icon helper.
- [Focus and activation](focus-and-activation.md): choosing activation behavior.

## Repository targets

- `src/DesktopFlyouts.Core`: deterministic geometry and interaction logic.
- `src/DesktopFlyouts.WinUI`: Windows App SDK / WinUI 3 component and host adapters.
- `samples/DesktopFlyoutsSample.WinUI`: packaged sample scenarios.
- `tests`: Core unit tests, WinRT metadata checks, and UI Automation interaction tests.

The old C# WASDK, UWP, and Uno projects are not part of this native-only branch.
