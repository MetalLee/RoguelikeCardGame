param(
    [string]$GodotPath = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$env:DOTNET_CLI_HOME = $projectRoot
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $projectRoot ".nuget\packages"
$env:APPDATA = Join-Path $projectRoot ".appdata\Roaming"
$env:LOCALAPPDATA = Join-Path $projectRoot ".appdata\Local"

New-Item -ItemType Directory -Force -Path $env:APPDATA, $env:LOCALAPPDATA, $env:NUGET_PACKAGES | Out-Null

if ([string]::IsNullOrWhiteSpace($GodotPath)) {
    $candidates = @(
        $env:GODOT4_MONO,
        "D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe",
        "D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64.exe"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            $GodotPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($GodotPath) -or -not (Test-Path -LiteralPath $GodotPath)) {
    throw "Godot .NET executable was not found. Pass -GodotPath or set GODOT4_MONO."
}

$godotVersion = & $GodotPath --version
Write-Host "Godot: $godotVersion"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($godotVersion -notmatch "\.mono\.") {
    throw "Godot .NET/Mono build is required for this C# project."
}

$godotRoot = Split-Path -Parent $GodotPath
$godotNugetSource = Join-Path $godotRoot "GodotSharp\Tools\nupkgs"
if (Test-Path -LiteralPath $godotNugetSource) {
    $nugetConfigDirectory = Join-Path $env:APPDATA "NuGet"
    $nugetConfigPath = Join-Path $nugetConfigDirectory "NuGet.Config"
    $escapedNugetSource = [System.Security.SecurityElement]::Escape($godotNugetSource)
    New-Item -ItemType Directory -Force -Path $nugetConfigDirectory | Out-Null
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="GodotBundled" value="$escapedNugetSource" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 -LiteralPath $nugetConfigPath
}

$dotnetSdkList = & dotnet --list-sdks
if ([string]::IsNullOrWhiteSpace($dotnetSdkList)) {
    throw ".NET SDK was not found. Install the .NET 8 SDK before building the Godot C# project."
}

$hasDotnet8Sdk = @($dotnetSdkList | Where-Object { $_ -match "^8\." }).Count -gt 0
if (-not $hasDotnet8Sdk) {
    throw ".NET 8 SDK was not found. Godot 4.6 C# requires the .NET 8 SDK."
}

Write-Host ".NET SDKs:"
Write-Host $dotnetSdkList

$csprojPath = Join-Path $projectRoot "RoguelikeCardGame.csproj"
dotnet build $csprojPath
$dotnetExitCode = $LASTEXITCODE
if ($dotnetExitCode -ne 0) {
    Write-Warning "Direct dotnet build returned exit code $dotnetExitCode. Continuing with Godot build verification."
}

$layoutLog = Join-Path $projectRoot "godot-build.log"
$buildLog = Join-Path $projectRoot ".appdata\godot-build.log"
$compiledAssembly = Join-Path $projectRoot ".godot\mono\temp\bin\Debug\RoguelikeCardGame.dll"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $buildLog) | Out-Null
Set-Content -Encoding UTF8 -LiteralPath $layoutLog -Value ""
Set-Content -Encoding UTF8 -LiteralPath $buildLog -Value ""

& $GodotPath --headless --path $projectRoot --build-solutions --quit --log-file $buildLog --disable-crash-handler
$godotExitCode = $LASTEXITCODE
$buildLogContent = ""
if (Test-Path -LiteralPath $buildLog) {
    $buildLogContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $buildLog
}

$dotnetBuildCompleted = $buildLogContent -match "\[ DONE \]\s+dotnet_build_project"
if ($godotExitCode -ne 0) {
    if ((Test-Path -LiteralPath $compiledAssembly) -and $dotnetBuildCompleted) {
        Write-Warning "Godot returned exit code $godotExitCode after compiling. Treating as pass because the .NET build completed and the assembly exists."
        exit 0
    }

    exit $godotExitCode
}
