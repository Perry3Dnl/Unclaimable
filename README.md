<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

**Strict, fast username and identifier validation for .NET.**

Prevent reserved, protected, misleading, degenerate, and unsafe identifiers before they can be claimed. Unclaimable combines curated datasets, Unicode-aware matching, structural validation, configurable identifier-pattern checks, and ASP.NET Core integration with no runtime dependencies in the core package.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Changelog**](CHANGELOG.md)

## 0.7.0: identifier-pattern protection

0.7.0 adds a separate **identifier-shape policy layer** on top of the reserved-name and structural-validation pipeline.

The new `Pattern` flags can reject suspicious or degenerate identifiers without turning those checks into reserved words:

- `Pattern.NumericOnly` — identifiers made entirely of Unicode decimal digits;
- `Pattern.Repeated` — long identifiers built by repeating a short unit, such as `aaaaaaaaaaaaaaaa` or `abcabcabcabc`;
- `Pattern.SymbolOnly` — identifiers containing punctuation or symbols but no Unicode letters or numbers;
- `Pattern.AsciiArt` — a conservative set of known ASCII-art constructions;
- `Pattern.UppercaseOnly` — identifiers whose cased letters are all uppercase.

`NumericOnly`, `Repeated`, `SymbolOnly`, and `AsciiArt` are enabled by default. `UppercaseOnly` is deliberately opt-in because uppercase handles are legitimate in many applications.

Pattern checks use the same deny-and-continue model as the rest of Unclaimable: disabling or passing one protection never positively clears an identifier. Structural rules still run, other pattern checks still run, and reserved-name matching still runs. For example, disabling `UppercaseOnly` does **not** make `ADMIN` claimable; it still matches the reserved `admin` identifier.

0.7.0 also adds the global `placeholders` category for literal null-like or missing-value identifiers such as `null`, `undefined`, `empty`, `none`, `nil`, `unset`, `missing`, and related forms. Actual C# `null` input keeps the existing claimable behavior so required-field validation remains a separate concern.

This is an intentionally stricter default-policy release. Applications upgrading from 0.6.0 should review the new default pattern checks and the placeholder category if they intentionally allow these identifier shapes.

## Features

- Reserved-name protection across **23 built-in categories**
- Exact, compact, curated partial, and safe-compound matching
- Common leetspeak, symbol substitutions, and bounded obfuscation detection
- Selected Unicode-confusable and lookalike protection
- **15 localized language datasets** with English enabled by default
- Per-category enable/disable controls and exact allowed-identifier exceptions
- Configurable numeric-only, repeated, symbol-only, ASCII-art, and uppercase-only pattern checks
- Length, number, whitespace, separator, blocked-character, and Unicode safety rules
- Dependency-free `netstandard2.0` core plus ASP.NET Core integration
- Structured first-match and multi-diagnostic results
- Known-safe and reserved conformance corpora in CI

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and DataAnnotations integration |

Install the core package:

```bash
dotnet add package Unclaimable --version 0.7.0
```

For ASP.NET Core:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.7.0
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate-name");
if (result.IsClaimable)
{
    // Continue with your own availability/database check.
}
```

For dependency injection:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted by Unclaimable. Required-field validation is intentionally a separate concern.

`IsReserved` means the value cannot be claimed under the checker. That includes dataset matches, structural failures, and enabled pattern failures.

## Why Unclaimable

A literal blocked-word list misses common impersonation variants and does nothing for many degenerate identifier shapes:

```text
admin
ADMIN
support-team
supp0rt
аpple        // Cyrillic lookalike
root.user
customer-support
39742397429374
abcabcabcabc
!@#$
8===3
```

Unclaimable's default pipeline includes:

- exact reserved-name matching;
- compact separator and punctuation matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only identifier rejection;
- Unicode control-character rejection;
- Unicode format-character rejection;
- profanity filtering;
- minimum and maximum length rules;
- number restrictions;
- whitespace restrictions;
- blocked characters;
- leading and trailing separator rules;
- numeric-only pattern detection;
- repeated-pattern detection;
- symbol-only pattern detection;
- conservative ASCII-art detection;
- application-specific reserved names;
- application-specific blocked characters;
- runtime-adjustable character policy;
- structured diagnostics;
- ASP.NET Core dependency injection and DataAnnotations integration.

Uppercase-only pattern detection is available but opt-in.

## Identifier pattern checks

Pattern checks are configured independently from `Rule` flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);

var checker = new Checker(options);
```

`Options.EnabledPatterns` exposes the captured pattern flags for inspection. `EnablePattern(...)` and `DisablePattern(...)` accept either one pattern or a bitwise combination.

Structural validation runs before fail-fast pattern matching. This matters when protections overlap. With the default `Rule.Numbers` policy, a value such as `123456` is rejected as `MatchKind.NumbersNotAllowed` before `NumericOnly` is reached. If numbers are otherwise allowed, the numeric-only pattern can independently reject an all-digit identifier:

```csharp
var options = new Options
{
    AllowNumbers = true
};

var checker = new Checker(options);
var result = checker.Check("39742397429374");
// result.MatchKind == MatchKind.NumericOnly
```

Pattern checks never act as allow rules. If `NumericOnly` is disabled, a repeated numeric value can still be rejected by `Repeated`. If `UppercaseOnly` is disabled, `ADMIN` can still be rejected by the reserved-name dataset.

`CheckDetailed(...)` can report pattern diagnostics together with other applicable reasons, including a reserved-name match.

## Placeholder identifiers

The global `placeholders` category protects literal names that commonly represent missing or unset data rather than a real identity, including:

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

These values use the normal exact/compact/confusable/obfuscation pipeline. They can be disabled like any other dataset category:

```csharp
options.DisableCategory(Category.Placeholders);
```

The literal string `"null"` is therefore distinct from actual C# `null`: the string is reserved by default, while a null input remains accepted.

## Dataset coverage

0.7.0 contains **10,748 filter entries across 23 categories**, representing **10,650 unique values within those categories**. The increase from 0.6.0 is the 17-entry global `placeholders` category.

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
| `placeholders` | 17 | 17 |
| `profanity` | 861 | 848 |
| `roles` | 813 | 757 |
| `security` | 33 | 33 |
| `support` | 1,010 | 1,001 |
| `system` | 1,349 | 1,331 |
| `technology` | 416 | 416 |
| **Total** | **10,748** | **10,650** |

The totals are dataset entries, not the total number of strings Unclaimable can detect. Normalization, compact matching, explicit partial entries, obfuscation detection, Unicode-confusable matching, structural rules, and pattern checks can reject additional variants without storing every spelling.

## Language support

English localized data is enabled by default. Additional language packs are additive.

Supported localized datasets:

- English
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

Example:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.AddLanguage(Language.German);
    options.AddLanguage(Language.French);
});
```

Remove English explicitly when you want a different localized set:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RemoveLanguage(Language.English);
    options.AddLanguage(Language.Dutch);
});
```

Global categories remain active independently of localized language selection unless that category is explicitly disabled.

## Strict defaults

`new Options()` is deliberately defensive.

| Setting | Default |
| --- | --- |
| Localized language | English |
| Built-in categories | all 23 enabled |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Consistent compact-rule handling | enabled |
| Partial matching capability | enabled through strict mode |
| Built-in partial eligibility | explicit dataset opt-in |
| Obfuscation / leetspeak matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity matching | enabled |
| Minimum length | `3` |
| Maximum length | `32` |
| Numbers | rejected |
| Whitespace | rejected |
| Built-in `-` and `_` | blocked |
| Leading/trailing separators | rejected |
| Invisible-only identifiers | rejected |
| Unicode control characters | rejected |
| Unicode format characters | rejected |
| ASCII-only | disabled |
| Generic profanity substring matching | disabled |
| `Pattern.NumericOnly` | enabled |
| `Pattern.Repeated` | enabled |
| `Pattern.SymbolOnly` | enabled |
| `Pattern.AsciiArt` | enabled |
| `Pattern.UppercaseOnly` | disabled |

Strict does **not** mean every possible substring is blocked. Since 0.6.0, built-in substring eligibility is dataset-authorized rather than inferred from length alone.

## Configure the policy

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(Language.Dutch);

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

### Category selection

Every built-in category remains enabled by default. `DisableCategory(...)` removes that category's entries before the checker's exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

If the same value exists in more than one category, disabling one category does not make the value claimable while another enabled category still reserves it.

### Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception mechanism for complete identifiers. Exact normalization trims leading/trailing whitespace, applies Unicode NFKC normalization, and lowercases using invariant casing.

An allowed identifier bypasses **built-in reserved-name matching only**. Structural and pattern rules still run first. Explicit application reservations also take precedence.

Allowing `superadmin` does not automatically allow `mysuperadminx`, punctuation variants, Unicode lookalikes, obfuscated variants, or an otherwise disallowed identifier shape.

