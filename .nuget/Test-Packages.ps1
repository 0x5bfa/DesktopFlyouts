# Copyright (c) 0x5BFA. All rights reserved.
# Licensed under the MIT license.

#requires -Version 7.3

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PackageVersion,
    [string] $PackageDirectory = "$PSScriptRoot/../artifacts/packages",
    [ValidateSet('WinUI', 'Uwp', 'Uno')]
    [string[]] $Flavor = @('WinUI', 'Uwp', 'Uno')
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$PackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
Add-Type -AssemblyName System.Reflection.Metadata

function Assert-AssemblyArchitecture($Entry, [string] $Architecture) {
    if (-not $Entry) { throw "Missing $Architecture assembly in package." }
    $stream = $Entry.Open()
    $buffer = [System.IO.MemoryStream]::new()
    try {
        $stream.CopyTo($buffer)
        $buffer.Position = 0
        $pe = [System.Reflection.PortableExecutable.PEReader]::new($buffer)
        try {
            $machine = $pe.PEHeaders.CoffHeader.Machine.ToString()
            $expected = @{ x86 = 'I386'; x64 = 'Amd64'; arm64 = 'Arm64'; AnyCPU = 'I386' }[$Architecture]
            if ($machine -ne $expected) { throw "$($Entry.FullName): expected $Architecture, found $machine." }
            if ($Architecture -eq 'AnyCPU' -and ($pe.PEHeaders.CorHeader.Flags -band 2)) { throw "$($Entry.FullName) requires a 32-bit process." }
        }
        finally { $pe.Dispose() }
    }
    finally { $stream.Dispose(); $buffer.Dispose() }
}

function Assert-UwpTemplateResources([string] $PriPath, [string] $MakePri) {
    $dumpPath = Join-Path ([IO.Path]::GetTempPath()) ("DesktopFlyouts-" + [Guid]::NewGuid() + ".xml")
    try {
        & $MakePri dump /if $PriPath /of $dumpPath /dt detailed | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Failed to inspect UWP resources in $PriPath." }
        [xml] $dump = Get-Content -LiteralPath $dumpPath -Raw
        $embeddedResources = $dump.SelectNodes("//NamedResource[Candidate/@type='EmbeddedData']/@uri")
        foreach ($template in @('Themes/Generic.xbf', 'DesktopFlyout.xbf')) {
            $resourcePath = "/Files/DesktopFlyouts.Uwp/$template"
            $matchingResource = $embeddedResources | Where-Object { $_.Value.EndsWith($resourcePath, [StringComparison]::Ordinal) }
            if (-not $matchingResource) { throw "Missing embedded $resourcePath in $PriPath." }
        }
    }
    finally {
        if (Test-Path -LiteralPath $dumpPath) { Remove-Item -LiteralPath $dumpPath }
    }
}

# Retain the repository's feed mappings, with this build's packages selected from the local feed.
[xml] $nugetConfig = Get-Content -LiteralPath (Join-Path $repositoryRoot 'nuget.config') -Raw
$source = $nugetConfig.SelectSingleNode('/configuration/packageSources/add[@key="Local packages"]')
if ($source) {
    $source.SetAttribute('value', $PackageDirectory)
}
else {
    $source = $nugetConfig.CreateElement('add')
    $source.SetAttribute('key', 'Local packages')
    $source.SetAttribute('value', $PackageDirectory)
    $nugetConfig.configuration.packageSources.AppendChild($source) | Out-Null
}
$mapping = $nugetConfig.SelectSingleNode('/configuration/packageSourceMapping/packageSource[@key="Local packages"]')
if (-not $mapping) {
    $mapping = $nugetConfig.CreateElement('packageSource')
    $mapping.SetAttribute('key', 'Local packages')
    $nugetConfig.configuration.packageSourceMapping.AppendChild($mapping) | Out-Null
}
if (-not $mapping.SelectSingleNode('package[@pattern="DesktopFlyouts.*"]')) {
    $pattern = $nugetConfig.CreateElement('package')
    $pattern.SetAttribute('pattern', 'DesktopFlyouts.*')
    $mapping.AppendChild($pattern) | Out-Null
}
$configPath = Join-Path $PackageDirectory 'consumer.nuget.config'
$nugetConfig.Save($configPath)

if (@($Flavor | Where-Object { $_ -ne 'Uno' }).Count -gt 0) {
    $vswhere = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/Current/Bin/MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'Visual Studio MSBuild is required to build the Windows samples.' }
}

