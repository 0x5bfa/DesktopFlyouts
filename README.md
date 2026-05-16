<h1 align="center">Tray Icon Flyout</h1>
<p align="center">Empower your app with a flyout for its tray icon in WinUI 2/3.</p>

https://github.com/user-attachments/assets/52f15ecf-6a91-491b-bf62-25294afc85d7

## Installing the package

You can consume this project via NuGet. Use NuGet Package Manager or run the following command in the Package Manager Console:

### WinUI for UWP (UWP/WinUI2)

The UWP version of sample app is currently under development. Recommend to use WinUI 3

<a style="text-decoration:none" href="https://www.nuget.org/packages/0x5BFA.TrayIconFlyout.Uwp"><img src="https://img.shields.io/nuget/v/0x5BFA.TrayIconFlyout.Uwp" alt="NuGet badge" /></a>

```console
> dotnet add package 0x5BFA.TrayIconFlyout.Uwp --prerelease
```

### WinUI (WinAppSDK/WinUI3)

<a style="text-decoration:none" href="https://www.nuget.org/packages/0x5BFA.TrayIconFlyout.WinUI"><img src="https://img.shields.io/nuget/v/0x5BFA.TrayIconFlyout.WinUI" alt="NuGet badge" /></a>

```console
> dotnet add package 0x5BFA.TrayIconFlyout.WinUI --prerelease
```

## Usage

There are two flyouts are available in this project. One is `TrayIconFlyout` for the Shell Flyout behavior, and the other is `TrayIconMenuFlyout` for the Context Menu behavior.

### TrayIconFlyout

```xml
<me:TrayIconFlyout x:Class="..." ... Width="360">

    <me:TrayIconFlyoutIsland Height="300">
        <!-- Put elements here -->
    </me:TrayIconFlyoutIsland>
    <me:TrayIconFlyoutIsland Height="300">
        <!-- Put elements here -->
    </me:TrayIconFlyoutIsland>

</me:TrayIconFlyout>
```

```cs
if (_trayIconFlyout.IsOpen)
    _trayIconFlyout.Hide();
else
    _trayIconFlyout.Show();
```

### TrayIconMeunFlyout

```xml
<me:TrayIconMenuFlyout x:Class="..." ...>

    <MenuFlyoutItem Text="Theme" />
    <MenuFlyoutItem Text="Language" />
    <MenuFlyoutItem Text="Settings" />

</me:TrayIconMenuFlyout>
```

```cs
if (_trayIconMenuFlyout.IsOpen)
    _trayIconMenuFlyout.Hide();

_trayIconMenuFlyout.Show(e.Point);
```

## Building from the source

1. Prerequisites
    - Windows 10 (Build 10.0.17763.0) onwards and Windows 11
    - Visual Studio 2022
    - .NET 9/10 SDK
2. Clone the repo
    ```console
    git clone https://github.com/0x5bfa/TrayIconFlyout.git
    ```
3. Open the solution
4. Build the solution
