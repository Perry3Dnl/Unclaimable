# Unclaimable

Fast, dependency-free reserved username and identifier validation for .NET.

**Package version: 0.8.5 (release candidate; not published)**

## Install

**Until v0.8.5 is published, install the latest stable v0.8.4 package.** After release, use `--version 0.8.5` or upgrade via your package manager.

```bash
dotnet add package Unclaimable --version 0.8.4
```

## What's new in 0.8.5 (release candidate — not yet published)

- Expanded Core profanity coverage, including Dutch and English reserved vocabulary, and new regression tests for embedded terms.
- Added `Options.FalsePositiveSensitivity`: `Low` (default, previous behavior), `Medium` and `High` for selectively suppressing one-sided short slang/profanity substring false positives. Whole-identifier, Core profanity and hate-speech matches remain protected.
- Optimized large-set matching with prefix indexes and reduced redundant normalization; kept the existing Core API, matching rules and `netstandard2.0` target.
- **Core-only CI benchmark:** checker construction 2.29 → 1.41 ms; no-match 9.61 → 7.04 µs; allocation 1.297 → 1.303 MB (+0.4%). Warm-process comparisons against v0.8.4; actual results depend on configuration and hardware.
- Dataset additions can cause some previously claimable identifiers to be blocked. The new false-positive guard is opt-in above `Low`.

```csharp
var options = new Options
{
    FalsePositiveSensitivity = FalsePositiveSensitivity.Medium
};
var checker = new Checker(options);
```


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

The Core package targets `netstandard2.0` and is intended for portable application code. The 0.8.0 compatibility workflow compile-checks it in .NET MAUI, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform consumers.

No MAUI-, Blazor-, Avalonia-, or Uno-specific adapter package is required for the Core checker; NuGet resolves the portable asset automatically.

## What's new in 0.8.4

- Curated default partial profanity detection now identifies additional embedded forms in larger identifiers. This intentionally rejects some identifiers that 0.8.3 allowed. Existing `Rule.Profanity` and `Rule.PartialMatching` controls still apply.
- A scoped profanity exception no longer hides a different reserved partial match in the same identifier; regression tests cover both match orders.
- Additional diagnostics, malformed-input and per-username benchmark fixtures support release verification.

The Core package remains `netstandard2.0` and has no FluentValidation dependency. Null input is claimable by design; use required-field validation separately.

## What's new in 0.8.3

0.8.3 is a correctness and performance hardening release. It fixes supplementary Unicode combining-mark handling, compatibility-normalized number detection, scoped exception normalization, repeated-run edit accounting, internal reservation-prefix collisions, and normalized duplicate loading.

It also reduces default identity-list false positives. Collision-heavy profession and military terms, short currency codes, selected ambiguous currency words, and short event abbreviations are claimable by default while the identity-rule flags remain enabled.

```csharp
var options = new Options
{
    IncludeHighCollisionIdentityTerms = true
};
```

Enable that switch only when the application intentionally wants the broader identity vocabulary.

The hot path also avoids several unnecessary allocations for ordinary ASCII identifiers, exception lookups, compact matching, and obfuscation substitutions.

## What's new in 0.8.2

0.8.2 hardens the obfuscation engine and adds a configurable sensitivity level:

```csharp
var options = new Options
{
    ObfuscationSensitivity = ObfuscationSensitivity.Medium
};
```

`Medium` is the default. It adds high-confidence visual matching, composed Unicode/leetspeak handling, and tightly bounded repair for curated safe roots when the candidate already contains genuine obfuscation evidence. Ordinary one-edit typos are deliberately left for `High`.

| Sensitivity | Behavior |
| --- | --- |
| `Low` | closest to 0.8.1 behavior; classic leetspeak/confusables, no fuzzy edit matching |
| `Medium` | default; high-confidence visual and composed obfuscation with guarded one-edit repair for curated roots |
| `High` | broader visual forms and one-edit insertion/deletion/substitution/transposition matching for sufficiently long protected values |
| `Extreme` | broadest visual forms and up to two bounded edits for longer protected values |

The new matcher no longer depends on the former 32-candidate expansion cap. It can compose recognized transformations in one path, including selected Unicode confusables, leetspeak, separators, case-aware uppercase `Q` visual forms, and bounded edits.

`Rule.ObfuscationMatching` still controls the feature globally. Disabling it disables the sensitivity layers as well.

## What's new in 0.8.1

0.8.1 significantly expands Unclaimable's profanity protection and introduces configurable multilingual profanity filtering.

Profanity continues to live in the normal localized datasets. English profanity belongs to the English dataset, Dutch profanity to the Dutch dataset, and so on across all 15 supported languages.

### Multilingual profanity

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

### Expanded profanity datasets

0.8.1 adds **474 profanity filter entries** compared with 0.8.0:

- 174 high-confidence explicit, sexual, and anatomical entries across all 15 supported languages.
- 300 additional exact-only profanity and vulgar-expression entries across the smaller language filters.
- 42 curated low-collision explicit terms that support partial matching.

Existing obfuscation matching continues to apply where appropriate, including forms such as `p3n1s`, `b00bs`, and `v4g1n4`.

Short and collision-prone terms remain exact-only unless broader profanity partial matching is explicitly enabled.

### Dataset totals

- **Filter entries:** 11,156 → **11,630**
- **Category-unique values:** 11,039 → **11,404**
- **Profanity entries:** 861 → **1,335**
- **Unique profanity values:** 848 → **1,213**
- **Built-in categories:** remains **23**

Profanity can still be disabled through the existing controls:

```csharp
options.DisableCategory(Category.Profanity);
```

or:

```csharp
options.DisableRule(Rule.Profanity);
```

The expanded profanity datasets are covered by automated tests across all 15 supported languages, including full multilingual matching, restricted multilingual language sets, individual language selection, obfuscation matching, curated partial matching, and profanity opt-outs.

## Strict defaults in 0.8.0

0.8.0 enables every built-in Core identity/protection `Rule` by default except `Rule.Numbers`. The numeric-only, repeated, symbol-only, and ASCII-art patterns are enabled by default; `Pattern.UppercaseOnly` remains opt-in. Mixed alphanumeric and ordinary uppercase identifiers remain allowed unless those stricter checks are explicitly enabled.

Relax only the checks your application intentionally permits:

```csharp
var options = new Options();

options.DisableRule(Rule.CountryNames);
// Uppercase-only rejection remains opt-in:
options.EnablePattern(Pattern.UppercaseOnly);

// Number rejection remains opt-in:
options.EnableRule(Rule.Numbers);

var checker = new Checker(options);
```

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

## Upgrading to 0.8.0

0.8.0 intentionally uses stricter defaults than 0.7.8. Applications should regression-test real identifiers before upgrading.

The main changes are:

- protected country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military rules are enabled by default;
- selected high-trust roots such as `support`, `help`, `admin`, `staff`, `root`, and `owner` participate in curated partial matching;
- direct runs of three identical Unicode text elements are rejected;
- cyclic repeated patterns use a default repeated-span threshold of six.

The stricter policy is paired with narrow exceptions so an application does not need to turn off a protection globally.

### Which exception should I use?

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

## Configure categories

All built-in categories are enabled by default.

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

A disabled category can be enabled again with `EnableCategory(...)`.

## Configure rules and patterns

The numeric-only, repeated, symbol-only, and ASCII-art patterns start enabled in 0.8.0; `Pattern.UppercaseOnly` is opt-in. All built-in rules start enabled except `Rule.Numbers`.

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
