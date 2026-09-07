# Changelog

All notable changes to Unclaimable are documented here.

## 0.1.0 - Unreleased

Initial public NuGet release.

### Added

- Runtime-neutral reserved-name datasets for roles, support/trust identities, system names, technology names, and broadly recognizable brands.
- 1,467 unique curated always-on reserved values across the shared datasets, plus 191 opt-in profanity values.
- Opt-in English profanity dataset with separate opt-in partial matching to reduce substring false positives.
- Exact matching with Unicode NFKC normalization and invariant case normalization.
- Compact matching for separator and punctuation variants.
- Bounded leetspeak and symbol-obfuscation matching.
- Selected Unicode-confusable and diacritic normalization for common impersonation attempts.
- Opt-in partial matching for embedded reserved values such as `old-admin` and `administrator2`.
- `UnclaimableStrictness` with `Basic`, `Standard`, and `Strict` presets; `Strict` enables embedded reserved-name matching so values such as `admin2`, `old-admin`, and `admin-old` are rejected.
- Explicit matching-rule overrides that take precedence over presets, plus `ResetMatchingRuleOverrides()` to resume preset behavior.
- Computed `EnabledRules` / `UnclaimableRule` flags for inspecting the final effective policy.
- Configurable minimum reserved-name length for partial matching.
- Optional fail-fast number policy through `AllowNumbers`.
- Optional printable-ASCII-only policy through `AsciiOnly`.
- Fast boolean checks through `IsClaimable` and `IsReserved`.
- Structured fail-fast results through `Check`.
- Multi-diagnostic validation through `CheckDetailed`, with optional user-facing diagnostic messages.
- Application-specific reservations through `AdditionalReserved`.
- ASP.NET Core dependency-injection integration.
- `[ClaimableUsername]` model-validation attribute.
- Application-wide ASP.NET validation-message configuration with `{FieldName}` support and attribute-level overrides.
- Reason-specific ASP.NET validation messages through `options.Messages`, with placeholders for `{FieldName}`, `{MatchedValue}`, `{Category}`, `{Character}`, and `{Index}` plus built-in fallbacks for every rejection reason.
- xUnit coverage for strictness behavior, message precedence/placeholders, profanity interaction, character policies, and packaged-consumer behavior.
- `netstandard2.0` dependency-free runtime core.
- `net8.0` ASP.NET Core integration package.
- MPL-2.0 licensing under Perry3D.nl.
- Centralized package versioning.
- NuGet README and package icon metadata.
- Source Link, deterministic builds, portable PDBs, and `.snupkg` symbol packages.
- CI package-content validation and a clean consumer smoke test that restores the generated packages from a local feed.
- GitHub Actions NuGet release workflow using Trusted Publishing/OIDC rather than a long-lived NuGet API key.