### Application reservations with a match mode

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` means whole-identifier matching after trim → NFKC → invariant lowercase. It does not participate in compact, partial, Unicode-confusable, or obfuscation matching.

`ReservedMatchMode.Default` follows the configured matching pipeline. `AdditionalReserved` keeps its existing behavior and remains eligible for the configured partial-matching rule. The dataset-eligibility change introduced in 0.6.0 applies to **built-in datasets**, not application-defined reservations.

Options are captured when a `Checker` is constructed. Runtime changes through `IPolicy` remain live.

## Reserved-name matching

### Exact matching

Exact matching trims leading/trailing whitespace, applies Unicode NFKC normalization, and lowercases using invariant casing before comparison. Structural and pattern validation still run first in fail-fast checks.

### Compact matching

Compact matching ignores separators and punctuation during the protected-name comparison. This catches punctuation-insertion variants when structural character rules have been relaxed.

### Curated partial matching

`Strictness.Strict` remains the default and enables partial matching. Built-in entries participate only when the dataset explicitly marks them as safe for substring matching.

Schema-v2 `partialValues` and combination entries with `"partial": true` are explicitly approved for substring matching. Ordinary `values` remain exact/compact/confusable/obfuscation-protected but do not automatically become generic substring rules.

```text
Should this identifier itself be reserved?       -> values / partialValues
Is it safe to reject inside another identifier?  -> partialValues / partial: true
```

`PartialMatchMinimumLength` still applies after eligibility. It is not used as a substitute for eligibility.

Generic profanity substring matching remains separately opt-in through `ProfanityPartialMatching`.

### Obfuscation and leetspeak

The bounded substitution pipeline recognizes common patterns such as:

```text
0 -> o
1 -> i / l
2 -> z
3 -> e
4 -> a
5 -> s
7 -> t
8 -> b
@ -> a
$ -> s
! -> i / l
| -> i / l
+ -> t
```

Candidate expansion is bounded to avoid uncontrolled combinatorial growth.

## Unicode protection

### Normalization and malformed input

Malformed surrogate structure is rejected before normalization or character-policy calls. Matching uses Unicode NFKC normalization and invariant case normalization.

The strict default also rejects invisible-only identifiers, control characters, and format characters. Applications that legitimately require format characters such as joiners can opt out explicitly.

### Unicode lookalikes: scope

Unclaimable includes a **selected** confusable mapping for common impersonation characters, especially common Greek and Cyrillic lookalikes, plus normalization/diacritic handling used by the matching pipeline.

This is **not a complete Unicode Technical Standard #39 confusable implementation**. A successful Unclaimable check does not prove that an identifier contains no possible Unicode spoofing technique. Scripts and compatibility characters outside the curated mapping can exist.

That boundary is intentional and documented rather than implied away. Unclaimable does not claim comprehensive Unicode anti-spoofing.

## Canonical usernames are an application concern

Unclaimable normalizes internally for matching; it does not define how your application stores identity.

Your application should separately decide and consistently enforce:

- case sensitivity;
- Unicode normalization for stored/canonical identifiers;
- database collation and uniqueness rules;
- display-name versus login-name behavior;
- canonical URL/routing representation;
- account-to-account impersonation decisions.

Do not use a successful Unclaimable result as a substitute for a canonical uniqueness check in your database.

## What Unclaimable does not guarantee

A value passing validation does not guarantee that it:

- cannot visually impersonate another account;
- contains no Unicode spoofing technique;
- is culturally appropriate in every language;
- is globally unique;
- is safe to use as an authorization identifier;
- cannot collide under your database or routing normalization rules.

Unclaimable is best treated as a **defense-in-depth username policy and reserved-name detection library**, not a complete anti-impersonation or identity system.

## Detailed results

Use `Check` for the first reason:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
```

Use `CheckDetailed` when you need multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);

foreach (var diagnostic in detailed.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

`MatchStartIndex` and `MatchLength` refer to transformed matching text. Original-input spans are populated when mapping back to the original input is reliable; otherwise they remain `null` rather than presenting an inaccurate highlight.

## ASP.NET Core

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.EnablePattern(Pattern.UppercaseOnly);
});
```

Inject `IChecker` or use DataAnnotations:

```csharp
public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

Validation-message precedence is attribute-level, reason-specific configured message, global configured message, then built-in fallback. 0.7.0 adds reason-specific message overrides for numeric-only, repeated-pattern, symbol-only, ASCII-art, and uppercase-only failures.

## Quality gates

The repository checks:

- unit and regression tests;
- shared reserved and known-safe conformance corpora;
- category/rule/toggle/pattern configuration behavior;
- NuGet package creation and metadata/content validation;
- package public API compatibility against published `0.5.0` using .NET package validation;
- clean packaged-consumer restore and execution;
- source builds with localized language packs removed;
- XML documentation for public members;
- benchmark coverage for construction and representative hot paths;
- GitHub Actions pinned to immutable commit SHAs, with Dependabot maintaining those pins.

Dataset and policy changes should be reviewed as consumer-visible behavior changes even when no existing public C# API is removed.

## Release history

### 0.7.0

Added configurable identifier-shape protections and literal placeholder reservations:

- new `Pattern` flags with numeric-only, repeated, symbol-only, ASCII-art, and opt-in uppercase-only checks;
- independent pattern semantics so relaxing one protection does not bypass another;
- a new global `placeholders` category for null-like and missing-value identifiers;
- dedicated `MatchKind` diagnostics and ASP.NET Core validation messages;
- expanded regression coverage for cross-filter behavior and configuration capture.

### 0.6.0

Reduced false positives by making built-in partial matching explicitly dataset-authorized, added a known-safe username regression corpus, documented Unicode-confusable scope and canonicalization responsibilities, and hardened release/compatibility checks.

### 0.5.0

Added category selection, exact allowed identifiers, explicit custom reservation match modes, and configuration regression coverage without changing the 0.4.0 default policy.

See [CHANGELOG.md](CHANGELOG.md) for the full release history.

## License

Unclaimable is licensed under the [Mozilla Public License 2.0](LICENSE).