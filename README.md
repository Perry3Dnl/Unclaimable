<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

> **Release status:** preparing the first public NuGet release, `0.1.0`.

Unclaimable answers one question: **should this identifier be claimable?**

It combines curated reserved-name datasets with structural identifier rules, strict impersonation matching, bounded obfuscation detection, Unicode lookalike handling, localized profanity and trusted-role filtering, application-specific blocked values, and ASP.NET Core integration.

The default policy is intentionally strict. For most applications, configuration is optional:

```csharp
builder.Services.AddUnclaimable();
```

The default localized dataset is **Dutch**. English can be selected explicitly, and applications can opt into checking both Dutch and English.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core dependency injection and model validation |

After `0.1.0` is published:

```bash
dotnet add package Unclaimable --version 0.1.0
```

For ASP.NET Core:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.1.0
```

## What Unclaimable provides

Unclaimable provides a strict baseline for usernames, handles, slugs, account names, and similar claimable identifiers.

The default policy includes:

- reserved and protected names;
- localized Dutch datasets by default;
- optional English localized datasets;
- optional Dutch + English multi-language checking;
- global brand and technology impersonation datasets regardless of selected language;
- strict embedded/partial reserved-name matching;
- separator and punctuation normalization for reserved-name matching;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable and lookalike detection;
- localized profanity filtering;
- minimum and maximum length rules;
- numeric-character restrictions;
- whitespace restrictions;
- built-in blocked characters;
- leading and trailing separator rules;
- application-specific reserved values;
- application-specific blocked characters;
- structured fail-fast results;
- detailed multi-diagnostic results;
- ASP.NET Core dependency injection;
- DataAnnotations validation;
- configurable validation messages;
- runtime-adjustable character policy.

## Strict defaults

`new UnclaimableOptions()` starts with the strict baseline:

| Rule | Default |
| --- | --- |
| Selected localized language | `Dutch` |
| Multi-language checking | disabled |
| Strict reserved-name matching | enabled |
| Compact matching | enabled |
| Partial matching | enabled through strict mode |
| Obfuscation / leetspeak matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity dataset | enabled for selected language(s) |
| Minimum length | `3` |
| Maximum length | `32` |
| Numbers | rejected |
| Whitespace | rejected |
| `-` | blocked |
| `_` | blocked |
| Leading separators | rejected |
| Trailing separators | rejected |

The baseline is opt-out. Applications that need a more permissive identifier policy can disable individual rules.

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.DisabledRules =
        UnclaimableRule.Numbers |
        UnclaimableRule.BlockedCharacters;
});
```

This keeps every other Unclaimable rule active.

## Language support

Localized datasets currently support:

- `UnclaimableLanguage.Dutch` — default;
- `UnclaimableLanguage.English`.

Global datasets such as `brands` and `technology` are always active. Selecting Dutch therefore does not make names such as `paypal`, `github`, or `nike` claimable.

### Dutch only — default

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Language = UnclaimableLanguage.Dutch;
});
```

The explicit assignment is optional because Dutch is the default.

### English only

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Language = UnclaimableLanguage.English;
});
```

### Dutch and English

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Language = UnclaimableLanguage.Dutch;
    options.AllowMultiLanguage = true;
});
```

When `AllowMultiLanguage` is enabled, Unclaimable loads every supported localized dataset rather than only `Language`.

The additional language data is merged into the checker's indexes when the checker is constructed. Exact and compact checks remain dictionary lookups, but multi-language mode uses more memory and increases the amount of work performed by strict partial, Unicode-confusable, and obfuscation matching. For applications that only need one language, leaving multi-language mode disabled is the leaner option.

Language selection controls **which built-in datasets are loaded**, not which language the submitted identifier is allowed to contain. For example, an English-only checker can still reject a mixed-language value if it literally contains an enabled English protected token.

The dataset format is designed for more languages later. Localized files carry a language code, while language-independent datasets are tagged `global`.

## Rule controls

Rules are represented by `UnclaimableRule` flags:

```text
MinimumLength
MaximumLength
Whitespace
BlockedCharacters
LeadingSeparator
TrailingSeparator
Numbers
CompactMatching
PartialMatching
Profanity
ObfuscationMatching
UnicodeConfusableMatching
```

Disable only what your application deliberately wants to relax:

```csharp
var options = new UnclaimableOptions
{
    DisabledRules =
        UnclaimableRule.Numbers |
        UnclaimableRule.Whitespace
};
```

To use conservative reserved-name behavior instead of the strict default:

```csharp
var options = new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Standard
};
```

## Structural identifier rules

### Length

The default accepted length is `3` through `32` characters.

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;
});
```

Length checks are fail-fast and run before the more expensive reserved-name pipeline.

They can also be disabled independently:

