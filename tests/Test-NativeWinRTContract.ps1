[CmdletBinding()]
param(
    [string[]] $WinmdPath = @(
        (Join-Path $PSScriptRoot '..\x64\Debug\DesktopFlyouts.WinUI\DesktopFlyouts.winmd'),
        (Join-Path $PSScriptRoot '..\x64\Debug\DesktopFlyouts.Uwp\DesktopFlyouts.winmd')
    )
)

$ErrorActionPreference = 'Stop'

$winmdidl = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\winmdidl.exe'
if (-not (Test-Path -LiteralPath $winmdidl)) {
    throw "winmdidl.exe was not found: $winmdidl"
}

foreach ($path in $WinmdPath) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "The native WinRT metadata file was not found: $path"
    }

    $resolvedPath = (Resolve-Path -LiteralPath $path).Path
    $isUwp = $resolvedPath -match 'DesktopFlyouts\.Uwp[\\/]'
    $xamlNamespace = if ($isUwp) { 'Windows.UI.Xaml' } else { 'Microsoft.UI.Xaml' }
    $outputDirectory = Join-Path $env:TEMP ('DesktopFlyouts-winmdidl-' + [guid]::NewGuid().ToString('N'))

    try {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
        & $winmdidl /nologo /utf8 "/outdir:$outputDirectory" $resolvedPath | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "winmdidl.exe failed with exit code $LASTEXITCODE."
        }

        $idlPath = Get-ChildItem -LiteralPath $outputDirectory -Filter '*.idl' -File | Select-Object -First 1 -ExpandProperty FullName
        if ($null -eq $idlPath) {
            throw 'winmdidl.exe did not produce an IDL file.'
        }

        $idl = Get-Content -LiteralPath $idlPath -Raw
        $requiredFragments = @(
            "runtimeclass DesktopFlyout : $xamlNamespace.Controls.Control",
            "runtimeclass DesktopFlyoutIsland : $xamlNamespace.Controls.ContentControl",
            "runtimeclass DesktopFlyoutIslandTemplateSettings : $xamlNamespace.DependencyObject",
            "runtimeclass DesktopFlyoutIslandsPanel : $xamlNamespace.Controls.Panel",
            "runtimeclass DesktopMenuFlyout : $xamlNamespace.Controls.ItemsControl",
            'Windows.Foundation.Collections.IObservableVector<DesktopFlyouts.DesktopFlyoutIsland*>',
            'HRESULT IslandsSource(',
            '[overload("Show")] HRESULT Show2([in] Windows.Foundation.Point bottomCenterPoint);',
            'HRESULT Show([in] Windows.Foundation.Point point);',
            'HRESULT ShowAt([in] INT32 x, [in] INT32 y);',
            '[overload("NavigateFocus")] HRESULT NavigateFocus();',
            ('[overload("NavigateFocus")] HRESULT NavigateFocus2([in] {0}.Hosting.XamlSourceFocusNavigationReason reason);' -f $xamlNamespace),
            'HRESULT TryPreTranslateMessage([in] INT64 message, [out] [retval] boolean* result);',
            'runtimeclass SystemTrayIconEventArgs',
            'HRESULT CreateInstance([in] HSTRING iconPath, [in] HSTRING tooltip, [in] GUID id, [out] [retval] DesktopFlyouts.SystemTrayIcon** value);',
            'HRESULT CreateFromIconHandle([in] INT64 iconHandle, [in] HSTRING tooltip, [in] GUID id, [out] [retval] DesktopFlyouts.SystemTrayIcon** result);',
            'HRESULT SetIcon([in] HSTRING iconPath);',
            'HRESULT SetIconHandle([in] INT64 iconHandle);',
            '[eventadd] HRESULT LeftClicked([in] Windows.Foundation.EventHandler<DesktopFlyouts.SystemTrayIconEventArgs*>* handler',
            '[eventadd] HRESULT RightClicked([in] Windows.Foundation.EventHandler<DesktopFlyouts.SystemTrayIconEventArgs*>* handler',
            '[eventadd] HRESULT LeftDoubleClicked([in] Windows.Foundation.EventHandler<DesktopFlyouts.SystemTrayIconEventArgs*>* handler',
            '[eventadd] HRESULT RightDoubleClicked([in] Windows.Foundation.EventHandler<DesktopFlyouts.SystemTrayIconEventArgs*>* handler'
        )

        foreach ($fragment in $requiredFragments) {
            if ($idl.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
                throw "The generated WinRT contract is missing from $resolvedPath`: $fragment"
            }
        }

        if (-not $isUwp) {
            $winUiOnlyFragments = @(
                '[propget] HRESULT SystemBackdrop([out] [retval] Microsoft.UI.Xaml.Media.SystemBackdrop** value);',
                '[propput] HRESULT SystemBackdrop([in] Microsoft.UI.Xaml.Media.SystemBackdrop* value);'
            )

            foreach ($fragment in $winUiOnlyFragments) {
                if ($idl.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
                    throw "The WinUI contract is missing: $fragment"
                }
            }
        }

        Write-Host "Native WinRT metadata contract passed: $resolvedPath"
    }
    finally {
        if (Test-Path -LiteralPath $outputDirectory) {
            Remove-Item -LiteralPath $outputDirectory -Recurse -Force
        }
    }
}
