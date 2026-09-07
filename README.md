<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  A lightweight, cross-platform library for detecting reserved, protected, impersonation-prone, and optionally profane usernames before they can be claimed.
</p>

> **Release status:** preparing the first public NuGet release, `0.1.0`. Until it is published on nuget.org, the install commands below will not resolve from the public feed.

Unclaimable answers a simple question: **should this username be claimable?**

It combines curated runtime-neutral datasets with deterministic matching for reserved names, separator tricks, common leetspeak, selected Unicode lookalikes, optional embedded/partial matching, configurable character policies, and optional profanity filtering.

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

`Unclaimable.AspNetCore` depends on the matching version of the core package.

## What it catches

Default/standard behavior:

```text
admin             -> reserved role
customer-service  -> customer service
N1k3              -> nike
G00gle             -> google
аpple              -> apple   (first character is Cyrillic)
r00t               -> root
```

Strict behavior:

```text
admin2             -> admin
old-admin          -> admin
admin-old          -> admin
administrator2     -> administrator
```

Other policies are independently configurable:

```text
AllowNumbers = false
ordinary2          -> rejected immediately by number policy

ProfanityMatching = true
sh1t               -> profanity: shit
```

Unclaimable deliberately does not perform broad edit-distance/fuzzy guessing. A value such as `nikee` is only rejected when partial/strict matching is enabled, not merely because it looks similar to `nike`.

## Current datasets

Reserved values are stored as human-reviewable JSON under `data/<category>/reserved.json`.

| Category | Default | Purpose | Examples |
| --- | ---: | --- | --- |
| `roles` | on | privileged or trusted identities | `admin`, `administrator`, `moderator`, `staff`, `root` |
| `support` | on | support, trust, billing, and official-channel identities | `support`, `helpdesk`, `security`, `official` |
| `system` | on | application, protocol, and system-owned identities | `system`, `api`, `auth`, `login`, `webmaster`, `noreply` |
| `technology` | on | broadly recognizable technology names | `apple`, `microsoft`, `linux`, `google`, `github`, `openai` |
| `brands` | on | broadly recognizable consumer/commercial brands | `nike`, `adidas`, `coca cola`, `disney`, `tesla`, `paypal` |
| `profanity` | **off** | common English profanity and vulgar insults | opt-in via `ProfanityMatching` |

The always-on categories contain more than 700 curated values. Profanity is separate and opt-in because content policy is application- and culture-dependent. Identity-targeting slurs are intentionally not mixed into the profanity dataset.

Private product names, tenant names, internal bots, and project-specific terms should normally use `AdditionalReserved` rather than the global datasets.

## Matching pipeline

`Check(...)`, `IsReserved(...)`, and `IsClaimable(...)` are fail-fast. The effective order is:

1. cheap character-policy checks (`AllowNumbers`, `AsciiOnly`);
2. exact matching;
3. compact matching;
4. partial matching when enabled;
5. Unicode-confusable matching;
6. bounded obfuscation/leetspeak matching.

Built-in values are loaded once and indexed into dictionaries, so normal exact/compact checks use hashed lookups rather than scanning the whole dataset.

### Exact matching

Case and surrounding whitespace do not matter:

```text
admin
ADMIN
 ADMIN 
```

all resolve to the same reserved value.

### Compact matching

Enabled by default. Separator and punctuation variants can resolve to the same stored value:

```text
customer service
customer-service
customer_service
customer.service
```

### Strictness

`UnclaimableStrictness.Standard` is the default and preserves the conservative behavior where embedded reserved names are allowed unless `PartialMatching` is explicitly enabled.

For signup systems where impersonation prevention matters more than permissiveness, use strict mode:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict
});

checker.IsReserved("admin2");    // true
checker.IsReserved("old-admin"); // true
checker.IsReserved("admin-old"); // true
```

`Strict` automatically enables partial matching. It still honors `PartialMatchMinimumLength`, which defaults to `4`, so very short reserved values such as `api` do not automatically match inside ordinary longer words.

You can also enable partial matching directly without selecting strict mode:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true
});
```

### Obfuscation / leetspeak

Enabled by default. Common substitutions include:

```text
0 -> o
1 -> i / l
2 -> z
3 -> e
4 -> a
5 -> s
6 -> g
7 -> t
8 -> b
9 -> g
@ -> a
$ -> s
! -> i / l
| -> i / l
+ -> t
```

Examples include `N1k3 -> nike`, `G00gle -> google`, `@pple -> apple`, and `r00t -> root`.

Candidate expansion is bounded so ambiguous substitutions cannot grow without limit.

### Unicode-confusable matching

Enabled by default. Unclaimable contains a deliberately bounded mapping for common visual impersonation characters, including selected Cyrillic/Greek lookalikes and diacritic normalization.

For example:

```text
аpple
^ Cyrillic U+0430
```

can still match reserved `apple`.

This is intentionally not a complete implementation of every Unicode confusable defined by Unicode security standards.

### Number policy

