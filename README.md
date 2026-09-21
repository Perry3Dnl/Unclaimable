<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![latest line coverage](https://img.shields.io/badge/latest%20line%20coverage-98.16%25-brightgreen.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

Latest measured production line coverage: **98.16%**. Engineering target: **100%**; enforced CI minimum: **98%**.

**Strict, fast username and identifier validation for .NET.**

Prevent reserved, protected, misleading, degenerate, and unsafe identifiers before they can be claimed. Unclaimable combines curated datasets, Unicode-aware matching, structural validation, configurable rules, identifier-shape checks, protected-identity lists, and optional integrations with no runtime dependencies in the core package.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Configuration guide**](docs/CONFIGURATION.md) · [**Changelog**](CHANGELOG.md)

## Current release: 0.8.0

0.8.0 establishes the default policy we intend to keep stable going forward: deny-first validation, default-on protected identity rules, clearer repeated-pattern behavior, and narrow exceptions that let applications relax one check without disabling an entire protection.

## 0.8.0: stricter defaults with narrow exceptions

The important behavioral change is that Unclaimable now leans consistently into deny-first validation. Protected identity rules are enabled by default, selected high-trust roots such as `admin`, `staff`, `root`, `owner`, `support`, and `help` can reject containing identifiers, and repeated-pattern defaults are more explicit.

That means applications upgrading from 0.7.8 should expect some identifiers that previously passed to be rejected in 0.8.0.

The fix is **not** to weaken the whole checker. 0.8.0 adds narrow exception APIs:

```csharp
var options = new Options();

// Skip only one rule for one complete identifier.
options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);

// Skip only one pattern for one complete identifier.
options.AllowIdentifierForPattern(
    "ababab",
    Pattern.Repeated);

// Permit one character without disabling BlockedCharacters globally.
options.AllowCharacters("_");

// Permit direct T runs without disabling Pattern.Repeated globally.
options.AllowRepeatedCharacters("T");
```

A scoped exception only skips that check. Every other enabled rule, pattern, dataset match, protected identity list, and application reservation still runs.

For example, a team convention can permit `TTT_user7` without making all repeated letters or separators legal:

```csharp
var options = new Options()
    .AllowCharacters("_")
    .AllowRepeatedCharacters("T");

var checker = new Checker(options);
```

The 0.8.0 repeated-pattern defaults are:

```text
aaa      -> rejected: direct run of 3
abab     -> below cyclic threshold
ababab   -> rejected: repeated span reaches 6
abcabc   -> rejected: repeated span reaches 6
```

All built-in rules are enabled by default except `Rule.Numbers`; this includes the protected country, city, celebrity, and other identity rules. Mixed alphanumeric names remain possible, while `Pattern.NumericOnly` continues to reject all-numeric identifiers. `Pattern.UppercaseOnly` remains opt-in.

For copy-paste recipes, precedence, migration guidance, ASP.NET Core setup, Email local-part customization, and Extended-data exceptions, see the **[0.8.0 configuration and exceptions guide](docs/CONFIGURATION.md)**.

## Companion packages

The repository contains three companion packages that share the same release version as Core. They are summarized here because they are part of the Unclaimable project, but their package-specific READMEs remain the source for detailed usage.

### Unclaimable.AspNetCore

Adds dependency injection and DataAnnotations integration around the Core checker. In 0.8.0 it ships explicit framework assets for `net6.0` through `net11.0` and uses the same Core default rules unless the application configures them differently.

See [the ASP.NET Core package README](platforms/dotnet/src/Unclaimable.AspNetCore/README.NUGET.md).

### Unclaimable.Email

Adds email local-part identity checking plus protected-domain lookalike and impersonation detection. Its local-part checker starts from the same Core 0.8.0 identity defaults, while email-specific syntax concerns such as username length, separator, blocked-character, whitespace, and shape checks are handled separately.

See [the Email package README](platforms/dotnet/src/Unclaimable.Email/README.NUGET.md).

### Unclaimable.Extended

