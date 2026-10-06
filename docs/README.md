# DesktopFlyouts documentation

DesktopFlyouts supports UWP and Windows App SDK / WinUI 3 through C++/WinRT native components
and C# projections. Its public ABI is authored for IdlGen 2.0.

## Start here

- [Getting started](getting-started.md): create flyouts and menus from an app.
- [DesktopFlyout](desktop-flyout.md): panel-style flyouts made from content and independent islands.
- [DesktopMenuFlyout](desktop-menu-flyout.md): context menus hosted at screen coordinates.
- [SystemTrayIcon](system-tray-icon.md): optional native tray icon helper.
- [Focus and activation](focus-and-activation.md): choosing activation behavior.

## Repository targets

- `src/DesktopFlyouts.Core`: deterministic geometry and interaction logic.
- `src/DesktopFlyouts.WinUI`: Windows App SDK / WinUI 3 component and host adapters.
- `src/DesktopFlyouts.Uwp`: UWP component and host adapters.
- `src/DesktopFlyouts.WinUI.Projection` and `src/DesktopFlyouts.Uwp.Projection`: C# projections.
- `samples/DesktopFlyoutsSample.WinUI`: packaged C# Windows App SDK sample.
- `samples/DesktopFlyoutsSample.Uwp`: packaged C# UWP sample.
- `samples/Shared`: common C# sample interaction and content code.
- `tests`: native unit tests, C# projection checks, and packaged runtime integration tests.

See the [sample instructions](../README.md#c-sample-apps) for building and running either sample.
