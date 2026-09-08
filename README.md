<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Unclaimable"><strong>Available on NuGet</strong></a>
</p>

Unclaimable answers one question: **should this identifier be claimable?**

It combines curated reserved-name datasets with structural identifier rules, strict impersonation matching, bounded obfuscation detection, Unicode lookalike handling, localized profanity and trusted-role filtering, application-specific blocked values, and ASP.NET Core integration.

The default policy is intentionally strict. For most applications, configuration is optional:

```csharp
builder.Services.AddUnclaimable();
```

English localized data is enabled by default. Additional language packs are additive: add only the languages your application needs, and remove English if you deliberately do not want it.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core dependency injection and model validation |

Install the core package from NuGet:

```bash
dotnet add package Unclaimable
```

For ASP.NET Core:

```bash
dotnet add package Unclaimable.AspNetCore
```

## What Unclaimable provides

Unclaimable provides a strict baseline for usernames, handles, slugs, account names, and similar claimable identifiers.

The default policy includes:

- reserved and protected names;
- English localized datasets by default;
- additive localized datasets for 15 languages using the Latin alphabet;
- global brand and technology impersonation datasets regardless of enabled languages;
- global security, automation, legal, commerce, community, and other reserved-name categories;
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

`new Options()` starts with the strict baseline:

| Rule | Default |
| --- | --- |
| Enabled localized languages | `English` |
| Strict reserved-name matching | enabled |
| Compact matching | enabled |
| Partial matching | enabled through strict mode |
| Obfuscation / leetspeak matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity dataset | enabled for enabled language(s) |
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
        Rule.Numbers |
        Rule.BlockedCharacters;
});
```

This keeps every other Unclaimable rule active.

## Language support

Localized datasets currently support:

- `Language.English` — enabled by default;
- `Language.Dutch`;
- `Language.German`;
- `Language.French`;
- `Language.Spanish`;
- `Language.Italian`;
- `Language.Portuguese`;
- `Language.Polish`;
- `Language.Turkish`;
- `Language.Indonesian`;
- `Language.Czech`;
- `Language.Vietnamese`;
- `Language.Hungarian`;
- `Language.Swedish`;
- `Language.Romanian`.

The language selection covers the 15 most-used website content languages written in the Latin alphabet in the [W3Techs survey of 8 September 2026](https://w3techs.com/technologies/overview/content_language). Native Latin letters and accents are preserved in the datasets.

The eight additional packs provide an initial set of 24 role names, 24 support terms, 32 system names and a small profanity list each.

Each language pack uses the same localized dataset categories:

- `roles`;
- `support`;
- `system`;
- `profanity`.

Global datasets contain language-independent protected names. Current global categories are:

- `brands`;
- `technology`;
- `security`;
- `automation`;
- `legal`;
- `commerce`;
- `community`;
- `other`.

They are always active, so disabling every localized language does not make protected global values claimable.

### English only — default

```csharp
builder.Services.AddUnclaimable();
```

No language configuration is required because English is enabled by default.

### English + Dutch

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
});
```

Adding a language does not replace English. It extends the enabled localized datasets.

### Dutch only

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RemoveLanguage(Language.English);
    options.AddLanguage(Language.Dutch);
});
```

### Several languages

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.AddLanguage(Language.German);
    options.AddLanguage(Language.French);
    options.AddLanguage(Language.Polish);
    options.AddLanguage(Language.Turkish);
    options.AddLanguage(Language.Vietnamese);
});
```

There is no separate multi-language mode. The enabled language set is additive. `options.Languages` is read-only; use `AddLanguage(...)` and `RemoveLanguage(...)` to configure runtime selection.

The additional language data is merged into the checker's indexes when the checker is constructed. Exact and compact checks remain dictionary lookups, while enabling more languages increases memory use and the amount of work performed by strict partial, Unicode-confusable, and obfuscation matching.

Language configuration controls **which built-in localized datasets are loaded**, not which language the submitted identifier is allowed to contain.

### Removing source language packs

Language folders are deliberately optional source data. They live below `data/languages/<code>/` and are included through a wildcard rather than fixed file references.

