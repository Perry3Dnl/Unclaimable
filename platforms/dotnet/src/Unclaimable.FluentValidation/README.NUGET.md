# Unclaimable.FluentValidation

FluentValidation rules for usernames and identifiers checked by Unclaimable.

**Package version: 0.8.5 (release candidate; not published)**

## Install

```bash
dotnet add package Unclaimable.FluentValidation --version 0.8.5
```

This package targets .NET 8 or newer and depends on Unclaimable and FluentValidation 12. FluentValidation is restored as a normal NuGet dependency; its license is Apache-2.0. The Unclaimable Core package does not acquire this dependency.

## 0.8.5 release candidate (not yet published)

The optional FluentValidation 12 connector keeps the existing `MustBeClaimable()` and `MustNotBeReserved()` API. v0.8.5 updates the underlying Core checker with broader profanity coverage, faster matching, and an optional `FalsePositiveSensitivity` setting (default `Low`). No new FluentValidation runtime dependency is added to Core.

An injected `IChecker` continues to honor its configured Core options. Some values accepted with the v0.8.4 datasets may now fail validation.


## Basic use

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
        RuleFor(x => x.Username).NotEmpty().MustBeClaimable();
    }
}
```

The rule accepts null; combine it with `NotNull()` or `NotEmpty()` for a required property. `MustNotBeReserved()` is an alias. The validator uses the same checker pipeline as Core, including scoped exception semantics. Only this optional connector brings the FluentValidation 12 dependency.

## Use application settings

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

Register IChecker in your application's service container or supply your own instance. The rule follows that checker's captured options and live character policy. On failure, the default message includes MatchKind; the Category placeholder is also available in a custom message. Avoid exposing matched reserved values in public error messages.
