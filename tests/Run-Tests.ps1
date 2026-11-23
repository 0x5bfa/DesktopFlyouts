param(
    [Parameter(Mandatory)]
    [ValidateSet('WinUI', 'Uwp')]
    [string]$Flavor,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('x86', 'x64', 'ARM64')]
    [string]$Platform = 'x64',

    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$projectName = "DesktopFlyouts.UnitTests.$Flavor"
$outputPath = Join-Path $PSScriptRoot "$projectName/bin/$Platform/$Configuration"
$recipes = @(Get-ChildItem -LiteralPath $outputPath -Filter "$projectName.build.appxrecipe" -File -Recurse)
if ($recipes.Count -ne 1) {
    throw "Build $projectName for $Configuration|$Platform first; expected exactly one build recipe in $outputPath."
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vstest = & $vswhere -latest -products * -find 'Common7\IDE\Extensions\TestPlatform\vstest.console.exe'
if ($LASTEXITCODE -ne 0 -or -not $vstest) {
    throw 'Visual Studio VSTest tools were not found.'
}

$runnerArguments = @(
    $recipes[0].FullName,
    "/Platform:$Platform",
    '/Framework:FrameworkUap10',
    '/InIsolation',
    "/Settings:$PSScriptRoot/Tests.runsettings",
    '/Logger:trx',
    "/ResultsDirectory:$PSScriptRoot/TestResults/$Flavor"
)
if ($Filter) {
    $runnerArguments += "/TestCaseFilter:$Filter"
}

& $vstest @runnerArguments
exit $LASTEXITCODE
