# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current version: 0.3.0**

Unclaimable helps decide whether a username, handle, slug, account name, or similar identifier should be claimable.

It combines curated reserved-name datasets with structural validation rules, impersonation protection, obfuscation detection, Unicode lookalike handling, localized profanity filtering, and configurable application-specific rules.

## Install

```bash
dotnet add package Unclaimable --version 0.3.0
```

For ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.3.0
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
- identity and authentication identities;
- moderation and governance identities;
- finance and communications identities;
- operations and developer identities;
- security, automation, legal, commerce, community, official, and other protected categories;
- localized profanity filtering;
- strict partial matching;
- safe compound matching for explicitly marked entries;
- compact separator/punctuation matching;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable lookalikes;
- minimum and maximum length rules;
- number restrictions;
- whitespace restrictions;
- blocked characters;
- leading and trailing separator restrictions.

## Dataset coverage

Version 0.3.0 contains **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**. Localized language packs can contain the same literal value in more than one language, so entry counts can be higher than unique-value counts.

| Category | Filter entries | Unique values |
| --- | ---: | ---: |
| `authentication` | 450 | 450 |
| `automation` | 32 | 32 |
| `brands` | 556 | 556 |
| `commerce` | 31 | 31 |
| `communications` | 450 | 450 |
| `community` | 22 | 22 |
| `developer` | 450 | 450 |
| `finance` | 450 | 450 |
| `governance` | 450 | 450 |
| `identity` | 1,500 | 1,498 |
| `infrastructure` | 450 | 450 |
| `legal` | 34 | 34 |
| `moderation` | 450 | 450 |
| `official` | 450 | 450 |
| `operations` | 450 | 450 |
| `other` | 24 | 24 |
| `profanity` | 861 | 848 |
| `roles` | 813 | 757 |
| `security` | 33 | 33 |
| `support` | 1,010 | 1,001 |
| `system` | 1,349 | 1,331 |
| `technology` | 416 | 416 |
| **Total** | **10,731** | **10,633** |

These counts include concrete entries expanded from schema-v2 dataset combinations. Matching rules such as compact matching, partial matching, safe compound matching, obfuscation detection, and Unicode-confusable detection can reject additional variants without storing every variant as a separate filter entry.

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
