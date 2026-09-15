# Unclaimable

Strict, fast username and identifier validation for .NET.

**Source version prepared for release: 0.7.2**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, compact matching, curated partial matching, obfuscation detection, selected Unicode lookalikes, localized filtering, category controls, application-specific rules, and configurable identifier-pattern checks.

## What's new in 0.7.2

0.7.2 makes the default username policy more practical and adds optional geography protections.

### Numbers are allowed in ordinary usernames by default

`Rule.Numbers` is disabled by default in 0.7.2, so values such as:

```text
user7
john2026
player123
```

can be claimable when no other protection rejects them.

`Pattern.NumericOnly` remains enabled by default, so a value made only of decimal digits is still rejected:

```text
123456789
```

Applications that want the old no-numbers policy can enable it explicitly:

```csharp
options.EnableRule(Rule.Numbers);
```

### Optional country and city rules

Two new rules are available and **disabled by default**:

```csharp
Rule.CountryNames
Rule.PopularCityNames
```

Enable either or both explicitly:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.EnableRule(Rule.CountryNames | Rule.PopularCityNames);
});
```

These are whole-identifier checks, not broad substring filters. When enabled, `france`, `United Kingdom`, `amsterdam`, or `New York` can be rejected, while ordinary compounds such as `francelover`, `newyorker`, and `amsterdammer` remain claimable unless another protection applies.

Compact geography matching follows `Rule.CompactMatching`, so separator/space variants are recognized only when compact matching and the relevant structural configuration allow them.

### New rule configuration helpers

Use `EnableRule(...)` and `DisableRule(...)` for incremental configuration:

```csharp
options.DisableRule(Rule.Whitespace);
options.EnableRule(Rule.Numbers);
```

`DisabledRules` remains available for compatibility, but assigning it replaces the full mask.

## 0.7.1 vocabulary expansion

The staged 0.7.1 dataset sweep expanded exact reserved-name coverage across Authentication, Automation, Commerce, Communications, Community, Developer, Finance, Governance, Identity, Infrastructure, Legal, Moderation, Official, Operations, Security, and English System data.

Representative exact additions include `member`, `vote`, `voting`, `active`, `pending`, `authentication`, `announcement`, `apikey`, `treasury`, `username`, `loadbalancer`, `banned`, `verified`, `operations`, `buyer`, `legalhold`, and `phishing`.

Generic additions remain exact values rather than broad substring roots: `vote` does not block `devote`, `member` does not block `rememberme`, and `active` does not block `hyperactive`.

## Install

After the 0.7.2 release is published:

```bash
dotnet add package Unclaimable --version 0.7.2
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.7.2
```

Until publication, use the current NuGet release or the CI-generated 0.7.2 package artifacts.

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

if (checker.IsClaimable(userName))
{
    // Continue with your own availability/database check.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted so required-field validation can remain separate, for example through `[Required]`.

## Default protection in 0.7.2

The default policy includes:

- all 23 built-in reserved-name categories;
- English localized data;
- exact reserved-name matching;
- compact punctuation/separator matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and bounded obfuscation detection;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only, control-character, and format-character protection;
- minimum length `3` and maximum length `32`;
- whitespace restrictions;
- built-in `-` and `_` blocking;
- leading and trailing separator restrictions;
- numeric-only pattern protection;
- repeated-pattern protection;
- symbol-only pattern protection;
- conservative ASCII-art protection.

The following protections are disabled by default:

- `Rule.Numbers` — mixed alphanumeric identifiers are allowed;
- `Rule.CountryNames`;
- `Rule.PopularCityNames`;
- `Pattern.UppercaseOnly`;
- generic profanity substring matching;
- ASCII-only input restriction.

`IsReserved` means “this value cannot be claimed under this checker,” including structural, pattern, geography, and reserved-name failures.

## Default matrix

| Setting | Default |
| --- | --- |
| Localized language | English |
| Built-in categories | all 23 enabled |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Curated partial matching | enabled through strict mode |
| Obfuscation / leetspeak matching | enabled |
| Unicode-confusable matching | enabled |
| Profanity matching | enabled |
| Minimum length | `3` |
| Maximum length | `32` |
| `Rule.Numbers` | disabled |
| `Rule.CountryNames` | disabled |
| `Rule.PopularCityNames` | disabled |
| Whitespace | rejected |
| Built-in `-` and `_` | blocked |
| Leading/trailing separators | rejected |
| Invisible-only identifiers | rejected |
| Unicode control characters | rejected |
| Unicode format characters | rejected |
| `Pattern.NumericOnly` | enabled |
| `Pattern.Repeated` | enabled |
| `Pattern.SymbolOnly` | enabled |
| `Pattern.AsciiArt` | enabled |
| `Pattern.UppercaseOnly` | disabled |

## Geography rules

Country and city rules are independent and opt-in:

```csharp
var countries = new Options()
    .EnableRule(Rule.CountryNames);

