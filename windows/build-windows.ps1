# ============================================================================
# NRBlox (Windows Native Edition) — WPF Build Script
# Compiles the pure C# WPF Windows application into NRBlox.exe
# ============================================================================

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$CscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$WpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$FwDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$IconPath = Join-Path $RepoRoot "assets\icons\IconNRBlox.ico"
$OutExe = Join-Path $RepoRoot "NRBlox.exe"
$WinBinDir = Join-Path $PSScriptRoot "bin"

if (-not (Test-Path $WinBinDir)) {
    New-Item -ItemType Directory -Path $WinBinDir -Force | Out-Null
}

$Sources = @(
    (Join-Path $PSScriptRoot "src\Core\NRBloxConfig.cs"),
    (Join-Path $PSScriptRoot "src\Core\NRBloxServices.cs"),
    (Join-Path $PSScriptRoot "src\UI\NRBloxTheme.cs"),
    (Join-Path $PSScriptRoot "src\UI\NRBloxBootstrapperWindow.cs"),
    (Join-Path $PSScriptRoot "src\UI\NRBloxMenuWindow.cs"),
    (Join-Path $PSScriptRoot "src\UI\NRBloxMainWindow.cs"),
    (Join-Path $PSScriptRoot "src\Program.cs")
)

& $CscPath /nologo /target:winexe /optimize+ `
    /out:"$OutExe" `
    /win32icon:"$IconPath" `
    /r:"$WpfDir\PresentationFramework.dll" `
    /r:"$WpfDir\PresentationCore.dll" `
    /r:"$WpfDir\WindowsBase.dll" `
    /r:"$FwDir\System.Xaml.dll" `
    /r:"$FwDir\System.Web.Extensions.dll" `
    /r:"$FwDir\System.Drawing.dll" `
    /r:"$FwDir\System.Windows.Forms.dll" `
    /r:"$FwDir\System.IO.Compression.dll" `
    /r:"$FwDir\System.IO.Compression.FileSystem.dll" `
    /r:"$FwDir\Microsoft.CSharp.dll" `
    $Sources

Copy-Item -Path $OutExe -Destination (Join-Path $WinBinDir "NRBlox.exe") -Force
Write-Host "[NRBlox-Windows] Compiled native WPF executable: $OutExe"
