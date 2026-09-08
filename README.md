<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

> **Release status:** preparing the first public NuGet release, `0.1.0`.

Unclaimable helps answer one question:

> **Should this identifier be claimable?**

Use it for usernames, handles, account names, slugs, community names, or any other identifier that users can claim.

It combines reserved-name datasets with structural validation, impersonation detection, profanity filtering, Unicode lookalike handling, configurable application rules, and ASP.NET Core integration.

## Quick start

### ASP.NET Core

Install the integration package:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.1.0
```

Register Unclaimable in `Program.cs`:

```csharp
builder.Services.AddUnclaimable();
```

Then validate a model with DataAnnotations:

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

Or inject the checker directly:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

### Core .NET

Install the core package:

```bash
dotnet add package Unclaimable --version 0.1.0
```

Use the default checker:

```csharp
using Unclaimable;

var checker = UnclaimableChecker.Default;

checker.IsClaimable("normalname");
checker.IsReserved("support");
```

For most applications, the defaults are enough to get started.

## What gets checked

The default policy includes:

- reserved and protected names;
- trusted-role and support impersonation names;
- global brand and technology names;
- English profanity filtering;
- embedded reserved names such as `supportive` containing `support`;
- separator and punctuation normalization for matching;
- common leetspeak and symbol substitutions;
- selected Unicode lookalikes and confusable characters;
- minimum and maximum length;
- numbers;
- whitespace;
- blocked characters;
- leading and trailing separators.

Examples with the default configuration:

| Value | Result | Reason |
| --- | --- | --- |
| `normalname` | claimable | no rule matched |
| `support` | rejected | reserved name |
| `supportive` | rejected | contains protected `support` |
| `john-doe` | rejected | `-` is blocked by default |
| `john_doe` | rejected | `_` is blocked by default |
| `user2` | rejected | numbers are disabled by default |
| `.john` | rejected | leading separator |
| `john.` | rejected | trailing separator |

## Default configuration

`new UnclaimableOptions()` uses a strict baseline:

| Setting | Default |
| --- | --- |
| Language data | English |
| Strict matching | enabled |
| Compact matching | enabled |
| Partial matching | enabled |
| Obfuscation / leetspeak matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity matching | enabled |
| Minimum length | `3` |
| Maximum length | `32` |
| Numbers | rejected |
| Whitespace | rejected |
| `-` and `_` | blocked |
| Leading separators | rejected |
| Trailing separators | rejected |

The defaults are opt-out: configure only the rules your application wants to change.

## Configuration

ASP.NET Core configuration lives in the `AddUnclaimable(...)` callback:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(UnclaimableLanguage.Dutch);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = UnclaimableRule.Numbers;
});
```

The same `UnclaimableOptions` type can be passed directly to `UnclaimableChecker` when using the core package.

### Length

The default accepted length is `3` through `32` UTF-16 characters.

```csharp
options.MinimumLength = 4;
options.MaximumLength = 24;
```

Length rules can also be disabled completely:

```csharp
options.DisabledRules =
    UnclaimableRule.MinimumLength |
    UnclaimableRule.MaximumLength;
```

### Allow numbers

Numbers are rejected by default.

Allow them by disabling the numbers rule:

```csharp
options.DisabledRules = UnclaimableRule.Numbers;
```

Unclaimable checks Unicode decimal digits, not only `0` through `9`.

### Block additional characters

Whitespace, `-`, and `_` are blocked by the default policy.

Add application-specific blocked characters with:

```csharp
options.AdditionalBlockedCharacters("^", "$", "@");
```

Each value must contain exactly one Unicode character.

### Reserve application-specific names

Protect product names, organization names, bots, tenants, or other identifiers that belong to your application:

```csharp
options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");
```

These values use the same normalization and matching pipeline as the built-in datasets.

### Disable individual rules

Rules are represented by `UnclaimableRule` flags and can be combined:

```csharp
options.DisabledRules =
    UnclaimableRule.Numbers |
    UnclaimableRule.BlockedCharacters |
    UnclaimableRule.Whitespace;
```

Available rules:

| Rule | Controls |
| --- | --- |
| `MinimumLength` | minimum identifier length |
| `MaximumLength` | maximum identifier length |
| `Whitespace` | whitespace rejection |
| `BlockedCharacters` | built-in and configured blocked characters |
| `LeadingSeparator` | leading `-`, `_`, or `.` |
| `TrailingSeparator` | trailing `-`, `_`, or `.` |
| `Numbers` | decimal digits |
| `CompactMatching` | matching after punctuation/separators are removed |
| `PartialMatching` | reserved names inside larger values |
| `Profanity` | localized profanity datasets |
| `ObfuscationMatching` | common leetspeak and symbol substitutions |
| `UnicodeConfusableMatching` | selected Unicode lookalikes |

### Use less aggressive reserved-name matching

Strict mode is the default and enables partial matching automatically.

Use standard mode when you deliberately want fewer substring matches:

```csharp
options.Strictness = UnclaimableStrictness.Standard;
```

In strict mode, values such as these can be rejected:

```text
supportive -> support
apples     -> apple
nikee      -> nike
```

`PartialMatchMinimumLength` defaults to `4`, preventing very short protected values from participating in normal substring matching.

```csharp
options.PartialMatchMinimumLength = 5;
```

## Language support

English is enabled by default.

Additional localized datasets are available for:

- English;
- Dutch;
- German;
- French;
- Spanish;
- Italian;
- Portuguese.

Each localized pack contains the same types of data: roles, support terms, system names, and profanity.

Global brand and technology datasets are language-independent and remain active regardless of the selected languages.

### Add a language

Adding a language keeps English enabled:

