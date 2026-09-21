# Changelog

## 0.8.0 - Unreleased

> **Default-behavior warning:** 0.8.0 intentionally establishes a stricter default policy than 0.7.8. Applications upgrading from an older release should run their real identifier regression corpus before deployment. The new exception APIs are designed to make narrow compatibility exceptions without disabling an entire protection.

### Why the defaults changed

Earlier releases accumulated two competing goals: block trusted/reserved identities aggressively, while also preserving a broad "known safe" corpus intended to avoid substring false positives. That older safe-corpus policy no longer matched the direction of the package. In particular, security-sensitive roots such as `admin`, `staff`, `root`, `owner`, `support`, and `help` were complete reserved identifiers but could still appear inside a larger identifier.

0.8.0 makes the deny-first policy explicit: if an enabled rule, pattern, protected-identity list, curated partial root, or application reservation rejects an identifier, validation stops and returns that deny reason. Passing one earlier check never clears the identifier.

The stricter defaults are paired with scoped exception APIs so an application can keep the protection globally and relax only the exact rule, pattern, character, repeated character, or identifier that its naming convention requires.

### Default behavior changes

- All built-in protected identity rules are enabled by default in Core except `Rule.Numbers`. This includes countries, popular cities, celebrities, nationalities, currencies, religions, landmarks, events, awards, fictional characters, franchises, professions, and military identities.
- `Rule.Numbers` remains disabled by default, so ordinary mixed alphanumeric identifiers can still be claimable. `Pattern.NumericOnly` remains enabled, so all-numeric identifiers are still rejected.
- `Pattern.UppercaseOnly` remains opt-in. Reserved identifiers are still case-normalized, so disabling or not enabling the uppercase-only pattern does not make values such as `ADMIN` claimable.
- `Pattern.Repeated` now treats direct runs and cyclic repetition separately:
  - three or more identical consecutive Unicode text elements are rejected by default;
  - multi-element cycles use `RepeatedPatternMinimumLength`, now defaulting to `6`;
  - `abab` is below the default cyclic threshold, while `ababab`, `abcabc`, and `hahaha` are rejected.
- English `support` and `help`, plus privileged role roots `admin`, `staff`, `root`, and `owner`, are explicitly eligible for curated partial matching. Compounds such as `supportive`, `helpful`, `badminton`, `stafford`, `rooted`, and `ownership` are therefore rejected by default.
- The old `conformance/safe-usernames.json` policy corpus has been removed. The behavioral conformance corpus now records the intended 0.8.0 outcomes instead of preserving the superseded 0.6 false-positive policy.

### Added

- `Options.AllowIdentifierForRule(value, rules)` for complete-identifier exceptions to one or more selected `Rule` checks.
- `Options.AllowIdentifierForPattern(value, patterns)` for complete-identifier exceptions to one or more selected `Pattern` checks.
- `Options.AllowCharacters(...)` for startup character-policy allowances without disabling `Rule.BlockedCharacters`.
- `Options.AllowRepeatedCharacters(...)` for permitting direct runs of selected Unicode characters without disabling `Pattern.Repeated` globally.
- Startup character allowances are seeded into the existing live `IPolicy`, so runtime policy changes can still re-block or re-allow those characters.
- A comprehensive configuration and exceptions guide at `docs/CONFIGURATION.md` with copy-paste recipes, precedence rules, migration guidance, ASP.NET Core examples, Email local-part examples, and Extended-data exception examples.
- Cross-ecosystem compatibility smoke coverage and discoverability metadata for .NET MAUI, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform for the portable packages.
- Explicit `Unclaimable.AspNetCore` target assets for `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0`, and `net11.0`.

### Scoped exception examples

Allow one city-name collision without disabling city protection globally:

```csharp
var options = new Options();

options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);
```

Allow one exact cyclic identifier through repetition detection while leaving other repeated identifiers protected:

```csharp
options.AllowIdentifierForPattern(
    "ababab",
    Pattern.Repeated);
```

Support a team naming convention such as `TTT_user7` without disabling either protection:

