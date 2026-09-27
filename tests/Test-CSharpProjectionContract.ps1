[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [ValidateSet('x64')]
    [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$projections = @(
    @{
        Name = 'WinUI'
        ProjectDirectory = Join-Path $PSScriptRoot '..\src\DesktopFlyouts.WinUI.Projection'
        XamlNamespace = 'Microsoft.UI.Xaml'
    },
    @{
        Name = 'UWP'
        ProjectDirectory = Join-Path $PSScriptRoot '..\src\DesktopFlyouts.Uwp.Projection'
        XamlNamespace = 'Windows.UI.Xaml'
    }
)

foreach ($projection in $projections) {
    $generatedRoot = Join-Path $projection.ProjectDirectory "obj\$Configuration"
    $generatedFile = Get-ChildItem -LiteralPath $generatedRoot -Filter 'DesktopFlyouts.cs' -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like '*Generated Files\CsWinRT\DesktopFlyouts.cs' } |
        Select-Object -First 1 -ExpandProperty FullName

    if ($null -eq $generatedFile) {
        throw "$($projection.Name) C# projection output was not found under: $generatedRoot"
    }

    $source = Get-Content -LiteralPath $generatedFile -Raw
    $xamlNamespace = $projection.XamlNamespace
    $requiredFragments = @(
        "public class DesktopFlyout : global::$xamlNamespace.Controls.Control",
        "public class DesktopFlyoutIsland : global::$xamlNamespace.Controls.ContentControl",
        "public class DesktopFlyoutIslandTemplateSettings : global::$xamlNamespace.DependencyObject",
        "public class DesktopFlyoutIslandsPanel : global::$xamlNamespace.Controls.Panel",
        "public class DesktopMenuFlyout : global::$xamlNamespace.Controls.ItemsControl",
        'public object IslandsSource',
        'public void Show(global::Windows.Foundation.Point bottomCenterPoint)',
        'public void NavigateFocus()',
        "public void NavigateFocus(global::$xamlNamespace.Hosting.XamlSourceFocusNavigationReason reason)",
        'public bool TryPreTranslateMessage(long message)',
        'public static SystemTrayIcon CreateFromIconHandle(long iconHandle, string tooltip, Guid id)',
        'public void SetIconHandle(long iconHandle)',
        'public event global::System.EventHandler<SystemTrayIconEventArgs> LeftClicked',
        'public event global::System.EventHandler<SystemTrayIconEventArgs> RightClicked',
        'public event global::System.EventHandler<SystemTrayIconEventArgs> LeftDoubleClicked',
        'public event global::System.EventHandler<SystemTrayIconEventArgs> RightDoubleClicked'
    )

    foreach ($fragment in $requiredFragments) {
        if ($source.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
            throw "$($projection.Name) C# projection is missing: $fragment"
        }
    }

    Write-Host "$($projection.Name) C# projection contract passed: $generatedFile"
}
