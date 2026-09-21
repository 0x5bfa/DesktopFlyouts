<h1 align="center">Desktop Flyouts</h1>
<p align="center">WinUI library for showing desktop flyouts from tray icons or programmatically.</p>

https://github.com/user-attachments/assets/52f15ecf-6a91-491b-bf62-25294afc85d7

## Installing the package

You can consume this project via NuGet. Use NuGet Package Manager or run the following command in the Package Manager Console:

### WinUI for UWP (UWP/WinUI2)

The UWP version of sample app is currently under development. Recommend to use WinUI 3

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

## Building from the source

1. Prerequisites
    - Windows 10 (Build 10.0.17763.0) onwards and Windows 11
    - Visual Studio 2022
    - .NET 9/10 SDK
2. Clone the repo
3. Open the solution
4. Build the solution