```csharp
options.AddLanguage(UnclaimableLanguage.Dutch);
```

For English + Dutch + German:

```csharp
options.AddLanguage(UnclaimableLanguage.Dutch);
options.AddLanguage(UnclaimableLanguage.German);
```

### Use Dutch only

```csharp
options.RemoveLanguage(UnclaimableLanguage.English);
options.AddLanguage(UnclaimableLanguage.Dutch);
```

`AddLanguage(...)` and `RemoveLanguage(...)` only control which localized built-in datasets are used. They do not restrict which Unicode characters a user may type.

## Matching behavior

Unclaimable applies several forms of reserved-name matching.

### Exact

Matching is case-insensitive and normalizes surrounding whitespace:

```text
support
SUPPORT
```

Both resolve to the same protected value.

### Compact

Compact matching ignores punctuation and separators when comparing a value with protected names. This remains useful if your application relaxes the structural character rules.

### Partial

Strict mode detects protected values inside a larger identifier:

```text
supportive -> support
```

### Obfuscation

Common substitutions are normalized during matching, including mappings such as:

```text
0 -> o
1 -> i / l
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

For example, when numbers are allowed structurally:

```text
N1k3   -> nike
G00gle -> google
```

Candidate expansion is bounded to keep matching predictable.

### Unicode lookalikes

Unclaimable includes a bounded set of common Unicode-confusable mappings and diacritic normalization for impersonation checks.

For example, a Cyrillic character that visually resembles a Latin character can still resolve to a protected name.

## Profanity

Profanity filtering is enabled for every enabled localized language.

Disable it independently with:

```csharp
options.DisabledRules = UnclaimableRule.Profanity;
```

Profanity participates in exact, compact, obfuscation, and Unicode-aware matching.

Substring matching for profanity is intentionally separate because it is much more aggressive. Enable it only when your application wants that behavior:

```csharp
options.ProfanityPartialMatching = true;
```

## Runtime character policy

`Unclaimable.AspNetCore` registers a singleton `IUnclaimablePolicy` that can be adjusted while the application is running.

```csharp
var policy = app.Services.GetRequiredService<IUnclaimablePolicy>();

policy.BlockCharacters("^", "$", "@");
policy.AllowCharacter("-");
```

Existing injected `IUnclaimableChecker` instances observe those changes immediately.

The core package supports the same pattern:

```csharp
var options = new UnclaimableOptions();
var policy = new UnclaimablePolicy(options.ConfiguredBlockedCharacters);
var checker = new UnclaimableChecker(options, policy);

policy.BlockCharacter("^");

var allowed = checker.IsClaimable("normal^name");
```

Runtime changes are process-local.

## Validation results

Use the simplest API that fits the job.

### Boolean checks

```csharp
checker.IsClaimable("normalname");
checker.IsReserved("support");
```

### First rejection reason

`Check(...)` is fail-fast and returns the first detected rejection:

```csharp
var result = checker.Check("john-doe");

Console.WriteLine(result.IsReserved);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.OffendingCharacter);
Console.WriteLine(result.OffendingCharacterIndex);
```

`UnclaimableMatchKind` includes:

```text
None
Exact
Compact
Partial
Obfuscated
UnicodeConfusable
InvalidCharacters
NumbersNotAllowed
TooShort
TooLong
BlockedCharacter
LeadingSeparator
TrailingSeparator
```

### All diagnostics

Use `CheckDetailed(...)` when a UI, log, or diagnostic screen needs more than the first failure:

```csharp
var result = checker.CheckDetailed(
    "support2",
    includeMessages: true);

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

A detailed result can contain both structural-policy violations and reserved-name matches.

## ASP.NET Core validation messages

`[ClaimableUsername]` includes built-in messages, but applications can replace them.

Set one fallback message:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ValidationMessage = "{FieldName} is not available.";
});
```

Or configure specific rejection reasons:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Messages.Reserved =
        "{FieldName} '{MatchedValue}' is reserved.";

    options.Messages.NumbersNotAllowed =
        "Numbers are not allowed in {FieldName}.";

    options.Messages.BlockedCharacter =
        "{FieldName} contains blocked character '{Character}'.";
});
```

Supported placeholders:

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
2. reason-specific `options.Messages.*`;
3. `options.ValidationMessage`;
4. built-in message.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | dependency injection and DataAnnotations integration |

Both packages generate XML documentation for IntelliSense.

## Datasets and source builds

Reserved values live in human-reviewable JSON files below `data/` and are embedded into the core package at build time.

```text
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
```

Language folders are optional source data. A fork or custom source build can remove language folders it does not want to embed. The project uses wildcard resource inclusion, so missing language folders do not require project-file changes.

Runtime `AddLanguage(...)` and `RemoveLanguage(...)` select from the language data that exists in that build.

CI also verifies that the .NET projects still build with the entire `data/languages/` directory removed.

## Validation pipeline

`Check(...)` is ordered to reject inexpensive policy failures before running more expensive reserved-name matching:

1. minimum / maximum length;
2. leading / trailing separators;
3. numbers and character policy;
4. exact reserved-name lookup;
5. compact matching;
6. partial matching;
7. Unicode-confusable matching;
8. bounded obfuscation matching.

Exact and compact protected-name lookups use indexed dictionaries rather than scanning the entire dataset for every request.

## Repository layout

```text
.github/workflows/                 CI and release workflows
assets/                            package and repository artwork
conformance/                       shared behavior cases
data/                              reserved-name datasets
platforms/dotnet/src/              .NET packages
platforms/dotnet/tests/            automated tests
platforms/dotnet/smoke/            packaged consumer smoke test
```

## License

MPL-2.0
