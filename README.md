<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![latest line coverage](https://img.shields.io/badge/latest%20line%20coverage-98.26%25-brightgreen.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

Latest measured production line coverage: **98.26%**. Engineering target: **100%**; enforced CI minimum: **98%**.

**Strict, fast username and identifier validation for .NET.**

Prevent reserved, protected, misleading, degenerate, and unsafe identifiers before they can be claimed. Unclaimable combines curated datasets, Unicode-aware matching, structural validation, configurable rules, identifier-shape checks, optional protected-identity lists, and ASP.NET Core integration with no runtime dependencies in the core package.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Changelog**](CHANGELOG.md)

## Current release: 0.7.6

0.7.6 introduces the optional `Unclaimable.Extended` package while keeping every first-party package on the same release version.

### Email identity protection

## Unclaimable.Extended

0.7.6 adds the optional `Unclaimable.Extended` package. It uses the Core matching engine and contributes a much larger identity snapshot without moving or removing anything that already ships in `Unclaimable`.

Installing the package alone does not change validation behavior. Enable it explicitly:

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseExtendedData();

var checker = new Checker(options);
```

All Extended groups are enabled once you opt in. Disable only the groups your application does not need:

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.Celebrities);
    extended.DisableCategory(ExtendedCategory.Sports);
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

The first 0.7.6 snapshot contains **36,313 additional identifiers**:

| Extended group | Entries |
| --- | ---: |
| Companies | 12,500 |
| Education | 10,155 |
| Geography / administrative subdivisions | 3,722 |
| Transport, airports and operators | 3,446 |
| Sports clubs, teams and leagues | 2,740 |
| Financial institutions | 1,105 |
| Regional brands | 616 |
| Healthcare / pharma | 608 |
| Media organizations | 260 |
| Professions / titles | 159 |
| Celebrities | 120 |
| Crypto projects | 120 |
| Fictional characters / franchises | 114 |
| Platforms / services | 99 |
| Historical figures | 96 |
| Entertainment properties | 91 |
| Government / public bodies | 86 |
| Public figures | 74 |
| Multilingual reserved vocabulary | 72 |
| Regional slang / profanity | 69 |
| International organizations | 61 |

Extended entries use `ReservedMatchMode.WholeIdentifier`: exact, compact, selected Unicode-confusable, and obfuscation checks still apply, but these large datasets do not become arbitrary substring roots. Core entries are indexed first, so an identifier already protected by Core keeps its existing Core match/category when Extended is enabled.

Large imported sets are embedded as deterministic snapshots; the package performs no runtime data downloads. Source/provenance information is shipped in `data/SOURCES.md` inside the package.

`Unclaimable.Email` validates both sides of an email address. The local part is checked with an email-adapted Unclaimable policy, while application-configured protected domains are checked for typographical variants, adjacent transpositions, common Unicode/ASCII confusables, protected-label reuse, and embedded-domain impersonation.

```csharp
using Unclaimable.Email;

var options = new EmailOptions();
options.ProtectedDomains.Add("lidl.nl");
options.IssuingDomains.Add("lidl.nl");

var emailChecker = new EmailChecker(options);

var existing = emailChecker.CheckExistingAddress("admin@lidi.nl");
// ReservedLocalPart, while DomainLookalikeKind also reports the lidl.nl typo.

