# Unclaimable

Strict, fast username and identifier validation for .NET.

Unclaimable helps decide whether a username, handle, slug, account name, or similar identifier should be claimable.

It combines curated reserved-name datasets with structural validation rules, impersonation protection, obfuscation detection, Unicode lookalike handling, localized profanity filtering, and configurable application-specific rules.

## Install

```bash
dotnet add package Unclaimable
```

For ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

if (checker.IsClaimable(userName))
{
    // Identifier passed the default policy.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

Then inject `IChecker` where needed:

```csharp
public sealed class UsernameService(IChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

## What it protects against

The default policy includes:

- reserved and protected names;
- trusted-role and support impersonation;
- system and infrastructure identities;
- global brand and technology names;
- security, automation, legal, commerce, community, and other protected categories;
- localized profanity filtering;
- strict partial matching;
- compact separator/punctuation matching;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable lookalikes;
- minimum and maximum length rules;
- number restrictions;
- whitespace restrictions;
- blocked characters;
- leading and trailing separator restrictions.

## Language support

English is enabled by default.

Additional localized datasets are available for:

- Dutch
- German
- French
- Spanish
- Italian
- Portuguese
- Polish
- Turkish
- Indonesian
- Czech
- Vietnamese
- Hungarian
- Swedish
- Romanian

Languages are additive:

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);

var checker = new Checker(options);
```

## Configuration

Example:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);

    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

Application-specific reserved values participate in the same normalization and matching pipeline as the built-in datasets.

## Detailed results

```csharp
var result = checker.Check("support-team");

Console.WriteLine(result.IsReserved);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
```

For diagnostics that may contain multiple failures:

```csharp
var result = checker.CheckDetailed(userName, includeMessages: true);
```

## ASP.NET Core DataAnnotations

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

## More information

Full documentation, dataset details, configuration guidance, source code, and development information are available on GitHub:

https://github.com/Perry3Dnl/Unclaimable