If a source checkout or fork does not need Dutch, it can simply delete:

```text
data/languages/nl/
```

The project still compiles. The same applies to all other supported languages, including English. A missing folder contributes no embedded localized entries; it does not create a compile-time dependency or require a code change.

This is separate from `AddLanguage(...)` / `RemoveLanguage(...)`: those methods select from language data that exists in the build. Physically deleting a language folder trims that language data from the build itself. If application code enables a language whose folder was removed, that language simply contributes no built-in entries.

CI explicitly builds the core and ASP.NET Core projects from a temporary checkout with the entire `data/languages/` directory removed, so the removable-pack behavior is continuously verified.

## Rule controls

Rules are represented by `Rule` flags:

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
var options = new Options
{
    DisabledRules =
        Rule.Numbers |
        Rule.Whitespace
};
```

To use conservative reserved-name behavior instead of the strict default:

```csharp
var options = new Options
{
    Strictness = Strictness.Standard
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
    Rule.MinimumLength |
    Rule.MaximumLength;
```

### Numbers

Unicode decimal digits are rejected by default.

```text
ordinary2 -> NumbersNotAllowed
user١     -> NumbersNotAllowed
```

Allow numbers by disabling the numeric rule:

```csharp
options.DisabledRules = Rule.Numbers;
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

ASP.NET Core registration exposes a live `IPolicy` singleton. Applications can tighten or relax the character policy without rebuilding the checker or restarting the validation pipeline.

```csharp
var policy = app.Services.GetRequiredService<IPolicy>();

policy.BlockCharacters("^", "$", "@");
policy.AllowCharacter("-");
```

Existing injected `IChecker` instances immediately observe those changes.

Runtime policy changes are process-local. Applications are free to load their desired policy from their own configuration source during startup or while the application is running.

The core package supports the same pattern directly:

```csharp
var options = new Options();
var policy = new Policy(options.ConfiguredBlockedCharacters);
var checker = new Checker(options, policy);

policy.BlockCharacter("^");

checker.IsClaimable("normal^name"); // false
```

## Reserved-name matching

### Exact matching

Reserved names are normalized for casing and surrounding whitespace during the reserved-name pipeline.

For the default English checker:

```text
customersupport
CUSTOMERSUPPORT
```

both resolve to the same protected value.

### Compact matching

Compact matching is enabled by default. Separators and punctuation can be ignored when resolving a value against the reserved-name dataset.

When structural character restrictions are relaxed, localized or global protected values can still be detected after punctuation is removed.

### Strict partial matching

Strict mode is the default. It catches protected names embedded inside larger values.

For example, with English enabled:

```text
supportive -> support
apples     -> apple
nikee      -> nike
```

Protected terms from every enabled localized language participate in the same matching pipeline.

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

Profanity from every enabled localized language participates in matching by default.

English is enabled by default:

```text
fuckwaffle -> profanity
```

Add other profanity datasets with the same language API:

```csharp
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

Profanity uses the same exact, compact, obfuscation, and Unicode-aware pipeline as the other datasets.

Applications can disable it independently:

```csharp
options.DisabledRules = Rule.Profanity;
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

if (Checker.Default.IsClaimable(userName))
{
    // Identifier passed the strict English default policy plus global datasets.
}
```

English + Dutch checker:

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
var checker = new Checker(options);
```

Dutch-only checker:

```csharp
var options = new Options();
options.RemoveLanguage(Language.English);
options.AddLanguage(Language.Dutch);
var checker = new Checker(options);
```

Public checker contract:

```csharp
public interface IChecker
{
    bool IsReserved(string? value);
    bool IsClaimable(string? value);
    Result Check(string? value);
    DetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
```

### Structured fail-fast results

```csharp
var result = Checker.Default.Check("john-doe");

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
var result = Checker.Default.CheckDetailed(
    "support2",
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
    options.AddLanguage(Language.Dutch);
    options.AddLanguage(Language.German);

    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

Inject the checker anywhere:

```csharp
public sealed class UsernameService(IChecker checker)
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
