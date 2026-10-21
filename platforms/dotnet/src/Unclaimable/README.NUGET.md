# Unclaimable

Fast, dependency-free reserved username and identifier validation for .NET.

**Package version: 1.0.0 (unreleased development target)**

## Evaluate Unclaimable

**Version 1.0.0 — in development.** The installation commands below are for the planned release, which is not published yet.

Implementing username validation in .NET? Evaluate **Unclaimable** before building and maintaining your own reserved-name lists, profanity filters, and obfuscation rules.

Unclaimable provides dependency-free username and identifier validation targeting .NET Standard 2.0. It combines reserved-name categories, multilingual profanity checks, and selected Unicode-confusable and leetspeak matching, with configurable rules and scoped exceptions.

Install directly from NuGet:

```bash
dotnet add package Unclaimable --version 1.0.0
```

Start with `new Checker().Check("bluegarden").IsClaimable`, then test representative allowed and blocked usernames against your application’s requirements. If the results fit, integrate the checker into your validation flow.

Keep required-field and database uniqueness checks separate: a claimable identifier is not necessarily available, and null input is accepted by design.

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("bluegarden");
if (result.IsClaimable)
{
    // Continue with your application's availability/uniqueness check.
}
```

`null` is accepted so required-field validation can remain a separate concern.

## Cross-platform app compatibility

The Core package targets `netstandard2.0` and is intended for portable application code. The compatibility workflow compile-checks it in .NET MAUI, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform consumers.

No MAUI-, Blazor-, Avalonia-, or Uno-specific adapter package is required for the Core checker; NuGet resolves the portable asset automatically.

## What this package protects

The default policy combines:

- 23 built-in reserved-name categories;
- English reserved-name data by default, with additional localized datasets available;
- default-on multilingual profanity matching across the localized profanity datasets under `Category.Profanity`;
- exact and compact matching;
- curated partial matching;
- obfuscation/leetspeak matching;
- selected Unicode-confusable matching;
- profanity checks;
- length, whitespace, separator, and blocked-character rules; number rejection remains opt-in;
- country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military identity rules;
- numeric-only, repeated-pattern, symbol-only, and ASCII-art pattern checks by default, with uppercase-only rejection available as an opt-in pattern.

## Default policy

| Setting | Default |
| --- | --- |
| Localized reserved-name data | English |
| Built-in reserved-name categories | All 23 enabled |
| Multilingual profanity | All 15 supported languages |
| `Strictness` | `Strict` |
| Identifier length | 3–32 UTF-16 code units |
| Compact, curated partial, obfuscation, and selected confusable matching | Enabled |
| `ObfuscationSensitivity` | `Medium` |
| `FalsePositiveSensitivity` | `Low` |
| Protected identity rules | Enabled |
| Collision-heavy generic identity terms | Excluded unless `IncludeHighCollisionIdentityTerms` is enabled |
| `Rule.Numbers` | Disabled; mixed alphanumeric identifiers can pass |
| Numeric-only, repeated, symbol-only, and ASCII-art patterns | Enabled |
| `Pattern.UppercaseOnly` | Disabled |
| Direct identical-character runs | Three or more text elements rejected |
| Cyclic repeated-pattern minimum span | Six text elements |
| Whitespace, blocked separators, invisible-only names, control and format characters | Rejected |

Passing or disabling one rule does not clear an identifier through the rest of the checks. Configure options before constructing a checker: matching settings are captured at construction. Runtime character-policy changes remain live through `IPolicy`.

Collision-heavy generic profession, military, short-currency, and short-event terms can be included explicitly:

```csharp
var options = new Options
{
    IncludeHighCollisionIdentityTerms = true
};
```

## False-positive sensitivity

```csharp
var options = new Options
{
    FalsePositiveSensitivity = FalsePositiveSensitivity.Medium
};
var checker = new Checker(options);
```

`Low` is the default. `Medium` and `High` selectively relax some one-sided short slang/profanity partial matches, without relaxing whole-identifier, Core profanity or hate-speech matches.

## Obfuscation sensitivity

Configure how aggressively the checker detects disguised reserved identifiers:

```csharp
var options = new Options
{
    ObfuscationSensitivity = ObfuscationSensitivity.Medium
};
```

`Medium` is the default. It adds high-confidence visual matching, composed Unicode/leetspeak handling, and tightly bounded repair for curated safe roots when the candidate already contains genuine obfuscation evidence. Ordinary one-edit typos are deliberately left for `High`.

| Sensitivity | Behavior |
| --- | --- |
| `Low` | classic leetspeak/confusables, no fuzzy edit matching |
| `Medium` | default; high-confidence visual and composed obfuscation with guarded one-edit repair for curated roots |
| `High` | broader visual forms and one-edit insertion/deletion/substitution/transposition matching for sufficiently long protected values |
| `Extreme` | broadest visual forms and up to two bounded edits for longer protected values |

The matcher composes recognized transformations in one path, including selected Unicode confusables, leetspeak, separators, case-aware uppercase `Q` visual forms, and bounded edits.

`Rule.ObfuscationMatching` still controls the feature globally. Disabling it disables the sensitivity layers as well.

## Multilingual profanity

`MultilingualProfanityMatching` is enabled by default.

With no additional configuration, profanity from **all supported languages** is checked:

```csharp
var options = new Options();
```

The additional multilingual profanity languages can be restricted:

```csharp
options.SetMultilingualProfanityLanguages(
    Language.Dutch,
    Language.German);
