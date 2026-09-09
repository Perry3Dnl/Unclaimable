# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current version: 0.5.0**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, impersonation protection, partial and compact matching, obfuscation detection, Unicode lookalikes, localized filtering, category selection, exact exceptions, and application-specific rules.

## Install

```bash
dotnet add package Unclaimable --version 0.5.0
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.5.0
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

if (checker.IsClaimable(userName))
{
    // Continue with your own availability/database check.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

Then inject `IChecker` where needed.

`null` is accepted by Unclaimable so required-field validation can remain a separate concern, for example through `[Required]`.

## What the default policy protects against

The default policy includes:

- reserved and protected names;
- identity and authentication impersonation;
- trusted-role and support impersonation;
- system and infrastructure identities;
- global brand and technology names;
- moderation and governance identities;
- finance and communications identities;
- operations and developer identities;
- security, automation, legal, commerce, community, official, and other protected categories;
- localized profanity filtering;
- strict partial matching;
- curated safe-compound matching;
- compact separator/punctuation matching;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only identifier rejection;
- Unicode control-character rejection;
- Unicode format-character rejection;
- minimum and maximum length rules;
- number restrictions;
- whitespace restrictions;
- blocked characters;
- leading and trailing separator restrictions.

`IsReserved` also reports structural validation failures. It therefore means “this value cannot be claimed under this checker,” not only “this literal exists in a reserved-name dataset.”

## Dataset coverage

Version 0.5.0 contains **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**. The built-in dataset contents are unchanged from 0.4.0.

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

Matching rules can reject many additional variants without storing every possible spelling as a separate dataset entry.

## Language support

English is enabled by default. Additional localized datasets are available for:

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

## Strict defaults

`new Options()` uses a defensive baseline:

- all 22 built-in categories enabled;
- `Strictness.Strict`;
- compact matching enabled;
- consistent compact-rule handling enabled;
- strict partial matching enabled;
- obfuscation matching enabled;
- Unicode-confusable matching enabled;
- profanity matching enabled;
- length `3` through `32`;
- numbers rejected;
- whitespace rejected;
- built-in `-` and `_` blocked;
- leading/trailing separators rejected;
- invisible-only identifiers rejected;
- Unicode control characters rejected;
- Unicode format characters rejected.

ASCII-only validation remains disabled because Unclaimable is Unicode-aware. Generic profanity substring matching also remains disabled to avoid unnecessary false positives.

Applications that legitimately require Unicode format characters such as joiners can relax that rule explicitly:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RejectFormatCharacters = false;
});
```

## Configuration

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);

    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.DisableCategory(Category.Brands);
    options.DisableCategory(Category.Technology);

    options.AllowedIdentifiers.Add("supportive");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    // Existing API remains supported with its existing matching behavior.
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

Every built-in category remains enabled unless explicitly disabled. Category selection applies to exact, compact, partial, Unicode-confusable, and obfuscation matching. If a value belongs to more than one category, an enabled category can still reserve it.

`AllowedIdentifiers` applies only to the complete normalized identifier and bypasses built-in reserved-name matching only. Exact normalization trims leading/trailing whitespace, applies Unicode NFKC normalization, and then lowercases using invariant casing. Structural validation still runs first, so the default whitespace rule is not bypassed; trimming matters only when whitespace validation has been relaxed. Explicit application reservations take precedence, and compounds or disguised variants are not automatically allowed.

`ReservedMatchMode.Exact` performs only whole-identifier matching after the same trim → NFKC → invariant-lowercase normalization. `ReservedMatchMode.Default` uses the existing configured matching pipeline. `AdditionalReserved` remains available and retains its existing behavior.

Options are captured when a `Checker` is constructed. Runtime changes through `IPolicy` remain live.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
```

`MatchStartIndex` and `MatchLength` refer to the transformed matching text and use UTF-16 code units.

Nullable original-input spans are also available:

```csharp
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
```

They are populated only when the mapping back to the original input is reliable. When normalization makes the mapping uncertain, they remain `null` rather than returning an inaccurate highlight.

Length failures expose the effective threshold through `Result.LengthLimit`.

For multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
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

Fallback minimum/maximum-length messages contain the effective captured threshold, including when `[ClaimableUsername]` runs without dependency injection.

## 0.5.0

0.5.0 adds category selection, exact built-in exceptions, and per-reservation matching modes without changing the strict defaults or built-in dataset contents from 0.4.0. `AdditionalReserved` remains compatible, structural validation still precedes allowed-identifier exceptions, and the release compatibility gate verifies unchanged default outcomes directly against 0.4.0.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
