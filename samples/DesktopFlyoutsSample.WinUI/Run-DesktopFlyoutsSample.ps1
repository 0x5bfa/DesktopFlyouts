[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $PSScriptRoot 'DesktopFlyoutsSample.WinUI.vcxproj'
$outputDirectory = Join-Path $PSScriptRoot ("x64\{0}\DesktopFlyoutsSample.WinUI" -f $Configuration)
$stagingDirectory = Join-Path 'C:\' ("df-framework-{0}" -f $Configuration)
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'

$buildArguments = @(
    $projectFile,
    '/restore',
    '/t:Build',
    "/p:Configuration=$Configuration",
    '/p:Platform=x64',
    '/p:GenerateAppxPackageOnBuild=false',
    '/m',
    '/v:minimal'
)
& $msbuild @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Native sample build failed with exit code $LASTEXITCODE."
}

if (Test-Path -LiteralPath $stagingDirectory) {
    $resolvedStagingDirectory = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $stagingDirectory).Path)
    $expectedStagingDirectory = [System.IO.Path]::GetFullPath($stagingDirectory)
    if ($resolvedStagingDirectory -ne $expectedStagingDirectory) {
        throw "Refusing to remove an unexpected staging path: $resolvedStagingDirectory."
    }
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $outputDirectory '*') -Destination $stagingDirectory -Recurse -Force

$assetsDirectory = Join-Path $stagingDirectory 'Assets'
New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null
$sourceAssets = Join-Path $PSScriptRoot 'Assets'
Copy-Item (Join-Path $sourceAssets 'LockScreenLogo.scale-200.png') (Join-Path $assetsDirectory 'LockScreenLogo.png') -Force
Copy-Item (Join-Path $sourceAssets 'SplashScreen.scale-200.png') (Join-Path $assetsDirectory 'SplashScreen.png') -Force
Copy-Item (Join-Path $sourceAssets 'Square150x150Logo.scale-200.png') (Join-Path $assetsDirectory 'Square150x150Logo.png') -Force
Copy-Item (Join-Path $sourceAssets 'Square44x44Logo.scale-200.png') (Join-Path $assetsDirectory 'Square44x44Logo.png') -Force
Copy-Item (Join-Path $sourceAssets 'StoreLogo.png') (Join-Path $assetsDirectory 'StoreLogo.png') -Force
Copy-Item (Join-Path $sourceAssets 'Wide310x150Logo.scale-200.png') (Join-Path $assetsDirectory 'Wide310x150Logo.png') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AppxManifest.Register.xml') -Destination (Join-Path $stagingDirectory 'AppxManifest.xml') -Force

Get-Process -Name 'DesktopFlyoutsSample.WinUI' -ErrorAction SilentlyContinue | Stop-Process -Force
& winapp run $stagingDirectory --manifest (Join-Path $stagingDirectory 'AppxManifest.xml') --detach
if ($LASTEXITCODE -ne 0) {
    throw "Native sample launch failed with exit code $LASTEXITCODE."
}
Write-Host ("Started the framework-dependent native sample from {0}" -f $stagingDirectory)
