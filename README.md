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

English localized data is enabled by default. Additional language packs are additive: add only the languages your application needs, and remove English if you deliberately do not want it.

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
- English localized datasets by default;
- additive Dutch, German, French, Spanish, Italian, and Portuguese localized datasets;
- global brand and technology impersonation datasets regardless of enabled languages;
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
        UnclaimableRule.Numbers |
        UnclaimableRule.BlockedCharacters;
});
```

This keeps every other Unclaimable rule active.

## Language support

Localized datasets currently support:

- `UnclaimableLanguage.English` — enabled by default;
- `UnclaimableLanguage.Dutch`;
- `UnclaimableLanguage.German`;
- `UnclaimableLanguage.French`;
- `UnclaimableLanguage.Spanish`;
- `UnclaimableLanguage.Italian`;
- `UnclaimableLanguage.Portuguese`.

Each language pack uses the same localized dataset categories:

- `roles`;
- `support`;
- `system`;
- `profanity`.

Global datasets contain language-independent proper names such as brands, companies, platforms, products, and technology ecosystems. They are always active, so disabling every localized language does not make names such as `paypal`, `github`, or `nike` claimable.

### English only — default

```csharp
builder.Services.AddUnclaimable();
```

No language configuration is required because English is enabled by default.

### English + Dutch

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(UnclaimableLanguage.Dutch);
});
```

Adding a language does not replace English. It extends the enabled localized datasets.

### Dutch only

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RemoveLanguage(UnclaimableLanguage.English);
    options.AddLanguage(UnclaimableLanguage.Dutch);
});
```

### Several languages

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(UnclaimableLanguage.Dutch);
    options.AddLanguage(UnclaimableLanguage.German);
    options.AddLanguage(UnclaimableLanguage.French);
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

The project still compiles. The same applies to German, French, Spanish, Italian, Portuguese, and even English. A missing folder contributes no embedded localized entries; it does not create a compile-time dependency or require a code change.

This is separate from `AddLanguage(...)` / `RemoveLanguage(...)`: those methods select from language data that exists in the build. Physically deleting a language folder trims that language data from the build itself. If application code enables a language whose folder was removed, that language simply contributes no built-in entries.

CI explicitly builds the core and ASP.NET Core projects from a temporary checkout with the entire `data/languages/` directory removed, so the removable-pack behavior is continuously verified.

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
options.AddLanguage(UnclaimableLanguage.Dutch);
options.AddLanguage(UnclaimableLanguage.German);
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
    // Identifier passed the strict English default policy plus global datasets.
}
```

English + Dutch checker:

```csharp
var options = new UnclaimableOptions();
options.AddLanguage(UnclaimableLanguage.Dutch);
var checker = new UnclaimableChecker(options);
```

Dutch-only checker:

```csharp
var options = new UnclaimableOptions();
options.RemoveLanguage(UnclaimableLanguage.English);
options.AddLanguage(UnclaimableLanguage.Dutch);
var checker = new UnclaimableChecker(options);
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
    options.AddLanguage(UnclaimableLanguage.Dutch);
    options.AddLanguage(UnclaimableLanguage.German);

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
| `languages/en` | profanity, roles, support, system | enabled by default; folder may be removed from source builds |
| `languages/nl` | profanity, roles, support, system | optional, additive; folder may be removed |
| `languages/de` | profanity, roles, support, system | optional, additive; folder may be removed |
| `languages/fr` | profanity, roles, support, system | optional, additive; folder may be removed |
| `languages/es` | profanity, roles, support, system | optional, additive; folder may be removed |
| `languages/it` | profanity, roles, support, system | optional, additive; folder may be removed |
| `languages/pt` | profanity, roles, support, system | optional, additive; folder may be removed |
| `global` | brands, technology | always loaded |

Localized dataset documents declare their language explicitly where possible:

```json
{
  "schema": 1,
  "category": "support",
  "language": "de",
  "description": "...",
  "values": ["..."]
}
```

Language-independent data uses:

```json
"language": "global"
```

The loader uses document metadata rather than hard-coded language-pack paths. The project file embeds `data/**/reserved.json`, so only files that actually exist are compiled into a build.

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

Built-in protected values for all enabled language scopes are indexed when the checker is constructed. Normal exact and compact lookups use hashed dictionaries rather than repeatedly scanning the full dataset.

## Repository layout

```text
.github/workflows/
assets/
conformance/
data/
  global/
    brands/
    technology/
  languages/
    en/
    nl/
    de/
    fr/
    es/
    it/
    pt/
platforms/dotnet/src/Unclaimable/
  Languages/
platforms/dotnet/src/Unclaimable.AspNetCore/
platforms/dotnet/tests/Unclaimable.Tests/
platforms/dotnet/smoke/Unclaimable.ConsumerSmoke/
```

## License

MPL-2.0