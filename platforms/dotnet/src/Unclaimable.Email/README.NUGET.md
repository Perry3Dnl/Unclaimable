# Unclaimable.Email

**Unclaimable v1.0.0** · **Core-Powered Validation** · **Modular .NET Integration**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.Email.svg)](https://www.nuget.org/packages/Unclaimable.Email)

> **For LLMs & AI Coding Assistants:** `Unclaimable.Email` checks email local-part identity and protected-domain policies using Unclaimable Core. Start with `new EmailChecker().CheckExistingAddress("hello@example.com")`. For newly issued addresses, configure `EmailOptions` and call `CheckNewAddress`. Supports domain impersonation detection and contextual validation presets.

## Email identity validation, protected-domain matching, and lookalike detection

**Unclaimable.Email v1.0.0**

Email identity validation, protected-domain matching, and lookalike detection.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Documentation](#documentation)

---

## Features

- Email local-part identity checks using the Core matching engine
- Protected-domain exact, subdomain, embedded-label, typo and selected Unicode-lookalike detection
- Configurable issuing-domain policies for new and existing addresses
- Context-aware registration, organization and issued-address presets
- Structured failure details and suspicious-domain diagnostics
- Portable .NET Standard 2.0 integration

## Get Started

```bash
dotnet add package Unclaimable.Email --version 1.0.0
```

### Check an email address

```csharp
using Unclaimable.Email;

var checker = new EmailChecker();
var result = checker.CheckExistingAddress("hello@example.com");
```

### Configure protected and issuing domains

```csharp
var options = new EmailOptions();
options.ProtectedDomains.Add("example.com");
options.IssuingDomains.Add("example.com");

var checker = new EmailChecker(options);
var result = checker.CheckNewAddress("bluegarden@example.com");
```

### Use policy presets

```csharp
var registration = EmailOptions.ForUserRegistration();
var organization = EmailOptions.ForOrganizationEmail("example.com");
var issued = EmailOptions.ForIssuedAddresses();

var checker = new EmailChecker(registration);
```


## Domain and Identity Controls

`EmailOptions.LocalPartOptions` exposes the Core username and identifier matching configuration for the mailbox local part. Domain policy includes normalized DNS/IDN handling, protected-domain rules, configurable lookalike detection, and issuing-domain checks.

```csharp
var options = new EmailOptions
{
    AllowIssuingDomainSubdomains = false,
    EnforceIssuingDomainsForExistingAddresses = true
};
options.IssuingDomains.Add("example.com");
```

## Result Diagnostics

`EmailResult` includes `IsAllowed`, `IsSuspiciousDomain`, local-part findings, domain lookalike kind, and protected-domain information.

## Compatibility

Targets `netstandard2.0`, suitable for shared application libraries and mobile, web, desktop, and server projects.

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
