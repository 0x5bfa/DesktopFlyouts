<h1 align="center">Desktop Flyouts</h1>
<p align="center">WinUI library for showing desktop flyouts from tray icons or programmatically.</p>

https://github.com/user-attachments/assets/52f15ecf-6a91-491b-bf62-25294afc85d7

## Installing the package

Install the package for your XAML stack with NuGet Package Manager or the .NET CLI.

### WinUI for UWP (UWP/WinUI2)

<a style="text-decoration:none" href="https://www.nuget.org/packages/DesktopFlyouts.Uwp"><img src="https://img.shields.io/nuget/v/DesktopFlyouts.Uwp" alt="NuGet badge" /></a>

```console
> dotnet add package DesktopFlyouts.Uwp
```

### WinUI (WinAppSDK/WinUI3)

<a style="text-decoration:none" href="https://www.nuget.org/packages/DesktopFlyouts.WinUI"><img src="https://img.shields.io/nuget/v/DesktopFlyouts.WinUI" alt="NuGet badge" /></a>

```console
> dotnet add package DesktopFlyouts.WinUI
```

### Uno Platform (Linux / X11)

<a style="text-decoration:none" href="https://www.nuget.org/packages/DesktopFlyouts.Uno"><img src="https://img.shields.io/nuget/v/DesktopFlyouts.Uno" alt="NuGet badge" /></a>

```console
> dotnet add package DesktopFlyouts.Uno
```

## Documentation

- [Usage guide](docs/README.md)
- [API reference](docs/api-reference.md)
- [Changelog](CHANGE_LOG.md)

## Usage

This project provides `DesktopFlyout` for lightweight desktop panels and `DesktopMenuFlyout` for context menu behavior.

### DesktopFlyout

```xml
<me:DesktopFlyout
	x:Class="MyApp.MainDesktopFlyout"
	xmlns:me="using:DesktopFlyouts"
	Width="360">

	<me:DesktopFlyoutIsland IslandHeight="300">
		<!-- Put elements here -->
	</me:DesktopFlyoutIsland>
	<me:DesktopFlyoutIsland IslandHeight="300">
		<!-- Put elements here -->
	</me:DesktopFlyoutIsland>

</me:DesktopFlyout>
```

Create and retain the flyout on the UI thread, and dispose it when no longer needed:

```cs
private readonly MainDesktopFlyout _desktopFlyout = new();

if (_desktopFlyout.TransitionState is not DesktopFlyoutTransitionState.Closed)
	_desktopFlyout.Hide();
else
	_desktopFlyout.Show(DesktopFlyoutPosition.InWorkArea(DesktopFlyoutWorkAreaPosition.BottomRight));
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
    - Windows 10 (Build 10.0.17763.0) onwards or Windows 11
    - Visual Studio with the Windows application build tools for your target
    - .NET 10 SDK
2. Clone the repo
3. Open the solution
4. Build the solution
