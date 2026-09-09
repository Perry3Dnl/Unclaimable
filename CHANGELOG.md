# Changelog

All notable changes to Unclaimable are documented here.

## 0.5.0 - 2026-09-09

Configurability release that keeps the 0.4.0 strict defaults and built-in dataset contents unchanged while making the existing 22 categories and application-specific overrides substantially more practical.

### Added

- `Category` with all 22 built-in reserved-name dataset categories and `Options.DisableCategory(...)` / `EnableCategory(...)` for per-checker category selection. Every category remains enabled by default.
- `Options.AllowedIdentifiers` for exact complete-identifier exceptions to built-in reserved-name matching. Structural validation still runs first, explicit application reservations take precedence, and compounds or disguised variants are not automatically allowed.
- `ReservedMatchMode` and `Options.Reserve(...)` for application reservations that need either `Exact` whole-identifier matching or `Default` matching through the existing configured pipeline.
- Regression coverage for disabled categories across exact, compact, partial, obfuscation, and Unicode-confusable matching; exact allowed identifiers; structural-validation precedence; custom exact reservations; case/Unicode normalization; punctuation; option capture; and values that occur in multiple categories.
- A v0.4.0 default-behavior release gate that compares fail-fast and detailed results across a deterministic Unicode corpus, including malformed UTF-16, while retaining the existing v0.3.0 legacy-compatible behavior check.

### Changed

- Built-in entries from disabled categories are excluded before the checker's exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed, so category selection has consistent semantics across the entire reserved-name pipeline.
- The release compatibility workflow now checks the exported public API against `v0.4.0`, verifies unchanged default behavior against `v0.4.0`, and separately verifies the 0.3-style valid-Unicode behavior available through the 0.4.0 opt-outs.
- GitHub and NuGet documentation now describes category selection, exact built-in exceptions, application-reservation matching modes, and their precedence rules.

### Compatibility

- No existing public types, methods, constructors, properties, or enum numeric values are removed or changed.
- `AdditionalReserved` is preserved with its existing matching behavior.
- With no new 0.5.0 configuration, checker defaults and results remain identical to 0.4.0 across the compatibility corpus.
- No built-in datasets are expanded or reclassified in this release.

## 0.4.0 - 2026-09-09

Validation-quality release that keeps the 0.3.0 public API, enum numeric values, enabled datasets, and dataset contents while intentionally strengthening the default security policy.

### Added

- `RejectInvisibleOnlyIdentifiers`, `RejectControlCharacters`, and `RejectFormatCharacters` settings for stricter Unicode identifier validation.
- `ConsistentCompactMatching` to apply `Rule.CompactMatching` consistently to direct compact, partial, and obfuscation matching.
- `OriginalMatchStartIndex` and `OriginalMatchLength` on `Result` and `Diagnostic` for reliable original-input UTF-16 spans. These values are nullable when normalization makes mapping uncertain; existing `MatchStartIndex` and `MatchLength` retain their transformed-text meaning.
- `Result.LengthLimit`, populated by `Checker` for minimum- and maximum-length failures from the configuration captured at construction time.
- Regression coverage for malformed UTF-16, strict Unicode defaults, explicit Unicode opt-outs, compact/partial/obfuscation/Unicode rule combinations, original spans, captured length thresholds, and DataAnnotations without dependency injection.
- A release compatibility gate that checks the exported public API and enum values against `v0.3.0` and verifies that 0.3-style valid-Unicode behavior remains available when the new 0.4.0 strict defaults are explicitly disabled.
- Benchmark coverage for checker construction, ordinary accepted input, exact rejection, obfuscation, and all languages enabled, with a recorded 0.3.0 baseline.

### Changed

