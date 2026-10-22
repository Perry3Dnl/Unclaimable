# Unclaimable 1.0.0 additive-feature wishlist

All features below are **opt-in** and must preserve the public and binary API, .NET target frameworks, existing default options, and validation behavior of 0.8.5. No 1.0.0 package is to be published without explicit authorization.

| Feature | Design | Release gate |
| --- | --- | --- |
| Validation presets | Named factories returning fresh `Options` instances. No changes to `new Options()` or `new Checker()`. | Pin each preset's intended acceptance/rejection corpus; document that using a preset opts into different rules. |
| Explainable validation | Read-only formatting of existing `CheckDetailed`/`Diagnostic` data. Do not modify result types, existing messages or matching. | Ensure diagnostic parity and do not expose raw identifiers in telemetry. |
| Dataset quality & collision detection | Build-time/offline duplicate and normalized collision reports. No automatic removal or category changes. | Stable deterministic output; zero changes to embedded datasets unless separately reviewed. |
| Policy configuration validator | Separate, opt-in inspection of `Options`, returning actionable errors/warnings without modifying the options or checker construction. | Legacy constructor exceptions and defaults remain unchanged. |

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