var created = emailChecker.CheckNewAddress("bluegarden@lidl.nl");
// Allowed when the local part is claimable and the issuing domain is approved.
```

Exact protected domains and their real subdomains are accepted. Issuing domains are automatically protected. The package validates practical unquoted mailbox syntax and DNS/IDN domain shape locally; it does not perform DNS or MX lookups and does not claim that a mailbox exists.

#### Protected-domain checks and tested spoof forms

Protected-domain matching is deterministic and runs in this order:

1. Parse and IDN-normalize the domain to lowercase ASCII.
2. Accept an exact configured protected domain or a real subdomain of it.
3. Reject an embedded protected domain such as `google.com.attacker.com`.
4. Decode IDN/punycode back to Unicode and compare a confusable skeleton. This covers selected Greek/Cyrillic lookalikes and common ASCII substitutions such as `0/o`, `2/z`, `3/e`, `4/a`, `5/s`, `6|9/g`, `7/t`, `8/b`, and `1/l`.
5. Apply bounded Damerau-Levenshtein typo matching. The default maximum distance is `1`, so one insertion, deletion, substitution, or adjacent transposition is suspicious.
6. Reject reuse of the protected registrant label on another TLD, hyphenated lure domain, or unrelated domain hierarchy.

The v0.7.5 regression suite exercises McDonald's, Nike, Google, Amazon, Visa, and Nvidia. Representative outcomes:

| Protected domain | Candidate domain | Result | Why |
| --- | --- | --- | --- |
| `mcdonalds.com` | `mcdonalds.com` | allowed | exact configured domain |
| `mcdonalds.com` | `mail.mcdonalds.com` | allowed | genuine subdomain |
| `mcdonalds.com` | `mcdonald.com` | `Typographical` | one deletion |
| `mcdonalds.com` | `mcdnoalds.com` | `Typographical` | adjacent transposition |
| `mcdonalds.com` | `mcd0nalds.com` | `Confusable` | ASCII `0/o` |
| `mcdonalds.com` | `mcdоnalds.com` | `Confusable` | Cyrillic `о` for Latin `o` |
| `mcdonalds.com` | `xn--mcdnalds-pbh.com` | `Confusable` | punycode decodes to the Unicode homograph |
| `mcdonalds.com` | `mcdonalds.net` | `ProtectedLabelReuse` | protected label on another TLD |
| `mcdonalds.com` | `mcdonalds-login.com` | `ProtectedLabelReuse` | protected label reused in a lure label |
| `mcdonalds.com` | `mcdonalds.com.attacker.com` | `EmbeddedProtectedDomain` | real domain text embedded before an attacker-controlled suffix |
| `nike.com` | `nkie.com` | `Typographical` | adjacent transposition |
| `nike.com` | `nik3.com` | `Confusable` | ASCII `3/e` |
| `nike.com` | `nіke.com` | `Confusable` | Cyrillic `і` for Latin `i` |
| `google.com` | `gogle.com` | `Typographical` | one deletion |
| `google.com` | `gooogle.com` | `Typographical` | one insertion |
| `google.com` | `googel.com` | `Typographical` | adjacent transposition |
| `google.com` | `g00gle.com` | `Confusable` | repeated ASCII `0/o` |
| `google.com` | `xn--gogle-rce.com` | `Confusable` | punycode Unicode homograph |
| `google.com` | `google.co` | `Typographical` | TLD deletion |
| `amazon.com` | `Amazon.com` | allowed | domain comparison is case-insensitive after normalization |
| `amazon.com` | `amazone.com` | `Typographical` | explicit one-character insertion case |
| `amazon.com` | `amaz0n.com` | `Confusable` | ASCII `0/o` |
| `amazon.com` | `ama2on.com` | `Confusable` | ASCII `2/z` |
| `visa.com` | `vsia.com` | `Typographical` | adjacent transposition |
| `visa.com` | `vi5a.com` | `Confusable` | ASCII `5/s` |
| `visa.com` | `vіsa.com` | `Confusable` | Cyrillic `і` |
| `nvidia.com` | `nvidai.com` | `Typographical` | adjacent transposition |
| `nvidia.com` | `nvidi4.com` | `Confusable` | ASCII `4/a` |
| `nvidia.com` | `nvіdia.com` | `Confusable` | Cyrillic `і` |
| `nvidia.com` | `nvidia.net` | `ProtectedLabelReuse` | protected label on another TLD |

The test suite also covers substitutions, extra/missing letters, uppercase domain input, nested real subdomains, hyphen-prefix and hyphen-suffix lure domains, multiple protected brands in the same checker, and distance-2 typo matching when `MaximumDomainEditDistance = 2`.

A local-part rejection remains the primary `EmailFailureKind` when both sides fail, but the domain result is still retained. For example, `admin@lidi.nl` can report `ReservedLocalPart` while `DomainLookalikeKind` still reports the `lidl.nl` typo.

The Unicode/confusable and leetspeak mapping table used for domain skeletons is shared from the `Unclaimable` core package. `Unclaimable.Email` adds domain-specific IDN/punycode normalization and protected-domain policy on top of that shared base, so generic confusable fixes are made once in Core.

The email-domain test suite is also data-driven. It loads the built-in core and extended `brands` and `technology` datasets and derives protected registrant labels from them, rather than relying only on a fixed list of example companies. In the current v0.7.5 dataset this produces **913 distinct protected labels** from 972 raw entries.

For every derived label, the suite verifies exact-domain and real-subdomain acceptance plus generated deletion, insertion, substitution, adjacent-transposition, alternate-TLD, hyphen-lure, and embedded-domain attacks. Where the label contains supported lookalike characters, the same generated suite also checks ASCII confusables, Unicode homographs, and their punycode representations. Current coverage includes ASCII-confusable generation for **901** labels, Unicode/punycode generation for **912** labels, and transposition generation for all **913** labels. Future compatible additions to those datasets automatically become new email-domain behavior tests.


### 0.7.4 opt-in identity lists

0.7.4 adds ten whole-identifier protection lists, all disabled by default:

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

### Celebrity-name protection

0.7.3 added `Rule.CelebrityNames`, also disabled by default, with 100 protected high-profile identity forms.

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

0.7.2 also introduced two opt-in whole-identifier geography rules:

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
- Exact built-in exceptions and application-specific reservations
- Configurable numeric-only, repeated, symbol-only, ASCII-art, and uppercase-only pattern checks
- Optional country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military identity protection
- Length, whitespace, separator, blocked-character, Unicode-safety, and optional no-number rules
- Dependency-free `netstandard2.0` core
- ASP.NET Core DI and DataAnnotations integration
- Structured fail-fast and multi-diagnostic results
- Known-safe and reserved conformance corpora in CI
- **98% minimum production line-coverage gate**, with a 100% engineering target

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and DataAnnotations integration |
| `Unclaimable.Email` | `netstandard2.0` | email local-part policy and protected-domain impersonation checks |
| `Unclaimable.Extended` | `netstandard2.0` | optional large reserved-identity datasets powered by the Core matcher |

Install the current release:

```bash
dotnet add package Unclaimable --version 0.7.6
dotnet add package Unclaimable.AspNetCore --version 0.7.6
dotnet add package Unclaimable.Email --version 0.7.6
dotnet add package Unclaimable.Extended --version 0.7.6
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

## Default core policy in 0.7.6

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
| `Pattern.Repeated` | enabled |
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

Multiple optional identity lists can be enabled together:

```csharp
options.EnableRule(
    Rule.Nationalities |
    Rule.Currencies |
    Rule.Religions |
    Rule.Landmarks |
    Rule.Events |
    Rule.Awards |
    Rule.FictionalCharacters |
    Rule.Franchises |
    Rule.Professions |
    Rule.Military);
```

`Options.EnabledOptionalRules` exposes the currently active opt-in rule set.

`DisabledRules` is retained for compatibility as a full mask. Assigning it replaces the mask, so `EnableRule(...)` and `DisableRule(...)` are preferred for incremental configuration.

## Protected identity matching

Country/city, celebrity, and 0.7.4 identity-list rules are independent opt-in deny rules.

The celebrity and 0.7.4 identity lists participate in:

- trim/NFKC/invariant-case normalization;
- exact matching;
- compact whole-identifier matching when `Rule.CompactMatching` is enabled;
- configured obfuscation/leetspeak matching;
- selected Unicode-confusable matching.

They do **not** become generic partial-match roots.

`AllowedIdentifiers` does not bypass an explicitly enabled protected-identity rule. This preserves deny-first behavior.

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
