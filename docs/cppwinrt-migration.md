# C++/WinRT 移行方針

この文書は C++/WinRT native implementation の境界と検証方針を定義する。現在の `cppwinrt`
ブランチには、実際に起動・表示・操作できる Windows App SDK / WinUI 3 implementation を
`src` と `samples` に置いている。旧 C# implementation、C# sample、UWP/Uno project はこの
ブランチから削除済みである。

## 結論

Windows App SDK/WinUI 3 の Windows 実装を C++/WinRT に移すことは可能である。一方、Uno の
Linux/macOS/WebAssembly を同じ native WinRT binary で支えることはできない。現在の repository
は Windows native component に責務を限定している。Uno または UWP を将来再追加する場合は、
同じ public contract と Core の fixture を共有する別 target として設計する。

```text
              公開 WinRT API / 挙動仕様
                         |
             DesktopFlyouts.WinUI
             C++/WinRT + WinUI/XAML
                         |
             Windows shell / HWND
                         |
             DesktopFlyouts.Core
             pure deterministic logic
```

Uno を再追加する場合でも、Windows native component 自体を Uno の共通実装にはしない。
非 Windows backend が必要なら、WinRT/WinUI に依存しない Core 相当の契約を別の platform
binding から利用する。

## IdlGen 2.0 前提

IdlGen 2.0 では手書き `.idl` と implementation type を source of truth にしない。
`winrt::<Namespace>::author` の header を編集し、生成された IDL・implementation header・
implementation source は build artifact とする。

今回の native slice では IdlGen v2.0 系の native NuGet package `IdlGen.IdlGen.Cpp` `0.2.18`
を使ってこの経路を検証した。package version `0.2.18` は `DesktopFlyouts.WinUI.vcxproj` の
native `PackageReference` で明示し、C++ native project では Visual Studio の restore と root
の中央パッケージ管理を混在させない。IdlGen は vcpkg の依存関係ではない。

守るルールは以下。

- public WinRT 型は `author` namespace 内の header に宣言する。
- authored header に function body を書かず、実装は `.cpp` に置く。
- authored type の宣言から `std::vector`、`HWND`、内部 mutex などを public ABI に漏らさない。
- XAML/Windows object を保持する runtime class は `author::internal<winrt::non_agile>` を使い、
  UI thread affinity を private implementation と実行時チェックの両方で表現する。
- projected type を使い、author type 同士の参照は IdlGen 2.0 の forward declaration 規則に従う。
- `*.idl`、`*.impl.h`、`*.impl.cpp` を Git 管理しない。
- `IdlGenCppInclude` を明示し、`pch.h` や通常の private header を reflection 対象にしない。

`src/DesktopFlyouts.WinUI/author/DesktopFlyout.author.h` が現時点の native MUX slice の
source of truth である。生成 IDL は build artifact とし、公開 ABI に HWND、STL、callback の
内部型を漏らさない。

## 推奨 project structure

```text
src/
  DesktopFlyouts.Core/          # C++20、WinRT/WinUI/HWND なし
  DesktopFlyouts.WinUI/         # C++/WinRT component、IdlGen、WinUI controls/host
    author/                     # IdlGen の public authored headers
    DesktopFlyout.cpp           # authored façade、Core と adapter の調停
    DesktopFlyoutHost.*         # HWND、activation、timer、XAML island host
    DesktopFlyoutVisual.*       # XAML tree、input、animation、backdrop
    Controls/                   # DesktopFlyout / Island / MenuFlyout の WinUI layer
    Shell/                      # tray icon、HWND、activation、DPI の shell adapter
    Themes/                     # Generic.xaml と control template
    Resources/                  # PRI/resource build input
  DesktopFlyouts.cppwinrt.slnx  # native-only solution

tests/
  DesktopFlyouts.Core.Tests/    # geometry/state machine、CppUnitTest/Catch2
  Run-DesktopFlyoutsSampleUiTests.ps1   # packaged sample + UI Automation
  Test-NativeWinRTContract.ps1  # generated metadata contract
```

