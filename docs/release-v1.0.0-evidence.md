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