- `RejectInvisibleOnlyIdentifiers` now defaults to `true`.
- `RejectControlCharacters` now defaults to `true`.
- `RejectFormatCharacters` now defaults to `true` as part of the strict policy. Applications that legitimately require Unicode formatting characters such as joiners can explicitly set it to `false`.
- `ConsistentCompactMatching` now defaults to `true`, so disabling `Rule.CompactMatching` has the same effect across direct compact, partial, and obfuscation paths.
- Options continue to be captured when a `Checker` is constructed, while runtime `IPolicy` character updates remain live.
- Public XML documentation now explicitly describes null handling, structural `IsReserved` failures, strictness-driven partial matching, captured options, live runtime policy updates, transformed legacy offsets, and UTF-16 index/length units.
- No built-in datasets were expanded in this release; 0.4.0 intentionally retains the 0.3.0 dataset contents.

### Fixed

- Malformed UTF-16 input is now rejected as `MatchKind.InvalidCharacters` before normalization or character-policy calls instead of potentially throwing. `IsClaimable` returns `false`, and `CheckDetailed` reports the invalid UTF-16 code-unit index and skips reserved-name normalization.
- `[ClaimableUsername]` fallback minimum/maximum-length messages now include the effective threshold, including when the attribute runs without dependency injection.
- Length placeholders prefer the checker result's captured `LengthLimit`, preventing later mutations to registered `Options` from changing the message for a checker that already captured different settings.

0.4.0 preserves the established public API and curated datasets, but it intentionally strengthens default validation for invisible-only, control, and format-character identifiers and enables consistent compact-rule handling by default.

## 0.3.0 - 2026-09-08

Third public NuGet release, focused on broader semantic coverage, maintainable dataset expansion, and safer compound matching.

### Added

- Dataset schema v2 with generated `roots × suffixes` combinations for maintainable large-scale coverage without duplicating thousands of literal strings in source files.
- Explicit `partialValues` support so entries can opt into safe compound matching independently of the global profanity-partial setting.
- Ten new language-independent categories: `identity`, `authentication`, `moderation`, `finance`, `communications`, `operations`, `infrastructure`, `developer`, `governance`, and `official`.
- Localized identity expansion across all 15 supported languages.
- Built-in dataset coverage of 10,731 filter entries across 22 categories, representing 10,633 unique values within those categories.
- Regression coverage for safe profanity compounds, exact-only short profanity, generated schema-v2 values, localized schema-v2 loading, and false-positive protection.

### Changed

- Safe compound profanity values can now participate in partial matching by default without making short ambiguous entries such as `ass` generic substring rules.
- Dataset statistics now include concrete values expanded from schema-v2 combinations.
- Language-pack tests now validate both literal values and generated schema-v2 combinations.
- NuGet and GitHub-facing documentation now reflects the 0.3.0 dataset totals and expanded category set.

### Fixed

- Prevented compound-matching regression tests from depending on unrelated reserved-token precedence.
- Updated localized dataset tests so schema-v2 files no longer fail legacy schema-v1 assumptions.
- Removed nullable-analysis warnings from `Checker` normalization and conformance test data without suppressing compiler diagnostics.

## 0.2.0 - 2026-09-08

Second public NuGet release, focused on substantially broader built-in dataset coverage and language support.

### Added

- Eight additional localized language packs: Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.
- Fifteen supported Latin-script languages in total when combined with English, Dutch, German, French, Spanish, Italian, and Portuguese.
- New language-independent global categories for `security`, `automation`, `legal`, `commerce`, `community`, and `other`.
- Built-in dataset coverage of 5,181 filter entries across 12 categories, representing 5,085 unique values within those categories.
- Category-expansion and additional-language tests that verify representative values, language isolation, global-category behavior, and dataset-category integrity.
- Dataset statistics tooling for reproducibly counting filter entries and unique values by category.
- A dedicated NuGet README, separate from the GitHub repository README.

### Changed

- Expanded the existing brand, technology, profanity, role, support, and system datasets with substantially more coverage.
- Expanded German, Spanish, French, Italian, Dutch, and Portuguese localized datasets.
- Updated both README files with current category totals, package installation guidance, and the full supported-language list.
- NuGet package metadata now uses the dedicated NuGet README while GitHub continues to render the repository README.