旧 `DesktopFlyouts.Shared` をそのまま C++ に翻訳するのではなく、次の依存方向にする。

```text
Core (pure C++)
  <- Windows shell adapter (Win32/C++/WinRT)
  <- WinUI control layer (DependencyObject/XAML island)
  <- package/sample/projection
```

`DesktopFlyout` の状態遷移、配置、dismiss 判定、サイズ解決は Core に寄せる。XAML template、
focus、backdrop、DispatcherQueue、DesktopWindowXamlSource は WinUI layer に閉じ込める。
`SystemTrayIcon` と host HWND は shell adapter に閉じ込め、Core から Win32 を参照させない。

現行コードとの対応は次のように分ける。

- `DesktopFlyout`、`DesktopMenuFlyout` は WinUI control layer。公開 surface は WinRT ABI、実装は
  `ControlT`/`ItemsControl` と `Generic.xaml` に置く。
- `DesktopFlyoutIsland`、`DesktopFlyoutIslandsPanel` は XAML layout layer。host window の生成や
  monitor/DPI 計算を control から直接行わず、shell adapter の service に依存させる。
- `SystemTrayIcon`、`XamlIslandHostWindow`、activation、message hook は Windows shell adapter。
- `DesktopFlyoutActivationMode` などの enum は公開するものと private state を分離し、公開 enum の
  数値を authored ABI contract で固定する。

### 旧 C# shared project について

旧 `DesktopFlyouts.Shared.shproj` は同じ C# source を複数の C# project に取り込む仕組みだった。
C++/WinRT project は C# compilation unit、DependencyProperty generator、XAML type system、
または conditional compilation を共有しないため、native project の構成要素にはならない。
旧 C# project はこのブランチから削除した。

共有すべきなのは source file ではなく contract と pure logic である。

- public enum、property、method の意味と数値は IdlGen authored header と WinRT metadata contract test で固定する。
- placement、lifecycle、dismiss 判定は `DesktopFlyouts.Core` の fixture で決定論的に検証する。
- HWND、`DesktopWindowXamlSource`、WinUI control/template、DependencyProperty は native MUX layer に置く。
- native MUX slice の legacy `Width`/`Height` は互換用の physical pixel API として残す。通常の
  `FlyoutWidth`/`FlyoutHeight` は同じ `GridLength`/DIP/Auto/Star semantics と DPI-aware
  sizing を使う。
- native default visual の外周は、透明な root、各 island の角丸 surface、1px surface border、
  `ThemeShadow`、per-island `SystemBackdropElement`、`FlyoutOverlayBackgroundBrush` まで実装済み。
  island 間は一枚の backdrop で埋めず、各 island が独立した floating surface になる。sample の inner
  content はシナリオ確認用で、製品 parity の typography、localized strings、high-contrast resource は
  `Controls/` と `Themes/Resources/` へ分離する。
- 旧 template にある `Translation.Z=36` は native XAML Island bridge で実画面が空白になる
  ため移植していない。native では `ThemeShadow` と独立 surface で浮遊表現を維持し、描画可能性と UIA
  tree を優先する。
つまり旧 C# shared project と C++ project の共通化は「同じファイルを compile する」形ではなく、
「同じ behavior/ABI contract を別実装で検証する」形だった。現在は native implementation を
source of truth とし、ABI と Core behavior を native tests で検証する。

Windows App SDK/WinUI 3 と UWP は同じ Windows Runtime ABI を使えるが、XAML の型名前空間、
host、package/runtime dependency は同一ではない。UWP を再追加する場合も `DesktopFlyouts.WinUI`
とは別 target とし、1つの XAML control DLL に両方を詰め込まない。

## 実装と検証の順序