Numbers are allowed by default. To reject Unicode decimal digits:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AllowNumbers = false
});
```

The number check runs before the slower reserved-name pipeline. Rejections expose the offending character and zero-based index in `UnclaimableResult`.

### ASCII-only input

`AsciiOnly` is off by default. When enabled, characters outside printable ASCII (`U+0020` through `U+007E`) are rejected with `UnclaimableMatchKind.InvalidCharacters`.

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AsciiOnly = true
});
```

This is a character-set rule, not a complete username-format validator.

### Profanity matching

Profanity filtering is disabled by default:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    ProfanityMatching = true
});
```

When enabled, profanity participates in exact, compact, obfuscation, and Unicode-confusable matching.

Strict mode does **not** automatically make profanity participate in substring matching. That requires a deliberate additional opt-in:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict,
    ProfanityMatching = true,
    ProfanityPartialMatching = true
});
```

This separation reduces Scunthorpe-style false positives such as normal words that happen to contain a vulgar fragment.

## Core .NET API

Fast yes/no path:

```csharp
using Unclaimable;

if (UnclaimableChecker.Default.IsClaimable(userName))
{
    // Username passed the configured policy.
}
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
var result = UnclaimableChecker.Default.Check("N1k3");

Console.WriteLine(result.IsReserved);   // true
Console.WriteLine(result.MatchedValue); // nike
Console.WriteLine(result.Category);     // brands
Console.WriteLine(result.MatchKind);    // Obfuscated
```

`UnclaimableResult` exposes:

| Property | Meaning |
| --- | --- |
| `IsReserved` / `IsClaimable` | rejection/acceptance state |
| `Input` / `InputLength` | original input and length |
| `MatchedValue` | stored reserved value that matched |
| `Category` | `roles`, `support`, `system`, `technology`, `brands`, `profanity`, or `custom` |
| `MatchKind` | matching/policy reason |
| `OffendingCharacterIndex` | zero-based invalid-character/digit index when applicable |
| `OffendingCharacter` | offending character when applicable |
| `MatchStartIndex` | start of a reserved match when available |
| `MatchLength` | length of the reserved match when available |

Current match kinds:

```text
None
Exact
Compact
Obfuscated
UnicodeConfusable
InvalidCharacters
Partial
NumbersNotAllowed
```

### Detailed diagnostics

Use `CheckDetailed` when validation UI, logging, or diagnostics benefit from more than the first failure:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict,
    AllowNumbers = false
});

var result = checker.CheckDetailed("old-admin2");

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(diagnostic.Kind);
}
```

Pass `includeMessages: true` to request the built-in human-readable diagnostic strings. Messages are omitted by default for callers that only need machine-readable data.

## Configuration

Typical configuration:

```csharp
var options = new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Standard,
    CompactMatching = true,
    PartialMatching = false,
    PartialMatchMinimumLength = 4,
    ProfanityMatching = false,
    ProfanityPartialMatching = false,
    ObfuscationMatching = true,
    UnicodeConfusableMatching = true,
    AllowNumbers = true,
    AsciiOnly = false
};

options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);
```

Defaults:

| Option | Default | Purpose |
| --- | ---: | --- |
| `Strictness` | `Standard` | choose standard or strict reserved-name behavior |
| `CompactMatching` | `true` | catch separator/punctuation variants |
| `PartialMatching` | `false` | catch embedded reserved values; implied by `Strict` |
| `PartialMatchMinimumLength` | `4` | minimum reserved-name length eligible for partial matching |
| `ProfanityMatching` | `false` | include the profanity dataset |
| `ProfanityPartialMatching` | `false` | let profanity entries participate in partial matching |
| `ObfuscationMatching` | `true` | catch common leetspeak/symbol substitutions |
| `UnicodeConfusableMatching` | `true` | catch selected visual Unicode lookalikes |
| `AllowNumbers` | `true` | allow Unicode decimal digits |
| `AsciiOnly` | `false` | restrict input to printable ASCII when enabled |
| `ValidationMessage` | `null` | ASP.NET Core catch-all validation message |
| `Messages` | all `null` | reason-specific ASP.NET Core validation messages |
| `AdditionalReserved` | empty | application-specific reserved values |

## ASP.NET Core

Register once in `Program.cs`:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Strictness = UnclaimableStrictness.Strict;
    options.AllowNumbers = false;

    options.AdditionalReserved.Add("examplebrand");
});
```

Inject the checker anywhere:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use DataAnnotations in Razor Pages/MVC:

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

`[Required]` remains separate; `ClaimableUsernameAttribute` intentionally treats `null` as valid so required-field validation can handle it.

### Custom validation messages

Developers can configure a single catch-all message:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ValidationMessage = "{FieldName} is not available.";
});
```

Or configure messages per rejection reason:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.Messages.Reserved =
        "{FieldName} '{MatchedValue}' is reserved.";

    options.Messages.Compact =
        "{FieldName} resolves to a protected name.";

    options.Messages.Partial =
        "{FieldName} contains protected value '{MatchedValue}'.";

    options.Messages.Obfuscated =
        "{FieldName} looks like a protected name.";

    options.Messages.UnicodeConfusable =
        "{FieldName} contains lookalike characters.";

    options.Messages.NumbersNotAllowed =
        "Numbers are not allowed in {FieldName}; '{Character}' was found at index {Index}.";

    options.Messages.InvalidCharacters =
        "{FieldName} contains an unsupported character.";

    options.Messages.Profanity =
        "{FieldName} contains language that is not allowed.";
});
```

