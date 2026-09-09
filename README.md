<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Unclaimable"><strong>NuGet</strong></a>
  ·
  <a href="CHANGELOG.md"><strong>Changelog</strong></a>
</p>

Unclaimable answers one question: **should this identifier be claimable?**

It provides a strong default policy for usernames, handles, slugs, account names, tenant names, public identifiers, and similar user-claimable values. It combines large curated reserved-name datasets with structural validation, impersonation protection, partial matching, compact matching, bounded obfuscation detection, Unicode lookalike handling, localized filtering, category selection, exact exceptions, and application-specific rules.

For most applications, the strict default is enough:

```csharp
builder.Services.AddUnclaimable();
```

## Why Unclaimable

A simple blocked-word list catches only exact strings. Real identifiers are more complicated:

```text
admin
ADMIN
support-team
supp0rt
аpple        // Cyrillic lookalike
root.user
customer-support
```

Unclaimable checks more than literal equality. Its default pipeline includes:

- exact reserved-name matching;
- compact separator and punctuation matching;
- strict embedded / partial matching;
- curated safe-compound matching;
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
- application-specific reserved names;
- application-specific blocked characters;
- runtime-adjustable character policy;
- structured fail-fast results;
- detailed multi-diagnostic results;
- ASP.NET Core dependency injection and DataAnnotations integration.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and DataAnnotations integration |

Install the core package:

```bash
dotnet add package Unclaimable --version 0.5.0
```

For ASP.NET Core:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.5.0
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

`null` is accepted by Unclaimable. Required-field validation is intentionally a separate concern.

`IsReserved` means the value cannot be claimed under the checker. That includes both dataset matches and structural failures such as invalid length, blocked characters, malformed Unicode, or disallowed characters.

## Dataset coverage

Version 0.5.0 contains **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**.

0.5.0 intentionally keeps the same curated dataset contents and strict defaults as 0.4.0. This release adds configuration controls around the existing data rather than changing which identifiers are rejected by default.

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

The totals are dataset entries, not the total number of strings Unclaimable can detect. Compact matching, partial matching, safe compounds, obfuscation detection, and Unicode-confusable matching can reject many additional variants without storing every possible spelling.

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

Adding a language does not replace English. Remove English explicitly when you want a different localized set:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RemoveLanguage(Language.English);
    options.AddLanguage(Language.Dutch);
});
```

Global categories such as brands, technology, infrastructure, security, and authentication remain active independently of localized language selection unless that category is explicitly disabled.

## Strict defaults

`new Options()` is deliberately defensive.

| Setting | Default |
| --- | --- |
| Localized language | English |
| Built-in categories | all 22 enabled |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Consistent compact-rule handling | enabled |
| Partial matching | enabled through strict mode |
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

Strict does **not** mean every boolean is enabled. `AsciiOnly`, for example, stays off because Unclaimable is Unicode-aware and should not reject ordinary international names merely for containing non-ASCII letters. Generic profanity substring matching also remains off because it can create unnecessary false positives.

## Configure the policy

Relax or extend only the rules your application needs:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(Language.Dutch);

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

### Category selection

Every built-in category remains enabled by default. `DisableCategory(...)` removes only that category's entries from the checker's reserved-name indexes, so the setting applies consistently to exact, compact, partial, Unicode-confusable, and obfuscation matching.

If the same value exists in more than one category, disabling one category does not make the value claimable while another enabled category still reserves it. Use `EnableCategory(...)` to re-enable a category on the same options object before constructing a checker.

### Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception mechanism for complete identifiers. Values use the same case and Unicode normalization as exact reserved-name matching.

An allowed identifier bypasses **built-in reserved-name matching only**. Structural rules such as length, blocked characters, numbers, malformed Unicode, and other character restrictions still run first. Explicit application reservations also take precedence.

Allowing `supportive` does not automatically allow `supportiveadmin`, punctuation variants, Unicode lookalikes, or disguised/obfuscated variants.

### Application reservations with a match mode

Use `Reserve(...)` when an application reservation needs explicit matching semantics:

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` means whole-identifier matching after exact case/Unicode normalization only. It does not participate in compact, partial, Unicode-confusable, or obfuscation matching.

`ReservedMatchMode.Default` follows the checker's existing configured matching pipeline. `AdditionalReserved` is preserved unchanged and continues to use that existing default pipeline.

Options are captured when a `Checker` is constructed. Mutating the same `Options` object afterwards does not silently change that checker's matching behavior.

Runtime changes through `IPolicy` are different: the character policy remains live.

```csharp
var policy = app.Services.GetRequiredService<IPolicy>();

policy.BlockCharacters("^", "$", "@");
policy.AllowCharacter("-");
```

Existing checker instances observe those runtime policy updates.

## Unicode protection