1. public WinRT API と実行時シナリオを IdlGen authored header と契約テストとして固定する。
2. 配置・サイズ・popup direction・swipe threshold・open/close state machine を Core に移し、
   Core の同一 fixture で結果を比較する。
3. IdlGen 2.0 の小さな runtime class を component として build し、C++/WinRT から activation
   できることを確認する。
4. native sample から component を使い、native DLL、winmd、PRI、package identity、architecture
   別 payload を検証する。
5. まず `SystemTrayIcon` と host window を移す。XAML control を先に巨大移植しない。
6. `DesktopFlyoutIsland`、template、backdrop、focus suppression、animation の順に移す。
7. `DesktopMenuFlyout`、nested popup、multi-monitor、DPI、activation mode を移す。
8. Windows implementation が安定した後、必要な別 target があれば同じ Core contract と
   behavior fixture を使って追加する。

現時点の `cppwinrt` branch では、MUX の `DesktopFlyout` core（Core 配置/lifecycle、IdlGen 2.0
component、XAML island popup、UIElement islands、独立した角丸 floating surface、island ごとの
border/backdrop、animation、pressed/swipe interaction、auto-close、`DesktopMenuFlyout`、
`SystemTrayIcon`、packaged sample、UI Automation test）まで実装している。実装は `DesktopFlyout.cpp`
の façade、`DesktopFlyoutHost.*` の HWND/XAML host、`DesktopFlyoutVisual.*` の visual/input に
分離し、XAML object を保持する runtime class は non-agile として UI thread を固定している。
これは Windows App SDK / WinUI 3 の native implementation として、sample の代表シナリオまで
通る状態である。UWP/Uno target はこのブランチの scope 外である。

開閉 transition は従来の behavior contract と同じ XAML `Storyboard` を native 側でも構成する。
開く場合は `DiscreteDoubleKeyFrame` から `SplineDoubleKeyFrame` へ、閉じる場合はその逆方向へ
`CompositeTransform.TranslateX/TranslateY` だけを動かす。方向別の時間は contract と同じく、vertical の
open が 267 ms、vertical の close が 200 ms、horizontal の open/close が 167 ms で、opacity や
scale を開閉 transition に追加しない。既定 `Margin=12` 相当の closed offset も native 側で
使用する。これにより、native component の完了通知も XAML `Storyboard.Completed` を source にする。

## IdlGen 2.0 の public type 境界

IdlGen 2.0 の authored header から cross-runtime-class の vector element を直接組み立てると、
author namespace の型と生成後の projected WinRT 型が C++ compile 時に一致しないことがある。
この branch ではその失敗を避けるため、public `Islands` は既存の `Microsoft.UI.Xaml.UIElement` を
要素型にした。これは ABI と IdlGen の生成順に対して安定しており、C++/WinRT consumer は通常の
`IObservableVector<UIElement>` として利用できる。

将来 `DesktopFlyoutIsland` を public runtime class にする場合は、同じ authored header 内の単純な
cross-reference に戻すのではなく、次のいずれかを別の metadata version として設計する。

- `DesktopFlyoutIsland` を独立した public interface/runtime class として先に生成し、projected type
  を使う別 contract にする。
- UIElement/ContentControl を ABI に残し、island-specific behavior は private template layer に置く。

生成された `.idl`、`*.impl.*`、projection header を source of truth にしない。

## ABI と lifetime の設計

- public は WinRT primitive、enum、struct、`IReference<T>`、`IVector<T>`、delegate/event に限定する。
- `std::string`、`std::vector`、`std::function`、`HWND`、`HHOOK`、`wil::unique_*` は private にする。
- WinRT object は projected type、classic COM は `winrt::com_ptr`、native handle は owner を明示する。
- event は `event_token` と revoker を使い、host close 時に XAML event、timer、hook、subclass を逆順で解放する。
- coroutine は UI thread affinity を明示し、close 後に戻る coroutine は `weak_ref` で target を再確認する。
- `NeverActivate` は host HWND、XAML island HWND、child HWND、owned popup の activation policy を同じ
  state から解決する。個別の window procedure に散在させない。