```csharp
options.DisabledRules =
    UnclaimableRule.MinimumLength |
    UnclaimableRule.MaximumLength;
```

### Numbers

Unicode decimal digits are rejected by default.

```text
ordinary2 -> NumbersNotAllowed
user١     -> NumbersNotAllowed
```

Allow numbers by disabling the numeric rule:

```csharp
options.DisabledRules = UnclaimableRule.Numbers;
```

### Whitespace and blocked characters

Whitespace, `-`, and `_` are blocked by the default structural policy.

```text
john doe -> BlockedCharacter
john-doe -> BlockedCharacter
john_doe -> BlockedCharacter
```

Applications can add their own blocked characters at startup:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AdditionalBlockedCharacters("^", " ", "$");
});
```

The character API accepts one Unicode scalar value per entry.

### Leading and trailing separators

Leading and trailing separator checks are enabled by default.

```text
.john -> LeadingSeparator
john. -> TrailingSeparator
```

These rules remain independent from the general blocked-character policy. For example, an application may allow `-` inside a name while still rejecting `-john` and `john-`.

## Runtime character policy

ASP.NET Core registration exposes a live `IUnclaimablePolicy` singleton. Applications can tighten or relax the character policy without rebuilding the checker or restarting the validation pipeline.

```csharp
var policy = app.Services.GetRequiredService<IUnclaimablePolicy>();

policy.BlockCharacters("^", "$", "@");
policy.AllowCharacter("-");
```

Existing injected `IUnclaimableChecker` instances immediately observe those changes.

Runtime policy changes are process-local. Applications are free to load their desired policy from their own configuration source during startup or while the application is running.

The core package supports the same pattern directly:

```csharp
var options = new UnclaimableOptions();
var policy = new UnclaimablePolicy(options.ConfiguredBlockedCharacters);
var checker = new UnclaimableChecker(options, policy);

policy.BlockCharacter("^");

checker.IsClaimable("normal^name"); // false
```

## Reserved-name matching

### Exact matching

Reserved names are normalized for casing and surrounding whitespace during the reserved-name pipeline.

For a Dutch checker:

```text
systeembeheerder
SYSTEEMBEHEERDER
```

both resolve to the same protected value.

### Compact matching

Compact matching is enabled by default. Separators and punctuation can be ignored when resolving a value against the reserved-name dataset.

When structural character restrictions are relaxed, localized or global protected values can still be detected after punctuation is removed.

### Strict partial matching

Strict mode is the default. It catches protected names embedded inside larger values.

For example, with English selected:

```text
supportive -> support
apples     -> apple
nikee      -> nike
```

With Dutch selected, localized protected terms participate in the same matching pipeline.

`PartialMatchMinimumLength` defaults to `4`, which keeps very short reserved values from participating in ordinary substring matching.

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.PartialMatchMinimumLength = 5;
});
```

### Obfuscation and leetspeak

Common substitutions are bounded and normalized during matching, including mappings such as:

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

If the numeric structural rule is relaxed, examples include:

```text
N1k3   -> nike
G00gle -> google
```

Candidate expansion is bounded so ambiguous substitutions cannot grow without limit.

### Unicode-confusable matching

Unicode-confusable matching is enabled by default. Unclaimable includes a bounded mapping for common visual impersonation characters, including selected Cyrillic and Greek lookalikes plus diacritic normalization.

For example:

```text
аpple
^ Cyrillic U+0430
```

resolves to protected `apple` because technology and brand-style impersonation datasets are global.

## Profanity matching

Profanity from the selected localized dataset participates in matching by default.

Dutch is used by default:

```text
godverdomme -> profanity
```

Select English when the application primarily serves English-speaking users:

```csharp
options.Language = UnclaimableLanguage.English;
```

Or enable both localized profanity datasets:

```csharp
options.AllowMultiLanguage = true;
```

Profanity uses the same exact, compact, obfuscation, and Unicode-aware pipeline as the other datasets.

Applications can disable it independently:

```csharp
options.DisabledRules = UnclaimableRule.Profanity;
```

Substring matching for profanity is deliberately configurable separately because it is substantially more aggressive:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ProfanityPartialMatching = true;
});
```

## Application-specific reserved names

Use `AdditionalReserved` for private product names, organization identities, internal bots, tenant names, or other protected values specific to your application.

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalReserved.Add("internalbot");
});
```

Application-specific values are language-independent and participate in the same normalization and strict matching pipeline as the built-in datasets.

## Core .NET API

Fast yes/no checks:

```csharp
using Unclaimable;

if (UnclaimableChecker.Default.IsClaimable(userName))
{
    // Identifier passed the strict Dutch default policy plus global datasets.
}
```

