# C++/WinRT native MUX implementation

このディレクトリは、Windows App SDK / WinUI 3 (MUX) 版の C++/WinRT native implementation です。
リポジトリの実装と公開サンプルは native project に統一し、`DesktopFlyouts.slnx` から直接
ビルドできる構成にしています。

## Project boundary

- `DesktopFlyouts.Core`: WinRT、WinUI、HWND に依存しない決定論的な配置計算。
- `DesktopFlyouts.WinUI`: IdlGen 2.0 で生成する WinRT component。HWND popup と
  `DesktopWindowXamlSource` を組み合わせ、`DesktopFlyout` を公開する。
- `DesktopFlyoutsSample.WinUI`: framework-dependent WASDK packaged sample。
- `DesktopFlyouts.Core.Tests`: Core の C++ unit tests。
- `tests/Run-DesktopFlyoutsSampleUiTests.ps1`: WinApp UI Automation による表示、Close、Hide、Esc、
  UIA tree、screenshot の interaction checks。
- `tests/Test-NativeWinRTContract.ps1`: 生成済み winmd を `winmdidl.exe` で読み戻し、runtime
  class、generic vector、enum、TimeSpan、method の metadata contract を確認する。

現在の native MUX core は、配置、DPI-aware なサイズ解決、`GridLength` の Auto/Pixel/Star、screen-point
起点の `ShowAt`、activation policy、lost-focus、任意の WinUI `UIElement` content、
`IObservableVector<UIElement>` による island 配列、backdrop、開閉アニメーション、pressed scale、
swipe dismiss、auto-close、`DesktopMenuFlyout`、`SystemTrayIcon`、XAML island の UIA tree
まで実装しています。`DesktopFlyoutIsland` という C# の cross-runtime class を public vector の
要素型として再現する設計は IdlGen 2.0 の author/projected type 境界に合わないため、native ABI は
まず `UIElement` を要素型にしています。island の visual contract は WinUI layer の surface として
扱い、将来別の native `DesktopFlyoutIsland` runtime class を追加する場合もこの ABI を壊さない形で
version 追加します。native slice の legacy `Width`/`Height` は互換用の physical pixel API として残しつつ、
通常の `FlyoutWidth`/`FlyoutHeight` は C# 版と同じ `GridLength`、DIP、Auto/Star sizing を使います。

このブランチの公開実装は Windows App SDK / WinUI 3 native component に統一しています。

## Native layer ownership

```text
DesktopFlyouts.Core
  placement + popup direction + lifecycle + swipe decision
          |
DesktopFlyouts.WinUI
  IdlGen authored ABI + HWND host + DesktopWindowXamlSource + XAML visual tree
          |
sample / package
```

`DesktopFlyout.cpp` は public WinRT runtime class の薄い façade とし、責務を次の private adapter
へ分けています。

- `DesktopFlyoutHost.*`: HWND、owner、activation/lost-focus、timer、`DesktopWindowXamlSource`
  の生成と破棄。
- `DesktopFlyoutVisual.*`: XAML content/islands、AutomationProperties、backdrop、pointer input、
  pressed scale、C# と同じ Storyboard transition visual。
- `DesktopFlyout.cpp`: IdlGen property、Core の配置、lifecycle と Host/Visual の調停。

UI/WinRT layer から Core へ逆依存させません。`DesktopFlyout` は XAML object を持つため
UI-thread-bound です。
IdlGen 2.0 の `author::internal<winrt::non_agile>` で生成 C++ implementation を non-agile にし、
public method では作成 thread 以外を `RPC_E_WRONG_THREAD` として拒否します。

開閉 transition は C# の `TransitionHelpers` と同じ `DiscreteDoubleKeyFrame` +
`SplineDoubleKeyFrame` を使用し、`TranslateX/TranslateY` のみをアニメーションします。C# と同じ
vertical open 267 ms、vertical close 200 ms、horizontal 167 ms、および default margin 12 の
closed offset を使います。native 独自の opacity/scale transition は入れていません。

native の default surface は、C# WASDK template と同じ外周契約を WinUI layer で実装しています。
root は透明なままにし、各 island を独立した角丸 surface として構成し、
`SurfaceStrokeColorDefaultBrush` の 1px border、`ThemeShadow`、per-island の
`SystemBackdropElement`、`FlyoutOverlayBackgroundBrush` を適用します。したがって island 間の
余白は一枚の host 背景ではなく、デスクトップへ抜ける透明な余白です。sample の内側 content は
動作確認用であり、製品側の typography、localized strings、high-contrast resource は将来
`Controls/`、`Themes/`、`Resources/` へ分離します。

非アクティブ化時の backdrop lifetime も C# と同じです。host の `WM_ACTIVATE/WA_INACTIVE` は
close storyboard を開始するだけで、`SystemBackdropElement` はその間 visual tree に接続したまま
にします。close の `Storyboard.Completed` 後に host を隠し、root から island surface を外してから
各 backdrop を null にします。C# の template にある `Translation.Z=36` は native XAML Island
bridge で UIA だけ残して描画を空白にするため、この実装では採用せず、`ThemeShadow` と独立 surface
で同じ浮遊面を表現しています。

## IdlGen 2.0 contract

`src/DesktopFlyouts.WinUI/author/DesktopFlyout.author.h` が public WinRT API の source of truth です。
IdlGen-generated `.idl`、implementation files、projection headers は `Generated Files` または
`obj` の build artifact であり、commit しません。

IdlGen 2.0 は native NuGet package `IdlGen.IdlGen.Cpp` として管理し、version `0.2.18` は
`DesktopFlyouts.WinUI.vcxproj` の native `PackageReference` で明示しています。C++ native
project では Visual Studio の restore と root の中央パッケージ管理を混在させず、package が
ユーザーの NuGet cache に復元されるようにしています。vcpkg はこの依存関係には不要です。

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild src\DesktopFlyouts.WinUI\DesktopFlyouts.WinUI.vcxproj `
    /restore /t:Build /p:Configuration=Debug /p:Platform=x64
```

## Build and run

`Run-DesktopFlyoutsSample.ps1` は build output を `C:\df-framework-<Configuration>` に staging し、
`winapp run` で package-aware に起動します。framework-dependent mode のため、対象環境には
対応する Windows App SDK runtime が必要です。

Visual Studio から F5 で起動する場合は、`DesktopFlyouts.slnx` を開き直した後、Native Sample
を startup project にし、`Debug|x64` を選択します。Native Sample は solution configuration の
Deploy 対象として登録済みなので、通常は build 後に MSIX deployment が行われます。`Skipped
Deploy` が残る場合は、ソリューションを閉じて再度開き、solution configuration の Deploy
チェックを確認してください。

```powershell
& .\samples\DesktopFlyoutsSample.WinUI\Run-DesktopFlyoutsSample.ps1 `
    -Configuration Debug

$app = Get-Process -Name DesktopFlyoutsSample.WinUI | Select-Object -First 1
& .\tests\Run-DesktopFlyoutsSampleUiTests.ps1 -AppPid $app.Id
& .\tests\Test-NativeWinRTContract.ps1
```

直接 `.exe` を起動せず、必ず `winapp run`、または同等の package-aware launch を使ってください。
`Add-AppxPackage -Register` に依存した起動は、この sample の framework-dependent runtime 構成の
検証経路ではありません。