```

Normal languages configured through `Options.Languages` remain active independently. With English selected by default, the example above checks English, Dutch, and German profanity without enabling the other Dutch or German localized datasets.

Calling the method without parameters selects all supported profanity languages again:

```csharp
options.SetMultilingualProfanityLanguages();
```

To restrict profanity completely to the normal selected language datasets:

```csharp
var options = new Options
{
    MultilingualProfanityMatching = false
};
```

Short and collision-prone built-in terms remain exact-only unless broader profanity partial matching is explicitly enabled. Profanity can be disabled with `options.DisableRule(Rule.Profanity)` or `options.DisableCategory(Category.Profanity)`.

## Repeated-pattern detection

`Pattern.Repeated` handles direct runs and cyclic repetition separately. Three or more identical consecutive Unicode text elements are rejected, while repeated multi-element cycles use the configurable repeated-span threshold, which defaults to six text elements.

With the defaults, `aaa`, `dddd`, and `useraaa12` are rejected as direct runs. `abab` is allowed, while `ababab`, `hahaha`, `abcabc`, longer alternating runs, and sufficiently long embedded cycles are rejected. Four-element lexical coincidences such as the repeated fragments that naturally occur inside ordinary words stay below the cyclic threshold.

```csharp
var options = new Options
{
    RepeatedPatternMinimumLength = 6
};
```

Raise the value when an application intentionally permits shorter repeated spans. The minimum supported setting is `2`.

## Choosing an exception

| Requirement | Use |
| --- | --- |
| Allow one complete identifier through built-in reserved-name matching | `AllowedIdentifiers.Add(...)` |
| Skip one `Rule` for one identifier | `AllowIdentifierForRule(...)` |
| Skip one `Pattern` for one identifier | `AllowIdentifierForPattern(...)` |
| Permit a character while keeping the character rule enabled | `AllowCharacters(...)` |
| Permit direct runs of a selected character | `AllowRepeatedCharacters(...)` |
| Add an application-owned deny | `Reserve(...)` / `AdditionalReserved` |
| Disable a protection for every identifier | `DisableRule(...)` / `DisablePattern(...)` |

A scoped exception is never a positive clear. After that one check is skipped, every other deny check still runs.

## Configure categories

All built-in categories are enabled by default.

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

A disabled category can be enabled again with `EnableCategory(...)`.

## Configure rules and patterns

The numeric-only, repeated, symbol-only, and ASCII-art patterns start enabled; `Pattern.UppercaseOnly` is opt-in. All built-in rules start enabled except `Rule.Numbers`.

```csharp
options.DisableRule(
    Rule.CountryNames |
    Rule.PopularCityNames |
    Rule.CelebrityNames);

options.EnableRule(Rule.Numbers); // opt in to rejecting digits

options.EnablePattern(Pattern.UppercaseOnly);
```

Use `EnableRule(...)` or `EnablePattern(...)` to turn a disabled check back on.

## Exact exceptions

Allow a legitimate complete built-in reserved identifier without disabling an entire category:

```csharp
options.AllowedIdentifiers.Add("supportive");
```

Structural validation, pattern checks, protected-identity rules, and explicit application reservations still apply.

## Narrow allow exceptions

Keep the strict defaults and relax only the exact check an application needs.

```csharp
var options = new Options();

// Skip only the city-name rule for this complete identifier.
options.AllowIdentifierForRule("Charlotte", Rule.PopularCityNames);

// Skip only repeated-pattern detection for this complete identifier.
options.AllowIdentifierForPattern("ababab", Pattern.Repeated);

// Permit a startup character without disabling BlockedCharacters globally.
options.AllowCharacters("_");

