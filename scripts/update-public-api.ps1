param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$SyncUnshipped,
    [switch]$Ship
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Projects {
    param([string]$Root)

    $projects =
        Get-ChildItem -Path $Root -Recurse -File -Filter "PublicAPI.Unshipped*.txt" |
        ForEach-Object {
            Get-ChildItem -Path $_.Directory.FullName -File -Filter "*.csproj"
        } |
        Sort-Object -Property FullName -Unique

    return @($projects)
}

function Invoke-DotNetFormat {
    param([System.IO.FileInfo[]]$Projects)

    foreach ($project in $Projects) {
        $commandText = 'dotnet format "{0}" analyzers --diagnostics RS0016 --severity warn' -f $project.FullName
        Write-Host "Running: $commandText"
        & dotnet format $project.FullName analyzers --diagnostics RS0016 --severity warn
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet format failed for $($project.FullName)"
        }
    }
}

function Read-ApiFile {
    param([string]$Path)

    $directives = [System.Collections.Generic.List[string]]::new()
    $entries = [System.Collections.Generic.List[string]]::new()

    if (-not (Test-Path -LiteralPath $Path)) {
        return @{ Directives = @(); Entries = @() }
    }

    foreach ($line in (Get-Content -LiteralPath $Path -Encoding utf8)) {
        $stripped = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($stripped)) {
            continue
        }

        if ($stripped.StartsWith("#")) {
            $directives.Add($stripped)
        }
        else {
            $entries.Add($stripped)
        }
    }

    return @{ Directives = @($directives); Entries = @($entries) }
}

function Get-UniqueInOrder {
    param([string[]]$Values)

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $result = [System.Collections.Generic.List[string]]::new()

    foreach ($value in $Values) {
        if (-not $seen.Add($value)) {
            continue
        }

        $result.Add($value)
    }

    return @($result)
}

function Write-ApiFile {
    param(
        [string]$Path,
        [string[]]$Directives,
        [string[]]$Entries
    )

    $output = [System.Collections.Generic.List[string]]::new()

    if ($Directives.Count -gt 0) {
        $output.AddRange((Get-UniqueInOrder -Values $Directives))
    }

    if ($Entries.Count -gt 0) {
        if ($output.Count -gt 0) {
            $output.Add("")
        }

        $sortedEntries = Get-UniqueInOrder -Values $Entries | Sort-Object
        $output.AddRange($sortedEntries)
    }

    Set-Content -LiteralPath $Path -Value $output -Encoding utf8BOM
}

function Invoke-PromoteUnshipped {
    param([string]$Root)

    Get-ChildItem -Path $Root -Recurse -File -Filter "PublicAPI.Unshipped*.txt" |
    Sort-Object -Property FullName |
    ForEach-Object {
        $unshippedPath = $_.FullName
        $shippedPath = Join-Path $_.DirectoryName ($_.Name -replace "Unshipped", "Shipped")

        if (-not (Test-Path -LiteralPath $shippedPath)) {
            return
        }

        $unshipped = Read-ApiFile -Path $unshippedPath
        $toShipList = [System.Collections.Generic.List[string]]::new()
        $keptUnshippedList = [System.Collections.Generic.List[string]]::new()
        foreach ($entry in $unshipped.Entries) {
            if ($entry.StartsWith("*REMOVED*")) {
                $keptUnshippedList.Add($entry)
            }
            else {
                $toShipList.Add($entry)
            }
        }

        $toShip = @($toShipList)
        $keptUnshipped = @($keptUnshippedList)

        if ($toShip.Count -eq 0) {
            return
        }

        $shipped = Read-ApiFile -Path $shippedPath
        $combinedDirectives = Get-UniqueInOrder -Values @($shipped.Directives + $unshipped.Directives)

        Write-ApiFile -Path $shippedPath -Directives $combinedDirectives -Entries @($shipped.Entries + $toShip)
        Write-ApiFile -Path $unshippedPath -Directives $unshipped.Directives -Entries $keptUnshipped

        Write-Host "Promoted $($toShip.Count) entries: $unshippedPath -> $shippedPath"
    }
}

if (-not $SyncUnshipped -and -not $Ship) {
    $SyncUnshipped = $true
    $Ship = $true
}

$resolvedRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$projects = Get-Projects -Root $resolvedRoot

if ($SyncUnshipped) {
    Invoke-DotNetFormat -Projects $projects
}

if ($Ship) {
    Invoke-PromoteUnshipped -Root $resolvedRoot
}
