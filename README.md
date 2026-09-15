<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![line coverage](https://img.shields.io/badge/line%20coverage-%E2%89%A598%25-brightgreen.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

**Strict, fast username and identifier validation for .NET.**

Prevent reserved, protected, misleading, degenerate, and unsafe identifiers before they can be claimed. Unclaimable combines curated datasets, Unicode-aware matching, structural validation, configurable rules, identifier-shape checks, and ASP.NET Core integration with no runtime dependencies in the core package.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Changelog**](CHANGELOG.md)

## 0.7.2: practical defaults and optional geography rules

The source tree is prepared for **0.7.2**. This release is intentionally **not published yet**.

0.7.2 changes one important default: ordinary usernames may contain numbers. It also adds optional country-name and popular-city-name rules.

### Numbers in usernames

`Rule.Numbers` is disabled by default in 0.7.2:

```text
john2026     -> can be claimable
user7        -> can be claimable
player123    -> can be claimable
```

`Pattern.NumericOnly` remains enabled by default:

```text
123456789    -> rejected as NumericOnly
```

This separates two policies that should not be conflated:

```text
May a username contain numbers?       yes, by default
May a username be only numbers?       no, by default
```

Applications that want to reject every decimal digit can explicitly restore that policy:

```csharp
options.EnableRule(Rule.Numbers);
```

### Optional geography protection

Two new rule flags are available and disabled by default:

```csharp
Rule.CountryNames
Rule.PopularCityNames
```

Enable either or both:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.EnableRule(Rule.CountryNames | Rule.PopularCityNames);
});
```

The rules are whole-identifier checks. They can reject values such as `france`, `United Kingdom`, `amsterdam`, or `New York` without turning those names into broad substring filters. Values such as `francelover`, `newyorker`, and `amsterdammer` remain claimable unless another protection applies.

They use dedicated result reasons:

```csharp
MatchKind.CountryName
MatchKind.PopularCityName
```

Country matching wins for country/city overlaps such as `singapore` when both rules are enabled.

## 0.7.1: reserved-vocabulary sweep

The staged 0.7.1 work systematically expanded exact reserved-name coverage across 16 functional categories.

Representative additions include:

```text
member
membership
vote
voting
ballot
election
active
inactive
pending
enabled
disabled
authentication
announcement
apikey
treasury
username
loadbalancer
banned
verified
operations
buyer
legalhold
phishing
```

These are intentionally exact values rather than generic substring roots. For example:

```text
vote        -> reserved
devote      -> not blocked by vote
member      -> reserved
rememberme  -> not blocked by member
active      -> reserved
hyperactive -> not blocked by active
```

The sweep also added bulk category-ownership regression checks so newly added exact values cannot silently become owned by multiple categories.

## Features

- Reserved-name protection across **23 built-in categories**
- **11,150 filter entries** representing **11,039 unique values**
- Exact, compact, curated partial, obfuscation, and selected Unicode-confusable matching
- **15 localized language datasets** with English enabled by default
- Per-category enable/disable controls
- Exact built-in exceptions and application-specific reservations
- Configurable numeric-only, repeated, symbol-only, ASCII-art, and uppercase-only pattern checks
- Optional country-name and popular-city-name rules
- Length, whitespace, separator, blocked-character, Unicode-safety, and optional no-number rules
- Dependency-free `netstandard2.0` core
- ASP.NET Core DI and DataAnnotations integration
- Structured fail-fast and multi-diagnostic results
- Known-safe and reserved conformance corpora in CI

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and DataAnnotations integration |

Release-candidate install commands:

```bash
dotnet add package Unclaimable --version 0.7.2
dotnet add package Unclaimable.AspNetCore --version 0.7.2
```

These packages are produced by CI during release preparation but are not pushed to NuGet until the tag-gated release workflow is intentionally triggered.

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate7");
if (result.IsClaimable)
{
    // Continue with your own database/availability check.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted by Unclaimable so required-field validation remains a separate concern.

## Default policy in 0.7.2

`new Options()` keeps strong protection while allowing ordinary alphanumeric usernames.

| Setting | Default |
| --- | --- |
| Localized language | English |
| Built-in categories | all 23 enabled |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Curated partial matching | enabled through strict mode |
| Obfuscation / leetspeak matching | enabled |
| Selected Unicode-confusable matching | enabled |
| Profanity matching | enabled |
| Minimum length | `3` |
| Maximum length | `32` |
| `Rule.Numbers` | **disabled** |
| `Rule.CountryNames` | **disabled** |
| `Rule.PopularCityNames` | **disabled** |
| Whitespace | rejected |
| Built-in `-` and `_` | blocked |
| Leading/trailing separators | rejected |
| Invisible-only identifiers | rejected |
| Unicode control characters | rejected |
| Unicode format characters | rejected |
| ASCII-only | disabled |
| Generic profanity substring matching | disabled |
| `Pattern.NumericOnly` | enabled |
| `Pattern.Repeated` | enabled |
| `Pattern.SymbolOnly` | enabled |
| `Pattern.AsciiArt` | enabled |
| `Pattern.UppercaseOnly` | disabled |

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

For example, disabling `Pattern.UppercaseOnly` does not make `ADMIN` claimable: reserved-name normalization still resolves it to `admin`.

## Rule configuration

Use the 0.7.2 helpers for incremental configuration:

```csharp
var options = new Options();