Adds a much larger optional identity snapshot covering companies, education, transport, sports, finance, public bodies, public figures, brands, healthcare, media, and other groups. Installing it alone does not change behavior; applications opt in with `UseExtendedData()`. Core rules remain in effect and Extended entries are additive.

See [the Extended package README](platforms/dotnet/src/Unclaimable.Extended/README.NUGET.md).

### Historical: 0.7.4 opt-in identity lists

0.7.4 added ten whole-identifier protection lists. They were opt-in in 0.7.8; the 0.8.0 policy enables them by default:

```csharp
Rule.Nationalities
Rule.Currencies
Rule.Religions
Rule.Landmarks
Rule.Events
Rule.Awards
Rule.FictionalCharacters
Rule.Franchises
Rule.Professions
Rule.Military
```

Enable only the scopes your application needs:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.EnableRule(
        Rule.Nationalities |
        Rule.Currencies |
        Rule.FictionalCharacters |
        Rule.Franchises);
});
```

Representative identities include `dutch`, `euro`, `bitcoin`, `christianity`, `eiffeltower`, `olympics`, `nobelprize`, `darthvader`, `starwars`, `doctor`, and `airforce`.

The lists use normalized exact matching, case-insensitive matching, compact separator/punctuation matching, configured obfuscation/leetspeak matching, and selected Unicode-confusable matching. They deliberately do **not** become generic substring roots.

### Historical: celebrity-name protection

0.7.3 added `Rule.CelebrityNames` with 100 protected high-profile identity forms. It was opt-in in 0.7.8; the 0.8.0 policy enables it by default.

```csharp
options.EnableRule(Rule.CelebrityNames);
```

Representative protected forms include `trump`, `donaldtrump`, `taylorswift`, `cristianoronaldo`, `messi`, `elonmusk`, and `mrbeast`.

A celebrity identity is protected as a complete identifier and through configured compact/obfuscation/confusable forms, but not as an arbitrary substring. For example, `trump` can be rejected while `trumpet` is not rejected merely because it contains those letters.

### Practical number and geography defaults

Since 0.7.2, `Rule.Numbers` is disabled by default so ordinary mixed alphanumeric usernames can be claimable:

```text
john2026     -> can be claimable
user7        -> can be claimable
player123    -> can be claimable
```

`Pattern.NumericOnly` remains enabled:

```text
123456789    -> rejected as NumericOnly
```

0.7.2 also introduced two whole-identifier geography rules. They were opt-in in 0.7.8; the 0.8.0 policy enables both by default:

```csharp
Rule.CountryNames
Rule.PopularCityNames
```

They can reject identifiers such as `france`, `United Kingdom`, `amsterdam`, or `New York` without turning those names into broad substring filters.

### Expanded reserved vocabulary

The staged 0.7.1 sweep expanded exact reserved-name vocabulary across authentication, automation, commerce, communications, community, developer, finance, governance, identity, infrastructure, legal, moderation, official, operations, security, and system data.

Representative values include `member`, `vote`, `active`, `authentication`, `announcement`, `apikey`, `treasury`, `username`, `loadbalancer`, `banned`, `verified`, `operations`, `buyer`, `legalhold`, and `phishing`.

Generic words remain exact rather than broad substring roots: `vote` does not block `devote`, `member` does not block `rememberme`, and `active` does not block `hyperactive`.

## Features

- Reserved-name protection across **23 built-in categories**
- **11,150 filter entries** representing **11,039 unique values**
- Exact, compact, curated partial, obfuscation, and selected Unicode-confusable matching
- **15 localized language datasets** with English enabled by default
- Per-category enable/disable controls
- Exact built-in exceptions, scoped rule/pattern allowances, and application-specific reservations
- Configurable numeric-only, repeated, symbol-only, ASCII-art, and uppercase-only pattern checks
- Configurable country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military identity protection
- Length, whitespace, separator, blocked-character, Unicode-safety, and optional no-number rules
- Dependency-free `netstandard2.0` core
- ASP.NET Core DI and DataAnnotations integration
- Structured fail-fast and multi-diagnostic results
- Behavioral conformance corpus in CI
- **98% minimum production line-coverage gate**, with a 100% engineering target

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net6.0`–`net11.0` | ASP.NET Core DI and DataAnnotations integration; framework-specific assets are tested per target |
| `Unclaimable.Email` | `netstandard2.0` | email local-part policy and protected-domain impersonation checks |
| `Unclaimable.Extended` | `netstandard2.0` | optional large reserved-identity datasets powered by the Core matcher |