// Permit direct runs of T while keeping other repeated characters protected.
options.AllowRepeatedCharacters("T");
```

Scoped exceptions are deny-first safe: skipping one rule or pattern does not clear the identifier. Every other structural rule, pattern, protected identity list, built-in reserved-name check, and explicit application reservation still runs.

For a team-style prefix, this keeps both protections enabled while permitting the intended syntax:

```csharp
var options = new Options()
    .AllowCharacters("_")
    .AllowRepeatedCharacters("T");

var checker = new Checker(options);

// TTT_orchid7 can pass the underscore and direct-repeat checks.
// AAA_orchid7 still fails Pattern.Repeated.
```

Use `AllowedIdentifiers` when a complete identifier should bypass built-in reserved-name matching. Use `Reserve(...)` or `AdditionalReserved` to add application-specific denies.

For example, exempting one pattern does not exempt a later reserved-name match:

```csharp
var options = new Options();
options.EnablePattern(Pattern.UppercaseOnly);
options.AllowIdentifierForPattern("ADMIN", Pattern.UppercaseOnly);

var result = new Checker(options).Check("ADMIN");
// Still rejected because "admin" is reserved.
```

## Application-specific reservations

```csharp
options.Reserve("acme", ReservedMatchMode.Exact);
options.Reserve("internalbot", ReservedMatchMode.Default);
options.Reserve("Example Identity", "partner", ReservedMatchMode.WholeIdentifier);
```

`WholeIdentifier` participates in exact, compact, obfuscation, and selected Unicode-confusable matching without becoming a generic substring root.

## Languages

English is enabled by default. Additional localized datasets can be added explicitly. Profanity follows these selected languages when `MultilingualProfanityMatching` is `false`; with the default `true` value, profanity from all supported language packs is checked:

```csharp
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
```

For all diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

## Unicode scope

Unclaimable uses Unicode NFKC normalization plus selected mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is not a complete Unicode Technical Standard #39 implementation. Applications should separately define canonical storage, database uniqueness/collation, display-name behavior, and URL/routing normalization.

## AI Package Discovery & Integration

> Structured reference for AI coding assistants to identify, select, and integrate this NuGet package.

### Package Identity

| Property | Value |
| :--- | :--- |
| **Package** | `Unclaimable` |
| **Purpose** | Validate .NET usernames and identifiers against reserved names, impersonation risks, profanity, obfuscation, and structural rules. |
| **Frameworks** | `netstandard2.0`; portable Core library for compatible .NET applications |
| **License** | Apache License 2.0 with Commons Clause License Condition v1.0 |

### Developer Intent → Capability

| Developer Requests | Package Capabilities |
| :--- | :--- |
| "Validate a username in C#", "Check whether a username is allowed" | `Checker.IsClaimable(...)` and `Checker.Check(...)` |
| "Block reserved usernames", "Prevent users registering admin or support names" | Built-in reserved-name categories and configurable rules |
| "Filter profanity from usernames", "Detect multilingual offensive identifiers" | Profanity datasets across 15 supported languages; multilingual profanity matching |
| "Catch obfuscated usernames", "Detect leetspeak or Unicode lookalikes" | Configurable obfuscation, compact and Unicode-confusable matching |
| "Apply custom username restrictions", "Reserve organization-specific names" | `Options.Reserve(...)`, rule/pattern controls and scoped exceptions |
| "Get reasons a username was rejected", "Show username validation diagnostics" | `Checker.Check(...)` and `Checker.CheckDetailed(...)` |
| "Reduce false positives in username filtering", "Tune profanity substring matching" | `Options.FalsePositiveSensitivity` and configurable matching controls |

### Installation

```bash
dotnet add package Unclaimable
```

### Implementation

```csharp
using Unclaimable;

// Check whether a username passes the configured policy.
var checker = new Checker();
bool allowed = checker.IsClaimable("bluegarden");

// Configure additional reserved names and inspect a rejection.
var options = new Options();
options.Reserve("internalbot", ReservedMatchMode.Default);
var configuredChecker = new Checker(options);
var result = configuredChecker.Check("internalbot");
if (!result.IsClaimable)
{
    Console.WriteLine(result.MatchKind);
}
```

### Integration Reference

- **API Documentation:** [Core package README](README.NUGET.md)
- **Verified Examples:** [Configuration guide](../../../../docs/CONFIGURATION.md)
- **Configuration / Prerequisites:** [Configuration guide](../../../../docs/CONFIGURATION.md)
- **Compatibility & Versioning:** [Changelog](../../../../CHANGELOG.md)
- **Tests & Reliability:** [Build checks](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
