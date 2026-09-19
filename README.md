# DesktopFlyouts

DesktopFlyouts is a Windows App SDK / WinUI 3 library for lightweight desktop flyouts, menu flyouts,
and tray-icon-driven UI. The implementation in this repository is C++/WinRT native and uses IdlGen 2.0
for its public WinRT ABI.

## Project layout

- `src/DesktopFlyouts.Core`: platform-independent placement, sizing, lifecycle, and interaction logic.
- `src/DesktopFlyouts.WinUI`: the IdlGen 2.0 WinRT component, HWND/XAML host, visuals, backdrops, and
  tray integration.
- `samples/DesktopFlyoutsSample.WinUI`: packaged Windows App SDK sample covering the supported
  scenarios.
- `tests`: Core unit tests, WinRT metadata contract checks, and UI Automation interaction checks.

The authored public ABI is under `src/DesktopFlyouts.WinUI/author`. Generated IDL, implementation
headers, projections, and build output are not source files and must not be edited or committed.

## Prerequisites

- Windows 10 19041 or later (Windows 11 recommended)
- Visual Studio 2026 with Desktop C++ and Windows App SDK tooling
- Windows SDK 10.0.26100.0 or later
- NuGet restore access

vcpkg is not required. IdlGen 2.0 and the Windows App SDK native dependencies are restored through the
MSBuild/NuGet project configuration.

## Build

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild DesktopFlyouts.slnx /restore /t:Build /p:Configuration=Debug /p:Platform=x64 /m
```

## Run the packaged sample

Do not start the generated executable directly. Use the package-aware launcher:

```powershell
& .\samples\DesktopFlyoutsSample.WinUI\Run-DesktopFlyoutsSample.ps1 -Configuration Debug
```

The launcher stages the package layout and starts the sample with `winapp run`.

## Test

```powershell
& .\tests\Test-NativeWinRTContract.ps1
$app = Get-Process -Name DesktopFlyoutsSample.WinUI | Select-Object -First 1
& .\tests\Run-DesktopFlyoutsSampleUiTests.ps1 -AppPid $app.Id
```

The UI tests use Windows UI Automation. For visual and accessibility review, use Accessibility Insights
for Windows; `axe-core` is a web-DOM tool and is not the primary validator for WinUI desktop UI.

## Documentation

- [C++/WinRT architecture and migration notes](docs/cppwinrt-migration.md)
- [Documentation index](docs/README.md)