### Application ecosystem compatibility

The portable `netstandard2.0` packages — `Unclaimable`, `Unclaimable.Email`, and `Unclaimable.Extended` — are intended for reuse across modern .NET application models. The 0.8.0 compatibility workflow compile-checks consumers for:

- .NET MAUI (Android);
- Blazor WebAssembly;
- WPF;
- Windows Forms;
- Console applications;
- Worker Services;
- Avalonia;
- Uno Platform.

Those application models use the same portable packages; there is no separate MAUI, Blazor, Avalonia, or Uno package to install. `Unclaimable.AspNetCore` remains the dedicated ASP.NET Core integration package and ships explicit `net6.0` through `net11.0` assets.

Install the current release:

```bash
dotnet add package Unclaimable --version 0.8.0
dotnet add package Unclaimable.AspNetCore --version 0.8.0
dotnet add package Unclaimable.Email --version 0.8.0
dotnet add package Unclaimable.Extended --version 0.8.0
```

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

## Previous 0.7.8 default policy

This section documents the previous 0.7.8 package for migration reference. The 0.8.0 defaults are intentionally stricter; see **0.8.0: stricter defaults with narrow exceptions** above and the [configuration guide](docs/CONFIGURATION.md).

`new Options()` in 0.7.8 keeps strong protection while allowing ordinary alphanumeric usernames.

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
| `Rule.CelebrityNames` | **disabled** |
| 0.7.4 identity-list rules | **disabled** |
| Whitespace | rejected |
| Built-in `-` and `_` | blocked |
| Leading/trailing separators | rejected |
| Invisible-only identifiers | rejected |
| Unicode control characters | rejected |
| Unicode format characters | rejected |
| ASCII-only | disabled |
| Generic profanity substring matching | disabled |
| `Pattern.NumericOnly` | enabled |
| `Pattern.Repeated` | enabled; repeated-span minimum `4` |
| `Pattern.SymbolOnly` | enabled |
| `Pattern.AsciiArt` | enabled |
| `Pattern.UppercaseOnly` | disabled |

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

For example, disabling `Pattern.UppercaseOnly` does not make `ADMIN` claimable: reserved-name normalization still resolves it to `admin`.

## Rule configuration

Use the incremental helpers for rule configuration:

```csharp
var options = new Options();

options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
options.EnableRule(Rule.Numbers);
options.DisableRule(Rule.Whitespace);
```

In 0.8.0 the protected identity rules start enabled. Disable only the scopes your application intentionally permits:

```csharp
options.DisableRule(
    Rule.Currencies |
    Rule.Awards |
    Rule.FictionalCharacters);
```

Use `EnableRule(...)` to turn a disabled rule back on.

`Options.EnabledOptionalRules` retains its existing API name for compatibility and exposes the currently active protected-identity rule set.

`DisabledRules` is retained for compatibility as a full mask. Assigning it replaces the mask, so `EnableRule(...)` and `DisableRule(...)` are preferred for incremental configuration.

## Protected identity matching

Country/city, celebrity, and the additional identity-list rules are independent deny rules. They are enabled by default in 0.8.0 and can be disabled individually.

The celebrity and 0.7.4 identity lists participate in:

- trim/NFKC/invariant-case normalization;
- exact matching;
- compact whole-identifier matching when `Rule.CompactMatching` is enabled;
- configured obfuscation/leetspeak matching;
- selected Unicode-confusable matching.

They do **not** become generic partial-match roots.

`AllowedIdentifiers` does not bypass a protected-identity rule. Use `AllowIdentifierForRule(...)` when one complete identifier should skip one of those rules without disabling it globally.

```csharp
options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);
```

This preserves deny-first behavior: every unrelated rule, pattern, dataset match, and application reservation still runs.

## Identifier pattern checks

