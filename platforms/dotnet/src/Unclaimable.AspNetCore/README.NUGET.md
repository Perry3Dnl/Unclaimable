# Unclaimable.AspNetCore

ASP.NET Core dependency-injection and DataAnnotations integration for Unclaimable.

**Package version: 0.8.0**

## Install

```bash
dotnet add package Unclaimable.AspNetCore --version 0.8.0
```

The required `Unclaimable` core dependency is installed transitively.

## Framework support

`Unclaimable.AspNetCore` 0.8.0 ships framework-specific assets for `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0`, and `net11.0`. NuGet selects the matching asset for the consuming application automatically.

The compatibility suite compiles and runs the DI and DataAnnotations integration on every advertised target. `net11.0` support is tested against the current .NET 11 prerelease SDK until .NET 11 reaches general availability.

## Register services

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddUnclaimable();
```

This registers the configured `Options`, a live singleton `IPolicy`, and an `IChecker`.

Configure the checker during registration:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^");
    options.DisableCategory(Category.Brands);
});
```

## Inject the checker

```csharp
public sealed class AccountService
{
    private readonly IChecker _checker;

    public AccountService(IChecker checker)
    {
        _checker = checker;
    }

    public bool CanClaim(string userName) => _checker.IsClaimable(userName);
}
```

## DataAnnotations

Use `[ClaimableUsername]` on a string property, field, or parameter:

```csharp
using System.ComponentModel.DataAnnotations;
using Unclaimable.AspNetCore;

public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

`ClaimableUsernameAttribute` accepts `null`, allowing `[Required]` or another required-field mechanism to remain independent.

When dependency injection is available, the attribute uses the registered `IChecker`. Otherwise it falls back to `Checker.Default`.

## Validation messages

A custom attribute message can be supplied normally:

```csharp
[ClaimableUsername(ErrorMessage = "Choose another username.")]
public string UserName { get; set; } = string.Empty;
```

Application-wide and reason-specific validation messages can also be configured through the registered `Options`.

## Startup allowances and scoped exceptions

Keep the 0.8.0 defaults enabled and express application conventions narrowly:

```csharp
builder.Services.AddUnclaimable(options =>
{
    // Team names may use underscores.
    options.AllowCharacters("_");

    // Team prefixes may repeat T directly.
    options.AllowRepeatedCharacters("T");

    // This complete identifier may skip only the city-name rule.
    options.AllowIdentifierForRule(
        "Charlotte",
        Rule.PopularCityNames);

    // This complete identifier may skip only repetition detection.
    options.AllowIdentifierForPattern(
        "ababab",
        Pattern.Repeated);

    // Application-owned names remain explicit denies.
    options.Reserve(
        "internalbot",
        ReservedMatchMode.Exact);
});
```

A scoped exception skips only the selected check. It does not make the identifier globally allowed.

For a convention such as `TTT_user7`, `AllowCharacters("_")` and `AllowRepeatedCharacters("T")` preserve the rest of the checker. `AAA_user7` can still fail repetition, `TTT-user7` can still fail the blocked-character policy, and an application reservation can still deny `TTT_user7` explicitly.

## Runtime character policy

The registered `IPolicy` remains live after startup:

```csharp
var policy = app.Services.GetRequiredService<IPolicy>();

policy.BlockCharacter("@");
policy.AllowCharacter("@");
```

Startup `Options` are captured when the checker is constructed; runtime character-policy changes remain visible to that checker.
