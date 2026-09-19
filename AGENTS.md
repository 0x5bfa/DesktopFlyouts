# AGENTS.md

DesktopFlyouts is a Windows App SDK / WinUI 3 library implemented in C++/WinRT. It exposes lightweight
desktop flyouts, menu flyouts, and tray-icon-driven UI from a native WinRT component.

## Code structure

```text
.
├── DesktopFlyouts.slnx
├── src
│   ├── DesktopFlyouts.Core       # WinRT/WinUI-independent geometry and state logic
│   └── DesktopFlyouts.WinUI      # IdlGen 2.0 component, HWND/XAML host, visuals, and tray support
├── samples
│   └── DesktopFlyoutsSample.WinUI
└── tests
    ├── DesktopFlyouts.Core.Tests
    ├── Run-DesktopFlyoutsSampleUiTests.ps1
    └── Test-NativeWinRTContract.ps1
```

`DesktopFlyouts.Core` must stay independent of WinUI, Windows Runtime, and HWND types. Keep WinRT ABI,
XAML, activation, backdrop, and host-window concerns in `DesktopFlyouts.WinUI`. The public IdlGen source
of truth is under `src/DesktopFlyouts.WinUI/author`; do not edit generated IDL, implementation headers,
projection headers, `Generated Files`, `bin`, or `obj` output.

## Formatting and editing rules

- This repository uses UTF-8 with CRLF line endings and a final newline.
- `.editorconfig` and `.gitattributes` define the repository-wide line-ending rules.
- Use `apply_patch` for source edits and preserve existing style.
- Keep public ABI changes in the authored IdlGen headers and update the WinRT contract test with them.
- Avoid unrelated refactors while changing focused behavior.

## Build commands

Run validation one command at a time and inspect each result before moving on. Native builds require
Windows, Visual Studio, the Windows SDK, and NuGet restore. vcpkg is not required; IdlGen 2.0 is restored
from the native NuGet package `IdlGen.IdlGen.Cpp`.

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild DesktopFlyouts.slnx /restore /t:Build /p:Configuration=Debug /p:Platform=x64 /m
```

The solution builds the Core library, WinUI component, packaged native sample, and Core tests. For a
focused component build:

```powershell
& $msbuild src\DesktopFlyouts.WinUI\DesktopFlyouts.WinUI.vcxproj /restore /t:Build /p:Configuration=Debug /p:Platform=x64
```

## Packaged sample launch

The sample is packaged and must be launched through the package-aware script. Do not validate it by
starting the generated `.exe` directly.

```powershell
& .\samples\DesktopFlyoutsSample.WinUI\Run-DesktopFlyoutsSample.ps1 -Configuration Debug
```

The script stages the package layout and starts it through `winapp run`. A successful launch has a live
`DesktopFlyoutsSample.WinUI` process with `Responding = True` and a non-zero `MainWindowHandle`.
Stop the process before rebuilding if Visual Studio reports locked output files:

```powershell
Get-Process -Name DesktopFlyoutsSample.WinUI -ErrorAction SilentlyContinue | Stop-Process -Force
```

## Testing

Run the deterministic Core tests and the generated metadata contract separately from the packaged UI
interaction tests. The UI script uses Windows UI Automation and requires a running sample process.

```powershell
& .\tests\Test-NativeWinRTContract.ps1
& .\tests\Run-DesktopFlyoutsSampleUiTests.ps1 -AppPid $app.Id
```

For accessibility, use the UI Automation assertions as the automated desktop contract and validate the
visual tree with Accessibility Insights for Windows. `axe-core` targets web DOM and is not the primary
validator for WinUI desktop UI.

## Validation checklist

1. Run `git status --short --branch` and preserve unrelated working-tree changes.
2. Build `DesktopFlyouts.slnx` for `Debug|x64`.
3. Run the native sample through `Run-DesktopFlyoutsSample.ps1` and verify a responsive process/window.
4. Run the WinRT metadata and UI Automation checks that are applicable to the change.
5. Run `git diff --check`.
6. Verify all changed text files are CRLF with a final newline.
7. Summarize what changed, what was validated, and any remaining runtime risk.
