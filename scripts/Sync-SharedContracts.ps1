[CmdletBinding()]
param(
    [string]$FrontendPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'ConsuCare_frontend'),
    [string]$MonolithPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'Project_ConsuCare'),
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$backendRoot = Split-Path -Parent $PSScriptRoot
$sharedFiles = @(
    'ConsuCare.Shared.csproj',
    'Dtos\Dtos.cs',
    'Models\Entities.cs'
)
$targets = @($FrontendPath, $MonolithPath)

foreach ($targetRoot in $targets) {
    if (-not (Test-Path -LiteralPath $targetRoot -PathType Container)) {
        throw "Repository path not found: $targetRoot"
    }

    $targetFiles = @($sharedFiles | ForEach-Object {
        Join-Path $targetRoot (Join-Path 'src\ConsuCare.Shared' $_)
    })

    if (-not $Check) {
        $gitStatus = & git -C $targetRoot status --porcelain -- @targetFiles
        if ($LASTEXITCODE -ne 0) {
            throw "Could not inspect target repository: $targetRoot"
        }
        if ($gitStatus) {
            throw "Refusing to overwrite locally modified shared contract files in $targetRoot. Commit or preserve those changes, then retry."
        }
    }

    for ($index = 0; $index -lt $sharedFiles.Count; $index++) {
        $sourceFile = Join-Path $backendRoot (Join-Path 'src\ConsuCare.Shared' $sharedFiles[$index])
        $targetFile = $targetFiles[$index]
        if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
            throw "Canonical contract file not found: $sourceFile"
        }
        if (-not (Test-Path -LiteralPath (Split-Path -Parent $targetFile) -PathType Container)) {
            throw "Shared project directory not found in target repository: $targetRoot"
        }

        if ($Check) {
            if (-not (Test-Path -LiteralPath $targetFile -PathType Leaf)) {
                throw "Contract file is missing from $targetRoot`: $($sharedFiles[$index])"
            }
            $sourceHash = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash
            $targetHash = (Get-FileHash -LiteralPath $targetFile -Algorithm SHA256).Hash
            if ($sourceHash -ne $targetHash) {
                throw "Shared contracts are out of sync in $targetRoot`: $($sharedFiles[$index]). Run scripts\Sync-SharedContracts.ps1 from the backend repository."
            }
        }
        else {
            Copy-Item -LiteralPath $sourceFile -Destination $targetFile -Force
        }
    }

    Write-Output "$(if ($Check) { 'Verified' } else { 'Synchronized' }): $targetRoot"
}
