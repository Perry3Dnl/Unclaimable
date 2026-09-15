# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current version: 0.7.0**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, compact matching, curated partial matching, obfuscation detection, selected Unicode lookalikes, localized filtering, category controls, exact exceptions, application-specific rules, and configurable identifier-pattern checks.

## What's new in 0.7.0

0.7.0 adds a dedicated identifier-shape policy layer and literal placeholder protection.

New `Pattern` flags can reject:

- `NumericOnly` — identifiers made entirely of Unicode decimal digits;
- `Repeated` — long identifiers built by repeating a short unit;
- `SymbolOnly` — identifiers containing punctuation or symbols but no Unicode letters or numbers;
- `AsciiArt` — a conservative set of known ASCII-art constructions;
- `UppercaseOnly` — identifiers whose cased letters are all uppercase.

`NumericOnly`, `Repeated`, `SymbolOnly`, and `AsciiArt` are enabled by default. `UppercaseOnly` is opt-in.

These checks are independent deny rules. Disabling or passing one never marks an identifier as allowed; the remaining structural, pattern, and reserved-name checks continue. For example, disabling `UppercaseOnly` does not make `ADMIN` claimable because it still matches the reserved `admin` identifier.

0.7.0 also adds the global `placeholders` category for literal values such as `null`, `undefined`, `empty`, `none`, `nil`, `unset`, `missing`, `unknown`, and related forms. Actual C# `null` input remains accepted so required-field validation can stay separate.

## Install

```bash
dotnet add package Unclaimable --version 0.7.0
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.7.0
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

- all 23 built-in categories;
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
- leading and trailing separator restrictions;
- numeric-only pattern protection;
- repeated-pattern protection;
- symbol-only pattern protection;
- conservative ASCII-art protection.

Uppercase-only protection is available but disabled by default.

`IsReserved` therefore means “this value cannot be claimed under this checker,” not only “this literal exists in a reserved-name dataset.”

## Pattern configuration

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);

var checker = new Checker(options);
```

`Options.EnabledPatterns` exposes the configured flags. `EnablePattern(...)` and `DisablePattern(...)` accept one pattern or a bitwise combination.

Structural validation runs before fail-fast pattern checks. For example, with the default number rule, an all-digit value is rejected as `NumbersNotAllowed` before `NumericOnly` is reached. If numbers are otherwise allowed, numeric-only protection can act independently:

```csharp
var options = new Options
{
    AllowNumbers = true
};

var result = new Checker(options).Check("39742397429374");
// result.MatchKind == MatchKind.NumericOnly
```

## Placeholder identifiers

The global `placeholders` category includes literal identifiers such as:

```text
null
undefined
empty
emptyvalue
none
nil
unset
missing
unknown
notset
novalue
nullvalue
undefinedvalue
missingvalue
placeholder
placeholdervalue
defaultvalue
```

Disable it like any other category when an application intentionally permits those names:

```csharp
options.DisableCategory(Category.Placeholders);
```

The literal string `"null"` is reserved by default; actual C# `null` remains accepted.

## Dataset coverage

0.7.0 contains **10,748 filter entries across 23 categories**, representing **10,650 unique values within those categories**. The increase from 0.6.0 is the 17-entry global `placeholders` category.

Matching rules, normalization, obfuscation detection, Unicode-confusable handling, structural validation, and pattern checks can reject additional variants without storing every possible spelling.

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

    options.EnablePattern(Pattern.UppercaseOnly);
    options.DisablePattern(Pattern.AsciiArt);

    options.AllowedIdentifiers.Add("superadmin");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

Every built-in category remains enabled unless explicitly disabled. Category selection applies before exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

`AllowedIdentifiers` is an exact normalized exception for built-in matching. Structural and pattern validation still run first, and explicit application reservations still win.

`ReservedMatchMode.Exact` performs whole-identifier matching after trim → NFKC → invariant lowercase. `ReservedMatchMode.Default` uses the configured matching pipeline. `AdditionalReserved` remains compatible with its existing behavior.

## Curated partial matching

Since 0.6.0, built-in dataset entries separate two decisions:

```text
Reserve this complete identifier?                 yes/no
Reject it whenever it appears inside another one? explicit opt-in
```

Schema-v2 `partialValues` and generated combinations with `"partial": true` are eligible for built-in substring matching. Ordinary `values` are still protected through exact and the other configured matching paths but are not automatically substring rules.

`PartialMatchMinimumLength` still applies after eligibility. Generic profanity substring matching remains separately opt-in through `ProfanityPartialMatching`.

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

0.7.0 adds dedicated match kinds and diagnostic messages for numeric-only, repeated-pattern, symbol-only, ASCII-art, and uppercase-only failures.

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

Reason-specific validation messages are available for the new pattern failures in addition to the existing reserved-name and structural reasons.

## Release quality

0.7.0 adds regression coverage for every pattern, enable/disable capture, cross-filter behavior, placeholder normalization/category controls, and the invariant that relaxing one protection does not bypass another. The package/build/smoke workflow and public-API compatibility workflow remain part of the release gates.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
