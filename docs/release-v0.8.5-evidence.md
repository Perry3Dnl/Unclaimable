# Unclaimable v0.8.5 — Release evidence and draft changelog notes

Status: **unreleased**. Branch: `feature/0.8.5`. Baseline: immutable `v0.8.4` release tag.

## Scope reviewed
GitHub compare `v0.8.4...feature/0.8.5` at preparation found 221 commits and changes in Core, Extended, test cases, benchmark tooling and NuGet project metadata. Review findings below distinguish source/binary compatibility from deliberate changes to validation decisions.

## Core changes
- Added `Options.FalsePositiveSensitivity` enum option: Low (default, no slang-match suppression), Medium and High. Higher sensitivities may allow certain short one-sided slang/profanity substrings; embedded terms with adjoining text on both sides remain blocked. Existing default suppression behavior preserved.
- Internal `PartialCandidateIndex` groups prefixes by first 1–3 UTF-16 characters for eligible embedded matches. Existing compact, exception and match-order logic is covered by targeted regression tests. New index is used only when partial matching is enabled and lists reach a size threshold.
- Internal normalization reuse, reservation value-type registration and inline partial-entry storage reduce allocations. Pre-sized dictionaries reduce large-config construction overhead.
- Core profanity datasets expanded (including Dutch explicit terms and English Core entries). These can change acceptance results for previously allowed identifiers.

## Extended changes
- Dataset-only package continues using Core's `Options.Reserve(..., ReservedMatchMode.Default)` pipeline through `UseExtendedData`. No independent Extended matching engine.
- New language-specific slang/profanity data files: Czech, German, English, Spanish, French, Hungarian, Indonesian, Italian, Dutch, Polish, Portuguese, Romanian, Swedish, Turkish and Vietnamese.
- Expanded categories include celebrities, companies, cryptocurrency, entertainment, fiction, government, hate speech, historical figures, organizations, media, platforms, professions, public figures and slang.
- Prior complete inventory reported 43,780 Extended raw entries across 43 JSON datasets; recalculate from the release commit before publication, including enabled/duplicate registration counts.

## Compatibility and behavioral notes
- API additions are additive in reviewed files: `FalsePositiveSensitivity` and `Options.FalsePositiveSensitivity`; no intended public API removals identified by repository file-diff review. **This is not a formal public API binary-diff result**; do not claim fully verified source/binary compatibility until package validation and API baseline comparison are complete.
- Public semantics change as lists grow: more identifiers may be rejected. Medium/High false-positive sensitivity may permit some previously blocked one-sided slang substrings. Low remains default.
- Major engine changes should preserve existing eligibility, compact matching, rule exceptions, ordered match precedence, diagnostic offsets, thread safety and no-match allocations. Test suite alone is not exhaustive.
- Existing v0.8.0 baseline package validation and ecosystem checks should be kept green.

## Benchmarks and thresholds
- Versioned matrix: `.github/workflows/extended-performance-085.yml`. It checks out the actual `v0.8.4` tag and `feature/0.8.5`, executes the same BenchmarkDotNet harness separately for Core and Core+Extended.
- Last verified matrix (run 38552005773): Core warm construction 2.292 ms -> 1.406 ms, allocations 1,297,233 B -> 1,302,813 B. Core+Extended warm construction 66.950 ms -> 72.042 ms, allocations 35,756,663 B -> 28,605,751 B. Both comply with maximum +15% time and managed allocations in that run; ordinary validations remain 0 B allocated.
- Cold-process matrix: `.github/workflows/cold-memory-085.yml`. Each version runs ten independent processes for Core and Core+Extended, capturing initialization stopwatch duration, process-side allocated bytes, GC heap after full collection and working set. Source build excluded from timing. These measures differ from warm checker-construction cost; heap after GC is **not an isolated retained-checker footprint**.

## Fresh-process cold audit results (20 processes per version: 10 Core, 10 Core+Extended)

Source: successful GitHub run 38554147577; .NET 8, independent runners for each version; timings are median of 10 new processes per version/configuration. Time includes Options initialization, Extended first dataset load (when enabled), and Checker construction, not the .NET host startup and not compilation.

| Cold metric | 0.8.4 | 0.8.5 | Interpretation |
| --- | ---: | ---: | --- |
| Core initialization time median | 104.35 ms | 67.42 ms | 35.4% faster |
| Core + Extended initialization time median | 274.57 ms | 214.00 ms | 22.1% faster |
| Core first-init managed allocations (typical) | ~8.56 MB | ~8.58 MB | ~0.2% higher |
| Core + Extended first-init allocations (typical) | ~58.74 MB | ~54.40 MB | ~7.4% lower |
| Core post-GC heap (typical) | ~2.85 MB | ~2.84 MB | roughly unchanged |
| Core + Extended post-GC heap (typical) | ~28.37 MB | ~26.91 MB | ~5.2% lower |
| Core process working set (typical) | ~59 MB | ~59 MB | roughly unchanged |
| Core + Extended process working set (typical) | ~83–86 MB | ~87.5 MB | increased; investigate if strict RSS cap matters |

