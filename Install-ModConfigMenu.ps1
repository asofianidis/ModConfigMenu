param(
    [string]$GameDir = 'F:\SteamLibrary\steamapps\common\SpiritVale'
)

$ErrorActionPreference = 'Stop'

$source = Join-Path $PSScriptRoot 'bin\Release\net6.0\SpiritVale.ModConfigMenu.dll'
$pluginDirectory = Join-Path $GameDir 'BepInEx\plugins\ModConfigMenu'
$target = Join-Path $pluginDirectory 'SpiritVale.ModConfigMenu.dll'
$backupDirectory = Join-Path $GameDir 'BepInEx\mod-backups\ModConfigMenu'

if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Release DLL not found at '$source'. Build the project first."
}

New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null

if (Test-Path -LiteralPath $target -PathType Leaf) {
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    try {
        $installedVersion = [System.Reflection.AssemblyName]::GetAssemblyName($target).Version.ToString()
    }
    catch {
        $installedVersion = 'unknown'
    }

    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupName = "SpiritVale.ModConfigMenu-$installedVersion-$timestamp.dll.bak"
    $backup = Join-Path $backupDirectory $backupName
    Copy-Item -LiteralPath $target -Destination $backup
    Write-Output "Backed up installed version to: $backup"
}

Copy-Item -LiteralPath $source -Destination $target -Force

$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($sourceHash -ne $installedHash) {
    throw 'Installed DLL hash does not match the release build.'
}

$version = [System.Reflection.AssemblyName]::GetAssemblyName($target).Version
Write-Output "Installed Mod Configuration Menu $version to: $target"
Write-Output "SHA256: $installedHash"
