# Unclaimable

Fast, dependency-free reserved username and identifier validation for .NET.

**Package version: 0.7.8**

## Install

```bash
dotnet add package Unclaimable --version 0.7.8
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate7");
if (result.IsClaimable)
{
    // Continue with your application's availability/uniqueness check.
}
```

`null` is accepted so required-field validation can remain a separate concern.

## What this package protects

The default policy combines:

- 23 built-in reserved-name categories;
- English reserved-name data by default, with additional localized datasets available;
- exact and compact matching;
- curated partial matching;
- obfuscation/leetspeak matching;
- selected Unicode-confusable matching;
- profanity checks;
- structural username rules;
- numeric-only, repeated-pattern, symbol-only, and ASCII-art checks.

Passing or disabling one rule never positively clears an identifier through the rest of the pipeline.

## Repeated-pattern threshold

`Pattern.Repeated` detects repeated spans anywhere inside an identifier. The minimum repeated span defaults to four Unicode text elements:

```csharp
var options = new Options
{
    RepeatedPatternMinimumLength = 4
};

var checker = new Checker(options);
```

Raise the value when an application intentionally permits shorter repetition. The minimum supported setting is `2`.

## Configure categories

All built-in categories are enabled by default.

```csharp
var options = new Options();

options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);

var checker = new Checker(options);
```

A disabled category can be enabled again with `EnableCategory(...)`.

## Exact exceptions

Allow a legitimate complete identifier without disabling an entire category:

```csharp
options.AllowedIdentifiers.Add("supportive");
```

Structural validation and explicit application reservations still apply.

## Application-specific reservations

```csharp
options.Reserve("acme", ReservedMatchMode.Exact);
options.Reserve("internalbot", ReservedMatchMode.Default);
options.Reserve("Example Identity", "partner", ReservedMatchMode.WholeIdentifier);
```

`WholeIdentifier` participates in exact, compact, obfuscation, and selected Unicode-confusable matching without becoming a generic substring root.

## Optional rules

Optional identity and geography rules are disabled by default. Enable only what your application needs:

```csharp
options.EnableRule(
    Rule.CountryNames |
    Rule.PopularCityNames |
    Rule.CelebrityNames |
    Rule.Nationalities |
    Rule.Currencies |
    Rule.Religions |
    Rule.Landmarks |
    Rule.Events |
    Rule.Awards |
    Rule.FictionalCharacters |
    Rule.Franchises |
    Rule.Professions |
    Rule.Military);
```

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
