param(
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "validate_data.py"
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}

$candidates = @()

if (-not [string]::IsNullOrWhiteSpace($env:PYTHON)) {
    $candidates += @{ Command = $env:PYTHON; Args = @() }
}

$codexPython = Join-Path $env:USERPROFILE ".cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
if (Test-Path -LiteralPath $codexPython) {
    $candidates += @{ Command = $codexPython; Args = @() }
}

$pythonCommand = Get-Command python.exe -ErrorAction SilentlyContinue
if ($pythonCommand) {
    $candidates += @{ Command = $pythonCommand.Source; Args = @() }
}

$pyCommand = Get-Command py.exe -ErrorAction SilentlyContinue
if ($pyCommand) {
    $candidates += @{ Command = $pyCommand.Source; Args = @("-3") }
}

foreach ($candidate in $candidates) {
    try {
        & $candidate.Command @($candidate.Args) --version | Out-Null
        & $candidate.Command @($candidate.Args) $scriptPath --project-root $ProjectRoot
        exit $LASTEXITCODE
    }
    catch {
        continue
    }
}

throw "Python 3 was not found. Install Python 3 or set PYTHON to a Python executable."
