param(
    [string]$ResultsPath = "./artifacts/coverage",
    [double]$MinimumLineCoverage = 0
)

$ErrorActionPreference = "Stop"

$reports = @(Get-ChildItem -Path $ResultsPath -Filter "coverage.cobertura.xml" -Recurse -File)
if ($reports.Count -eq 0) {
    throw "No coverage.cobertura.xml report was found under '$ResultsPath'."
}

$linesCovered = 0L
$linesValid = 0L
$branchesCovered = 0L
$branchesValid = 0L

foreach ($report in $reports) {
    [xml]$document = Get-Content -Path $report.FullName -Raw
    $coverage = $document.coverage

    $linesCovered += [long]$coverage.'lines-covered'
    $linesValid += [long]$coverage.'lines-valid'
    $branchesCovered += [long]$coverage.'branches-covered'
    $branchesValid += [long]$coverage.'branches-valid'
}

if ($linesValid -le 0) {
    throw "Coverage report contains no executable production lines."
}

$lineCoverage = [math]::Round(($linesCovered / $linesValid) * 100, 2)
$branchCoverage = if ($branchesValid -gt 0) {
    [math]::Round(($branchesCovered / $branchesValid) * 100, 2)
} else {
    100.0
}

Write-Host "Coverage reports: $($reports.Count)"
Write-Host "Line coverage:   $lineCoverage% ($linesCovered/$linesValid)"
Write-Host "Branch coverage: $branchCoverage% ($branchesCovered/$branchesValid)"
Write-Host "COVERAGE_LINE_PERCENT=$lineCoverage"
Write-Host "COVERAGE_BRANCH_PERCENT=$branchCoverage"

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## Code coverage

| Metric | Coverage | Covered / total |
| --- | ---: | ---: |
| Lines | **$lineCoverage%** | $linesCovered / $linesValid |
| Branches | **$branchCoverage%** | $branchesCovered / $branchesValid |

Production assemblies only: `Unclaimable` and `Unclaimable.AspNetCore`. Test assemblies and generated files are excluded.
"@ | Add-Content -Path $env:GITHUB_STEP_SUMMARY
}

if ($lineCoverage -lt $MinimumLineCoverage) {
    throw "Line coverage is $lineCoverage%, below the required $MinimumLineCoverage%."
}
