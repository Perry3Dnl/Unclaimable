# Unclaimable.Extended

**Unclaimable v1.0.0** · **44,189 Optional Dataset Entries** · **Modular .NET Integration**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.Extended.svg)](https://www.nuget.org/packages/Unclaimable.Extended)

> **For LLMs & AI Coding Assistants:** `Unclaimable.Extended` provides curated, embedded identity and multilingual datasets for the Unclaimable validation engine. Add `options.UseExtendedData()` before constructing `new Checker(options)` to enable the 43,779-entry Extended snapshot. A separate 410-entry exact-only supplemental identity policy is enabled with `options.UseSupplementalIdentityData()`.

## 43,779 optional reserved-identity and multilingual vocabulary entries, powered by Unclaimable

**Unclaimable.Extended v1.0.0**

43,779 optional reserved-identity and multilingual vocabulary entries, powered by Unclaimable.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Dataset Snapshot](#dataset-snapshot)
- [Documentation](#documentation)

---

## Features

- 43,779 raw source entries in 28 categories across 43 JSON datasets
- Companies, education, geography, sports, public figures, entertainment and regional identities
- Multilingual slang, profanity and hate-speech vocabularies
- Unclaimable-powered exact, compact, eligible partial, Unicode and obfuscation matching
- Configurable Extended categories and exact allowed-identifier exceptions
- Optional supplemental identity vocabulary with 410 exact-only entries

## Get Started

```bash
dotnet add package Unclaimable.Extended --version 1.0.0
```

### Enable Extended datasets

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseExtendedData();
var checker = new Checker(options);
```

### Select categories

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.Celebrities);
    extended.DisableCategory(ExtendedCategory.Sports);
});
```

### Allow a specific Extended identifier

```csharp
options.UseExtendedData(extended =>
{
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

### Add supplemental identity protection

```csharp
var options = new Options();
options.UseSupplementalIdentityData();
var checker = new Checker(options);
```


## Dataset Snapshot

| Dataset | Source entries |
| :--- | ---: |
| Standard Extended groups | **43,779** |
| Supplemental exact-only identity vocabulary | **410** |
| **Total optional Extended entries** | **44,189** |

The standard snapshot is enabled explicitly through `UseExtendedData()`, while the supplemental identity policy is selected independently. All matching is performed by Unclaimable using its normal matching pipeline and configured sensitivity.

## Dataset Provenance

The package includes source information and third-party notices in `data/SOURCES.md` and `data/THIRD_PARTY_NOTICES.md`.

## Compatibility

Targets `netstandard2.0` and supports applications using Unclaimable across mobile, web, desktop, and server environments.

## Documentation

- [GitHub project guide](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/README.md)
- [Configuration guide](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/docs/CONFIGURATION.md)

**Unclaimable v1.0.0 — One validation engine. Modular packages. Flexible policies. Built for .NET.**