```csharp
var options = new Options()
    .AllowCharacters("_")
    .AllowRepeatedCharacters("T");

var checker = new Checker(options);
```

The exceptions are not positive clears. Every unrelated deny check still runs after the exception. For example, allowing `ADMIN` through `Pattern.UppercaseOnly` does not bypass the reserved `admin` entry.

### Migration guidance

When upgrading from 0.7.8:

1. Run existing production identifiers and signup fixtures against 0.8.0.
2. Review new rejections from the now-default protected identity rules.
3. Review identifiers containing `support`, `help`, `admin`, `staff`, `root`, or `owner`.
4. Review direct runs of three characters and cyclic repeated spans of six or more text elements.
5. Prefer `AllowIdentifierForRule(...)`, `AllowIdentifierForPattern(...)`, `AllowCharacters(...)`, or `AllowRepeatedCharacters(...)` over globally disabling a protection.
6. Keep application-owned denies in `Reserve(...)` / `AdditionalReserved`; scoped exceptions to unrelated checks do not remove explicit application reservations.
7. For `Unclaimable.Extended`, configure one-off Extended exceptions through `ExtendedOptions.AllowedIdentifiers` during `UseExtendedData(...)`.

See `docs/CONFIGURATION.md` for the full configuration model and examples.

## 0.7.8 - 2026-09-18

Repeated-pattern hotfix.

### Fixed

- Detect repeated spans anywhere inside an identifier instead of only when the complete identifier is made from one repeated unit.
- Reject embedded repeated runs such as `sssssssss2234423`, `useraaaa12`, and `testabababab99`.
- Cover repeated units consistently while avoiding incidental two-cycle substrings inside ordinary words; multi-character units require sustained repetition, while single-character runs still honor the configured minimum.

### Added

- `Options.RepeatedPatternMinimumLength` for configuring the minimum repeated span in Unicode text elements. The default is `4`; the minimum supported value is `2`.
- Regression coverage for default and custom thresholds, option capture, embedded repeated spans, and the lower configuration bound.

### Compatibility

- `Pattern.Repeated` remains enabled by default, but its detection is intentionally stricter in 0.7.8.
- Core and ASP.NET Core package validation now use the published 0.7.7 packages as the compatibility baseline.
- All first-party packages remain version-aligned at 0.7.8.

## 0.7.7 - 2026-09-18

### Fixed

- Give each NuGet package its own package-specific `README.NUGET.md` instead of embedding the same project-wide README in every package.
- Keep the Core package README focused on Core, the ASP.NET Core package README focused on DI/DataAnnotations integration, the Email package README focused on email identity protection, and the Extended package README focused on Extended datasets.
- Add package validation that verifies the expected README heading and rejects duplicate README contents across first-party packages.
- Update Core and ASP.NET Core package-validation baselines to the published `0.7.6` packages.


## 0.7.6 - 2026-09-18

### Added

- New `Unclaimable.Extended` `netstandard2.0` package, versioned together with all first-party packages through the shared `UnclaimableVersion` property.
- 36,313 optional additional identifiers across 21 Extended groups: companies, regional brands, financial institutions, government/public bodies, international organizations, sports, education, media, transport, healthcare, historical figures, public figures, celebrities, fiction, entertainment, professions, multilingual reserved vocabulary, regional slang/profanity, crypto, platforms, and geography.
- `Options.UseExtendedData(...)` explicit opt-in registration with per-group enable/disable controls and exact Extended exceptions.
- `ReservedMatchMode.WholeIdentifier` and categorized `Options.Reserve(value, category, matching)` in Core, allowing sibling packages to reuse exact, compact, Unicode-confusable, and obfuscation matching without enabling generic substring matching.
- Deterministic embedded dataset snapshots with source/provenance notes shipped in the Extended package.
- Extended package integration in coverage, package validation, compatibility packing, release packing/publishing, and packaged-consumer smoke tests.

### Compatibility

- No existing Core dataset is removed or moved. Installing `Unclaimable.Extended` alone changes no validation result; applications must call `UseExtendedData()`.
- Existing `ReservedMatchMode` numeric values remain unchanged: `Default = 0`, `Exact = 1`; `WholeIdentifier = 2` is additive.
- Core entries are indexed before Extended entries, preserving Core diagnostics/categories when the same identity exists in both datasets.

