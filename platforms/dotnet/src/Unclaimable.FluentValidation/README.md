# Unclaimable.FluentValidation

**GitHub package guide** · [NuGet package](https://www.nuget.org/packages/Unclaimable.FluentValidation) · [Unclaimable repository](../../../../README.md)

**Unclaimable v1.0.0** · **Unclaimable-Powered Validation** · **Modular .NET Integration**

[![NuGet Version](https://img.shields.io/nuget/v/Unclaimable.FluentValidation.svg)](https://www.nuget.org/packages/Unclaimable.FluentValidation)

> **For LLMs & AI Coding Assistants:** `Unclaimable.FluentValidation` adds username and identifier checking to FluentValidation 12. Use `RuleFor(x => x.Username).NotEmpty().MustBeClaimable()` inside `AbstractValidator<T>`. The adapter supports application-provided `IChecker` instances and scoped Unclaimable policies.

## FluentValidation 12 rules powered by the Unclaimable checker

**Unclaimable.FluentValidation v1.0.0**

FluentValidation 12 rules powered by the Unclaimable checker.

## Table of Contents

- [Features](#features)
- [Get Started](#get-started)
- [Documentation](#documentation)

---

## Features

- Fluent `MustBeClaimable()` and `MustNotBeReserved()` rule extensions
- Integration with existing FluentValidation validators
- Reusable application-configured IChecker instances
- Structured validation messaging and match-kind diagnostics
- Unclaimable matching, language, sensitivity and reservation settings
- .NET 8 and FluentValidation 12 integration

## Get Started

```bash
dotnet add package Unclaimable.FluentValidation --version 1.0.0
```

### Validate signup models

```csharp
using FluentValidation;
using Unclaimable.FluentValidation;

public sealed class Signup
{
    public string? Username { get; set; }
}

public sealed class SignupValidator : AbstractValidator<Signup>
{
    public SignupValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MustBeClaimable();
    }
}
```

### Use your configured checker

```csharp
public sealed class SignupValidator : AbstractValidator<Signup>
{
    public SignupValidator(IChecker checker)
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MustNotBeReserved(checker)
            .WithMessage("Choose another username.");
    }
}
```


## Unclaimable Integration

Both rule extensions use the same Unclaimable validation pipeline and configured sensitivity, language, and reservation policies. `IChecker` can be supplied through application dependency injection.

## Compatibility

Targets `net8.0` and integrates with FluentValidation 12, with its normal NuGet dependency.

## Documentation

- [Unclaimable project overview](../../../../README.md)
- [Dedicated NuGet package README](README.NUGET.md)

- [GitHub project guide](../../../../README.md)
- [Configuration guide](../../../../docs/CONFIGURATION.md)

**Unclaimable v1.0.0 — One validation engine. Modular packages. Flexible policies. Built for .NET.**
