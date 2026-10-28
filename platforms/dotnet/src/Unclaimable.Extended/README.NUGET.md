# Unclaimable.Extended

Large optional reserved-identity datasets for Unclaimable.

**Package version: 1.0.0 (unreleased development target)**

## Install

**The command below is for the planned 1.0.0 release, which is not published yet.**

```bash
dotnet add package Unclaimable.Extended --version 1.0.0
```

Installing the package alone does not change validation behavior. Enable the data explicitly.

## How Extended works

Extended supplies optional datasets to the Core matching engine. It covers companies, institutions, public figures, regional roles, crypto identities, multilingual slang/profanity, hate speech, and other reserved-name groups. Core rules, matching sensitivity, false-positive settings, and application reservations continue to apply.

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

All Extended dataset groups, are enabled when `UseExtendedData()` is called. Developers can disable individual categories explicitly.

The checker still uses the normal Core defaults at the same time, including the lower-collision default treatment for generic Core identity vocabulary. Extended data remains explicitly opt-in and additive: enabling it does not disable or replace Core structural, matching, pattern, or identity rules already active in `Options`.

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

### Regional and cryptocurrency categories

All categories are included when Extended data is enabled. Disable any categories your application does not want:

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.RegionalRolesNetherlands);
    extended.DisableCategory(ExtendedCategory.CryptoWallets);
    extended.DisableCategory(ExtendedCategory.CryptoExchanges);
});
```

Also available: `RegionalRolesUnitedStates`, `CryptoProtocols`, and `CryptoTokens`. Each starts enabled when `UseExtendedData()` is called. These are curated embedded sets rather than a continuously updated market list. Review the terms for your application and use `AllowedIdentifiers` for exact exceptions.

## Exact Extended exceptions

```csharp
options.UseExtendedData(extended =>
{
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

The exception applies to that complete Extended identifier without disabling the entire category.

Configure this exception inside `UseExtendedData(...)` because Extended entries are registered into Core as explicit reservations using `ReservedMatchMode.Default`. Core rule or pattern exceptions do not erase an Extended reservation after it has been registered.

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

## Additional exact-only identity vocabulary (new, separate opt-in)

The 1.0.0 package also contains **410 conservatively screened supplemental identity
names**, split into official roles, government bodies, cryptocurrency exchanges,
protocols, wallets and projects. These are deliberately separate from the existing
43,779-entry Extended snapshot.

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseSupplementalIdentityData(); // explicitly opt in
var checker = new Checker(options);
```

Registration uses Core's `ReservedMatchMode.Exact`, so new entries do not
automatically become substring, compact or obfuscation denials. Installing Extended,
or calling the preexisting `UseExtendedData()`, **does not register these new entries**.
The four policy presets live in Core and likewise require an explicit selection.
Short and collision-prone generic names were excluded from this supplement.
The full staged 1.0.0 candidate vocabulary is not packaged as active rules.

## Dataset snapshot

The default-enabled Extended snapshot contains **43,779 raw entries across 43 JSON files** and **28 categories**. These are source-entry counts, not globally unique normalized identifiers; Core can merge equivalent reservations.

| Extended group | Raw entries |
| --- | ---: |
| Companies | 12,458 |
| Education | 10,155 |
| Slang Profanity | 4,145 |
| Geography | 3,722 |
| Transport | 3,446 |
| Sports | 2,739 |
| Financial Institutions | 1,105 |
| Regional Brands | 615 |
| Healthcare | 608 |
| Professions | 587 |
| Historical Figures | 561 |
| Platforms | 544 |
| Celebrities | 523 |
| Fiction | 507 |
| Entertainment | 505 |
| Media | 500 |
| Public Figures | 202 |
| Government | 175 |
| International Organizations | 133 |
| Hate Speech | 112 |
| Crypto | 95 |
| Crypto Tokens | 87 |
| Multilingual | 72 |
| Crypto Wallets | 63 |
| Crypto Protocols | 61 |
| Crypto Exchanges | 49 |
| Regional Roles Netherlands | 5 |
| Regional Roles United States | 5 |

Use `ExtendedData.TotalEntries` and `ExtendedData.GetCount(...)` to inspect the packaged snapshot.

## Hate-speech dataset

`ExtendedCategory.HateSpeech` supplies a curated set of identity-targeting abusive identifiers and hateful slogans. It is included when `UseExtendedData()` is enabled, and may be disabled independently:

```csharp
options.UseExtendedData(extended =>
    extended.DisableCategory(ExtendedCategory.HateSpeech));
```

As with all Extended groups, installation alone does not enable it. Entries are passed into Core using `ReservedMatchMode.Default`, so Core controls exact, compact, partial, obfuscation, and confusable matching according to the configured rules. Coverage is curated rather than exhaustive and should not be treated as a complete multilingual content-moderation system.

## Matching behavior

Extended registers entries with `ReservedMatchMode.Default`. Core applies its configured exact, compact, partial, selected Unicode-confusable, and obfuscation matching pipeline. Eligible entries can therefore match inside larger identifiers when partial matching is enabled. Tune the Core options and test representative allowed names before enabling broad datasets.

`ObfuscationSensitivity` defaults to `Medium`; `FalsePositiveSensitivity` defaults to `Low`. Extended has no independent detection engine. Core entries are indexed first; an identity already protected by Core retains its Core match and category.

## Embedded snapshot and provenance

The datasets are embedded in the package as deterministic snapshots. No runtime data download is performed.

Source and provenance information is included in:

- `data/SOURCES.md`
- `data/THIRD_PARTY_NOTICES.md`