All notable changes to Unclaimable are documented here.

## 0.7.5 - 2026-09-18

Email-identity release introducing a focused sibling package while keeping the first-party package line version-aligned.

### Added

- New `Unclaimable.Email` `netstandard2.0` package, versioned `0.7.5` together with `Unclaimable` and `Unclaimable.AspNetCore`.
- `EmailChecker` / `IEmailChecker` with explicit `CheckExistingAddress(...)` and `CheckNewAddress(...)` flows.
- Email-adapted local-part validation that preserves normal email punctuation and lengths while still applying the Unclaimable reserved-name, compact, curated partial, obfuscation, Unicode-confusable, category, language, and application-reservation pipeline.
- `EmailOptions.ProtectedDomains` for application-owned or otherwise protected domains.
- Protected-domain detection for bounded typo variants, adjacent transpositions, selected Unicode/ASCII confusables, protected registrant-label reuse, and embedded protected domains such as `lidl.nl.attacker.com`.
- `EmailOptions.IssuingDomains` for applications that create addresses only on approved domains. Issuing domains are automatically protected against lookalikes.
- Structured `EmailResult` diagnostics with a primary `EmailFailureKind`, the underlying Unclaimable local-part result, `DomainLookalikeKind`, and the matched protected domain.
- Package-content validation, packaged-consumer smoke coverage, CI packing, compatibility packing, and tag-gated NuGet publishing for the new package.
- Shared internal confusable normalization in the `Unclaimable` core package, consumed by `Unclaimable.Email`, so selected Unicode and leetspeak mappings have one source of truth.
- Expanded brand-domain regression coverage for McDonald's, Nike, Google, Amazon, Visa, and Nvidia, including `amazone.com`, insertion/deletion/substitution/transposition cases, Unicode and punycode homographs, alternate TLDs, hyphen lure domains, embedded protected domains, casing, and legitimate subdomains.
- Production coverage now includes `Unclaimable.Email`; the expanded v0.7.5 suite passes 3,560 tests at 98.26% line coverage (`2,147 / 2,185`).
- Dataset-driven email-domain behavior tests derive 913 current protected labels from the built-in core/extended brands and technology datasets and automatically exercise generated typo, TLD-reuse, lure-domain, embedded-domain, ASCII-confusable, Unicode-homograph, and punycode-homograph variants.

### Behavior

- Existing external addresses are not treated as syntax-only. For example, `admin@lidi.nl` can report `ReservedLocalPart` while also retaining the `lidl.nl` protected-domain typo diagnostic.
- Exact protected domains and their actual subdomains are accepted before lookalike detection.
- New-address checks optionally require the address to use a configured issuing domain or its subdomain.
- Email validation accepts practical unquoted mailbox local parts, including common `.`, `_`, `-`, and `+` forms, while rejecting malformed local-part dot usage and malformed UTF-16.
- Domain parsing uses DNS-style labels plus IDN-to-ASCII normalization and a plausible top-level-domain shape check.
- The package performs no DNS or MX lookup and does not claim that a syntactically accepted domain or mailbox exists.

### Versioning

- `Unclaimable`, `Unclaimable.AspNetCore`, and `Unclaimable.Email` all build and package as `0.7.5` from the shared `UnclaimableVersion` property.

## 0.7.2 - 2026-09-15

Configuration and optional-geography release focused on making the default username policy more practical while keeping strict protections independently available.

### Added

- `Rule.CountryNames` for opt-in whole-identifier matching against a broad built-in country-name and common-alias list.
- `Rule.PopularCityNames` for opt-in whole-identifier matching against a curated global list of major and widely recognized cities.
- `Options.EnableRule(...)` and `Options.DisableRule(...)` helpers for incremental rule configuration without replacing the legacy `DisabledRules` mask.
- `Options.EnabledOptionalRules` for inspecting the enabled opt-in rule set.
- `MatchKind.CountryName` and `MatchKind.PopularCityName`, appended as numeric values `18` and `19`.
- Reason-specific validation-message support for the two geography rejection reasons.
- Regression coverage for geography defaults, independent enable/disable behavior, compact whole-identifier forms, category independence, option capture, deny-first precedence, and false-positive protection.

