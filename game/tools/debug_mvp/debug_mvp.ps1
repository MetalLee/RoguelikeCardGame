param(
    [string]$EncounterId,
    [int]$Seed = -1,
    [string]$StarterDeck,
    [string[]]$AddCard = @(),
    [string[]]$RewardPackId = @(),
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "debug_mvp.py"
$arguments = @($pythonScript)

if ($EncounterId) {
    $arguments += @("--encounter-id", $EncounterId)
}

if ($Seed -ge 0) {
    $arguments += @("--seed", $Seed.ToString())
}

if ($StarterDeck) {
    $arguments += @("--starter-deck", $StarterDeck)
}

foreach ($cardId in $AddCard) {
    $arguments += @("--add-card", $cardId)
}

foreach ($packId in $RewardPackId) {
    $arguments += @("--reward-pack-id", $packId)
}

if ($OutputDir) {
    $arguments += @("--output-dir", $OutputDir)
}

python @arguments