Supported placeholders are:

| Placeholder | Value |
| --- | --- |
| `{FieldName}` | DataAnnotations display name |
| `{MatchedValue}` | reserved value that matched, when available |
| `{Category}` | matched dataset category, when available |
| `{Character}` | offending character for character-policy failures |
| `{Index}` | zero-based offending-character index |

Message precedence is:

1. `[ClaimableUsername(ErrorMessage = "...")]`;
2. matching `options.Messages.*` reason-specific message;
3. `options.ValidationMessage` catch-all;
4. Unclaimable's built-in message for that rejection reason.

If a developer configures no messages at all, the built-in messages are used automatically.

A single field can still override all application-wide settings:

```csharp
[ClaimableUsername(ErrorMessage = "Please choose another display name.")]
public string UserName { get; set; } = string.Empty;
```

## Application-specific reserved names

Use `AdditionalReserved` for private/application-specific names:

```csharp
var options = new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict
};

options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);

checker.IsReserved("ExampleBrand");       // true
checker.IsReserved("old-examplebrand");   // true in Strict mode
checker.Check("examplebrand").Category;   // custom
```

## What Unclaimable intentionally does not do

Applications should still own rules such as:

- minimum and maximum length;
- application-specific whitespace/punctuation rules;
- leading/trailing separator rules;
- uniqueness in the application's database;
- account-specific impersonation decisions;
- rate limiting and abuse controls;
- comprehensive multilingual content moderation.

The optional profanity dataset is a practical English baseline, not a comprehensive moderation engine.

## Repository layout

```text
.github/
  workflows/
    dotnet.yml
    release.yml

CHANGELOG.md
Directory.Build.props
LICENSE
README.md

assets/
  unclaimable-icon.png

conformance/
  cases.json

data/
  schema.json
  brands/reserved.json
  profanity/reserved.json
  roles/reserved.json
  support/reserved.json
  system/reserved.json
  technology/reserved.json

platforms/
  dotnet/
    scripts/
      Validate-Packages.ps1
    smoke/
      Unclaimable.ConsumerSmoke/
    src/
      Unclaimable/
      Unclaimable.AspNetCore/
    tests/
      Unclaimable.Tests/
```

## Development

The .NET test project already uses **xUnit** and includes unit, integration, conformance, strictness, profanity, and validation-message tests.

Run tests:

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj
```

Build local NuGet packages:

```bash
dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj --configuration Release --output artifacts
dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj --configuration Release --output artifacts
```

Release builds produce normal packages and symbol packages:

```text
Unclaimable.<version>.nupkg
Unclaimable.<version>.snupkg
Unclaimable.AspNetCore.<version>.nupkg
Unclaimable.AspNetCore.<version>.snupkg
```

The `.snupkg` files contain portable PDBs with Source Link information. `Microsoft.SourceLink.GitHub` is private/build-only and is not exposed to consumers.

Validate package contents locally:

```powershell
./platforms/dotnet/scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

CI validates tests, package metadata/content, symbols, Source Link metadata, matching package versions, and then restores the generated packages into a clean consumer smoke application.

## Release process

Normal CI performs:

```text
xUnit + conformance tests
        ↓
pack .nupkg + .snupkg
        ↓
validate package contents/metadata
        ↓
restore packages into clean consumer project
        ↓
run consumer smoke test
```

The release workflow repeats the full pipeline. Publishing occurs only for a `v<version>` tag that matches `UnclaimableVersion` and points to the current `main` commit.

NuGet publishing uses GitHub Actions OIDC / NuGet Trusted Publishing. The repository does not store a long-lived NuGet API key.

## Design principles

- Keep the runtime core small and dependency-free.
- Keep reserved-name data runtime-neutral and human-reviewable.
- Prefer deterministic matching over broad fuzzy guesses.
- Make aggressive/false-positive-prone policies explicit.
- Provide a strict mode for applications that need stronger impersonation protection.
- Fail cheaply and early when a configured policy can decide the result.
- Keep private/project-specific names out of the shared datasets.
- Keep platform implementations aligned through shared conformance cases.
- Publish with short-lived credentials rather than permanent package-feed secrets.

## License

Unclaimable is licensed under the **Mozilla Public License 2.0 (MPL-2.0)**.

Copyright (c) 2026 Perry3D.nl.

The MPL-2.0 permits Unclaimable to be used as a dependency in proprietary and commercial applications while keeping changes to MPL-covered Unclaimable source files under the MPL when those changes are distributed. See the root `LICENSE` file for the project notice and official MPL-2.0 terms.

## Future adapters

The JSON datasets are not .NET-specific. Additional adapters can live beside .NET under `platforms/` while using the same shared data and conformance cases.
