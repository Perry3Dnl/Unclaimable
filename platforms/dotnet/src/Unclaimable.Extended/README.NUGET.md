# Unclaimable.Extended

Large optional reserved-identity datasets for Unclaimable.

**Package version: 0.8.5 (release candidate; not published)**

## Install

**Until v0.8.5 is published, install the latest stable v0.8.4 package.** After release, use `--version 0.8.5` or upgrade via your package manager.

```bash
dotnet add package Unclaimable.Extended --version 0.8.4
```

Installing the package alone does not change validation behavior. Enable the data explicitly.

## What's new in 0.8.5 (release candidate — not yet published)

- Expanded reserved identities, institutions, companies, media, professions, public figures, cryptocurrency and other curated categories.
- Added language-specific profanity/slang datasets covering Czech, German, English, Spanish, French, Hungarian, Indonesian, Italian, Dutch, Polish, Portuguese, Romanian, Swedish, Turkish and Vietnamese, plus other hate-speech and offensive-language coverage.
- Extended **supplies data, not a separate detection engine**. Explicitly activate it using `options.UseExtendedData()`; Core's matching and false-positive settings apply.
- **Core + Extended CI benchmark:** warm checker construction 66.95 → 72.04 ms (+7.6%), construction allocations 35.76 → 28.61 MB (−20.0%), ordinary lookup 9.81 → 5.34 µs, and embedded lookup 11.79 → 2.76 µs. Warm-process results; increased vocabulary and application-specific settings may affect timings.
- Larger reserved lists intentionally block additional names. Applications should evaluate categories and `FalsePositiveSensitivity` before upgrading.


## Cross-platform app compatibility

`Unclaimable.Extended` targets `netstandard2.0` and is compile-checked in .NET MAUI, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform consumers.

The datasets remain embedded and deterministic on those application models; enabling Extended data does not require a platform-specific integration package.

## Quick start

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseExtendedData();

var checker = new Checker(options);
```

All Extended dataset groups, including the new 0.8.4 categories, are enabled when `UseExtendedData()` is called. Developers can disable individual categories explicitly.

The checker still uses the normal 0.8.3 Core defaults at the same time, including the lower-collision default treatment for generic Core identity vocabulary. Extended data remains explicitly opt-in and additive: enabling it does not disable or replace Core structural, matching, pattern, or identity rules already active in `Options`.

## Select categories

Disable only the groups your application does not need:

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.Celebrities);
    extended.DisableCategory(ExtendedCategory.Sports);
});
```

A disabled group can be enabled again with `EnableCategory(...)`.

### New 0.8.4 modules

All new modules are included automatically. Disable any categories your application does not want:

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.RegionalRolesNetherlands);
    extended.DisableCategory(ExtendedCategory.CryptoWallets);
    extended.DisableCategory(ExtendedCategory.CryptoExchanges);
});
```

Also available: `RegionalRolesUnitedStates`, `CryptoProtocols`, and `CryptoTokens`. Each starts enabled when `UseExtendedData()` is called. Entries use whole-identifier matching; these are deliberately small curated sets rather than a broad or continuously updated market list. Review the terms for your application and use `AllowedIdentifiers` for exact exceptions.

## Exact Extended exceptions

```csharp
options.UseExtendedData(extended =>
{
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

The exception applies to that complete Extended identifier without disabling the entire category.

Configure this exception inside `UseExtendedData(...)` because Extended entries are registered into Core as explicit whole-identifier reservations. Core rule or pattern exceptions do not erase an Extended reservation after it has been registered.

Use Core's scoped APIs separately for Core rules and patterns:

```csharp
var options = new Options()
    .AllowRepeatedCharacters("T")
    .AllowCharacters("_");

options.UseExtendedData(extended =>
{
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

This keeps Extended identity exceptions distinct from username-shape exceptions.

## Dataset snapshot

The original Extended snapshot contains **36,313 identifiers**. 0.8.4 adds 30 entries across six new regional/crypto modules and a separate curated hate-speech dataset; all are included with `UseExtendedData()` by default. The counts below describe the original snapshot and exclude the 0.8.4 additions:

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

## Hate-speech dataset (0.8.4)

`ExtendedCategory.HateSpeech` contributes an additional curated set of identity-targeting abusive identifiers and hateful slogans. It is included when `UseExtendedData()` is enabled, and may be disabled independently:

```csharp
options.UseExtendedData(extended =>
    extended.DisableCategory(ExtendedCategory.HateSpeech));
```

As with all Extended groups, installation alone does not enable it. Entries are passed into Core using `ReservedMatchMode.WholeIdentifier`; no independent detection engine or blanket substring matching is introduced. Coverage is curated rather than exhaustive and should not be treated as a complete multilingual content-moderation system.

## Matching behavior

Extended entries use `ReservedMatchMode.WholeIdentifier`.

That means exact, compact, selected Unicode-confusable, and sensitivity-controlled obfuscation checks still apply, while these large identity datasets do **not** become arbitrary substring roots. In 0.8.3 they use the same Core `ObfuscationSensitivity` setting as other whole-identifier reservations; `Medium` remains the default. The 0.8.3 runtime matching and allocation optimizations also apply to these whole-identifier reservations.

Core entries are indexed first. If an Extended identity is already protected by Core, the existing Core match and category remain authoritative.

## Embedded snapshot and provenance

The datasets are embedded in the package as deterministic snapshots. No runtime data download is performed.

Source and provenance information is included in:

- `data/SOURCES.md`
- `data/THIRD_PARTY_NOTICES.md`
