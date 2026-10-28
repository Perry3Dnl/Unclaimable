# Unclaimable

**98.10% Code Coverage** · **0 B Allocations (Boolean Validation)** · **0.61 µs Exact Matching** · **55,869 Dataset Entries** · **100% Tested Backward Compatibility**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.svg)](https://www.nuget.org/packages/Unclaimable)
![Code Coverage](https://img.shields.io/badge/coverage-98.10%25-brightgreen.svg)

## Intelligent username and identifier validation for .NET

**Unclaimable v1.0.0**

Unclaimable is a high-performance .NET library for protecting usernames and identifiers through reserved-name detection, multilingual profanity filtering, Unicode-confusable matching, and advanced obfuscation detection.

With 23 built-in reserved-name categories, 15 supported languages, configurable sensitivity, and optimized matching paths, Unclaimable provides a flexible foundation for modern application identity validation.

Designed for performance, flexibility, and easy integration.

## Quick Start

Install Unclaimable:

```bash
dotnet add package Unclaimable --version 1.0.0
```

Validate a username:

```csharp
using Unclaimable;

var checker = new Checker();

// Fast boolean validation — ideal for simple allow/block decisions.
bool isClaimable = checker.IsClaimable("bluegarden");

// Structured validation — ideal for displaying rejection reasons.
var result = checker.Check("bluegarden");

if (result.IsClaimable)
{
    // Continue registration
}
```

## Table of Contents

- [Performance at a Glance](#performance-at-a-glance)
- [Features](#features)
- [Configuration Examples](#configuration-examples)
- [Extensive Built-in Datasets](#extensive-built-in-datasets)
- [Modular by Design](#modular-by-design)
- [Architecture and Configuration](#architecture-and-configuration)
- [Built for the .NET Ecosystem](#built-for-the-.net-ecosystem)
- [Quality and Reliability](#quality-and-reliability)
- [For AI Coding Assistants](#for-ai-coding-assistants)
- [Documentation](#documentation)

---

## Performance at a Glance

Unclaimable is built for efficient, high-frequency validation with optimized matching paths and low garbage-collection overhead.

| Validation metric | v1.0.0 benchmark |
| :--- | ---: |
| Ordinary accepted boolean validation | **0 B allocated** |
| Unclaimable ordinary no-match check | **10.01 µs** |
| Unclaimable + Extended ordinary no-match check | **9.46 µs** |
| Unclaimable + Extended embedded matching | **4.90 µs** |
| Unclaimable + Extended exact matching | **0.61 µs** |
| Detailed diagnostic match | **104 B allocated** |


*BenchmarkDotNet 0.14.0 · .NET 8 · Ubuntu 24.04. Unclaimable and Unclaimable + Extended use identical benchmark strings with separately constructed matching indexes. These results describe the specified warm-checker scenarios.*

## Features

### Advanced reserved-name protection

- 23 built-in reserved-name categories
- Brand, company, and organization protection
- Government and official identity protection
- Geographic and regional identity validation
- Celebrity and public-figure matching
- Profanity, slang, and hate-speech filtering
- Configurable identity and application-specific reservations
- Placeholder and system identifier protection

### Multilingual validation

Built-in support for **15 localized language datasets**: English, Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

Multilingual profanity detection is enabled by default, with customizable language selection.

### Intelligent matching engine

| Matching strategy | Capability |
| :--- | :--- |
| Exact matching | Direct reserved-name recognition |
| Compact matching | Recognition across separators and punctuation |
| Partial matching | Detection inside longer identifiers |
| Obfuscation matching | Leetspeak and disguised identifier detection |
| Unicode matching | Selected visually confusable character recognition |
| Pattern detection | Repeated, numeric-only, symbol-only, and ASCII-art patterns |

### Configurable validation

- Four application-oriented validation presets
- Four obfuscation sensitivity levels
- Three false-positive sensitivity levels
- Per-category configuration
- Flexible rule and pattern controls
- Scoped validation exceptions
- Application-specific reserved identifiers
- Detailed validation diagnostics

## Configuration Examples

### Sensitivity configuration

```csharp
var options = new Options
{
    ObfuscationSensitivity = ObfuscationSensitivity.High,
    FalsePositiveSensitivity = FalsePositiveSensitivity.Medium
};

var checker = new Checker(options);
```

### Ready-to-use validation presets

```csharp
var username = new Checker(ValidationPresets.Username());
var displayName = new Checker(ValidationPresets.DisplayName());
var organization = new Checker(ValidationPresets.OrganizationName());
var gameTag = new Checker(ValidationPresets.GameTag());
```

### Multilingual configuration

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
var checker = new Checker(options);
```

### Custom reservations and scoped exceptions

```csharp
var options = new Options();
options.Reserve("internalbot", matching: ReservedMatchMode.Exact);
options.AllowIdentifierForRule("Charlotte", Rule.PopularCityNames);
var checker = new Checker(options);
```



### Detailed diagnostics

```csharp
var checker = new Checker();
var result = checker.CheckDetailed("bluegarden");
```

## Extensive Built-in Datasets

Unclaimable combines its built-in datasets with optional Extended and supplemental identity vocabularies.

| Dataset | Source entries |
| :--- | ---: |
| Unclaimable built-in datasets | **11,680** |
| Extended datasets | **43,779** |
| Supplemental identity vocabulary | **410** |
| **Total source entries** | **55,869** |

The Extended package adds 28 categories across 43 embedded JSON datasets, including companies, education, geography, entertainment, sports, professions, regional identities, multilingual profanity, and more.

All Extended matching is performed through the Unclaimable validation engine. The totals are source-entry counts across the available datasets.

## Modular by Design

**Unclaimable** provides high-performance username and identifier validation through a lightweight, dependency-free .NET Standard 2.0 library.

The Unclaimable ecosystem includes optional packages that extend its functionality while sharing the same validation engine.

| Package | Purpose | NuGet |
| :--- | :--- | :--- |
| **Unclaimable** | Username and identifier validation | [NuGet](https://www.nuget.org/packages/Unclaimable) |
| **Unclaimable.Extended** | Expanded reserved-name and multilingual datasets | [NuGet](https://www.nuget.org/packages/Unclaimable.Extended) |
| **Unclaimable.AspNetCore** | ASP.NET Core dependency injection and DataAnnotations | [NuGet](https://www.nuget.org/packages/Unclaimable.AspNetCore) |
| **Unclaimable.FluentValidation** | FluentValidation 12 integration | [NuGet](https://www.nuget.org/packages/Unclaimable.FluentValidation) |
| **Unclaimable.Email** | Email identity and protected-domain validation | [NuGet](https://www.nuget.org/packages/Unclaimable.Email) |
| **Unclaimable.Files** | Portable filename and extension policies | [NuGet](https://www.nuget.org/packages/Unclaimable.Files) |

Each package has its own installation instructions, configuration options, and integration examples.

### Explore the Companion Packages

- [Extended datasets](platforms/dotnet/src/Unclaimable.Extended/README.md)
- [ASP.NET Core integration](platforms/dotnet/src/Unclaimable.AspNetCore/README.md)
- [FluentValidation integration](platforms/dotnet/src/Unclaimable.FluentValidation/README.md)
- [Email validation](platforms/dotnet/src/Unclaimable.Email/README.md)
- [Filename validation](platforms/dotnet/src/Unclaimable.Files/README.md)

## Architecture and Configuration

### Reusable checker instances

Unclaimable is designed around reusable checker instances with validation rules and matching indexes prepared during construction.

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
var checker = new Checker(options);
```

The checker captures its matching configuration when constructed, supporting consistent validation behavior throughout the application's lifetime.

### Runtime character policies

Applications can use the Unclaimable `IPolicy` interface for runtime character-policy updates while retaining their configured matching behavior.

### Embedded datasets

Unclaimable and Extended use embedded datasets that are loaded and indexed for validation. Extended supplies additional data through the same Unclaimable validation engine.

### Initialization and memory

Unclaimable prepares its matching indexes during checker construction, enabling efficient repeated validation.

Unclaimable and Extended use optimized indexing structures tailored to their respective datasets, with initialization performed when a checker instance is created.

## Built for the .NET Ecosystem

Unclaimable targets **.NET Standard 2.0**, supporting a broad range of modern .NET application platforms.

Validated application environments include ASP.NET Core, Blazor WebAssembly, .NET MAUI Android, Windows Forms, WPF, Avalonia, Uno Platform, Console applications, and Worker Services.

## Quality and Reliability

Unclaimable v1.0.0 is backed by comprehensive automated testing and quality verification.

| Quality measure | Verified result |
| :--- | ---: |
| Automated tests | **20,380 passed** |
| Production line coverage | **98.10%** |

## For AI Coding Assistants

> **Package context:** `Unclaimable` is a high-performance username and identifier validation library targeting `.NET Standard 2.0`. Its validation engine provides reserved-name matching, multilingual profanity filtering, selected Unicode-confusable detection, leetspeak analysis, configurable validation policies, and structured diagnostics. For simple boolean checks, use `new Checker().IsClaimable("identifier")`. For structured results, use `new Checker().Check("identifier")`. Optional packages provide Extended vocabulary, ASP.NET Core integration, FluentValidation rules, email identity checks, and filename validation.

## Documentation

- [Configuration guide](docs/CONFIGURATION.md)
- [Unclaimable API documentation](platforms/dotnet/src/Unclaimable/README.md)
- [Unclaimable package on NuGet](https://www.nuget.org/packages/Unclaimable)

## Unclaimable v1.0.0

**One validation engine. Modular datasets. Flexible policies. Built for .NET.**

Unclaimable makes it easy to bring advanced identifier validation into applications of every size.
