# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current release: 0.7.4**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, configurable matching, identifier-shape checks, Unicode-aware protections, optional protected-identity lists, and ASP.NET Core integration.

## Install

```bash
dotnet add package Unclaimable --version 0.7.4
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.7.4
```

## What's new in 0.7.4

0.7.4 adds ten opt-in protected-identity lists. All are disabled by default:

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

Enable only the lists your application needs:

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

Representative protected identities include `dutch`, `euro`, `bitcoin`, `christianity`, `eiffeltower`, `olympics`, `nobelprize`, `darthvader`, `starwars`, `doctor`, and `airforce`.

These lists use whole-identifier protection. They support normalized exact matching, case-insensitive matching, compact separator/punctuation forms, configured obfuscation/leetspeak matching, and selected Unicode-confusable matching. They do **not** become generic substring roots, so values such as `doctorwho`, `starwarsfan`, or `americanfootball` are not rejected merely because they contain a protected identity.

## Celebrity-name protection from 0.7.3

0.7.3 added the opt-in `Rule.CelebrityNames` list with 100 protected high-profile identity forms, including values such as `trump`, `donaldtrump`, `taylorswift`, `cristianoronaldo`, `messi`, `elonmusk`, and `mrbeast`.

```csharp
options.EnableRule(Rule.CelebrityNames);
```

Celebrity matching follows the same whole-identifier contract as the 0.7.4 identity lists. `trump`, `TRUMP`, compact forms, supported obfuscations, and selected confusable forms can be rejected without turning `trump` into a broad substring rule that blocks unrelated words such as `trumpet`.

## Practical defaults from 0.7.2

Ordinary mixed alphanumeric usernames are allowed by default:

```text
user7       -> can be claimable
john2026    -> can be claimable
player123   -> can be claimable
```

`Rule.Numbers` is disabled by default, while `Pattern.NumericOnly` remains enabled. Therefore:

```text
123456789   -> rejected as NumericOnly
```

Applications that want to reject every decimal digit can enable that rule explicitly:

```csharp
options.EnableRule(Rule.Numbers);
```

0.7.2 also added two opt-in geography rules:

```csharp
Rule.CountryNames
Rule.PopularCityNames
```

They are whole-identifier checks. Enabling them can reject `france`, `United Kingdom`, `amsterdam`, or `New York` without turning those names into generic substring filters.

## Reserved-vocabulary expansion from 0.7.1

The 0.7.1 work expanded exact reserved-name coverage across authentication, automation, commerce, communications, community, developer, finance, governance, identity, infrastructure, legal, moderation, official, operations, security, and system vocabulary.

Representative exact values include `member`, `vote`, `active`, `authentication`, `announcement`, `apikey`, `treasury`, `username`, `loadbalancer`, `banned`, `verified`, `operations`, `buyer`, `legalhold`, and `phishing`.

Generic values remain exact rather than broad substring roots: `vote` does not block `devote`, `member` does not block `rememberme`, and `active` does not block `hyperactive`.

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

`null` is accepted by Unclaimable so required-field validation can remain a separate concern, for example through `[Required]`.

## Default protection in 0.7.4

The default policy includes:

- all 23 built-in reserved-name categories;
- English localized data;
- exact reserved-name matching;
- compact punctuation/separator matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and bounded obfuscation detection;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only, control-character, and format-character protection;
- minimum length `3` and maximum length `32`;
- whitespace restrictions;
- built-in `-` and `_` blocking;
- leading and trailing separator restrictions;
- numeric-only, repeated-pattern, symbol-only, and conservative ASCII-art checks.

The following protections are disabled by default and must be explicitly enabled when wanted:

- `Rule.Numbers`;
- `Rule.CountryNames`;
- `Rule.PopularCityNames`;
- `Rule.CelebrityNames`;
- `Rule.Nationalities`;
- `Rule.Currencies`;
- `Rule.Religions`;
- `Rule.Landmarks`;
- `Rule.Events`;
- `Rule.Awards`;
- `Rule.FictionalCharacters`;
- `Rule.Franchises`;
- `Rule.Professions`;
- `Rule.Military`;
- `Pattern.UppercaseOnly`;
- generic profanity substring matching;
- ASCII-only input restriction.

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

## Rule configuration

Use the incremental helpers rather than replacing the full legacy rule mask:

```csharp
var options = new Options();

options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
options.DisableRule(Rule.Whitespace);
options.EnableRule(Rule.Numbers);
```

`Options.EnabledOptionalRules` exposes currently enabled opt-in rules.

`DisabledRules` remains for compatibility as a full mask. Assigning it replaces the mask; prefer `EnableRule(...)` and `DisableRule(...)` for incremental configuration.

## Pattern configuration

Pattern checks are configured independently from rule flags:

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

## Category selection

Every built-in category is enabled by default:

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

Disabled categories are excluded before exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed.

## Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception for complete built-in reserved identifiers after trim → NFKC → invariant-lowercase normalization.

```csharp
options.AllowedIdentifiers.Add("superadmin");
```

It does not bypass structural rules, pattern rules, opt-in geography/protected-identity rules, or explicit application reservations.

## Application reservations

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` performs whole-identifier matching. `ReservedMatchMode.Default` participates in the configured matching pipeline.

## Language support

English is enabled by default. Additional localized datasets are available for Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

```csharp
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

## Unicode scope

Unclaimable uses Unicode NFKC normalization and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is **not** a complete Unicode Technical Standard #39 implementation. Passing Unclaimable does not prove that no visual spoofing technique exists.

Applications should separately define canonical username storage, case sensitivity, database uniqueness/collation, display-name behavior, and URL/routing normalization.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
```

For multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

## Release quality

0.7.4 is validated with **644 passing tests**, package-content validation, packaged-consumer smoke tests, public-API compatibility checks, source builds without localized language packs, and a production line-coverage gate.

Latest measured production line coverage before release: **98.07%** (`1,775 / 1,810`). The engineering target is **100%** and CI enforces a **98% minimum**.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
