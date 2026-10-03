# Native package restore

DesktopFlyouts is restored as a native C++/WinRT Windows App SDK component. The project files restore
Windows App SDK, C++/WinRT, WebView2, and IdlGen 2.0 through NuGet; vcpkg is not required.

The public WinRT contract is authored under `src/DesktopFlyouts.WinUI/author`. Generated IDL,
implementation headers, projections, and package cache output are build artifacts.

See [the native getting-started guide](../docs/getting-started.md) and
[the C++/WinRT architecture notes](../docs/cppwinrt-migration.md).
