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

The original C# samples have separate XAML views and code-behind for each platform:

- [Windows App SDK / WinUI 3](samples/DesktopFlyouts.Wasdk.Sample.App/DesktopFlyouts.Wasdk.Sample.App.csproj): eight flyout examples, settings, menus, and a tray icon.
- [UWP tray host](samples/DesktopFlyouts.Uwp.Sample.TrayHost/DesktopFlyouts.Uwp.Sample.TrayHost.csproj): a desktop process hosting Windows.UI.Xaml through XAML Islands.
- [UWP secondary app](samples/DesktopFlyouts.Uwp.Sample.App/DesktopFlyouts.Uwp.Sample.App.csproj): opened from the tray menu using the sample protocol.
- [UWP packaging project](samples/DesktopFlyouts.Uwp.Sample.Packaging/DesktopFlyouts.Uwp.Sample.Packaging.wapproj): packages the tray host and secondary app together.

Open `DesktopFlyouts.slnx` and use the `x64` or `arm64` platform. Set
`DesktopFlyouts.Wasdk.Sample.App` or `DesktopFlyouts.Uwp.Sample.Packaging` as the
startup project. In the UWP sample, left-click the tray icon to toggle the flyout,
or right-click it to open the menu. Choose **Devices** to open the secondary app.

Build from the command line with Visual Studio MSBuild, because the solution
includes native C++ projects and a Windows Application Packaging Project:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe' DesktopFlyouts.slnx /restore /t:Build /p:Configuration=Debug /p:Platform=x64 /m
```

With WinApp CLI installed, launch the built WASDK sample:

```powershell
winapp run samples\DesktopFlyouts.Wasdk.Sample.App\DesktopFlyouts.Wasdk.Sample.App.csproj -c Debug --arch x64 -p Platform=x64 --no-build --detach
```

Launch the built UWP package from its layout directory:

```powershell
winapp run samples\DesktopFlyouts.Uwp.Sample.Packaging\bin\x64\Debug --manifest samples\DesktopFlyouts.Uwp.Sample.Packaging\bin\x64\Debug\AppxManifest.xml --exe DesktopFlyouts.Uwp.Sample.TrayHost\DesktopFlyouts.Uwp.Sample.TrayHost.exe --detach
```

## Building from the source

1. Prerequisites
    - Windows 10 (Build 10.0.17763.0) onwards and Windows 11
    - Visual Studio 2026 with C++ desktop and Windows application development tools
    - .NET 10 SDK
2. Clone the repo
3. Open the solution
4. Build the solution
