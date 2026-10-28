# Unclaimable

**98.10% Code Coverage** · **0 B Validation Allocations** · **0.61 µs Exact Matching** · **55,869 Dataset Entries** · **100% Tested Backward Compatibility**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.svg)](https://www.nuget.org/packages/Unclaimable)

> **For LLMs & AI Coding Assistants:** `Unclaimable` provides high-performance username and identifier validation for .NET Standard 2.0, including reserved-name matching, multilingual profanity protection, selected Unicode-confusable matching and obfuscation detection. Start with `new Checker().Check("identifier").IsClaimable` and customize `Options` to suit the application.

## Fast, configurable username and identifier protection for .NET Standard 2.0

**Unclaimable v1.0.0**

Fast, configurable username and identifier protection for .NET Standard 2.0.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Performance at a Glance](#performance-at-a-glance)
- [Documentation](#documentation)

---

## Features

- 23 built-in reserved-name categories and 15 localized language datasets
- Multilingual profanity filtering enabled by default
- Exact, compact, partial, obfuscation, and selected Unicode-confusable matching
- Four ready-to-use validation presets
- Scoped exceptions and configurable rule, pattern, and sensitivity policies
- Fast boolean validation and structured diagnostics

## Get Started

```bash
dotnet add package Unclaimable --version 1.0.0
```

### Basic validation

```csharp
using Unclaimable;

var checker = new Checker();
bool claimable = checker.IsClaimable("bluegarden");
var result = checker.Check("bluegarden");
```

### Choose a validation preset

```csharp
var checker = new Checker(ValidationPresets.Username());
var displayNames = new Checker(ValidationPresets.DisplayName());
```

### Customize sensitivity and languages

```csharp
var options = new Options
{
    ObfuscationSensitivity = ObfuscationSensitivity.High,
    FalsePositiveSensitivity = FalsePositiveSensitivity.Medium
};
options.AddLanguage(Language.Dutch);
options.Reserve("internalbot", matching: ReservedMatchMode.Exact);
var checker = new Checker(options);
```


## Performance at a Glance

Unclaimable is built for efficient, high-frequency validation with optimized matching paths and low garbage-collection overhead.

| Validation metric | v1.0.0 benchmark |
| :--- | ---: |
| Ordinary accepted boolean validation | **0 B allocated** |
| Core ordinary no-match check | **10.01 µs** |
| Core + Extended ordinary no-match check | **9.46 µs** |
| Core + Extended embedded matching | **4.90 µs** |
| Core + Extended exact matching | **0.61 µs** |
| Detailed diagnostic match | **104 B allocated** |

## Validation Defaults

| Setting | Default |
| :--- | :--- |
| Length | 3–32 UTF-16 code units |
| Localized reserved-name data | English |
| Multilingual profanity matching | Enabled across supported languages |
| Obfuscation sensitivity | Medium |
| False-positive sensitivity | Low |
| Structural and pattern checks | Configurable |
| Ordinary accepted boolean validation | 0 B allocated in measured benchmark |

## Detailed Diagnostics

```csharp
var detailed = checker.CheckDetailed("bluegarden");
```

Detailed results provide structured information for validation messages and application policies.

## Compatibility

Targets `netstandard2.0` with broad compatibility across .NET desktop, mobile, web, server, and worker applications.

## Documentation

- [Unclaimable documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable/README.NUGET.md)
- [Unclaimable.Extended documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable.Extended/README.NUGET.md)
- [Unclaimable.AspNetCore documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable.AspNetCore/README.NUGET.md)
- [Unclaimable.Email documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable.Email/README.NUGET.md)
- [Unclaimable.Files documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable.Files/README.NUGET.md)
- [Unclaimable.FluentValidation documentation](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/platforms/dotnet/src/Unclaimable.FluentValidation/README.NUGET.md)
- [Main project README](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/README.md)
- [Configuration guide](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/docs/CONFIGURATION.md)

**Unclaimable v1.0.0 — One validation engine. Modular packages. Flexible policies. Built for .NET.**
