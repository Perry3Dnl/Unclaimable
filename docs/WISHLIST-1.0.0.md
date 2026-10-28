# Unclaimable 1.0.0 additive-feature wishlist

All features below are **opt-in** and must preserve the public and binary API, .NET target frameworks, existing default options, and validation behavior of 0.8.5. No 1.0.0 package is to be published without explicit authorization.

| Feature | Design | Release gate | Implementation status |
| --- | --- | --- | --- |
| Validation presets | Named factories returning fresh `Options` instances. No changes to `new Options()` or `new Checker()`. | Pin each preset's intended acceptance/rejection corpus; document that using a preset opts into different rules. | Implemented; latest CI pending. |
| Explainable validation | Read-only formatting of existing `CheckDetailed`/`Diagnostic` data. Do not modify result types, existing messages or matching. | Ensure diagnostic parity and do not expose raw identifiers in telemetry. | Implemented; previous CI passed, latest CI pending. |
| Dataset quality & collision detection | Build-time/offline duplicate and normalized collision reports. No automatic removal or category changes. | Stable deterministic output; zero changes to embedded datasets unless separately reviewed. | Full audit completed: zero new normalized entries; 410 existing identity entries separately exposed exact-only by explicit opt-in; latest CI pending. |
| Policy configuration validator | Separate, opt-in inspection of `Options`, returning actionable errors/warnings without modifying the options or checker construction. | Legacy constructor exceptions and defaults remain unchanged. | Implemented; previous CI passed. |

## Compatibility baseline

- NuGet API comparison against the five published 0.8.5 packages; Files is new and must not change existing Core behavior.
- Preserve the existing 0.8.0 frozen defaults test as well as the 0.8.5 package compatibility checks.
- Compare allowed/rejected decisions against prior versions before accepting any dataset change.
- No new runtime dependencies or additional allocations on the ordinary `IsClaimable` path.
- Documentation must clearly distinguish analyzer warnings from actual validation failures.

## Implementation phases

1. Introduce opt-in diagnostics helpers and non-throwing policy inspection.
2. Add tested named presets with explicit policy differences; never silently change defaults.
3. Add deterministic offline dataset quality and collision reporting without altering package data.
4. Verify build, tests, package API compatibility, sample consumers, and baseline behavior before marking a feature complete.

## Latest security, performance and compatibility work

- Added malformed UTF-16, control/format-character and long-input security regressions.
- Added a same-source dual-consumer comparison using 0.8.5 NuGet packages versus current 1.0.0 source, over a deterministic corpus for Core and Core + Extended. This check is mandatory in the compatibility workflow; latest CI confirmation pending.
- Added clean packaged-consumer smoke coverage of the preset factories and opt-in identity registration.
- Added separate Core and Core + Extended construction/latency/allocation benchmark workflow; measured results pending.
- **Do not change default behavior or publish any 1.0.0 package without explicit approval.**
