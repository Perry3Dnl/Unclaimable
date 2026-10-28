# Unclaimable 1.0.0 — release verification evidence

Development branch: `feature/1.0.0`. This document records verified evidence, not release authorization.

## Core matching / false-positive regression pass

- Commit `e9bfff4e9f732595cc15ea633a3143a101947db4`: added boundary regression tests for one-sided slang suppression, Medium/High thresholds, embedded matches, Core profanity, and consistency across boolean/diagnostic APIs.
- Verified against GitHub Actions run [39745140413](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39745140413) on 2026-10-22:
  - Tests: **20,340 passed, 0 failed, 0 skipped**.
  - Production line coverage: **98.09% (3,701/3,773)**.
  - Branch coverage: **86.11% (2,660/3,089)**.
  - Main test-and-pack job passed, including six package builds, NuGet package validation and packaged-consumer smoke tests.
  - Additional compatible workflow runs for the same commit: .NET ecosystem (39745140431) and .NET 0.8.0 (39745140480), both successful.
  - NuGet release workflow [39745140610](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39745140610): package build/validation passed; **publish and tagged-release jobs skipped**. This is not a released package.
- Commit `f2392e226e19af85c4b56421b38233cc1a1e482e`: expanded sensitivity boundary tests for six- and seven-character terms and independent custom-match survival. **Requires a fresh successful CI result; the previous result does not cover this commit.**

## Outstanding before release

- Verify latest HEAD CI, regression suite, source and binary API baseline, behavior drift and all six package smoke tests.
- Perform multilingual false-positive and dataset audit with reviewed samples.
- Measure performance with Core alone and Core + Extended using comparable conditions.
- Validate release README, dependency metadata, license, changelog and migration guidance.
- Obtain explicit authorization before NuGet publication or GitHub release/tag.

## Backward-compatibility gate (2026-10-22)

**Strict policy:** v1.0.0 must preserve the published v0.8.5 public API, existing target framework support, package dependency contracts and established default configuration behavior. The frozen v0.8.0 defaults test remains in CI as a longer-term historical contract. Do not accept source/binary breaking changes or behavioral regressions without an explicit review and authorization.

- Changed package validation baseline from `0.8.0` to **`0.8.5`** in Core, ASP.NET Core, Email and Extended.
- Enabled NuGet package validation against published **0.8.5** for FluentValidation, which previously did not have the package validation gate.
- Updated the compatibility workflow to pack and validate all **five previously published packages**; the new Files package has no released 0.8.5 baseline.
- Existing `Options080DefaultContractTests`, behavior corpus, framework compatibility matrix, and packaged-consumer smoke tests remain in place.
- **CI result for these gate changes is pending and must be confirmed** before recording them as passed.
- NuGet package API validation does not, by itself, establish full behavioral or source compatibility. Continue regression and clean-consumer verification before release.

## Verified 1.0.0 gate before the latest additive changes

- Commit `10fabf2ce77299d8e5b9ad85e8ba3ecbee061cdf`:
  - [Main .NET workflow](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39778286716) successful: **20,357 passed, 0 failed**, production line coverage **98.11% (3,736/3,808)**, branch **86.25%**.
  - Ecosystem compatibility [39778287002](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39778287002) and backward compatibility [39778286839](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39778286839) passed.
  - NuGet validation [39778286641](https://github.com/Perry3Dnl/Unclaimable/actions/runs/39778286641) passed; **publication and tagged release skipped**.
  - The staged-vocabulary audit executed and uploaded a JSON artifact.
- This evidence does **not** apply to later commits adding presets, opt-in identity vocabulary, new regression tests, smoke checks, or performance workflow. Those changes require their own green CI run.

## New additive changes awaiting fresh CI

- Four explicit, fresh-instance policy presets (`ValidationPresets.Username`, `DisplayName`, `OrganizationName`, `GameTag`). Default `Options` and `Checker` behavior unchanged.
- `UseSupplementalIdentityData()` in the Extended package registers **410** screened identity candidates using only Core `ReservedMatchMode.Exact`. This is separate from and never loaded by the existing `UseExtendedData()`. The complete 4,782-entry staged audit corpus is not automatically used.
- A full supplemental-identity regression matrix checks all 410 entries and their prefixed/suffixed variants against Core to establish exact-only scope.
- Six-package clean consumer smoke includes optional identity and preset API calls.
- A dedicated Core versus Core+Extended benchmark workflow has been added. Measurements must be collected and compared on suitable hardware before signing off performance.
- **Still required:** Core+Extended full overlap report review, per-language linguistic/false-positive review for remaining candidates, direct 0.8.5 versus 1.0.0 behavioral differential over identical fixtures, final quality/security pass, full successful latest-commit CI, clean-consumer package checks and release metadata sign-off.

No package is authorized for publication, tagging, merging or release.
