# Unclaimable.AspNetCore

ASP.NET Core dependency-injection and DataAnnotations integration for Unclaimable.

**Package version: 0.8.0**

## Install

```bash
dotnet add package Unclaimable.AspNetCore --version 0.8.0
```

The required `Unclaimable` core dependency is installed transitively.

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

## Runtime character policy

The registered `IPolicy` remains live after startup:

```csharp
var policy = app.Services.GetRequiredService<IPolicy>();

policy.BlockCharacter("@");
policy.AllowCharacter("@");
```

Startup `Options` are captured when the checker is constructed; runtime character-policy changes remain visible to that checker.
