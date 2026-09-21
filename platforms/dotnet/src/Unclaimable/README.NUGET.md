# Unclaimable

Fast, dependency-free reserved username and identifier validation for .NET.

**Package version: 0.8.0**

## Install

```bash
dotnet add package Unclaimable --version 0.8.0
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

English is enabled by default. Additional localized datasets can be added explicitly:

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
