# AGENTS.md

DesktopFlyouts is a library implemented in C++/WinRT that supports both UWP and Windows App SDK. It exposes lightweight desktop flyouts, menu flyouts, and tray-icon-driven UI from a native WinRT component.

- `DesktopFlyouts.Core` must stay independent of WinUI, Windows Runtime, and HWND types.
- Keep WinRT ABI, XAML, activation, backdrop, and host-window concerns in `DesktopFlyouts.Uwp` or `DesktopFlyouts.WinUI`.
- The public IdlGen source of truth is under `src/DesktopFlyouts.WinUI/author`; do not edit generated IDL, implementation headers, projection headers, `Generated Files`, `bin`, or `obj` output.
- Run validation one command at a time and inspect each result before moving on.
- MSBuild resides in `C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe`; use this for local validation (e.g. `$msbuild DesktopFlyouts.slnx /restore /t:Build /p:Configuration=Debug /p:Platform=x64 /m`).