### Malformed UTF-16

Malformed surrogate structure is rejected before normalization or character-policy calls.

```text
unpaired high surrogate -> InvalidCharacters
unpaired low surrogate  -> InvalidCharacters
valid surrogate pair    -> normal Unicode processing
```

The reported offending index is the original UTF-16 code-unit index.

### Invisible-only identifiers

The strict default requires at least one scalar outside whitespace, control, format, and combining-mark categories.

This is intentionally described as an approximation: Unicode categories do not determine how every font or renderer will display a sequence.

### Control and format characters

Control and formatting characters are rejected by default because they can create confusing or misleading identifiers.

Some languages legitimately use format characters such as joiners. Applications that need them can opt out explicitly:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RejectFormatCharacters = false;
});
```

The checks are separate so applications can relax format handling without also allowing control characters or invisible-only identifiers.

## Reserved-name matching

### Exact matching

Case and normalization differences resolve to the same protected value where applicable.

### Compact matching

Compact matching ignores separators and punctuation during the protected-name comparison. This helps catch variants such as punctuation inserted into a protected identifier.

`ConsistentCompactMatching` has been enabled by default since 0.4.0 so `Rule.CompactMatching` has the same meaning across direct compact, partial, and obfuscation paths.

### Strict partial matching

`Strictness.Strict` is the default. It allows protected values to be detected inside larger identifiers where the dataset/rules permit it.

`PartialMatchMinimumLength` defaults to `4` to reduce false positives from very short terms.

Schema-v2 datasets can also mark individual entries as safe compounds so they can participate in partial matching without turning every short term into a generic substring rule.

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

Candidate expansion is bounded to prevent ambiguous substitutions from growing without limit.

### Unicode lookalikes

Unclaimable includes a selected mapping for common visual impersonation characters, including Cyrillic and Greek lookalikes and diacritic-based variants.

For example, a value visually resembling a protected brand through a Cyrillic character can still be recognized as an impersonation attempt.

## Profanity filtering

Profanity from each enabled localized language participates in the normal matching pipeline.

Generic profanity substring matching remains separately configurable because it is significantly more aggressive:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ProfanityPartialMatching = true;
});
```

Curated safe-compound entries can still participate in partial matching without enabling generic profanity substring rules. This avoids obvious false positives around short ambiguous terms.

## Detailed results

Use `Check` when you want the first rejection reason:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
```

Existing `MatchStartIndex` and `MatchLength` refer to the transformed matching text. Indexes and lengths are measured in UTF-16 code units.

0.4.0 introduced nullable original-input spans:

```csharp
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
```

They are populated only when the mapping back to the original input is reliable. Compact-match original spans include intervening punctuation. When normalization makes a precise mapping uncertain, the original span is `null` rather than an inaccurate highlight.

Length failures also expose the effective captured threshold through `Result.LengthLimit`.

Use `CheckDetailed` when you want multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);

foreach (var diagnostic in detailed.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

## ASP.NET Core

Register once:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
});
```

Inject `IChecker`:

```csharp
public sealed class UsernameService(IChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use DataAnnotations:

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

Validation-message precedence is:

1. attribute-level message;
2. reason-specific configured message;
3. global configured message;
4. built-in fallback.

Minimum/maximum placeholders use the threshold captured by the checker result first, so later mutation of a registered `Options` object cannot produce a misleading length message.

## Public API and quality gates

The core targets `netstandard2.0` and has no runtime package dependency. The ASP.NET Core integration targets `net8.0`.

The repository continuously checks:

- unit and regression tests;
- package creation;
- NuGet package metadata/content;
- clean packaged-consumer restore and execution;
- source builds with localized language packs removed;
- public API compatibility against `v0.4.0`;
- unchanged default behavior against `v0.4.0` across a deterministic compatibility corpus;
- legacy-compatible behavior against `v0.3.0` when the 0.4.0 strict additions are explicitly disabled;
- XML documentation for every public member;
- benchmark coverage for construction and representative hot paths.

## What is new in 0.5.0

0.5.0 is a configurability release with unchanged defaults and unchanged built-in dataset contents:

- any of the 22 built-in categories can be disabled independently;
- category selection is applied before all reserved-name matching indexes are built;
- exact `AllowedIdentifiers` exceptions can resolve individual built-in false positives without relaxing structural validation;
- explicit application reservations still win over an allowed-identifier exception;
- `Reserve(...)` supports `ReservedMatchMode.Exact` for whole-identifier-only application reservations and `Default` for the existing matching pipeline;
- `AdditionalReserved` keeps its existing public API and behavior;
- release regression checks compare both public API compatibility and default outcomes directly against 0.4.0.

See [CHANGELOG.md](CHANGELOG.md) for the release history.

## License

See [LICENSE](LICENSE).