Post-GC total heap is not an isolated object-graph retained-memory measurement; each version is run in a separate GitHub runner, so physical host differences may affect absolute timing. Repeated paired hardware testing and dedicated retained-heap measurements would provide stronger confirmation.

## Remaining release gates
- Review cold-process audit above and optionally repeat on paired hardware; inspect Core+Extended process RSS increase despite lower managed heap.
- Confirm .NET compatibility, test/coverage, package validation, and smoke tests against the final release commit.
- Audit full dataset totals, duplicates, provenance, language mapping, and representative accepted/rejected values for changed categories.
- Do not publish, merge or tag until explicitly approved.

## Evidence
- Code diff: https://github.com/Perry3Dnl/Unclaimable/compare/v0.8.4...feature/0.8.5
- Four-case warm benchmark: https://github.com/Perry3Dnl/Unclaimable/actions/runs/38552005773
- Cold benchmark: https://github.com/Perry3Dnl/Unclaimable/actions/runs/38554147577

## Release candidate documentation and QC pass — 2026-10-14

The GitHub README, all five package `README.NUGET.md` files (Core, Extended, ASP.NET Core, Email, FluentValidation), and `CHANGELOG.md` have been prepared for v0.8.5 on the development branch. Release notes explicitly describe the expanded detection coverage, the optional `FalsePositiveSensitivity` behavior, Core-only versus Core+Extended performance, the +7.6% Extended warm-construction trade-off, and the observation that working set increased in the cold-process audit.

Package pages are marked **release candidate / not yet published** and give a stable v0.8.4 installation command until publication. Package versions in `Directory.Build.props` are set to 0.8.5 but no NuGet publish or tag action has been taken.

### QC review findings and limits

- The diff from tag `v0.8.4` to `feature/0.8.5` includes added Core option/enum APIs; no source-level public API removals were identified in the inspected changed files. **Formal binary API-baseline compatibility has not independently been established.**
- `Unclaimable.Extended` remains a data registration extension: `UseExtendedData()` explicitly calls `Options.Reserve`, feeding Core's matcher; disabling Core partial matching still disables Extended substring detections in regression tests.
- Existing targeted regression cases cover default Low false-positive sensitivity, Medium/High one-sided suppression, wrapped-term blocking, category toggles, short Unicode terms, long indexed candidate lists, disabled compact matching and rule exceptions.
- Dataset expansion intentionally changes validation outputs. A complete human-language accuracy/provenance audit of all records is **not** certified by passing automated tests. Raw file entry counts do not equal unique normalized registered terms.
- The latest verified passing full test-and-pack run before these documentation edits is linked in GitHub CI history. Final commit checks must pass before release approval.
- Do not describe v0.8.5 as already available on NuGet; installation examples on package README files point to the currently released v0.8.4 until v0.8.5 is published.

### Release approval remaining

1. Verify final-commit .NET full test, coverage threshold, five NuGet package validations, and packaged-consumer smoke tests.
2. Verify final-commit frozen defaults and ecosystem compatibility workflows.
3. Conduct explicit public API compatibility comparison and final dataset quality/count review.
4. Confirm package-page rendering and the published-version installation commands immediately before release.


## Final dataset audit findings (2026-10-14)

- Audited all 43 Extended JSON files with a new repeatable CI validator (`platforms/dotnet/scripts/audit_extended_085.py`). Prior to corrections: 43,780 raw records and 43,623 NFKC+lower normalized unique values; zero duplicate normalized values within a single JSON file; one cross-category collision; eight C1 control-character encoding artifacts.
- Repaired four quoted Polish institution names containing Windows-1252 C1 quote codes; repaired four Slovenian geography names where the `ž` letter was stored as C1 code U+009E.
- Removed the `palermo` record incorrectly classified as Portuguese slang profanity (also a sports/geographical identity). After correction: **43,779 raw Extended records**; expected **43,622 normalized unique values**; the actual post-change CI audit is authoritative.
- Normalized duplicates across separate files can represent intentional same-language/alternate dataset overlap; this scan only identifies collisions, and does not certify idiomatic accuracy or completeness of language labels.
- Added CI checking all five candidate packages against the published v0.8.4 NuGet package API baseline; first attempt failed in workflow setup because it lacked the project's required .NET 11 preview SDK, not because of an API diagnostic. SDK setup was updated; final API verdict awaits the new run.
- No changes to public API were made as part of these dataset corrections. Validation outcomes may change for corrected terms.

