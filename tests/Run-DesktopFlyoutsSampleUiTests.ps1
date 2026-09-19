[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [int] $AppPid,
    [string] $OutputDirectory = (Join-Path $env:TEMP 'DesktopFlyoutsSample-UI')
)

$ErrorActionPreference = 'Stop'

function Invoke-WinApp {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $output = @(& winapp @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "winapp $($Arguments -join ' ') failed with exit code $LASTEXITCODE.`n$($output -join "`n")"
    }
    return $output
}

function Invoke-WinAppJson {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $json = (Invoke-WinApp ($Arguments + '--json')) -join "`n"
    return $json | ConvertFrom-Json
}

function Assert-Condition {
    param(
        [Parameter(Mandatory)]
        [bool] $Condition,
        [Parameter(Mandatory)]
        [string] $Message
    )

    if (-not $Condition) {
        throw "UI assertion failed: $Message"
    }
}

function Get-AppWindows {
    return @(Invoke-WinAppJson @('ui', 'list-windows') | Where-Object {
        $_.processId -eq $AppPid
    })
}

function Get-FlyoutWindow {
    return @(Get-AppWindows | Where-Object {
        $_.processId -eq $AppPid -and $_.className -eq 'DesktopFlyouts.NativeFlyoutHost'
    }) | Select-Object -First 1
}

function Wait-ForFlyout {
    param(
        [int] $ReadyDelayMilliseconds = 350
    )

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $flyout = Get-FlyoutWindow
        if ($null -ne $flyout) {
            # The host is shown before the 267 ms Windows 11 open transition
            # completes. Match the C# contract: interaction during a
            # transition is ignored, so wait until the flyout is actionable.
            Start-Sleep -Milliseconds $ReadyDelayMilliseconds
            return $flyout
        }
        Start-Sleep -Milliseconds 100
    }
    throw 'The native flyout window did not appear within 3 seconds.'
}

function Wait-ForFlyoutToClose {
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $currentFlyout = Get-FlyoutWindow
        if ($null -eq $currentFlyout) {
            return
        }
        Start-Sleep -Milliseconds 100
    }
    throw 'The native flyout window did not close within 3 seconds.'
}

function Wait-ForFlyoutElement {
    param(
        [Parameter(Mandatory)]
        [int] $WindowHandle,
        [Parameter(Mandatory)]
        [string] $AutomationId
    )

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $search = Invoke-WinAppJson @('ui', 'search', $AutomationId, '-w', $WindowHandle)
            if ($search.matchCount -gt 0) {
                return $search.matches[0]
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds 100
    }

    throw "The native flyout element '$AutomationId' was not exposed through UI Automation within 3 seconds."
}

function Wait-ForFlyoutElements {
    param(
        [Parameter(Mandatory)]
        [int] $WindowHandle,
        [Parameter(Mandatory)]
        [string[]] $AutomationIds
    )

    foreach ($automationId in $AutomationIds) {
        Wait-ForFlyoutElement -WindowHandle $WindowHandle -AutomationId $automationId | Out-Null
    }
}

$mainWindow = @(Get-AppWindows | Where-Object {
    $_.processId -eq $AppPid -and $_.className -eq 'WinUIDesktopWin32WindowClass'
}) | Select-Object -First 1
Assert-Condition ($null -ne $mainWindow) "the sample main window is present for PID $AppPid"

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

Invoke-WinApp @('ui', 'invoke', 'ShowFlyoutButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Assert-Condition ($flyout.title -eq 'DesktopFlyout') 'the flyout has the expected window title'
Assert-Condition ($flyout.width -eq 384 -and $flyout.height -eq 170) 'the flyout has the configured size including the 12 DIP outer margin'
Assert-Condition ($flyout.ownerHwnd -eq $mainWindow.hwnd) 'the flyout is owned by the sample main window'

$closeButton = Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'CloseFlyoutButton'
$title = Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'FlyoutTitle'
Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'DesktopFlyoutIslandSurface' | Out-Null
Assert-Condition ($closeButton.name -eq 'Close') 'the close action has an accessible name'
Assert-Condition ($title.name -eq 'Default') 'the flyout title has an accessible name'

$screenshotPath = Join-Path $OutputDirectory 'native-flyout.png'
Invoke-WinApp @('ui', 'screenshot', '-w', $flyout.hwnd, '--capture-screen', '-o', $screenshotPath) | Out-Null
Assert-Condition (Test-Path -LiteralPath $screenshotPath) 'the flyout can be captured'

Invoke-WinApp @('ui', 'invoke', 'CloseFlyoutButton', '-w', $flyout.hwnd) | Out-Null
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowFlyoutButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Invoke-WinApp @('ui', 'invoke', 'HideFlyoutButton', '-w', $mainWindow.hwnd) | Out-Null
Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'DesktopFlyoutIslandSurface' | Out-Null
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowFlyoutButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Invoke-WinApp @('ui', 'send-keys', 'esc', '-w', $flyout.hwnd) | Out-Null
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowIslandsButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Assert-Condition ($flyout.width -eq 444 -and $flyout.height -eq 324) 'the two-island flyout has the configured frame size'
Wait-ForFlyoutElements -WindowHandle $flyout.hwnd -AutomationIds @(
    'DesktopFlyoutIslandSurface',
    'DesktopFlyoutIslandSurface1',
    'IslandOneText',
    'IslandTwoText',
    'IslandsCloseButton'
)
$islandsScreenshotPath = Join-Path $OutputDirectory 'native-islands.png'
Invoke-WinApp @('ui', 'screenshot', '-w', $flyout.hwnd, '--capture-screen', '-o', $islandsScreenshotPath) | Out-Null
Assert-Condition (Test-Path -LiteralPath $islandsScreenshotPath) 'the two-island flyout can be captured'
Invoke-WinApp @('ui', 'invoke', 'IslandsCloseButton', '-w', $flyout.hwnd) | Out-Null
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowIslandsButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'IslandsCloseButton' | Out-Null
Invoke-WinApp @('ui', 'touch', 'DesktopFlyoutRoot', '-w', $flyout.hwnd, '--gesture', 'swipe', '--direction', 'down', '--distance', '120', '--duration-ms', '160') | Out-Null
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowAutoCloseButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout -ReadyDelayMilliseconds 100
Wait-ForFlyoutElements -WindowHandle $flyout.hwnd -AutomationIds @('CloseFlyoutButton', 'FlyoutTitle')
Start-Sleep -Milliseconds 1200
Wait-ForFlyoutToClose

Invoke-WinApp @('ui', 'invoke', 'ShowFlyoutButton', '-w', $mainWindow.hwnd) | Out-Null
$flyout = Wait-ForFlyout
Wait-ForFlyoutElement -WindowHandle $flyout.hwnd -AutomationId 'DesktopFlyoutIslandSurface' | Out-Null
# With ExtendsContentIntoTitleBar enabled, the system TitleBar UIA node is
# intentionally non-client and has no clickable bounds. Click visible content
# in the owner window to exercise the same lost-focus close path.
Invoke-WinApp @('ui', 'click', 'FlyoutStatus', '-w', $mainWindow.hwnd) | Out-Null
Wait-ForFlyoutToClose
Assert-Condition ($null -ne (Get-Process -Id $AppPid -ErrorAction SilentlyContinue)) 'the sample remains alive after lost-focus close'

Write-Host "Native sample UI, interaction, and UI Automation checks passed for PID $AppPid."
Write-Host "Screenshot: $screenshotPath"