### Changed

- `Rule.Numbers` is disabled by default. Ordinary alphanumeric identifiers such as `user7` and `john2026` are now accepted unless another enabled protection rejects them.
- `Pattern.NumericOnly` remains enabled by default, so an identifier consisting only of decimal digits remains rejected even though digits are otherwise permitted.
- Country-name and popular-city-name rules are disabled by default and must be explicitly enabled.
- The shared conformance corpus now treats ordinary alphanumeric identifiers such as `ordinary123` as claimable under the default policy.
- The benchmark workflow targets `release/0.7.2`.

### Geography behavior

- Geography matching is whole-identifier only and does not create generic substring rules. For example, enabling the rules can reject `france`, `New York`, or `United Kingdom`, while `francelover`, `newyorker`, and similar compounds remain claimable unless another rule applies.
- Compact matching can recognize separator/space variants when the relevant structural rules are relaxed. Disabling `Rule.CompactMatching` disables that geography compaction too.
- Country matching takes precedence for country/city overlaps such as `singapore` when both geography rules are enabled.
- `AllowedIdentifiers` does not bypass an explicitly enabled geography rule; the enabled deny rule still runs.

### Default policy

The 0.7.2 default rule/pattern matrix intentionally separates “digits may appear” from “numeric-only identifiers are allowed”:

- `Rule.Numbers`: disabled;
- `Rule.CountryNames`: disabled;
- `Rule.PopularCityNames`: disabled;
- `Pattern.NumericOnly`: enabled;
- `Pattern.Repeated`: enabled;
- `Pattern.SymbolOnly`: enabled;
- `Pattern.AsciiArt`: enabled;
- `Pattern.UppercaseOnly`: disabled.

All existing reserved-name categories, strict curated partial matching, obfuscation matching, Unicode-confusable matching, profanity protection, length rules, whitespace/separator restrictions, blocked characters, and Unicode structural protections retain their established defaults.

### Compatibility and release validation

- Existing `Rule` numeric values remain unchanged; the geography flags are appended as bits `12` and `13`.
- Existing `MatchKind` numeric values remain unchanged; geography reasons are appended as values `18` and `19`.
- `DisabledRules` remains a legacy full-mask property. Assigning it replaces the mask; `EnableRule(...)` and `DisableRule(...)` are preferred for incremental configuration.
- Package validation remains against published NuGet `0.7.0` while 0.7.1 is staged but not yet published.
- Release validation covers 518 tests, source builds without localized packs, NuGet package metadata/content validation, packaged-consumer restore/execution, and public-API package validation.
- The release workflow remains tag-gated for NuGet publishing; preparing 0.7.2 does not create a tag or publish a package.

## 0.7.1 - 2026-09-15

Reserved-vocabulary expansion focused on systematically covering application-owned identities, states, governance terms, operational concepts, and other exact names that should not normally be claimable.

### Added

- Broad exact-value sweeps across Authentication, Automation, Commerce, Communications, Community, Developer, Finance, Governance, Identity, Infrastructure, Legal, Moderation, Official, Operations, Security, and English System data.
- Representative additions include `member`, `membership`, `vote`, `voting`, `ballot`, `election`, `active`, `inactive`, `pending`, `enabled`, `disabled`, `authentication`, `announcement`, `apikey`, `treasury`, `username`, `loadbalancer`, `banned`, `verified`, `operations`, `buyer`, `legalhold`, and `phishing`.
- Bulk category-ownership regression coverage that disables the owning category for newly added exact values and verifies that another category does not silently continue reserving the same identifier.

### Matching behavior

- The vocabulary expansion uses exact reserved values rather than turning generic roots into broad substring filters.
- `vote` does not make `devote` reserved, `member` does not make `rememberme` reserved, and `active` does not make `hyperactive` reserved.
- Existing category ownership is preserved when a value was already protected elsewhere; the sweep removed cross-category collisions found during validation instead of duplicating them.