### Fixed

- Prevented new global category values from silently reclassifying existing localized values when datasets overlap.
- Corrected NuGet README packaging so both generated packages contain the README declared in package metadata.

## 0.1.0

Initial public NuGet release.

### Added

- Strict-by-default validation so `AddUnclaimable()` enables the recommended protection set without additional configuration.
- `Rule` flags and `DisabledRules` for selectively relaxing individual checks while keeping all other protections enabled.
- Localized built-in datasets for English, Dutch, German, French, Spanish, Italian, and Portuguese across profanity, roles, support/trust identities, and system names.
- English as the default localized language, with additive language configuration through `AddLanguage(...)` and `RemoveLanguage(...)`; `Options.Languages` is exposed read-only for inspection.
- Language packs organized below `data/languages/<code>/`, with wildcard resource discovery so unused language folders can be physically removed from source builds without creating compile-time dependencies.
- CI verification that the core and ASP.NET Core projects still compile with the entire `data/languages/` directory removed.
- Language-independent global datasets for technology names and broadly recognizable brands.
- Extensible language-tagged JSON dataset format using ISO-style language codes or `global` scope.
- Exact matching with Unicode NFKC normalization and invariant case normalization.
- Compact matching for separator and punctuation variants.
- Strict embedded/partial reserved-name matching by default, with configurable `PartialMatchMinimumLength`.
- `Strictness.Standard` as an explicit more-permissive reserved-name mode and `Strictness.Strict` as the default.
- Bounded leetspeak and symbol-obfuscation matching.
- Selected Unicode-confusable and diacritic normalization for common impersonation attempts.
- Localized profanity matching enabled by default for every enabled language, with separate opt-in `ProfanityPartialMatching` for more aggressive substring filtering.
- Fail-fast minimum and maximum length validation with defaults of 3 and 32 characters.
- Fail-fast Unicode decimal-digit rejection by default.
- Whitespace restrictions and a default blocked-character policy for `-` and `_`.
- Leading and trailing separator validation.
- Application-specific blocked characters through `AdditionalBlockedCharacters(...)`.
- Thread-safe runtime character policy through `IPolicy` and `Policy`, including `BlockCharacter(s)` and `AllowCharacter(s)` operations.
- Application-specific reservations through `AdditionalReserved`.
- Optional printable-ASCII-only policy through `AsciiOnly`.
- Fast boolean checks through `IsClaimable` and `IsReserved`.
- Structured fail-fast results through `Check`, including structural rejection reasons and offending-character metadata.
- Multi-diagnostic validation through `CheckDetailed`, with optional user-facing diagnostic messages.
- ASP.NET Core dependency-injection integration with a live runtime `IPolicy` singleton.
- `[ClaimableUsername]` model-validation attribute.
- Application-wide ASP.NET validation-message configuration with attribute-level overrides.
- Reason-specific ASP.NET validation messages with placeholders for `{FieldName}`, `{MatchedValue}`, `{Category}`, `{Character}`, `{Index}`, `{Length}`, `{MinimumLength}`, and `{MaximumLength}`.
- xUnit coverage for strict defaults, additive language behavior, language isolation after removal, structural validation, runtime policy changes, message precedence/placeholders, profanity interaction, matching behavior, and packaged-consumer behavior.
- `netstandard2.0` dependency-free runtime core.
- `net8.0` ASP.NET Core integration package.
- MPL-2.0 licensing under Perry3D.nl.
- Centralized package versioning.
- NuGet README and package icon metadata.
- Source Link, deterministic builds, portable PDBs, and `.snupkg` symbol packages.
- CI package-content validation and a clean consumer smoke test that restores the generated packages from a local feed.
- GitHub Actions NuGet release workflow using Trusted Publishing/OIDC rather than a long-lived NuGet API key.