Pattern rules are separate from `Rule` flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);
```

Default pattern state:

```text
NumericOnly    on
Repeated       on
SymbolOnly     on
AsciiArt       on
UppercaseOnly  off
```

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
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

Global categories remain active independently of language selection unless explicitly disabled.

## Category selection

Every built-in category is enabled by default. `DisableCategory(...)` excludes that category before exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed.

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

## Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception for complete built-in reserved identifiers after trim → NFKC → invariant-lowercase normalization.

```csharp
options.AllowedIdentifiers.Add("superadmin");
```

It does not bypass structural rules, pattern rules, protected-identity rules, or explicit application reservations.

## Narrow rule and pattern allowances

0.8.0 can relax a single check without disabling that check globally:

```csharp
var options = new Options();

options.AllowIdentifierForRule("Charlotte", Rule.PopularCityNames);
options.AllowIdentifierForPattern("ababab", Pattern.Repeated);
options.AllowCharacters("_");
options.AllowRepeatedCharacters("T");
```

An exception skips only the named rule or pattern. The rest of the deny pipeline continues, and explicit application reservations still take precedence. This supports conventions such as repeated team-prefix letters or selected separator characters without weakening unrelated usernames.

## Application reservations

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` performs whole-identifier matching. `ReservedMatchMode.Default` participates in the configured matching pipeline. `AdditionalReserved` retains its established behavior.

## Curated partial matching

Built-in substring matching is explicitly dataset-authorized.

```text
Reserve this complete identifier?                 values / partialValues
Reject it safely inside a larger identifier?      partialValues / partial: true
```

Ordinary `values` do not automatically become generic substring roots.

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
    options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
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

## Test coverage and release quality

The current 0.7.5 release line has **3,560 passing tests**.

Production coverage is measured across `Unclaimable`, `Unclaimable.AspNetCore`, `Unclaimable.Email`, and `Unclaimable.Extended`; test assemblies and generated files are excluded.

- latest measured line coverage: **98.26%** (`2,147 / 2,185`);
- latest measured branch coverage: **83.55%** (`1,366 / 1,635`);
- engineering target: **100% production line coverage**;
- enforced CI minimum: **98% production line coverage**.

CI also validates source builds without localized language packs, NuGet package contents and metadata, packaged-consumer restore/execution, public API compatibility, deterministic builds, Source Link, portable PDBs, and `.snupkg` symbol packages.

## Release history

### 0.8.0

Establishes the new deny-first default baseline, enables protected identity rules by default, separates direct character runs from cyclic repetition, adds scoped rule/pattern/character exceptions, aligns all first-party packages at 0.8.0, and expands compatibility coverage across ASP.NET Core .NET 6–11 and portable .NET application models.

### 0.7.8

Adds configurable embedded repeated-pattern detection and the original repeated-span threshold control.

### 0.7.6

Adds the optional `Unclaimable.Extended` package with 36,313 additional identifiers across 21 groups while keeping all existing Core datasets and behaviors in place. Also adds categorized `ReservedMatchMode.WholeIdentifier` reservations to Core so extension packages can reuse exact/compact/confusable/obfuscation behavior without creating broad substring roots.

### 0.7.5

Adds the `Unclaimable.Email` package with local-part Unclaimable checks, configurable protected/issuing domains, and domain-lookalike detection.

### 0.7.4

Adds ten opt-in protected-identity lists and the 98% production line-coverage gate, with a 100% engineering target.

### 0.7.3

Adds opt-in celebrity-name protection with 100 curated high-profile identity forms.

### 0.7.2

Allows mixed alphanumeric usernames by default, keeps numeric-only protection enabled, and adds optional country/city rules plus incremental rule helpers.

### 0.7.1

Expands exact reserved-name vocabulary while preserving exact-only behavior for generic roots and adding category-ownership regression coverage.

### 0.7.0

Adds identifier-shape `Pattern` protections and the global `placeholders` category.

See [CHANGELOG.md](CHANGELOG.md) for older release history.

## License

Unclaimable is licensed under the [Mozilla Public License 2.0](LICENSE).
