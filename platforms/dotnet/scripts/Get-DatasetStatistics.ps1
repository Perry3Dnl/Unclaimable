param(
    [string]$DataPath = (Join-Path $PSScriptRoot "../../../data")
)

$resolvedDataPath = (Resolve-Path $DataPath).Path
$files = Get-ChildItem -Path $resolvedDataPath -Recurse -Filter reserved.json -File

$entries = foreach ($file in $files) {
    $dataset = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json

    foreach ($value in @($dataset.values)) {
        [pscustomobject]@{
            Category = [string]$dataset.category
            Value = [string]$value
        }
    }
}

$groups = $entries | Group-Object Category | Sort-Object Name
$totalEntries = 0
$totalUnique = 0

Write-Output "DATASET_STATISTICS_BEGIN"
Write-Output "| Category | Entries | Unique values |"
Write-Output "| --- | ---: | ---: |"

foreach ($group in $groups) {
    $entryCount = @($group.Group).Count
    $uniqueCount = @($group.Group.Value | Sort-Object -Unique).Count
    $totalEntries += $entryCount
    $totalUnique += $uniqueCount

    Write-Output "| $($group.Name) | $entryCount | $uniqueCount |"
}

Write-Output "| **Total** | **$totalEntries** | **$totalUnique** |"
Write-Output "DATASET_STATISTICS_END"
