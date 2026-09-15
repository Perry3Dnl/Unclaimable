param(
    [string]$BaseRef,
    [string]$HeadRef = "HEAD",
    [int]$MaximumRowsPerSection = 40
)

$ErrorActionPreference = "Stop"

function Normalize-Value([string]$Value) {
    return $Value.Normalize([System.Text.NormalizationForm]::FormKC).ToLowerInvariant()
}

function Resolve-Language($Dataset) {
    if ($null -ne $Dataset.language -and -not [string]::IsNullOrWhiteSpace([string]$Dataset.language)) {
        return ([string]$Dataset.language).ToLowerInvariant()
    }

    $category = ([string]$Dataset.category).ToLowerInvariant()
    if ($category -eq "brands" -or $category -eq "technology") {
        return "global"
    }

    return "en"
}

function Get-DatasetEntriesAtRef([string]$Ref) {
    $paths = @(& git ls-tree -r --name-only $Ref -- data)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not enumerate data files at git ref '$Ref'."
    }

    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($path in @($paths | Where-Object { $_ -match '(^|/)reserved\.json$' } | Sort-Object)) {
        $spec = "{0}:{1}" -f $Ref, $path
        $jsonText = (@(& git show $spec) -join "`n")
        if ($LASTEXITCODE -ne 0) {
            throw "Could not read '$path' at git ref '$Ref'."
        }

        $dataset = $jsonText | ConvertFrom-Json
        $category = [string]$dataset.category
        $language = Resolve-Language $dataset

        foreach ($value in @($dataset.values)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
                $entries.Add([pscustomobject]@{
                    Value = [string]$value
                    Normalized = Normalize-Value ([string]$value)
                    Category = $category
                    Language = $language
                    Partial = $false
                    Source = $path
                })
            }
        }

        foreach ($value in @($dataset.partialValues)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
                $entries.Add([pscustomobject]@{
                    Value = [string]$value
                    Normalized = Normalize-Value ([string]$value)
                    Category = $category
                    Language = $language
                    Partial = $true
                    Source = $path
                })
            }
        }

        foreach ($combination in @($dataset.combinations)) {
            if ($null -eq $combination) {
                continue
            }

            $partial = [bool]$combination.partial
            foreach ($root in @($combination.roots)) {
                foreach ($suffix in @($combination.suffixes)) {
                    $value = "{0}{1}" -f [string]$root, [string]$suffix
                    if (-not [string]::IsNullOrWhiteSpace($value)) {
                        $entries.Add([pscustomobject]@{
                            Value = $value
                            Normalized = Normalize-Value $value
                            Category = $category
                            Language = $language
                            Partial = $partial
                            Source = $path
                        })
                    }
                }
            }
        }
    }

    return @($entries)
}

function Get-ExactMap($Entries) {
    $map = @{}
    foreach ($entry in $Entries) {
        $key = "$($entry.Language)|$($entry.Category)|$($entry.Normalized)"
        if (-not $map.ContainsKey($key)) {
            $map[$key] = $entry
        }
    }
    return $map
}

function Get-PartialMap($Entries) {
    $map = @{}
    foreach ($entry in @($Entries | Where-Object Partial)) {
        $key = "$($entry.Language)|$($entry.Category)|$($entry.Normalized)"
        if (-not $map.ContainsKey($key)) {
            $map[$key] = $entry
        }
    }
    return $map
}

function Get-CategoryMap($Entries) {
    $working = @{}
    foreach ($entry in $Entries) {
        $key = "$($entry.Language)|$($entry.Normalized)"
        if (-not $working.ContainsKey($key)) {
            $working[$key] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }
        [void]$working[$key].Add([string]$entry.Category)
    }

    $map = @{}
    foreach ($key in $working.Keys) {
        $map[$key] = (@($working[$key]) | Sort-Object) -join ","
    }
    return $map
}

function Add-EntrySection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title,
        [array]$Records,
        [int]$MaximumRows
    )

    $Lines.Add("")
    $Lines.Add("### $Title ($($Records.Count))")
    $Lines.Add("")

    if ($Records.Count -eq 0) {
        $Lines.Add("None.")
        return
    }

    $Lines.Add("| Identifier | Category | Language |")
    $Lines.Add("| --- | --- | --- |")
    foreach ($record in @($Records | Select-Object -First $MaximumRows)) {
        $Lines.Add("| ``$($record.Value)`` | ``$($record.Category)`` | ``$($record.Language)`` |")
    }

    if ($Records.Count -gt $MaximumRows) {
        $Lines.Add("")
        $Lines.Add("_Showing first $MaximumRows of $($Records.Count)._ ")
    }
}

if ([string]::IsNullOrWhiteSpace($BaseRef) -or $BaseRef -match '^0+$') {
    $BaseRef = (@(& git rev-parse "$HeadRef^") -join "").Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($BaseRef)) {
        throw "A dataset comparison base could not be resolved."
    }
}