English checker:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    Language = UnclaimableLanguage.English
});
```

Public checker contract:

```csharp
public interface IUnclaimableChecker
{
    bool IsReserved(string? value);
    bool IsClaimable(string? value);
    UnclaimableResult Check(string? value);
    UnclaimableDetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
```

### Structured fail-fast results

```csharp
var result = UnclaimableChecker.Default.Check("john-doe");

Console.WriteLine(result.IsReserved);             // true
Console.WriteLine(result.MatchKind);              // BlockedCharacter
Console.WriteLine(result.OffendingCharacter);     // -
Console.WriteLine(result.OffendingCharacterIndex);// 4
```

Current match kinds include:

```text
None
Exact
Compact
Obfuscated
UnicodeConfusable
InvalidCharacters
Partial
NumbersNotAllowed
TooShort
TooLong
BlockedCharacter
LeadingSeparator
TrailingSeparator
```

### Detailed diagnostics

`Check(...)` returns the first failure as quickly as possible.

Use `CheckDetailed(...)` when UI, logging, or diagnostics benefit from seeing multiple reasons:

```csharp
var result = UnclaimableChecker.Default.CheckDetailed(
    "systeembeheerder2",
    includeMessages: true);

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

A detailed check can report both a structural-policy violation and a protected-name match.

## ASP.NET Core

Register once in `Program.cs`:

```csharp
builder.Services.AddUnclaimable();
```

Or customize the policy:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Language = UnclaimableLanguage.English;
    options.AllowMultiLanguage = false;

    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = UnclaimableRule.Numbers;
});
```

Inject the checker anywhere:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use DataAnnotations in Razor Pages / MVC:

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

`[Required]` remains useful for required-field semantics; Unclaimable focuses on whether a supplied identifier is claimable.

## Validation messages

Configure one application-wide fallback:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ValidationMessage = "{FieldName} is not available.";
});
```

Or configure messages by rejection reason:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Messages.Reserved =
        "{FieldName} '{MatchedValue}' is reserved.";

    options.Messages.Partial =
        "{FieldName} contains protected value '{MatchedValue}'.";

    options.Messages.NumbersNotAllowed =
        "Numbers are not allowed in {FieldName}; '{Character}' was found at index {Index}.";

    options.Messages.BlockedCharacter =
        "{FieldName} contains blocked character '{Character}'.";

    options.Messages.TooShort =
        "{FieldName} has {Length} characters; at least {MinimumLength} are required.";

    options.Messages.TooLong =
        "{FieldName} has {Length} characters; at most {MaximumLength} are allowed.";
});
```

Supported placeholders include:

| Placeholder | Value |
| --- | --- |
| `{FieldName}` | DataAnnotations display name |
| `{MatchedValue}` | matched protected value |
| `{Category}` | matched dataset category |
| `{Character}` | offending character |
| `{Index}` | zero-based offending-character index |
| `{Length}` | supplied value length |
| `{MinimumLength}` | configured minimum length |
| `{MaximumLength}` | configured maximum length |

Message precedence is:

1. `[ClaimableUsername(ErrorMessage = "...")]`;
2. reason-specific `options.Messages.*` message;
3. `options.ValidationMessage` fallback;
4. Unclaimable's built-in message.

## Datasets

Reserved values are stored as human-reviewable JSON below `data/` and embedded into the core package.

Current scopes:

| Scope | Categories | Behavior |
| --- | --- | --- |
| `nl` | profanity, roles, support, system | loaded when Dutch is selected |
| `en` | profanity, roles, support, system | loaded when English is selected |
| `global` | brands, technology | always loaded |

The repository also contains the original top-level datasets. For compatibility, legacy top-level `brands` and `technology` data is treated as global, while the other legacy top-level datasets are treated as English.

Localized dataset documents can declare:

```json
{
  "schema": 1,
  "category": "support",
  "language": "nl",
  "description": "...",
  "values": ["..."]
}
```

Language-independent data uses:

```json
"language": "global"
```

This keeps the format extensible: a future language can receive its own localized datasets without changing the matching model.

## Matching pipeline

`Check(...)` is fail-fast. The effective order is designed to reject inexpensive policy violations before performing more expensive normalization work:

1. minimum / maximum length;
2. leading / trailing separator checks;
3. numeric and character policy checks;
4. exact reserved-name matching;
5. compact matching;
6. strict partial matching;
7. Unicode-confusable matching;
8. bounded obfuscation / leetspeak matching.

Built-in protected values for the selected language scope are indexed when the checker is constructed. Normal exact and compact lookups use hashed dictionaries rather than repeatedly scanning the full dataset.

## Repository layout

```text
.github/workflows/
assets/
conformance/
data/
  en/
  nl/
  global/
platforms/dotnet/src/Unclaimable/
platforms/dotnet/src/Unclaimable.AspNetCore/
platforms/dotnet/tests/Unclaimable.Tests/
platforms/dotnet/smoke/Unclaimable.ConsumerSmoke/
```

## License

MPL-2.0
