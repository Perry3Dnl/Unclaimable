# Unclaimable.Extended

Large optional reserved-identity datasets for Unclaimable.

**Package version: 0.8.0**

## Install

```bash
dotnet add package Unclaimable.Extended --version 0.8.0
```

Installing the package alone does not change validation behavior. Enable the data explicitly.

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

All Extended groups are enabled after opt-in.

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

## Exact Extended exceptions

```csharp
options.UseExtendedData(extended =>
{
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

The exception applies to that complete Extended identifier without disabling the entire category.

## Dataset snapshot

The initial Extended snapshot contains **36,313 additional identifiers**:

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

## Matching behavior

Extended entries use `ReservedMatchMode.WholeIdentifier`.

That means exact, compact, selected Unicode-confusable, and obfuscation checks still apply, while these large identity datasets do **not** become arbitrary substring roots.

Core entries are indexed first. If an Extended identity is already protected by Core, the existing Core match and category remain authoritative.

## Embedded snapshot and provenance

The datasets are embedded in the package as deterministic snapshots. No runtime data download is performed.

Source and provenance information is included in:

- `data/SOURCES.md`
- `data/THIRD_PARTY_NOTICES.md`