options.EnableRule(Rule.CountryNames);
options.EnableRule(Rule.Numbers);
options.DisableRule(Rule.Whitespace);
```

Multiple flags can be changed together:

```csharp
options.EnableRule(Rule.CountryNames | Rule.PopularCityNames);
options.DisableRule(Rule.Whitespace | Rule.BlockedCharacters);
```

`Options.EnabledOptionalRules` exposes the currently active opt-in rule set.

`DisabledRules` is retained for compatibility as a full mask. Assigning it replaces the mask, so `EnableRule(...)` and `DisableRule(...)` are preferred for incremental 0.7.2 configuration.

## Geography matching

Country and city rules are intentionally independent from dataset categories.

```csharp
var countryChecker = new Checker(
    new Options().EnableRule(Rule.CountryNames));

var cityChecker = new Checker(
    new Options().EnableRule(Rule.PopularCityNames));
```

With both enabled, representative behavior is:

```text
france          -> CountryName
netherlands     -> CountryName
United Kingdom  -> CountryName when structural spacing is relaxed
amsterdam       -> PopularCityName
New York        -> PopularCityName when structural spacing is relaxed
francelover     -> not rejected by the geography rule
newyorker       -> not rejected by the geography rule
```

Compact geography matching follows `Rule.CompactMatching`. Disabling compact matching also disables geography compaction.

`AllowedIdentifiers` does not bypass an explicitly enabled geography rule. This preserves the package's deny-first behavior.

## Identifier pattern checks

Pattern rules are separate from `Rule` flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);
```

The default pattern set is:

```text
NumericOnly    on
Repeated       on
SymbolOnly     on
AsciiArt       on
UppercaseOnly  off
```

A numeric-only identifier therefore remains rejected even though `Rule.Numbers` is disabled by default.

## Placeholder identifiers

The global `placeholders` category reserves literal null-like and missing-value identifiers such as:

```text
null
undefined
empty
emptyvalue
none
nil
unset
missing
unknown
notset
novalue
nullvalue
undefinedvalue
missingvalue
placeholder
placeholdervalue
defaultvalue
```

Disable it only when the application intentionally permits these identifiers:

```csharp
options.DisableCategory(Category.Placeholders);
```

The literal string `"null"` is reserved by default; actual C# `null` remains accepted.

## Dataset coverage

0.7.2 carries the vocabulary produced by the 0.7.1 sweep:

| Category | Entries | Unique values |
| --- | ---: | ---: |
| `authentication` | 478 | 478 |
| `automation` | 48 | 48 |
| `brands` | 556 | 556 |
| `commerce` | 56 | 56 |
| `communications` | 476 | 474 |
| `community` | 37 | 37 |
| `developer` | 475 | 474 |
| `finance` | 479 | 479 |
| `governance` | 488 | 488 |
| `identity` | 1,523 | 1,521 |
| `infrastructure` | 469 | 468 |
| `legal` | 59 | 59 |
| `moderation` | 475 | 475 |
| `official` | 480 | 472 |
| `operations` | 476 | 475 |
| `other` | 24 | 24 |
| `placeholders` | 17 | 17 |
| `profanity` | 861 | 848 |
| `roles` | 813 | 757 |
| `security` | 53 | 53 |
| `support` | 1,010 | 1,001 |
| `system` | 1,381 | 1,363 |
| `technology` | 416 | 416 |
| **Total** | **11,150** | **11,039** |

These are stored dataset entries, not the total number of forms the matcher can detect. Normalization, compact matching, authorized partial matching, obfuscation, Unicode-confusable handling, patterns, and optional geography rules extend coverage without storing every possible spelling.

