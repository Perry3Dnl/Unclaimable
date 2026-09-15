# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current version: 0.6.0**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, compact matching, curated partial matching, obfuscation detection, selected Unicode lookalikes, localized filtering, category controls, exact exceptions, and application-specific rules.

## Why 0.6.0 exists

0.6.0 is a response to production-oriented feedback rather than a bulk feature or dataset release.

The main issue found in 0.5.0 was false positives from generic built-in substring matching. Short protected terms could reject unrelated words merely because the protected text appeared inside them. Examples included values such as `supportive`, `helpful`, `apples`, `nikee`, `badminton`, `stafford`, and `rooted`.

0.6.0 changes that behavior deliberately:

- `Strictness.Strict` still enables partial matching;
- built-in entries must now be explicitly marked as safe for partial matching before they enter the substring index;
- exact, compact, obfuscation, Unicode-confusable, and structural protections remain independent;
- application-defined `AdditionalReserved` and `Reserve(..., Default)` keep their existing configured matching behavior;
- the built-in reserved vocabulary is not bulk-expanded in this release;
- CI now includes a checked-in known-safe username corpus to prevent this class of regression from returning unnoticed.

Exact reserved identifiers such as `support`, `apple`, `nike`, `admin`, and `root` remain reserved. Curated high-risk compounds such as `superadmin`, `systemadministrator`, `customersupport`, `passwordreset`, and generated authentication-service identities can still participate in partial matching.

This is an intentional default-policy relaxation. Consumers upgrading from 0.5.0 should review it if they relied on generic built-in substring blocking.

## Install

```bash
dotnet add package Unclaimable --version 0.6.0
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.6.0
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

`null` is accepted so required-field validation can remain separate, for example through `[Required]`.

## Default protection

The default policy includes:

- all 22 built-in categories;
- English localized data;
- exact reserved-name matching;
- compact punctuation/separator matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and bounded obfuscation detection;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only, control-character, and format-character protection;
- length `3` through `32`;
- numbers rejected;
- whitespace rejected;
- built-in `-` and `_` blocked;
- leading and trailing separator restrictions.

`IsReserved` therefore means “this value cannot be claimed under this checker,” not only “this literal exists in a reserved-name dataset.”

## Dataset coverage

0.6.0 keeps the same built-in vocabulary cardinality as 0.5.0: **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**. Selected entries changed matching-policy metadata rather than adding another large vocabulary batch.

Matching rules can reject additional variants without storing every possible spelling.

## Language support

English is enabled by default. Additional localized datasets are available for Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);

var checker = new Checker(options);
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

    options.AllowedIdentifiers.Add("superadmin");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

Every built-in category remains enabled unless explicitly disabled. Category selection applies before exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

`AllowedIdentifiers` is an exact normalized exception for built-in matching. Structural validation still runs first, and explicit application reservations still win.

`ReservedMatchMode.Exact` performs whole-identifier matching after trim → NFKC → invariant lowercase. `ReservedMatchMode.Default` uses the configured matching pipeline. `AdditionalReserved` remains compatible with its existing behavior.

## Partial matching in 0.6.0

Built-in dataset entries now separate two decisions:

```text
Reserve this complete identifier?                 yes/no
Reject it whenever it appears inside another one? explicit opt-in
```

Schema-v2 `partialValues` and generated combinations with `"partial": true` are eligible for built-in substring matching. Ordinary `values` are still protected through exact and the other configured matching paths but are not automatically substring rules.

`PartialMatchMinimumLength` still applies after eligibility. It no longer acts as the only protection against false positives.

Generic profanity substring matching remains separately opt-in through `ProfanityPartialMatching`.

## Unicode scope

Matching uses Unicode NFKC normalization and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is **not a complete Unicode Technical Standard #39 confusable implementation**. Passing Unclaimable does not prove that an identifier contains no possible Unicode spoofing technique.

Applications should separately define their canonical username model, including case sensitivity, storage normalization, database uniqueness/collation, display-name behavior, and URL/routing normalization.

Unclaimable is a defense-in-depth policy and reserved-name library, not a complete anti-impersonation or identity system.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
```

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

## Release quality

0.6.0 adds a known-safe regression corpus, retains shared reserved conformance cases, validates NuGet public API compatibility against published 0.5.0, pins GitHub Actions to immutable commit SHAs, and uses Dependabot to maintain those pins.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
