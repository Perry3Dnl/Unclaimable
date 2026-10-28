# Unclaimable.AspNetCore

**Unclaimable v1.0.0** · **Core-Powered Validation** · **Modular .NET Integration**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.AspNetCore.svg)](https://www.nuget.org/packages/Unclaimable.AspNetCore)

> **For LLMs & AI Coding Assistants:** `Unclaimable.AspNetCore` integrates the Unclaimable Core checker into ASP.NET Core applications. Register with `builder.Services.AddUnclaimable()` and validate models using `[ClaimableUsername]`. Application policy can be configured through the registration callback.

## Dependency injection and DataAnnotations integration for Unclaimable

**Unclaimable.AspNetCore v1.0.0**

Dependency injection and DataAnnotations integration for Unclaimable.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Documentation](#documentation)

---

## Features

- One-line dependency-injection registration
- IChecker injection throughout application services
- ClaimableUsername DataAnnotations attribute
- Configurable sensitivity and reserved-name policy at registration
- Scoped allowances, application reservations, and runtime character-policy integration
- Framework-specific support across .NET 6 through .NET 11

## Get Started

```bash
dotnet add package Unclaimable.AspNetCore --version 1.0.0
```

### Register the checker

```csharp
using Unclaimable;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddUnclaimable();
```

### Apply custom policy

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.FalsePositiveSensitivity = FalsePositiveSensitivity.Medium;
    options.AllowCharacters("_");
});
```

### Validate a model

```csharp
using System.ComponentModel.DataAnnotations;
using Unclaimable.AspNetCore;

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string Username { get; set; } = string.Empty;
}
```

### Inject into a service

```csharp
public sealed class AccountService
{
    private readonly IChecker _checker;

    public AccountService(IChecker checker) => _checker = checker;

    public bool CanClaim(string username) => _checker.IsClaimable(username);
}
```


## Framework Support

Targets `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0`, and `net11.0` with the matching NuGet asset selected by the target application.

## Core Policy Integration

The adapter uses the same Core `Options` object, matching pipeline, sensitivity settings, and scoped exceptions. Runtime character policies are accessible through the registered `IPolicy`.

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