## Language support

English localized data is enabled by default. Additional language packs are additive:

- English
- Dutch
- German
- French
- Spanish
- Italian
- Portuguese
- Polish
- Turkish
- Indonesian
- Czech
- Vietnamese
- Hungarian
- Swedish
- Romanian

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.AddLanguage(Language.German);
});
```

Global categories remain active independently of language selection unless explicitly disabled.

## Configure the policy

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(Language.Dutch);

    options.DisableCategory(Category.Brands);
    options.DisableCategory(Category.Technology);

    options.EnableRule(Rule.CountryNames);
    options.EnableRule(Rule.Numbers);

    options.EnablePattern(Pattern.UppercaseOnly);
    options.DisablePattern(Pattern.AsciiArt);

    options.AllowedIdentifiers.Add("superadmin");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");
});
```

### Category selection

Every built-in category is enabled by default. `DisableCategory(...)` excludes that category before exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed.

The 0.7.1 ownership tests guard against newly swept exact values being silently duplicated across categories.

### Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception for complete built-in reserved identifiers after trim → NFKC → invariant-lowercase normalization.

It does not bypass structural rules, pattern rules, geography rules, or explicit application reservations.

### Application reservations

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` performs whole-identifier matching only. `ReservedMatchMode.Default` participates in the configured matching pipeline. `AdditionalReserved` retains its established behavior.

## Curated partial matching

Since 0.6.0, built-in substring matching is explicitly dataset-authorized.

```text
Reserve this complete identifier?                 values / partialValues
Reject it safely inside a larger identifier?      partialValues / partial: true
```

Ordinary `values` do not automatically become generic substring roots. This is why words such as `vote`, `member`, and `active` can be exact-reserved without blocking `devote`, `rememberme`, or `hyperactive`.

## Unicode protection

Unclaimable uses Unicode NFKC normalization, invariant casing, and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

Malformed UTF-16 is rejected before normalization. Invisible-only identifiers, control characters, and format characters are rejected by default.

The confusable mapping is **not a complete Unicode Technical Standard #39 implementation**. Passing Unclaimable does not prove that no visual spoofing technique exists.

## Canonical usernames are an application concern

Unclaimable is a policy and reserved-name layer. Your application should separately define:

- case sensitivity;
- stored Unicode normalization;
- database uniqueness and collation;
- display-name versus login-name behavior;
- canonical URL/routing representation;
- account-specific impersonation decisions.

Do not use a successful Unclaimable result as a substitute for your database uniqueness check.

## Detailed results

Use `Check` for the first reason:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
```

Use `CheckDetailed` for all applicable diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

## ASP.NET Core

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.EnableRule(Rule.CountryNames);
});
```

Use `IChecker` directly or DataAnnotations:

```csharp
public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

Validation-message precedence is attribute-level, reason-specific configured message, global configured message, then built-in fallback.

## Release quality

The 0.7.2 release candidate is validated through:

- **518 unit/regression tests**;
- shared conformance corpora;
- category/rule/toggle/pattern/geography configuration tests;
- dataset statistics and category-ownership checks;
- source builds with localized language packs removed;
- NuGet package creation and metadata/content validation;
- clean packaged-consumer restore and execution;
- public API package validation against the latest published baseline (`0.7.0` while 0.7.1 remains staged);
- deterministic builds, Source Link, portable PDBs, and `.snupkg` symbol packages;
- pinned GitHub Actions with Dependabot maintenance.

The release workflow only publishes for a version-matching `v*` tag on the current `main` commit. No 0.7.2 tag or NuGet publish is created during release preparation.

## Release history

### 0.7.2

Allows mixed alphanumeric usernames by default, keeps numeric-only protection enabled, adds optional country/city rules, introduces incremental rule helpers, and adds dedicated geography diagnostics.

### 0.7.1

Systematically expands exact reserved-name vocabulary while preserving exact-only behavior for generic roots and adding category-ownership regression coverage.

### 0.7.0

Adds identifier-shape `Pattern` protections and the global `placeholders` category.

### 0.6.0

Makes built-in partial matching explicitly dataset-authorized to reduce false positives.

### 0.5.0

Adds category selection, exact allowed identifiers, and explicit custom reservation match modes.

See [CHANGELOG.md](CHANGELOG.md) for the full history.

## License

Unclaimable is licensed under the [Mozilla Public License 2.0](LICENSE).
