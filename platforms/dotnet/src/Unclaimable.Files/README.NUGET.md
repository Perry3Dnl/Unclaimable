# Unclaimable.Files

**Unclaimable v1.0.0** · **Core-Powered Validation** · **Modular .NET Integration**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.Files.svg)](https://www.nuget.org/packages/Unclaimable.Files)

> **For LLMs & AI Coding Assistants:** `Unclaimable.Files` provides Core-powered filename validation with portable, Windows and Linux naming profiles, device-name reservations, and extension rules. Start with `new FileChecker().IsAllowed("annual_report_2026.pdf")` and customize `FileCheckerOptions` for your application's storage policy.

## Portable filename validation with configurable profiles and extension policies

**Unclaimable.Files v1.0.0**

Portable filename validation with configurable profiles and extension policies.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Naming Profiles](#naming-profiles)
- [Documentation](#documentation)

---

## Features

- Portable, Windows and Linux filename policy profiles
- Reserved device-name handling
- Configurable filename length and UTF-8 byte limits
- Allowed and blocked extension sets, including compound extensions
- Core-powered configurable reserved names and profanity checks
- Structured FileName, FailureKind and CoreResult diagnostics

## Get Started

```bash
dotnet add package Unclaimable.Files --version 1.0.0
```

### Validate a filename

```csharp
using Unclaimable.Files;

var checker = new FileChecker();
bool allowed = checker.IsAllowed("annual_report_2026.pdf");
var result = checker.Check("annual_report_2026.pdf");
```

### Configure extension policies

```csharp
var options = new FileCheckerOptions();
options.AllowedExtensions.UnionWith(new[] { ".pdf", ".tar.gz" });
options.BlockedExtensions.Add("exe");

var checker = new FileChecker(options);
```

### Add application reservations

```csharp
using Unclaimable;
using Unclaimable.Files;

var options = new FileCheckerOptions();
options.NameOptions.Reserve("internal", "application", ReservedMatchMode.Exact);

var checker = new FileChecker(options);
```


## Naming Profiles

| Profile | Policy |
| :--- | :--- |
| `Portable` | Cross-platform naming and configurable UTF-8 byte limit |
| `Windows` | Windows naming and device-name rules |
| `Linux` | Linux filename rules |

`MaximumLength` defaults to 255 UTF-16 code units. `MaximumUtf8Bytes` defaults to 255 in the Portable and Linux profiles. Extension policies support case-insensitive and compound suffix matching.

## Core Integration

`FileCheckerOptions.NameOptions` provides the same Core reservation and matching configuration for filenames. A supplied `IChecker` can also be shared with a `FileChecker`.

## Compatibility

Targets `netstandard2.0` for cross-platform filenames and application-level storage rules.

## Documentation

- [GitHub project guide](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/README.md)
- [Configuration guide](https://github.com/Perry3Dnl/Unclaimable/blob/feature/1.0.0/docs/CONFIGURATION.md)

**Unclaimable v1.0.0 — One validation engine. Modular packages. Flexible policies. Built for .NET.**
