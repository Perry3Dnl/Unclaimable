<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Unclaimable"><strong>NuGet package</strong></a>
</p>

Unclaimable answers one question: **should this identifier be claimable?**

It combines curated reserved-name datasets with structural validation, partial and compact matching, bounded obfuscation detection, Unicode lookalike handling, localized filtering, application-specific policy, and ASP.NET Core integration.

The default policy remains intentionally strict, while **0.4.0 is compatibility-focused**: existing public APIs, enum values, datasets, defaults, and valid-Unicode outcomes from 0.3.0 are preserved. New Unicode hardening and corrected compact-rule interactions are opt-in.

```csharp
builder.Services.AddUnclaimable();
```

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | dependency injection and DataAnnotations integration |

For the 0.4.0 release:

```bash
dotnet add package Unclaimable --version 0.4.0
dotnet add package Unclaimable.AspNetCore --version 0.4.0
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate-name");
if (result.IsClaimable)
{
    // Continue with application-specific availability checks.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.AdditionalReserved.Add("examplebrand");
});
```

Then inject either `IChecker` or `IPolicy`:

```csharp
public sealed class UsernameService(IChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

`null` is accepted by Unclaimable. Required-field validation is deliberately separate, for example through `[Required]` in ASP.NET Core.

## What 0.4.0 changes

0.4.0 does **not** expand the built-in datasets or make the default policy stricter. That is intentional: adding a reserved value can reject a username that 0.3.0 accepted, which is a behavior change even if every application still compiles.

This release adds:

- explicit malformed UTF-16 rejection instead of allowing normalization/policy code to throw;
- opt-in rejection of invisible-only identifiers;
- opt-in rejection of Unicode control characters;
- opt-in rejection of Unicode formatting characters;
- opt-in consistent handling of `Rule.CompactMatching` across partial and obfuscation matching;
- nullable original-input match spans alongside the existing transformed-text offsets;
- the effective length threshold on length failures;
- complete fallback length messages in `[ClaimableUsername]`;
- stronger regression, compatibility, package, and benchmark coverage.

All new strictness options default to `false`.

## Unicode hardening

Malformed UTF-16 is always rejected before normalization or calls into `IPolicy`.

```text
unpaired high surrogate -> InvalidCharacters
unpaired low surrogate  -> InvalidCharacters
valid surrogate pair    -> normal Unicode processing
```

The offending index is the original UTF-16 code-unit index. `CheckDetailed` returns an invalid-character diagnostic and skips reserved-name normalization for malformed input.

Additional Unicode checks are opt-in:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RejectInvisibleOnlyIdentifiers = true;
    options.RejectControlCharacters = true;
    options.RejectFormatCharacters = true;
});
```

### `RejectInvisibleOnlyIdentifiers`

Requires at least one Unicode scalar outside these categories:

- whitespace;
- control;
- format;
- non-spacing mark;
- spacing combining mark;
- enclosing mark.

This is deliberately documented as an approximation of visible content. Unicode categories cannot determine what every font, renderer, shaping engine, or display environment will render visibly.

### `RejectControlCharacters`

Rejects Unicode control characters. Disabled by default.

### `RejectFormatCharacters`

Rejects Unicode formatting characters, including zero-width and bidirectional formatting characters. Disabled by default because joiners and other formatting characters can serve legitimate purposes in some languages.

## Compact-rule consistency

0.3.0 has a legacy interaction where disabling `Rule.CompactMatching` disables direct compact matching but compact forms can still participate in some partial and obfuscation paths.

0.4.0 preserves that behavior by default.

Applications that want the compact rule applied consistently can opt in:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ConsistentCompactMatching = true;
});
```

When `ConsistentCompactMatching` is enabled and compact matching is disabled:

- exact partial matching remains active when partial matching is enabled;
- compact partial matching is skipped;
- separators and punctuation remain in obfuscation candidates;
- those candidates are compared against exact entries;
- Unicode-confusable matching remains independently controlled.

When compact matching is enabled, the established matching behavior remains active.

## Strict defaults

`new Options()` keeps the 0.3.0 baseline:

| Setting | Default |
| --- | --- |
| Localized language | English |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Partial matching | enabled through strict mode |
| Obfuscation matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity matching | enabled |
| Minimum length | `3` |
| Maximum length | `32` |
| Numbers | rejected |
| Whitespace | rejected |
| Built-in `-` / `_` policy | blocked |
| Leading/trailing separators | rejected |
| Invisible-only rejection | disabled |
| Control-character rejection | disabled |
| Format-character rejection | disabled |
| Consistent compact interaction | disabled |

Strictness can enable partial matching even when `PartialMatching = false` has not been explicitly changed. Use `Strictness.Standard` or disable `Rule.PartialMatching` when the application deliberately wants to relax substring matching.

## Dataset coverage

0.4.0 intentionally contains the **same built-in dataset as 0.3.0**:

- **10,731 filter entries**;
- **10,633 unique values within categories**;
- **22 categories**.

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

The totals include concrete entries expanded from schema-v2 combinations. Matching rules can reject additional variants without storing every variant as a separate dataset entry.

## Language support

English is enabled by default. Localized datasets are also available for:

- Dutch;
- German;
- French;
- Spanish;
- Italian;
- Portuguese;
- Polish;
- Turkish;
- Indonesian;
- Czech;
- Vietnamese;
- Hungarian;
- Swedish;
- Romanian.

Languages are additive:

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);

var checker = new Checker(options);
```

To build with Dutch only:

