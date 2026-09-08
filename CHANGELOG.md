# Changelog

All notable changes to Unclaimable are documented here.

## 0.1.0 - Unreleased

Initial public NuGet release.

### Added

- Strict-by-default validation so `AddUnclaimable()` enables the recommended protection set without additional configuration.
- `UnclaimableRule` flags and `DisabledRules` for selectively relaxing individual checks while keeping all other protections enabled.
- Localized built-in datasets for English, Dutch, German, French, Spanish, Italian, and Portuguese across profanity, roles, support/trust identities, and system names.
- English as the default localized language, with additive language configuration through `UnclaimableOptions.Languages`, `AddLanguage(...)`, and `RemoveLanguage(...)`.
- Language packs organized below `data/languages/<code>/`, with wildcard resource discovery so unused language folders can be removed from source builds without creating compile-time dependencies.
- Language-independent global datasets for technology names and broadly recognizable brands.
- Extensible language-tagged JSON dataset format using ISO-style language codes or `global` scope.
- Exact matching with Unicode NFKC normalization and invariant case normalization.
- Compact matching for separator and punctuation variants.
- Strict embedded/partial reserved-name matching by default, with configurable `PartialMatchMinimumLength`.
- `UnclaimableStrictness.Standard` as an explicit more-permissive reserved-name mode and `UnclaimableStrictness.Strict` as the default.
- Bounded leetspeak and symbol-obfuscation matching.
- Selected Unicode-confusable and diacritic normalization for common impersonation attempts.
- Localized profanity matching enabled by default for every enabled language, with separate opt-in `ProfanityPartialMatching` for more aggressive substring filtering.
- Fail-fast minimum and maximum length validation with defaults of 3 and 32 characters.
- Fail-fast Unicode decimal-digit rejection by default.
- Whitespace restrictions and a default blocked-character policy for `-` and `_`.
- Leading and trailing separator validation.
- Application-specific blocked characters through `AdditionalBlockedCharacters(...)`.
- Thread-safe runtime character policy through `IUnclaimablePolicy` and `UnclaimablePolicy`, including `BlockCharacter(s)` and `AllowCharacter(s)` operations.
- Application-specific reservations through `AdditionalReserved`.
- Optional printable-ASCII-only policy through `AsciiOnly`.
- Fast boolean checks through `IsClaimable` and `IsReserved`.
- Structured fail-fast results through `Check`, including structural rejection reasons and offending-character metadata.
- Multi-diagnostic validation through `CheckDetailed`, with optional user-facing diagnostic messages.
- ASP.NET Core dependency-injection integration with a live runtime `IUnclaimablePolicy` singleton.
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
