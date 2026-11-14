# Testing

The repository has two MSTest projects. Both compile the production source files they cover directly into the test assembly, so the checks exercise current implementation code.

## Unit tests

The platform-neutral project targets `net10.0`. It source-links the host-layout and flyout-positioning helpers and checks:

- Screen-to-monitor coordinate mapping across negative monitor origins and multiple DPI scales, monitor-sized host bounds, and rejection of invalid scales or out-of-monitor regions.
- Taskbar-edge placement, overflow contact edges and tray-icon row or column alignment, popup margin and transition direction, explicit anchors, and work-area clamping.

Build and run from the repository root:

```powershell
dotnet build tests/DesktopFlyouts.UnitTests/DesktopFlyouts.UnitTests.csproj --configuration Debug
dotnet test tests/DesktopFlyouts.UnitTests/DesktopFlyouts.UnitTests.csproj --configuration Debug --no-build --no-restore
```

## Windows integration tests

The Windows-only project targets `net10.0-windows10.0.26100.0`. It source-links the production overflow-window discovery and monitor code, creates test-owned top-level windows, and exercises real User32 window operations and WinEvent hooks. It checks discovery of matching visible windows, current bounds after resize, hide and destroy notifications, filtering of unrelated events, exactly-once deferred completion, callback cancellation on disposal, native callback lifetime across garbage collection, and cleanup when dispatch is rejected.

The dispatcher is a controlled queued callback used by the test; it is not the WinUI dispatcher. The tests do not deterministically cover the initial discovery-to-hook race or Explorer restarts. They do not launch the sample app or guarantee rendered-frame behavior or absence of visual flicker.

Run on Windows with the Windows SDK and .NET 10 installed:

```powershell
dotnet build tests/DesktopFlyouts.Windows.IntegrationTests/DesktopFlyouts.Windows.IntegrationTests.csproj --configuration Debug
dotnet test tests/DesktopFlyouts.Windows.IntegrationTests/DesktopFlyouts.Windows.IntegrationTests.csproj --configuration Debug --no-build --no-restore
```

CI runs unit tests on Ubuntu and native integration tests on Windows. Both projects inherit repository compiler settings while disabling AOT publishing and Windows runtime identifiers for framework-dependent VSTest execution.
