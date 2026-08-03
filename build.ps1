# Build RimTaxi.dll into Assemblies/
# Prefers `dotnet build`; falls back to Framework csc if SDK is missing.

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src = Join-Path $Root "Source\RimTaxi"
$Out = Join-Path $Root "Assemblies\RimTaxi.dll"
$Csproj = Join-Path $Src "RimTaxi.csproj"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    Write-Host "Building with dotnet..."
    Push-Location $Src
    try {
        dotnet build -c Release
    } finally {
        Pop-Location
    }
    if (Test-Path $Out) {
        Write-Host "OK: $Out"
        exit 0
    }
}

Write-Host "dotnet not available; falling back to csc..."
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    throw "No dotnet SDK and no csc.exe found."
}

$managed = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
$harmony = "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll"
if (-not (Test-Path $harmony)) {
    throw "0Harmony.dll not found at: $harmony"
}

$refs = @(
    "$managed\Assembly-CSharp.dll",
    "$managed\UnityEngine.CoreModule.dll",
    "$managed\UnityEngine.IMGUIModule.dll",
    "$managed\UnityEngine.TextRenderingModule.dll",
    "$managed\netstandard.dll",
    "$managed\System.Runtime.dll",
    $harmony,
    "System.dll",
    "System.Core.dll",
    "System.Xml.dll"
)
$refArgs = ($refs | ForEach-Object { "/r:`"$_`"" }) -join " "
$sources = (Get-ChildItem $Src -Recurse -Filter "*.cs" | ForEach-Object { "`"$($_.FullName)`"" }) -join " "
$argLine = "/nologo /target:library /optimize+ /out:`"$Out`" $refArgs $sources"
$p = Start-Process -FilePath $csc -ArgumentList $argLine -Wait -NoNewWindow -PassThru
if ($p.ExitCode -ne 0) { throw "csc failed with exit $($p.ExitCode)" }
Write-Host "OK: $Out"