var cities = new Options()
    .EnableRule(Rule.PopularCityNames);
```

Or enable both:

```csharp
var options = new Options()
    .EnableRule(Rule.CountryNames | Rule.PopularCityNames);
```

They use dedicated result reasons:

```csharp
MatchKind.CountryName
MatchKind.PopularCityName
```

Country matching wins for overlaps such as `singapore` when both are enabled.

These opt-in deny rules are not bypassed by `AllowedIdentifiers`. If an application explicitly enables `Rule.CountryNames`, adding `france` to `AllowedIdentifiers` does not positively clear the country-name rule.

## Pattern configuration

Pattern checks are configured independently from `Rule` flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);

var checker = new Checker(options);
```

`Pattern.NumericOnly`, `Pattern.Repeated`, `Pattern.SymbolOnly`, and `Pattern.AsciiArt` are enabled by default. `Pattern.UppercaseOnly` is opt-in.

Pattern checks are deny rules, not allow rules. Disabling one protection never clears an identifier through the remaining checks. For example, disabling `UppercaseOnly` does not make `ADMIN` claimable because the reserved-name pipeline still resolves it to `admin`.

## Placeholder identifiers

The global `placeholders` category protects literal null-like or missing-value identifiers including:

```text
null
undefined
empty
emptyvalue
none
nil
unset
missing
unknown
notset
novalue
nullvalue
undefinedvalue
missingvalue
placeholder
placeholdervalue
defaultvalue
```

Disable the category only when the application intentionally permits these identifiers:

```csharp
options.DisableCategory(Category.Placeholders);
```

The literal string `"null"` is reserved by default; actual C# `null` remains accepted.

## Dataset coverage

0.7.2 carries **11,150 filter entries across 23 categories**, representing **11,039 unique values within those categories**.

These totals describe stored dataset entries. Normalization, compact matching, explicitly authorized partial matching, obfuscation detection, Unicode-confusable matching, structural rules, patterns, and opt-in geography rules can reject additional forms without storing every spelling.

## Language support

English is enabled by default. Additional localized datasets are available for Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);

var checker = new Checker(options);
```

Global categories remain active independently of localized language selection unless their category is explicitly disabled.

## Configuration example

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(Language.Dutch);

    options.DisableCategory(Category.Brands);
    options.DisableCategory(Category.Technology);

    options.EnableRule(Rule.CountryNames);
    options.EnableRule(Rule.Numbers);
    options.DisableRule(Rule.Whitespace);

    options.EnablePattern(Pattern.UppercaseOnly);
    options.DisablePattern(Pattern.AsciiArt);

    options.AllowedIdentifiers.Add("superadmin");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");
});
```

Every built-in category remains enabled unless explicitly disabled. Category selection is applied before exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

`AllowedIdentifiers` is an exact normalized exception for built-in reserved-name matching. Structural validation, pattern checks, opt-in geography checks, and explicit application reservations still take precedence.

`ReservedMatchMode.Exact` performs whole-identifier matching after trim → NFKC → invariant lowercase. `ReservedMatchMode.Default` uses the configured matching pipeline. `AdditionalReserved` retains its established behavior.

## Curated partial matching

Built-in dataset entries distinguish between reserving a complete identifier and safely rejecting the same value inside a larger identifier.

Schema-v2 `partialValues` and generated combinations with `"partial": true` are eligible for built-in substring matching. Ordinary exact `values` remain protected through exact and the other configured matching paths but do not become generic substring rules.

This is why short or ordinary protected roots can remain reserved without causing false positives in unrelated words.

## Unicode scope

Matching uses Unicode NFKC normalization and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is **not a complete Unicode Technical Standard #39 confusable implementation**. Passing Unclaimable does not prove that an identifier contains no possible Unicode spoofing technique.

Applications should separately define canonical username storage, case sensitivity, database uniqueness/collation, display-name behavior, and URL/routing normalization.

Unclaimable is a defense-in-depth policy and reserved-name library, not a complete anti-impersonation or identity system.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
```

For multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

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

Reason-specific messages are available for reserved-name, structural, pattern, country-name, and popular-city-name failures.

## Release quality

The 0.7.2 release candidate is gated by the full unit/regression suite, the shared conformance corpus, public-API package validation, source builds without localized language packs, NuGet package-content validation, and a clean packaged-consumer restore/run.

The publishing workflow remains tag-gated. Preparing version 0.7.2 does not publish it.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
