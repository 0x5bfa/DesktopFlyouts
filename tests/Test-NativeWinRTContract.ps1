[CmdletBinding()]
param(
    [string] $WinmdPath = (Join-Path $PSScriptRoot '..\src\DesktopFlyouts.WinUI\x64\Debug\DesktopFlyouts.WinUI\DesktopFlyouts.winmd')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $WinmdPath)) {
    throw "The native WinRT metadata file was not found: $WinmdPath"
}

$winmdidl = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\winmdidl.exe'
if (-not (Test-Path -LiteralPath $winmdidl)) {
    throw "winmdidl.exe was not found: $winmdidl"
}

$outputDirectory = Join-Path $env:TEMP ('DesktopFlyouts-winmdidl-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    & $winmdidl /nologo /utf8 "/outdir:$outputDirectory" $WinmdPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "winmdidl.exe failed with exit code $LASTEXITCODE."
    }

    $idlPath = Get-ChildItem -LiteralPath $outputDirectory -Filter '*.idl' -File | Select-Object -First 1 -ExpandProperty FullName
    if ($null -eq $idlPath) {
        throw 'winmdidl.exe did not produce an IDL file.'
    }

    $idl = Get-Content -LiteralPath $idlPath -Raw
    $requiredFragments = @(
        'runtimeclass DesktopFlyout',
        'IObservableVector<Microsoft.UI.Xaml.UIElement',
        'IObservableVector<Microsoft.UI.Xaml.Controls.MenuFlyoutItemBase',
        'DesktopFlyoutOrientation',
        'DesktopFlyoutBackdropKind',
        'Windows.Foundation.TimeSpan',
        'HRESULT ShowAt'
    )

    foreach ($fragment in $requiredFragments) {
        if ($idl.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
            throw "The generated WinRT contract is missing: $fragment"
        }
    }

    Write-Host "Native WinRT metadata contract passed: $WinmdPath"
}
finally {
    if (Test-Path -LiteralPath $outputDirectory) {
        Remove-Item -LiteralPath $outputDirectory -Recurse -Force
    }
}