### Dataset

- Built-in dataset coverage increases from 10,748 to **11,150 filter entries**.
- Built-in unique values increase from 10,650 to **11,039 unique values**.
- The category count remains **23**.

### Compatibility

- No existing public C# API or enum numeric value is intentionally removed or renumbered.
- This release intentionally expands reserved-name outcomes for newly covered exact identifiers.
- Full test, source-build, package-content, packaged-consumer, and public-API compatibility validation is required before release.

## 0.7.0 - 2026-09-15

Identifier-pattern release focused on rejecting suspicious or degenerate identifier shapes without turning those checks into reserved-name entries.

0.7.0 is intentionally stricter by default than 0.6.0. Existing APIs remain available, but newly enabled pattern checks and the new placeholder category can reject values that 0.6.0 allowed.

### Added

- New `[Flags]` `Pattern` enum for higher-level identifier-shape protections.
- `Options.EnabledPatterns` plus `EnablePattern(...)` and `DisablePattern(...)` for captured per-checker pattern configuration.
- `Pattern.NumericOnly` for all-digit identifiers. Enabled by default.
- `Pattern.Repeated` for long identifiers built from repeated short units. Enabled by default.
- `Pattern.SymbolOnly` for identifiers containing punctuation or symbols but no Unicode letters or numbers. Enabled by default.
- `Pattern.AsciiArt` for a conservative set of known ASCII-art constructions. Enabled by default.
- `Pattern.UppercaseOnly` for identifiers whose cased letters are all uppercase. Disabled by default because uppercase handles can be legitimate.
- Dedicated `MatchKind` values for `NumericOnly`, `RepeatedPattern`, `SymbolOnly`, `AsciiArt`, and `UppercaseOnly`.
- Reason-specific `ValidationMessages` properties and ASP.NET Core DataAnnotations fallback messages for every new pattern failure.
- New global `placeholders` category containing 17 literal null-like or missing-value identifiers including `null`, `undefined`, `empty`, `none`, `nil`, `unset`, `missing`, `unknown`, and related forms.
- `Category.Placeholders` as a normal configurable dataset category.
- Regression coverage for every pattern, bitwise pattern configuration, checker configuration capture, cross-filter behavior, placeholder normalization/category controls, and actual-null versus literal-`"null"` behavior.

### Behavior

- Pattern checks are independent deny rules. Passing or disabling one pattern does not mark an identifier as claimable and does not bypass structural validation, other pattern checks, or reserved-name matching.
- Fail-fast structural validation runs before pattern checks. For example, an all-digit identifier still reports `NumbersNotAllowed` under the default number rule; `NumericOnly` becomes the rejection reason when numbers are otherwise permitted.
- `CheckDetailed(...)` can report pattern failures alongside other applicable diagnostics and a reserved-name match.
- `AllowedIdentifiers` remains a narrow built-in reserved-name exception and does not bypass structural or pattern checks.
- Disabling `UppercaseOnly` does not make a reserved uppercase identifier such as `ADMIN` claimable; reserved-name normalization still resolves it to `admin`.
- Disabling `NumericOnly` does not clear an identifier that is independently rejected by `Repeated`.
- Literal placeholder strings use the normal exact/compact/confusable/obfuscation pipeline and can be removed with `DisableCategory(Category.Placeholders)`.
- Actual C# `null` input keeps the established claimable behavior so required-field validation remains separate.

### Dataset

- Built-in dataset coverage increases from 10,731 to **10,748 filter entries**.
- Built-in unique values increase from 10,633 to **10,650 unique values**.
- The category count increases from 22 to **23 categories** through the new 17-entry global `placeholders` dataset.
- Existing category numeric values remain unchanged; `Category.Placeholders` is appended as value `22`.

### Compatibility

