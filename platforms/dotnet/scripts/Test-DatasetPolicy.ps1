param(
    [string]$DataPath = (Join-Path $PSScriptRoot "../../../data"),
    [string]$SafeCorpusPath = (Join-Path $PSScriptRoot "../../../conformance/safe-usernames.json"),
    [int]$ShortTokenMaximumLength = 6,
    [int]$MinimumSafeCorpusSize = 150
)

$ErrorActionPreference = "Stop"

function Normalize-Value([string]$Value) {
    return $Value.Normalize([System.Text.NormalizationForm]::FormKC).ToLowerInvariant()
}

function Get-CompactValue([string]$Value) {
    $normalized = Normalize-Value $Value
    return [System.Text.RegularExpressions.Regex]::Replace($normalized, "[^\p{L}\p{Nd}]", "")
}

function Add-DatasetEntry {
    param(
        [System.Collections.Generic.List[object]]$Entries,
        [System.Collections.Generic.List[string]]$Errors,
        [hashtable]$SeenInFile,
        [string]$FilePath,
        [string]$Category,
        [string]$Language,
        [string]$Value,
        [bool]$Partial,
        [string]$Kind
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $Errors.Add("$FilePath contains a blank $Kind entry.")
        return
    }

    $normalized = Normalize-Value $Value
    if ($SeenInFile.ContainsKey($normalized)) {
        $Errors.Add("$FilePath emits duplicate normalized value '$Value' from $Kind and $($SeenInFile[$normalized]).")
        return
    }

    $SeenInFile[$normalized] = $Kind
    $compact = Get-CompactValue $Value

    if ($Partial) {
        $minimumLength = if ($Category -eq "profanity") { 6 } else { 7 }
        if ($compact.Length -lt $minimumLength) {
            $Errors.Add("$FilePath marks '$Value' as partial-safe with compact length $($compact.Length); category '$Category' requires at least $minimumLength characters by dataset policy.")
        }
    }

    $Entries.Add([pscustomobject]@{
        Category = $Category
        Language = $Language
        Value = $Value
        Normalized = $normalized
        Compact = $compact
        Partial = $Partial
        File = $FilePath
        Kind = $Kind
    })
}

$resolvedDataPath = (Resolve-Path $DataPath).Path
$resolvedSafeCorpusPath = (Resolve-Path $SafeCorpusPath).Path
$datasetFiles = Get-ChildItem -Path $resolvedDataPath -Recurse -Filter reserved.json -File | Sort-Object FullName

$entries = [System.Collections.Generic.List[object]]::new()
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($file in $datasetFiles) {
    $relativePath = [System.IO.Path]::GetRelativePath((Split-Path $resolvedDataPath -Parent), $file.FullName).Replace('\\', '/')
    $dataset = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json

    $schema = [int]$dataset.schema
    $category = [string]$dataset.category
    $description = [string]$dataset.description
    $language = if ($null -ne $dataset.language) { [string]$dataset.language } else { "legacy-default" }

    if ($schema -ne 1 -and $schema -ne 2) {
        $errors.Add("$relativePath declares unsupported schema '$schema'.")
    }

    if ([string]::IsNullOrWhiteSpace($category)) {
        $errors.Add("$relativePath has no category.")
    }

    if ([string]::IsNullOrWhiteSpace($description)) {
        $errors.Add("$relativePath has no description.")
    }

    if ($schema -lt 2 -and (($null -ne $dataset.partialValues) -or ($null -ne $dataset.combinations))) {
        $errors.Add("$relativePath uses schema-v2 partial/combinations fields while declaring schema $schema.")
    }

    $seenInFile = @{}

    foreach ($value in @($dataset.values)) {
        Add-DatasetEntry -Entries $entries -Errors $errors -SeenInFile $seenInFile -FilePath $relativePath -Category $category -Language $language -Value ([string]$value) -Partial $false -Kind "values"
    }

    foreach ($value in @($dataset.partialValues)) {
        Add-DatasetEntry -Entries $entries -Errors $errors -SeenInFile $seenInFile -FilePath $relativePath -Category $category -Language $language -Value ([string]$value) -Partial $true -Kind "partialValues"
    }

    foreach ($combination in @($dataset.combinations)) {
        if ($null -eq $combination) {
            continue
        }

        $isPartial = [bool]$combination.partial
        foreach ($root in @($combination.roots)) {
            foreach ($suffix in @($combination.suffixes)) {
                $value = "{0}{1}" -f [string]$root, [string]$suffix
                Add-DatasetEntry -Entries $entries -Errors $errors -SeenInFile $seenInFile -FilePath $relativePath -Category $category -Language $language -Value $value -Partial $isPartial -Kind "combinations"
            }
        }
    }
}

$safeValues = @((Get-Content -Path $resolvedSafeCorpusPath -Raw | ConvertFrom-Json))
if ($safeValues.Count -lt $MinimumSafeCorpusSize) {
    $errors.Add("Known-safe corpus contains $($safeValues.Count) values; expected at least $MinimumSafeCorpusSize so coverage cannot silently shrink.")
}

