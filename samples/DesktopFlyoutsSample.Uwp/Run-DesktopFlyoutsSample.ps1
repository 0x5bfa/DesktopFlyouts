[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [ValidateSet('x64', 'arm64')]
    [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DesktopFlyoutsSample.Uwp.csproj'
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild $project /restore /t:Build "/p:Configuration=$Configuration" "/p:Platform=$Platform" /m /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Sample build failed with exit code $LASTEXITCODE." }

& winapp run $project -c $Configuration --arch $Platform --no-build --detach
if ($LASTEXITCODE -ne 0) { throw "Sample launch failed with exit code $LASTEXITCODE." }