- Existing `MatchKind` numeric values remain unchanged; the five new pattern reasons are appended as values `13` through `17`.
- Existing public types, methods, constructors, properties, rule values, category values, and matching APIs are not intentionally removed or renumbered.
- The new `Pattern` API is additive.
- Default behavior intentionally becomes stricter for newly covered identifier shapes and placeholder literals. Consumers upgrading from 0.6.0 should review these policy additions if such values are intentionally allowed.
- Full test/package/smoke validation and public-API compatibility checks remain green on the release branch.

## 0.6.0 - 2026-09-15

Feedback-response release focused on reducing false positives, making policy changes safer to review, clarifying Unicode guarantees, and hardening the release pipeline.

0.6.0 intentionally does **not** bulk-expand the built-in reserved vocabulary. The primary change is how built-in entries become eligible for substring matching.

### Feedback addressed

A production-oriented review of 0.5.0 identified strict substring matching as the highest-priority risk. In 0.5.0, every sufficiently long non-profanity reserved entry could enter the partial-match index. That meant short or ordinary protected terms could reject unrelated usernames solely because the same letters appeared inside a larger word.

Representative 0.5.0 false positives included:

- `supportive` through `support`;
- `helpful` through `help`;
- `apples` through `apple`;
- `nikee` through `nike`;
- `badminton` through `admin`;
- `stafford` through `staff`;
- `rooted` through `root`;
- `ownership` through `owner`.

0.6.0 treats this feedback as a policy defect rather than asking consumers to maintain exception lists for ordinary words.

### Changed

- Built-in partial matching is now **explicitly dataset-authorized**. `Strictness.Strict` still enables the partial-matching capability, but ordinary built-in `values` no longer become generic substring rules automatically.
- Schema-v2 `partialValues` and generated combinations with `"partial": true` are eligible for built-in partial matching.
- `PartialMatchMinimumLength` still applies after eligibility; length alone is no longer treated as sufficient evidence that an entry is safe as a substring rule.
- Selected high-risk role, support, and authentication identities are explicitly marked partial-safe, including values such as `superadmin`, `systemadministrator`, `customersupport`, `passwordreset`, and generated authentication-service identities.
- Short ambiguous entries such as `support`, `help`, `apple`, `nike`, `admin`, `root`, `staff`, and `owner` remain exact reserved identifiers but no longer block unrelated larger words by default.
- Application-defined `AdditionalReserved` and `Reserve(..., ReservedMatchMode.Default)` retain their existing configured partial-matching behavior. The eligibility change is scoped to built-in datasets.
- The shared conformance corpus now records the intended 0.6.0 relaxations and curated high-risk partial matches.
- The compatibility workflow now validates package/public API compatibility against published NuGet `0.5.0` using .NET package validation instead of requiring all default outcomes to remain identical to 0.4.0.

### Added

- A checked-in `conformance/safe-usernames.json` corpus covering ordinary compound words, personal names, international names, developer/gaming handles, business-style names, and Unicode identifiers that must remain claimable under the default policy.
- Regression tests that fail when a known-safe username becomes blocked.
- Explicit tests that ambiguous built-in roots remain reserved as complete identifiers while no longer becoming generic substring rules.
- Explicit tests that curated partial-safe role, support, authentication, and profanity compounds still reject dangerous larger identifiers.
- NuGet package baseline validation against `0.5.0` for both `Unclaimable` and `Unclaimable.AspNetCore`.
- Dependabot configuration for pinned GitHub Actions.

### Documentation

- The GitHub and NuGet READMEs now explain why 0.6.0 exists, including concrete before/after false-positive examples.
- Unicode-confusable protection is now explicitly scoped as a selected mapping, **not** a complete Unicode Technical Standard #39 implementation.
- Added canonical username guidance covering case sensitivity, Unicode normalization, database uniqueness/collation, display-name behavior, and URL/routing normalization.
- Added an explicit statement that Unclaimable is a defense-in-depth policy and reserved-name library rather than a complete anti-impersonation or identity system.
- Dataset/policy changes are documented as consumer-visible behavior changes even when the public C# API does not change.

### Release hardening