## テスト戦略

### Unit test

Core は UI thread、Windows App Runtime、package identity なしでテストする。最低限、次を fixture
化する。

- work area の各 edge、負の座標、taskbar、DPI 変換、desired size の clamp
- top/bottom/left/right の自動 direction と明示 direction
- open/close/reopen、auto-close、cancel、swipe threshold、pointer cancel
- invalid/empty content、double dispose、event unsubscription

WinRT component には別の contract test を置く。metadata の runtime class 名、constructor、
property type、enum 数値、event signature、activation、x64/ARM64 payload を検証する。Core test と
component/WinUI test を同じテストプロジェクトに混ぜない。

### Interaction test

UI test 用の packaged sample は本体ライブラリと分離し、各 control に安定した
`AutomationProperties.AutomationId` と意味のある `Name` を付ける。Windows UI Automation を
使って次を検証する。

- tray left/right click から flyout/menu が出る
- show/hide/reopen 後に一つの host window と一つの論理 tree だけが残る
- menu item invoke、keyboard Tab/Arrow/Escape、focus restoration
- `Activate`、`NoActivateOnOpen`、`NeverActivate` の foreground/focus 差
- lost focus、auto-close、nested popup、monitor edge、DPI change、high contrast
- animation を off にした deterministic path と animation on の completion path

直接 `.exe` を起動せず、package-aware launch を使う。UI Automation が見る tree は interaction
test と accessibility test で共有し、視覚的なクリック座標だけに依存しない。

### Axe / accessibility

`axe-core` は本来 web DOM 用なので、WinUI desktop の合否判定にはそのまま使えない。Windows
desktop では Accessibility Insights for Windows の Live Inspect/FastPass を基準にし、必要なら
UI Automation の `IUIAutomation` client で自動 assertion を追加する。AccChecker は legacy/補助
用途として扱う。

最低限の自動 assertion は Name、ControlType、AutomationId、IsEnabled、IsKeyboardFocusable、
invoke/selection/expand-collapse pattern、focus changed event、popup の tree 出現/消滅である。
「axe を使う」要件が社内 wrapper や Deque の Windows scanner を指す場合も、UIA tree の検査を
主テストにし、scanner は補助的なルール検査にする。

native sample では `Run-DesktopFlyoutsSampleUiTests.ps1` が UIA search による Name、AutomationId、control
type、invoke、popup lifecycle、surface 境界、custom islands、swipe、auto-close、lost-focus close を検査する。これは axe の代替を
名乗るものではなく、Windows desktop の accessibility contract test である。Accessibility Insights
for Windows の FastPass/Live Inspect は、colors/high contrast/keyboard/screen reader の手動確認として別に実行する。

Accessibility Insights の手動 FastPass 結果は versioned checklist として保存し、UIA snapshot
の差分を PR で確認できるようにする。色、contrast、keyboard、screen reader、high contrast は
別のシナリオとして実行する。

## Done の条件

次のすべてを満たすまでは「C++ rewrite 完了」と呼ばない。

- WASDK x64 の packaged sample が起動し、native DLL/winmd/PRI の activation が成功する。
- native sample の代表シナリオが interaction test と一致する。
- Core unit、WinRT contract、interaction、accessibility の結果を別々に報告できる。
- UWP/Uno target を再追加する場合は C++/WinRT DLL に直接依存させない。
- public API を変更する場合は、IDL/metadata versioning と移行メモを同時に用意する。

参考:

- https://github.com/roxk/idlgen/tree/v2.0
- https://learn.microsoft.com/en-us/windows/apps/develop/cpp-winrt/author-apis
- https://learn.microsoft.com/en-us/windows/apps/develop/testing/
- https://learn.microsoft.com/en-us/windows/win32/winauto/accessibility-testingtools
