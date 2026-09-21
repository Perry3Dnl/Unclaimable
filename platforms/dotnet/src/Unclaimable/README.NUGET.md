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

## Strict defaults in 0.8.0

0.8.0 makes every built-in Core `Rule` and every built-in `Pattern` check enabled by default. This includes number rejection, geography and protected-identity lists, and uppercase-only detection.

Relax only the checks your application intentionally permits:

```csharp
var options = new Options();

options.DisableRule(Rule.Numbers | Rule.CountryNames);
options.DisablePattern(Pattern.UppercaseOnly);

var checker = new Checker(options);
```

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

## Repeated-pattern detection

`Pattern.Repeated` scans from every Unicode text-element offset instead of relying on one chunk alignment. The default minimum repeated span is four text elements.

With the default threshold, examples such as `dddd`, `asas`, `sasasasasasasasas`, `sasasasasasasasasa`, and embedded runs such as `useraaaa12` are rejected.

```csharp
var options = new Options
{
    RepeatedPatternMinimumLength = 4
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
- length, whitespace, separator, blocked-character, and number rules;
- country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military identity rules;
- numeric-only, repeated-pattern, symbol-only, ASCII-art, and uppercase-only pattern checks.

## Configure categories

All built-in categories are enabled by default.

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

A disabled category can be enabled again with `EnableCategory(...)`.

## Configure rules and patterns

All built-in rules and patterns start enabled in 0.8.0.

```csharp
options.DisableRule(
    Rule.Numbers |
    Rule.CountryNames |
    Rule.PopularCityNames |
    Rule.CelebrityNames);

options.DisablePattern(Pattern.UppercaseOnly);
```

Use `EnableRule(...)` or `EnablePattern(...)` to turn a disabled check back on.

## Exact exceptions

Allow a legitimate complete built-in reserved identifier without disabling an entire category:

```csharp
options.AllowedIdentifiers.Add("supportive");
```

Structural validation, pattern checks, protected-identity rules, and explicit application reservations still apply.

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
