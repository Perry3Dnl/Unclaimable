param(
    [string]$DataPath = (Join-Path $PSScriptRoot "../../../data")
)

$resolvedDataPath = (Resolve-Path $DataPath).Path
$files = Get-ChildItem -Path $resolvedDataPath -Recurse -Filter reserved.json -File

$entries = foreach ($file in $files) {
    $dataset = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json

    foreach ($value in @($dataset.values)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            [pscustomobject]@{
                Category = [string]$dataset.category
                Value = [string]$value
            }
        }
    }

    foreach ($value in @($dataset.partialValues)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            [pscustomobject]@{
                Category = [string]$dataset.category
                Value = [string]$value
            }
        }
    }

    foreach ($combination in @($dataset.combinations)) {
        foreach ($root in @($combination.roots)) {
            foreach ($suffix in @($combination.suffixes)) {
                $value = "{0}{1}" -f [string]$root, [string]$suffix
                if (-not [string]::IsNullOrWhiteSpace($value)) {
                    [pscustomobject]@{
                        Category = [string]$dataset.category
                        Value = $value
                    }
                }
            }
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