```csharp
options.RemoveLanguage(Language.English);
options.AddLanguage(Language.Dutch);
```

Language selection controls which localized datasets are loaded. It does not restrict which Unicode scripts a submitted identifier may contain.

Language folders below `data/languages/<code>/` are optional source data. Source builds remain valid when language folders are physically removed; CI verifies a build with the entire `data/languages/` directory absent.

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

Disable only the rules your application deliberately wants to relax:

```csharp
var options = new Options
{
    Strictness = Strictness.Standard,
    DisabledRules = Rule.Numbers | Rule.Whitespace
};
```

Enum numeric values are part of the compatibility surface and are regression-checked against 0.3.0.

## Application-specific policy

Reserve application-specific identifiers at checker construction time:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalReserved.Add("internalbot");
    options.AdditionalBlockedCharacters("^", "$");
});
```

`Options` are captured when a `Checker` is constructed. Mutating those options afterward does not rewrite the checker's effective settings.

Character policy is different: the supplied `IPolicy` remains live.

```csharp
var policy = app.Services.GetRequiredService<IPolicy>();

policy.BlockCharacter("@");
policy.AllowCharacter("-");
```

Existing checker instances observe those runtime policy changes.

## Results and positions

For a fail-fast result:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsReserved);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
```

`IsReserved` includes structural validation failures; it does not mean only “found in the reserved-name dataset.”

### Existing transformed offsets

`MatchStartIndex` and `MatchLength` retain their 0.3.0 meaning. They refer to the transformed text used by the matching stage and are measured in UTF-16 code units.

They were intentionally not repurposed in 0.4.0.

### Original-input offsets

0.4.0 adds:

```csharp
result.OriginalMatchStartIndex
result.OriginalMatchLength
```

These are also UTF-16 code-unit offsets. They are populated only when the transformation can be mapped reliably to the original input.

For compact matches, the original span includes punctuation between matched characters. If normalization expands, contracts, or otherwise makes the mapping uncertain, both properties are `null` rather than returning an inaccurate highlight.

The same original-span properties are available on `Diagnostic`.

### Length failures

`Result.LengthLimit` contains the configured threshold that actually caused a `TooShort` or `TooLong` result when the result was produced by `Checker`.

This reflects the configuration captured by that checker, not later mutations to an `Options` object.

## Detailed diagnostics

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);

foreach (var diagnostic in detailed.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

Malformed UTF-16 produces an invalid-character diagnostic and stops before reserved-name normalization. Valid input can continue collecting structural diagnostics and a reserved-name diagnostic.

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

Validation-message precedence remains:

1. attribute `ErrorMessage`;
2. reason-specific configured message;
3. global configured message;
4. built-in message.

Supported placeholders include:

```text
{FieldName}
{MatchedValue}
{Category}
{Character}
{Index}
{Length}
{MinimumLength}
{MaximumLength}
```

For length placeholders, 0.4.0 uses the effective threshold in this order:

1. `Result.LengthLimit`;
2. registered `Options`, for custom checker implementations;
3. default `Options` when the attribute selected `Checker.Default`.

This fixes incomplete default messages without changing the existing precedence system.

## Project structure

```text
assets/                         package/repository artwork
conformance/                    shared conformance corpus
data/                           global and localized reserved datasets
platforms/dotnet/src/           runtime packages
platforms/dotnet/tests/         xUnit regression tests
platforms/dotnet/smoke/         packaged consumer smoke test
platforms/dotnet/compat/        0.3.0 API/behavior compatibility gate
platforms/dotnet/benchmarks/    BenchmarkDotNet release benchmarks
platforms/dotnet/scripts/       package and dataset validation scripts
```

## Release engineering

0.4.0 adds an explicit compatibility workflow that builds the immutable `v0.3.0` tag and the current source side by side.

It checks:

- exported public types;
- constructors;
- methods and parameter signatures;
- properties, fields, and events;
- optional parameter defaults;
- enum numeric values;
- a deterministic valid-Unicode default-behavior corpus using legacy `Result` and `Diagnostic` fields.

The normal .NET workflow additionally verifies:

- xUnit tests;
- builds with localized source packs removed;
- core and ASP.NET Core NuGet packages;
- package metadata/content validation;
- a clean consumer restore and execution from the generated packages.

BenchmarkDotNet coverage records:

- checker construction;
- ordinary accepted input;
- exact rejection;
- obfuscation matching;
- all-language construction.

The pre-change 0.3.0 baseline is stored in `platforms/dotnet/benchmarks/BASELINE-0.3.0.md`.

## Build locally

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj --configuration Release

dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj --configuration Release --output artifacts
dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj --configuration Release --output artifacts

pwsh ./platforms/dotnet/scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

Run benchmarks separately:

```bash
dotnet run --project platforms/dotnet/benchmarks/Unclaimable.Benchmarks/Unclaimable.Benchmarks.csproj --configuration Release
```

## Compatibility philosophy

A release can be source-compatible and still be behaviorally breaking. For identifier validation, adding one blocked value can reject an identifier that an existing application previously accepted.

For that reason, 0.4.0 separates:

- API compatibility;
- default-behavior compatibility for valid Unicode input;
- explicit exception-behavior fixes for malformed input;
- opt-in stricter behavior.

Future dataset expansion can then be reviewed as an intentional behavior change instead of being hidden inside an otherwise compatible maintenance release.

## License

Unclaimable is licensed under the [Mozilla Public License 2.0](LICENSE).

Copyright © 2026 Perry3D.nl.