$safeRecords = [System.Collections.Generic.List[object]]::new()
$safeSeen = @{}
foreach ($rawSafeValue in $safeValues) {
    $safeValue = [string]$rawSafeValue
    if ([string]::IsNullOrWhiteSpace($safeValue)) {
        $errors.Add("Known-safe corpus contains a blank value.")
        continue
    }

    $normalized = Normalize-Value $safeValue
    if ($safeSeen.ContainsKey($normalized)) {
        $errors.Add("Known-safe corpus contains duplicate normalized value '$safeValue'.")
        continue
    }

    $safeSeen[$normalized] = $true
    $safeRecords.Add([pscustomobject]@{
        Value = $safeValue
        Normalized = $normalized
        Compact = Get-CompactValue $safeValue
    })
}

# The runtime can have the same value in multiple categories. For collision analysis,
# one representative record per normalized value/partial-state is sufficient.
$collisionEntries = @($entries | Sort-Object Normalized, Partial -Unique)
$guardedCollisions = [System.Collections.Generic.List[object]]::new()
$guardedSeen = @{}

foreach ($entry in $collisionEntries) {
    foreach ($safe in $safeRecords) {
        if ($safe.Normalized -eq $entry.Normalized) {
            $errors.Add("Known-safe username '$($safe.Value)' is also a concrete reserved dataset value '$($entry.Value)' ($($entry.Category)).")
            continue
        }

        if ([string]::IsNullOrEmpty($entry.Compact) -or $entry.Compact.Length -gt $ShortTokenMaximumLength) {
            continue
        }

        $exactContains = $safe.Normalized.Length -gt $entry.Normalized.Length -and $safe.Normalized.Contains($entry.Normalized, [System.StringComparison]::Ordinal)
        $compactContains = $safe.Compact.Length -gt $entry.Compact.Length -and $safe.Compact.Contains($entry.Compact, [System.StringComparison]::Ordinal)
        if (-not $exactContains -and -not $compactContains) {
            continue
        }

        if ($entry.Partial) {
            $errors.Add("Partial-safe short token '$($entry.Value)' ($($entry.Category)) collides with known-safe username '$($safe.Value)'.")
            continue
        }

        $guardKey = "$($entry.Normalized)|$($safe.Normalized)"
        if (-not $guardedSeen.ContainsKey($guardKey)) {
            $guardedSeen[$guardKey] = $true
            $guardedCollisions.Add([pscustomobject]@{
                Token = $entry.Value
                SafeUsername = $safe.Value
                Category = $entry.Category
            })
        }
    }
}

$totalEntries = $entries.Count
$uniqueValues = @($entries.Normalized | Sort-Object -Unique).Count
$partialEntries = @($entries | Where-Object Partial).Count
$shortTokens = @($collisionEntries | Where-Object { $_.Compact.Length -le $ShortTokenMaximumLength }).Count

Write-Output "DATASET_POLICY_BEGIN"
Write-Output "Concrete entries: $totalEntries"
Write-Output "Unique normalized values: $uniqueValues"
Write-Output "Explicit partial-safe entries: $partialEntries"
Write-Output "Short tokens reviewed (<= $ShortTokenMaximumLength compact chars): $shortTokens"
Write-Output "Known-safe corpus size: $($safeRecords.Count)"
Write-Output "Guarded short-token collisions: $($guardedCollisions.Count)"

if ($guardedCollisions.Count -gt 0) {
    Write-Output "Guarded collision examples:"
    foreach ($collision in @($guardedCollisions | Select-Object -First 25)) {
        Write-Output "- '$($collision.Token)' ($($collision.Category)) occurs inside known-safe '$($collision.SafeUsername)' but remains exact-only."
    }
}
Write-Output "DATASET_POLICY_END"

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    $summary = [System.Collections.Generic.List[string]]::new()
    $summary.Add("## Dataset policy guard")
    $summary.Add("")
    $summary.Add("- Concrete entries: **$totalEntries**")
    $summary.Add("- Unique normalized values: **$uniqueValues**")
    $summary.Add("- Explicit partial-safe entries: **$partialEntries**")
    $summary.Add("- Short tokens reviewed: **$shortTokens**")
    $summary.Add("- Known-safe usernames: **$($safeRecords.Count)**")
    $summary.Add("- Guarded short-token collisions: **$($guardedCollisions.Count)**")

    if ($guardedCollisions.Count -gt 0) {
        $summary.Add("")
        $summary.Add("### Guarded collision examples")
        $summary.Add("")
        $summary.Add("These are deliberate regression guards: the short reserved token is exact-only and must not make the known-safe username unclaimable through generic partial matching.")
        $summary.Add("")
        $summary.Add("| Reserved token | Category | Known-safe identifier |")
        $summary.Add("| --- | --- | --- |")
        foreach ($collision in @($guardedCollisions | Select-Object -First 20)) {
            $summary.Add("| ``$($collision.Token)`` | ``$($collision.Category)`` | ``$($collision.SafeUsername)`` |")
        }
    }

    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value ($summary -join "`n")
}

if ($errors.Count -gt 0) {
    Write-Error ("Dataset policy validation failed:`n- " + ($errors -join "`n- "))
    exit 1
}

Write-Output "Dataset policy validation passed."
