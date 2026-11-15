# Testing

The repository has two MSTest projects. Unit tests source-link the platform-neutral helpers. WinUI integration tests reference the actual WinUI library and source-link the internal User32 overflow helpers.

## Unit tests

The platform-neutral project targets `net10.0`. It source-links the host-layout and flyout-positioning helpers and checks:

- Screen-to-monitor coordinate mapping across negative monitor origins and multiple DPI scales, monitor-sized host bounds, and rejection of invalid scales or out-of-monitor regions.
- Taskbar-edge placement, overflow contact edges and tray-icon row or column alignment, popup margin and transition direction, explicit anchors, and work-area clamping.
- Live tray-bound providers, side changes across monitors, visible auto-hide overlap on all four edges, partially revealed bars, and combining reserved and overlay appbar areas without double subtraction.

Build and run from the repository root:

```powershell
dotnet build tests/DesktopFlyouts.UnitTests/DesktopFlyouts.UnitTests.csproj --configuration Debug
dotnet test tests/DesktopFlyouts.UnitTests/DesktopFlyouts.UnitTests.csproj --configuration Debug --no-build --no-restore
```

## WinUI integration tests

`DesktopFlyouts.WinUI.IntegrationTests` is a Windows-only WinUI test application targeting `net10.0-windows10.0.26100.0`. It uses MSTest's Microsoft.Testing.Platform runner inside a real WinUI application with a dispatcher and message loop. It includes:

- Live XAML-island lifecycle tests for Desktop Acrylic and Mica: repeated open/close preserves the connected backdrop instance; transitions run on the owning UI thread; changing the backdrop kind or enabling/disabling it updates the visible element; a close requested during opening completes and permits reopening; and disposal cancels an already-queued open.
- Forced garbage collections while backdrops are connected and after disposal, with managed `RPC_E_WRONG_THREAD` exceptions recorded as failures. A native fail-fast also fails the test process. This caught a `ContentExternalBackdropLink` being released on the finalizer thread after its element had detached it.
- The former `DesktopFlyouts.Windows.IntegrationTests` User32 tests, now in this project: overflow-window discovery, bounds after resize, hide/destroy notifications, unrelated-event filtering, exactly-once deferred completion, callback cancellation on disposal, native callback lifetime across collection, and cleanup when dispatch is rejected.
- Layout-monitor tests using test-owned windows and real WinEvents: move/hide/destroy notifications, deferred coalescing, unrelated-window filtering, replacement HWNDs, queued-callback cancellation, hook lifetime across GC, and rejected-dispatch cleanup.
- Live placement tests inject only a shell-bounds provider into the actual library: the WinUI host moves when bounds change, retains the last anchor during temporary lookup failure, preserves the backdrop and open transition, and stops sampling when closed. No Explorer registration or global work-area settings are changed by these tests.

The test app is intentionally **unpackaged** and carries its Windows App SDK runtime alongside it, so it needs neither MSIX registration nor a separately installed Windows App Runtime. This differs from the packaged sample's launch requirements. Build and run on Windows with an interactive desktop, the Windows SDK, and .NET 10 installed (x64 example):

```powershell
dotnet build tests/DesktopFlyouts.WinUI.IntegrationTests/DesktopFlyouts.WinUI.IntegrationTests.csproj --configuration Debug -p:Platform=x64
dotnet run --project tests/DesktopFlyouts.WinUI.IntegrationTests/DesktopFlyouts.WinUI.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:Platform=x64
```

Use `dotnet run` for the self-hosted MTP application; the existing VSTest-based `dotnet test` command still applies to the portable unit tests. To filter the integration suite, append `-- --filter "FullyQualifiedName~DesktopFlyoutLifecycleTests"` to the run command.

The lifecycle tests exercise the public flyout methods used by the sample's tray icon and Toggle open button. They do not automate Explorer's tray, reproduce every scheduling interleaving, capture every native first-chance notification, or verify pixels and animation smoothness. The User32 tests retain their controlled queued dispatcher and do not deterministically cover the initial discovery-to-hook race or Explorer restarts.

CI runs unit tests on Ubuntu and the combined WinUI/User32 integration suite on Windows. Both projects disable AOT publishing; the portable project uses VSTest without Windows runtime identifiers, while the WinUI app uses an architecture-specific runtime identifier and MTP.
