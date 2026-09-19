# Getting started

DesktopFlyouts is consumed as a C++/WinRT Windows App SDK component. Build the solution first so
IdlGen 2.0 generates the WinRT metadata and projection headers:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild DesktopFlyouts.slnx /restore /t:Build /p:Configuration=Debug /p:Platform=x64 /m
```

Include the generated projection in a C++/WinRT consumer:

```cpp
#include <winrt/DesktopFlyouts.h>
```

Create a flyout on the UI thread and assign the owner window handle before showing it:

```cpp
auto flyout = winrt::DesktopFlyouts::DesktopFlyout{};
flyout.OwnerWindowHandle(reinterpret_cast<std::int64_t>(windowHandle));
flyout.FlyoutWidth({ 360.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
flyout.FlyoutHeight({ 280.0, Microsoft::UI::Xaml::GridUnitType::Pixel });
flyout.ActivationMode(winrt::DesktopFlyouts::DesktopFlyoutActivationMode::no_activate_on_open);
flyout.Content(contentElement);
flyout.Show();
```

Use `Hide()` to close it. Keep the object on the creating UI thread because the component owns WinUI
and XAML-island objects and is intentionally non-agile.

## Show from a screen point

Use `ShowAt(x, y)` when the caller already has a physical screen coordinate, such as the center of a
tray icon:

```cpp
flyout.ShowAt(screenPoint.X, screenPoint.Y);
```

## Add independent islands

`Islands()` is an observable vector of `Microsoft::UI::Xaml::UIElement`. Each element becomes an
independent floating surface:

```cpp
flyout.Islands().Append(firstElement);
flyout.Islands().Append(secondElement);
flyout.IslandSpacing(8);
flyout.IslandsOrientation(winrt::DesktopFlyouts::DesktopFlyoutOrientation::vertical);
flyout.Show();
```

## Run the sample

The sample is packaged. Use the package-aware launcher rather than starting the generated executable:

```powershell
& .\samples\DesktopFlyoutsSample.WinUI\Run-DesktopFlyoutsSample.ps1 -Configuration Debug
```

The sample includes flyout, menu, tray, backdrop, animation, auto-close, swipe, focus, sizing, and
multiple-island scenarios. UI Automation checks can be run against its process with
`tests/Run-DesktopFlyoutsSampleUiTests.ps1`.