- GitHub Actions are pinned to immutable commit SHAs instead of mutable major-version tags.
- Dependabot maintains those pinned action revisions.
- The benchmark workflow now targets the active `release/0.6.0` branch instead of the stale `release/0.4.0` branch.
- Package builds continue to use deterministic builds, Source Link, portable PDBs, symbol packages, NuGet Trusted Publishing, package-content validation, and clean packaged-consumer smoke tests.

### Compatibility

- No existing public types, methods, constructors, properties, or enum numeric values are intentionally removed or changed.
- Built-in dataset vocabulary cardinality remains 10,731 filter entries across 22 categories, representing 10,633 unique values within those categories; selected entries moved between schema buckets to declare partial eligibility without duplicating vocabulary.
- Exact, compact, obfuscation, Unicode-confusable, structural, category-selection, allowed-identifier, and custom-reservation behavior remain independently configurable.
- Default behavior intentionally differs from 0.5.0 for documented built-in false-positive cases. Consumers that relied on generic built-in substring rejection should review those policy changes before upgrading.

## 0.5.0 - 2026-09-09

Configurability release that keeps the 0.4.0 strict defaults and built-in dataset contents unchanged while making the existing 22 categories and application-specific overrides substantially more practical.

This release marks a more stable point in Unclaimable's development: the core validation behavior, configuration semantics, compatibility guarantees, test coverage, and release/package gates now form a stronger baseline for future versions.

### Added

- `Category` with all 22 built-in reserved-name dataset categories and `Options.DisableCategory(...)` / `EnableCategory(...)` for per-checker category selection. Every category remains enabled by default.
- `Options.AllowedIdentifiers` for exact complete-identifier exceptions to built-in reserved-name matching. Structural validation still runs first, explicit application reservations take precedence, and compounds or disguised variants are not automatically allowed.
- `ReservedMatchMode` and `Options.Reserve(...)` for application reservations that need either `Exact` whole-identifier matching or `Default` matching through the existing configured pipeline.
- Regression coverage for disabled categories across exact, compact, partial, obfuscation, and Unicode-confusable matching; exact allowed identifiers; structural-validation precedence; custom exact reservations; trim/NFKC/invariant-case exact normalization; punctuation; option capture; values that occur in multiple categories; and exact dataset-to-`Category` enum completeness.
- A v0.4.0 default-behavior release gate that compares fail-fast and detailed results across a deterministic Unicode corpus, including malformed UTF-16, while retaining the existing v0.3.0 legacy-compatible behavior check.

### Changed

- Built-in entries from disabled categories are excluded before the checker's exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed, so category selection has consistent semantics across the entire reserved-name pipeline.
- The release compatibility workflow now checks the exported public API against `v0.4.0`, verifies unchanged default behavior against `v0.4.0`, and separately verifies the 0.3-style valid-Unicode behavior available through the 0.4.0 opt-outs.
- GitHub and NuGet documentation describe category selection, exact built-in exceptions, application-reservation matching modes, their precedence rules, and the exact trim → NFKC → invariant-lowercase normalization contract.

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
- Public XML documentation explicitly describes null handling, structural `IsReserved` failures, strictness-driven partial matching, captured options, live runtime policy updates, transformed legacy offsets, and UTF-16 index/length units.
- No built-in datasets were expanded in this release; 0.4.0 intentionally retains the 0.3.0 dataset contents.

### Fixed

- Malformed UTF-16 input is rejected as `MatchKind.InvalidCharacters` before normalization or character-policy calls instead of potentially throwing. `IsClaimable` returns `false`, and `CheckDetailed` reports the invalid UTF-16 code-unit index and skips reserved-name normalization.
- `[ClaimableUsername]` fallback minimum/maximum-length messages include the effective threshold, including when the attribute runs without dependency injection.
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

- Safe compound profanity values can participate in partial matching by default without making short ambiguous entries such as `ass` generic substring rules.
- Dataset statistics include concrete values expanded from schema-v2 combinations.
- Language-pack tests validate both literal values and generated schema-v2 combinations.
- NuGet and GitHub-facing documentation reflects the 0.3.0 dataset totals and expanded category set.

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
- NuGet package metadata uses the dedicated NuGet README while GitHub continues to render the repository README.

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