$baseEntries = @(Get-DatasetEntriesAtRef $BaseRef)
$headEntries = @(Get-DatasetEntriesAtRef $HeadRef)

$baseExact = Get-ExactMap $baseEntries
$headExact = Get-ExactMap $headEntries
$basePartial = Get-PartialMap $baseEntries
$headPartial = Get-PartialMap $headEntries
$baseCategories = Get-CategoryMap $baseEntries
$headCategories = Get-CategoryMap $headEntries

$newlyBlocked = @($headExact.Keys | Where-Object { -not $baseExact.ContainsKey($_) } | Sort-Object | ForEach-Object { $headExact[$_] })
$newlyAllowed = @($baseExact.Keys | Where-Object { -not $headExact.ContainsKey($_) } | Sort-Object | ForEach-Object { $baseExact[$_] })
$newPartial = @($headPartial.Keys | Where-Object { -not $basePartial.ContainsKey($_) } | Sort-Object | ForEach-Object { $headPartial[$_] })
$removedPartial = @($basePartial.Keys | Where-Object { -not $headPartial.ContainsKey($_) } | Sort-Object | ForEach-Object { $basePartial[$_] })

$categoryChanges = [System.Collections.Generic.List[object]]::new()
foreach ($key in @($headCategories.Keys | Where-Object { $baseCategories.ContainsKey($_) } | Sort-Object)) {
    if ($headCategories[$key] -ne $baseCategories[$key]) {
        $parts = $key.Split('|', 2)
        $categoryChanges.Add([pscustomobject]@{
            Language = $parts[0]
            Value = $parts[1]
            Before = $baseCategories[$key]
            After = $headCategories[$key]
        })
    }
}

$baseTotal = $baseExact.Count
$headTotal = $headExact.Count
$basePartialTotal = $basePartial.Count
$headPartialTotal = $headPartial.Count
$totalDelta = $headTotal - $baseTotal
$partialDelta = $headPartialTotal - $basePartialTotal

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("## Dataset policy diff")
$lines.Add("")
$lines.Add("Compared ``$BaseRef`` → ``$HeadRef``.")
$lines.Add("")
$lines.Add("| Metric | Base | Head | Delta |")
$lines.Add("| --- | ---: | ---: | ---: |")
$lines.Add("| Concrete category/language/value entries | $baseTotal | $headTotal | $totalDelta |")
$lines.Add("| Explicit partial-safe entries | $basePartialTotal | $headPartialTotal | $partialDelta |")
$lines.Add("| Newly blocked concrete identifiers |  | $($newlyBlocked.Count) | +$($newlyBlocked.Count) |")
$lines.Add("| Newly allowed concrete identifiers | $($newlyAllowed.Count) |  | -$($newlyAllowed.Count) |")
$lines.Add("| New partial-match entries |  | $($newPartial.Count) | +$($newPartial.Count) |")
$lines.Add("| Removed partial-match entries | $($removedPartial.Count) |  | -$($removedPartial.Count) |")
$lines.Add("| Category-set changes |  | $($categoryChanges.Count) |  |")

Add-EntrySection -Lines $lines -Title "Newly blocked identifiers" -Records $newlyBlocked -MaximumRows $MaximumRowsPerSection
Add-EntrySection -Lines $lines -Title "Newly allowed identifiers" -Records $newlyAllowed -MaximumRows $MaximumRowsPerSection
Add-EntrySection -Lines $lines -Title "New partial-match entries" -Records $newPartial -MaximumRows $MaximumRowsPerSection
Add-EntrySection -Lines $lines -Title "Removed partial-match entries" -Records $removedPartial -MaximumRows $MaximumRowsPerSection

$lines.Add("")
$lines.Add("### Category changes ($($categoryChanges.Count))")
$lines.Add("")
if ($categoryChanges.Count -eq 0) {
    $lines.Add("None.")
}
else {
    $lines.Add("| Identifier | Language | Before | After |")
    $lines.Add("| --- | --- | --- | --- |")
    foreach ($change in @($categoryChanges | Select-Object -First $MaximumRowsPerSection)) {
        $lines.Add("| ``$($change.Value)`` | ``$($change.Language)`` | ``$($change.Before)`` | ``$($change.After)`` |")
    }
    if ($categoryChanges.Count -gt $MaximumRowsPerSection) {
        $lines.Add("")
        $lines.Add("_Showing first $MaximumRowsPerSection of $($categoryChanges.Count)._ ")
    }
}

$output = $lines -join "`n"
Write-Output "DATASET_POLICY_DIFF_BEGIN"
Write-Output $output
Write-Output "DATASET_POLICY_DIFF_END"

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $output
}
