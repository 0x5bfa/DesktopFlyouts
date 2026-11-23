# Testing

The tests use two Windows application hosts built from the WinUI and UWP unit-testing templates:

- `DesktopFlyouts.UnitTests.WinUI` references `src/DesktopFlyouts.WinUI`.
- `DesktopFlyouts.UnitTests.Uwp` references `src/DesktopFlyouts.Uwp`.
- `DesktopFlyouts.UnitTests.Shared` is a shared project containing the common geometry and
  placement tests. Both application projects import its test sources.

Tests exercise the actual library assemblies. `InternalsVisibleTo` grants each platform's test
assembly access to its library's internal helpers; no production files under `src` are compiled
into the test applications. The UWP project additionally links the sample's collection declaration
so its WinRT interface regression test continues to cover the real sample type.

Both hosts target `net10.0-windows10.0.26100.0`. They include application XAML, package manifests,
assets, launch settings, and x86/x64/ARM64 publish profiles. WinUI also has a test window and a
Win32 application manifest. MSTest's `UnitTestClient` hosts VSTest execution, and `[UITestMethod]`
runs tests on the appropriate XAML UI thread.

## Build

Use Visual Studio's MSBuild on Windows with the Windows SDK, UWP tooling, and .NET 9/10 installed.
Run each build separately and inspect its result before continuing. From a Visual Studio
Developer PowerShell, build x64 Debug tests with:

```powershell
msbuild /restore tests/DesktopFlyouts.UnitTests.WinUI/DesktopFlyouts.UnitTests.WinUI.csproj /p:Configuration=Debug /p:Platform=x64 /p:AppxBundle=Never /p:AppxPackageSigningEnabled=false
```

```powershell
msbuild /restore tests/DesktopFlyouts.UnitTests.Uwp/DesktopFlyouts.UnitTests.Uwp.csproj /p:Configuration=Debug /p:Platform=x64 /p:AppxBundle=Never /p:AppxPackageSigningEnabled=false
```

Use `Release` for CI builds, or change `Platform` to `x86` or `ARM64` for another architecture.
Test hosts disable Native AOT publishing so VSTest can discover and execute tests through
reflection. The UWP host retains the dynamic-code-disabled runtime feature switch for its
WinRT collection marshalling regression tests.

## Run

Running either suite starts a test application and requires an interactive Windows desktop.
Enable Windows developer mode for deployment of the unsigned development layout. In Visual
Studio, build and run the tests through Test Explorer. For command-line execution after building:

```powershell
./tests/Run-Tests.ps1 -Flavor WinUI -Configuration Debug -Platform x64
```

```powershell
./tests/Run-Tests.ps1 -Flavor Uwp -Configuration Debug -Platform x64
```

To select a subset, add `-Filter 'FullyQualifiedName~DesktopFlyoutLifecycleTests'`, for example,
to the WinUI command. Test Explorer also supports selecting individual tests.

The script locates Visual Studio's VSTest runner and passes it the matching `.build.appxrecipe`,
which supplies the package deployment and activation information. It returns the runner's exit
code, fails if no tests are discovered, and writes TRX results under `tests/TestResults/<flavor>`.
Use the recipe rather than invoking a test DLL with `dotnet test` or launching its executable
outside the package context. Build-only verification does not execute any test methods.

## Coverage

The shared suite runs in both platform assemblies and covers:

- Monitor coordinates, negative origins, fractional DPI, host bounds, and invalid regions.
- Content/stretch sizing, min/max constraints, work-area clipping, and placement margins.
- Independent screen-edge gaps and shadow bounds, including tiny work areas.
- Explicit and tray anchors, taskbar edges, overflow alignment, and work-area clamping.
- Live tray-bound providers and visible auto-hide taskbar overlap on all four edges.

The WinUI suite additionally covers live flyout lifecycle, backdrop reuse and cleanup, queued
opening and dismissal, UI-thread ownership and garbage collection, bindings and sizing, live
item sources/templates, protected chrome, weighted island allocation, visibility/orientation,
scrollable island content, and clipping of overflowing collections. Its native tests create
controlled test windows to verify WinEvent filtering, coalescing, deferred callbacks, disposal,
replacement HWNDs, and rejected dispatch. These tests call the monitors in the library assembly;
test-only native calls reuse that assembly's native types.

The UWP suite additionally validates native Acrylic and Mica effect graphs on a dedicated
Composition dispatcher, including their source inputs, and queries the generated WinRT
collection interfaces on the library's island collection and the sample's data source. The Mica
tests substitute a color brush for wallpaper pixels.

These tests do not automate Explorer's tray, verify rendered pixels or animation smoothness,
reproduce every scheduling interleaving, or exercise Linux/X11 runtime behavior. UWP material
tests do not verify desktop wallpaper capture or all system fallback settings.

CI builds and runs both x64 Release application suites on Windows and uploads their TRX results.
