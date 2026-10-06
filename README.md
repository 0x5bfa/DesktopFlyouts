<h1 align="center">Desktop Flyouts</h1>
<p align="center">WinUI library for showing desktop flyouts from tray icons or programmatically.</p>

https://github.com/user-attachments/assets/52f15ecf-6a91-491b-bf62-25294afc85d7

## Installing the package

You can consume this project via NuGet. Use NuGet Package Manager or run the following command in the Package Manager Console:

### WinUI for UWP (UWP/WinUI2)

For .NET 10 applications, set `<UseUwp>true</UseUwp>` in the application project.
The library uses the SDK's `Windows.UI.Xaml` projection so that XAML controls have
the same managed types in the application and the library.

<a style="text-decoration:none" href="https://www.nuget.org/packages/DesktopFlyouts.Uwp"><img src="https://img.shields.io/nuget/v/DesktopFlyouts.Uwp" alt="NuGet badge" /></a>

```console
> dotnet add package DesktopFlyouts.Uwp
```

### WinUI (WinAppSDK/WinUI3)

<a style="text-decoration:none" href="https://www.nuget.org/packages/DesktopFlyouts.WinUI"><img src="https://img.shields.io/nuget/v/DesktopFlyouts.WinUI" alt="NuGet badge" /></a>

```console
> dotnet add package DesktopFlyouts.WinUI
```

## Documentation

- [Getting started](docs/getting-started.md)
- [DesktopFlyout](docs/desktop-flyout.md)
- [DesktopMenuFlyout](docs/desktop-menu-flyout.md)
- [SystemTrayIcon](docs/system-tray-icon.md)
- [Focus and activation](docs/focus-and-activation.md)

## Usage

This project provides `DesktopFlyout` for lightweight desktop panels and `DesktopMenuFlyout` for context menu behavior.

### DesktopFlyout

```xml
<me:DesktopFlyout
    x:Class="..."
    xmlns:me="using:DesktopFlyouts"
    FlyoutWidth="360">

    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>
    <me:DesktopFlyoutIsland IslandHeight="300">
        <!-- Put elements here -->
    </me:DesktopFlyoutIsland>

</me:DesktopFlyout>
```

```cs
if (_desktopFlyout.IsOpen)
    _desktopFlyout.Hide();
else
    _desktopFlyout.Show();
```

### DesktopMenuFlyout

```xml
<me:DesktopMenuFlyout
    x:Class="..."
    xmlns:me="using:DesktopFlyouts">

    <MenuFlyoutItem Text="Theme" />
    <MenuFlyoutItem Text="Language" />
    <MenuFlyoutItem Text="Settings" />

</me:DesktopMenuFlyout>
```

```cs
if (_desktopMenuFlyout.IsOpen)
    _desktopMenuFlyout.Hide();

_desktopMenuFlyout.Show(e.Point);
```

## C# sample apps

Both samples use the library's C# projection and share their flyout examples:

- [Windows App SDK / WinUI 3](samples/DesktopFlyoutsSample.WinUI/DesktopFlyoutsSample.WinUI.csproj)
- [UWP](samples/DesktopFlyoutsSample.Uwp/DesktopFlyoutsSample.Uwp.csproj)

Open `DesktopFlyouts.slnx`, select either sample as the startup project, and run it with
the `x64` or `arm64` platform. Both demonstrate eight content examples, placement,
popup direction, activation, backdrops, swipe dismissal, auto-close, islands, menus,
and tray icons. The UWP sample uses .NET 10's UWP support and hosts `Windows.UI.Xaml`
in a desktop window through XAML Islands, as required by the desktop flyout component.
Its numeric settings use standard UWP text boxes instead of WinUI 3's `NumberBox`.

With WinApp CLI installed, the scripts build with Visual Studio MSBuild and launch
the packaged app:

```powershell
.\samples\DesktopFlyoutsSample.WinUI\Run-DesktopFlyoutsSample.ps1
.\samples\DesktopFlyoutsSample.Uwp\Run-DesktopFlyoutsSample.ps1
```

Each script accepts `-Configuration Debug|Release` and `-Platform x64|arm64`.
Use Visual Studio MSBuild when building from the command line because the samples
reference the native C++ projects as well as the managed projections.

## Building from the source

1. Prerequisites
    - Windows 10 (Build 10.0.17763.0) onwards and Windows 11
    - Visual Studio 2026 with C++ desktop and Windows application development tools
    - .NET 10 SDK
2. Clone the repo
3. Open the solution
4. Build the solution