foreach ($libraryFlavor in $Flavor) {
    $assemblyName = "DesktopFlyouts.$libraryFlavor"
    $packagePath = Join-Path $PackageDirectory "$assemblyName.$PackageVersion.nupkg"
    $zip = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        if ($libraryFlavor -eq 'Uno') {
            $assemblies = @($zip.Entries | Where-Object { $_.FullName -match "^lib/[^/]+/$assemblyName.dll$" })
            if ($assemblies.Count -ne 1) { throw 'Expected the .NET 10 target framework in the Uno package.' }
            foreach ($assembly in $assemblies) { Assert-AssemblyArchitecture $assembly 'AnyCPU' }
        }
        else {
            $references = @($zip.Entries | Where-Object { $_.FullName -match "^ref/[^/]+/$assemblyName.dll$" })
            if ($references.Count -ne 1) { throw "Expected the .NET 10 target framework in $assemblyName." }
            foreach ($reference in $references) {
                Assert-AssemblyArchitecture $reference 'AnyCPU'
                $framework = $reference.FullName.Split('/')[1]
                foreach ($architecture in @('x86', 'x64', 'arm64')) {
                    Assert-AssemblyArchitecture ($zip.GetEntry("runtimes/win-$architecture/lib/$framework/$assemblyName.dll")) $architecture
                }
                if (-not $zip.GetEntry("ref/$framework/$assemblyName.pri")) { throw "Missing $assemblyName PRI." }
            }
            if (@($zip.Entries | Where-Object { $_.FullName.StartsWith('lib/') }).Count) { throw 'Windows implementations must be selected by RID.' }
        }
    }
    finally { $zip.Dispose() }
    Write-Host "Verified package assemblies: $assemblyName"

    $sample = Join-Path $repositoryRoot "samples/DesktopFlyouts.SampleApp.$libraryFlavor/DesktopFlyouts.SampleApp.$libraryFlavor.csproj"
    $consumerArguments = @("/p:DesktopFlyoutsPackageVersion=$PackageVersion", "/p:DesktopFlyoutsPackageOutputPath=$PackageDirectory", "/p:RestoreConfigFile=$configPath", '/p:AppxPackageSigningEnabled=false', '/p:SkipDesktopFlyoutsAutoPack=true')
    if ($libraryFlavor -eq 'Uno') {
        & dotnet build $sample -c Release -a x64 @consumerArguments
        if ($LASTEXITCODE -ne 0) { throw 'Uno package consumer build failed.' }
        continue
    }
    foreach ($architecture in @('x86', 'x64', 'arm64')) {
        $architectureArguments = @('/p:Configuration=Release', "/p:Platform=$architecture", "/p:RuntimeIdentifier=win-$architecture")
        & $msbuild $sample /restore /nologo /verbosity:minimal @architectureArguments @consumerArguments
        if ($LASTEXITCODE -ne 0) { throw "$libraryFlavor $architecture package consumer build failed." }
        $sampleProperties = ((& $msbuild $sample /nologo /getProperty:TargetDir,TargetFramework,MakePriExeFullPath @architectureArguments @consumerArguments) -join "`n") | ConvertFrom-Json
        $targetDir = $sampleProperties.Properties.TargetDir
        $outputDll = Join-Path $targetDir "$assemblyName.dll"
        $runtimeFramework = [regex]::Replace($sampleProperties.Properties.TargetFramework, '(windows[0-9]+\.[0-9]+\.[0-9]+)\.0$', '$1')
        $runtimeEntry = "runtimes/win-$architecture/lib/$runtimeFramework/$assemblyName.dll"
        $zip = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
        try {
            $entryStream = $zip.GetEntry($runtimeEntry).Open()
            try { $expectedHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($entryStream)) }
            finally { $entryStream.Dispose() }
            if ((Get-FileHash -LiteralPath $outputDll -Algorithm SHA256).Hash -ne $expectedHash) { throw "Incorrect runtime DLL copied to $targetDir." }
        }
        finally { $zip.Dispose() }
        if ($libraryFlavor -eq 'Uwp') {
            Assert-UwpTemplateResources (Join-Path $targetDir 'resources.pri') $sampleProperties.Properties.MakePriExeFullPath
        }
        Write-Host "Verified package consumer: $libraryFlavor win-$architecture"
    }
}
