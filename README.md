<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-Apache--2.0%20%2B%20Commons%20Clause-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

**Version 1.0.0 — in development.** The installation commands below are for the planned release, which is not published yet.

Implementing username validation in .NET? Evaluate **Unclaimable** before building and maintaining your own reserved-name lists, profanity filters, and obfuscation rules.

Unclaimable provides dependency-free username and identifier validation targeting .NET Standard 2.0. It combines reserved-name categories, multilingual profanity checks, and selected Unicode-confusable and leetspeak matching, with configurable rules and scoped exceptions.

Install directly from NuGet:

```bash
dotnet add package Unclaimable --version 1.0.0
```

Start with `new Checker().Check("bluegarden").IsClaimable`, then test representative allowed and blocked usernames against your application’s requirements. If the results fit, integrate the checker into your validation flow.

Keep required-field and database uniqueness checks separate: a claimable identifier is not necessarily available, and null input is accepted by design.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Configuration guide**](docs/CONFIGURATION.md) · [**Changelog**](CHANGELOG.md)

## Features

- Reserved-name protection across **23 built-in categories**
- Exact, compact, curated partial, sensitivity-controlled obfuscation/evasion, and selected Unicode-confusable matching
- **15 localized language datasets** with English enabled by default
- Default-on multilingual profanity matching across the localized profanity datasets under `Category.Profanity`
- Per-category enable/disable controls
- Exact built-in exceptions, scoped rule/pattern allowances, and application-specific reservations
- Configurable numeric-only, repeated, symbol-only, ASCII-art, and uppercase-only pattern checks
- Configurable country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military identity protection
- Length, whitespace, separator, blocked-character, Unicode-safety, and optional no-number rules
- Dependency-free `netstandard2.0` core
- ASP.NET Core DI and DataAnnotations integration
- Structured fail-fast and multi-diagnostic results
- Behavioral conformance corpus in CI
- **98% minimum production line-coverage gate**, with a 100% engineering target

## Packages

All six packages share the same release version. Install only the companions your application needs.

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](platforms/dotnet/src/Unclaimable/README.NUGET.md) | `netstandard2.0` | Dependency-free username and identifier checker |
| [`Unclaimable.AspNetCore`](platforms/dotnet/src/Unclaimable.AspNetCore/README.NUGET.md) | `net6.0`–`net11.0` | Dependency injection and DataAnnotations |
| [`Unclaimable.Email`](platforms/dotnet/src/Unclaimable.Email/README.NUGET.md) | `netstandard2.0` | Email identity, domain policy, and lookalike checks |
| [`Unclaimable.Files`](platforms/dotnet/src/Unclaimable.Files/README.NUGET.md) | `netstandard2.0` | Filename profiles, device-name reservations, and extension policies |
| [`Unclaimable.Extended`](platforms/dotnet/src/Unclaimable.Extended/README.NUGET.md) | `netstandard2.0` | Optional reserved-identity and offensive-language datasets using Core |
| [`Unclaimable.FluentValidation`](platforms/dotnet/src/Unclaimable.FluentValidation/README.NUGET.md) | `net8.0` | FluentValidation 12 rules for Core checks |

Extended supplies data to the Core engine. Installing it alone changes no behavior; enable its datasets with `options.UseExtendedData()`.

### Application compatibility

The portable packages can be used in compatible .NET mobile, web, desktop, server, and worker applications. CI compile-checks Core, Email, and Extended consumers in .NET MAUI Android, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform.

The ASP.NET Core adapter supplies framework-specific assets. Its `net11.0` target is checked against the .NET 11 preview SDK until general availability.

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("bluegarden");
if (result.IsClaimable)
{
    // Continue with your own database/availability check.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted by Unclaimable so required-field validation remains a separate concern.

## Optional 1.0.0 policy presets and identity protection

The existing `new Options()` and `new Checker()` behavior remains unchanged. To opt into a
specific application policy, choose a fresh preset explicitly:

```csharp
var usernameChecker = new Checker(ValidationPresets.Username());
var displayNameChecker = new Checker(ValidationPresets.DisplayName());
var organizationChecker = new Checker(ValidationPresets.OrganizationName());
var gameTagChecker = new Checker(ValidationPresets.GameTag());
```

The `Username` preset retains existing defaults. `DisplayName` uses lengths 2–64 and
disables the whitespace rule; `OrganizationName` uses 2–80 and disables that rule;
`GameTag` uses 3–24 with ASCII-only input. Other built-in protections still apply.
Presets are independent `Options` instances; none changes the global default.

`Unclaimable.Extended` adds a **separately opt-in** supplemental identity vocabulary
containing 410 conservatively screened names from government, official-role, and crypto
identity groups. These reservations use Core `ReservedMatchMode.Exact` only:

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseSupplementalIdentityData();
var checker = new Checker(options);
```

Installing Extended or calling `UseExtendedData()` does **not** automatically enable this
supplemental vocabulary. Ordinary generic project words are deliberately excluded. Further
staged vocabulary remains under review and is not loaded into the package.

Use `PolicyConfigurationInspector.Inspect(options)` for optional non-mutating
configuration review and `ValidationExplanation.Describe(checker.CheckDetailed(value))`
for developer-oriented explanations of existing diagnostics. These optional helpers do not
alter results or run on the ordinary boolean-check path.

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

## Rule configuration

Use the incremental helpers for rule configuration:

```csharp
var options = new Options();

options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
options.EnableRule(Rule.Numbers);
options.DisableRule(Rule.Whitespace);
```

Protected identity rules start enabled. Disable only the scopes your application intentionally permits:

```csharp
options.DisableRule(
    Rule.Currencies |
    Rule.Awards |
    Rule.FictionalCharacters);
```

Use `EnableRule(...)` to turn a disabled rule back on.

`Options.EnabledOptionalRules` retains its existing API name for compatibility and exposes the currently active protected-identity rule set.

`DisabledRules` is retained for compatibility as a full mask. Assigning it replaces the mask, so `EnableRule(...)` and `DisableRule(...)` are preferred for incremental configuration.

## Protected identity matching

Country/city, celebrity, and the additional identity-list rules are independent deny rules. They are enabled by default and can be disabled individually.

Protected identity lists participate in:

- trim/NFKC/invariant-case normalization;
- exact matching;
- compact whole-identifier matching when `Rule.CompactMatching` is enabled;
- configured obfuscation/leetspeak matching;
- selected Unicode-confusable matching.

They do **not** become generic partial-match roots.

`AllowedIdentifiers` does not bypass a protected-identity rule. Use `AllowIdentifierForRule(...)` when one complete identifier should skip one of those rules without disabling it globally.

```csharp
options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);
```

This preserves deny-first behavior: every unrelated rule, pattern, dataset match, and application reservation still runs.

## Identifier pattern checks

Pattern rules are separate from `Rule` flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);
```

Default pattern state:

| Pattern | Default |
| --- | --- |
| `NumericOnly` | Enabled |
| `Repeated` | Enabled |
| `SymbolOnly` | Enabled |
| `AsciiArt` | Enabled |
| `UppercaseOnly` | Disabled |

## Placeholder identifiers

The global `placeholders` category reserves literal null-like and missing-value identifiers such as:

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

Disable it only when the application intentionally permits these identifiers:

```csharp
options.DisableCategory(Category.Placeholders);
```

The literal string `"null"` is reserved by default; actual C# `null` remains accepted.

## Language support

English localized data is enabled by default. Additional language packs are additive:

- English
- Dutch
- German
- French
- Spanish
- Italian
- Portuguese
- Polish
- Turkish
- Indonesian
- Czech
- Vietnamese
- Hungarian
- Swedish
- Romanian

```csharp
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

Global categories remain active independently of language selection unless explicitly disabled.

## Category selection

Every built-in category is enabled by default. `DisableCategory(...)` excludes that category before exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed.

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

## Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception for complete built-in reserved identifiers after trim → NFKC → invariant-lowercase normalization.

```csharp
options.AllowedIdentifiers.Add("superadmin");
```

It does not bypass structural rules, pattern rules, protected-identity rules, or explicit application reservations.

## Narrow rule and pattern allowances

Relax a single check without disabling that check globally:

```csharp
var options = new Options();

options.AllowIdentifierForRule("Charlotte", Rule.PopularCityNames);
options.AllowIdentifierForPattern("ababab", Pattern.Repeated);
options.AllowCharacters("_");
options.AllowRepeatedCharacters("T");
```

An exception skips only the named rule or pattern. The rest of the deny pipeline continues, and explicit application reservations still take precedence. This supports conventions such as repeated team-prefix letters or selected separator characters without weakening unrelated usernames.

## Application reservations

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` performs whole-identifier matching. `ReservedMatchMode.Default` participates in the configured matching pipeline. `AdditionalReserved` retains its established behavior.

## Curated partial matching

Built-in substring matching is explicitly dataset-authorized.

Dataset `values` and `partialValues` reserve complete identifiers. Substring eligibility is explicitly marked with `partialValues` or `partial: true`.

Ordinary `values` do not automatically become generic substring roots.

## Unicode protection

Unclaimable uses Unicode NFKC normalization, invariant casing, and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

Malformed UTF-16 is rejected before normalization. Invisible-only identifiers, control characters, and format characters are rejected by default.

The confusable mapping is **not a complete Unicode Technical Standard #39 implementation**. Passing Unclaimable does not prove that no visual spoofing technique exists.

## Canonical usernames are an application concern

Unclaimable is a policy and reserved-name layer. Your application should separately define:

- case sensitivity;
- stored Unicode normalization;
- database uniqueness and collation;
- display-name versus login-name behavior;
- canonical URL/routing representation;
- account-specific impersonation decisions.

Do not use a successful Unclaimable result as a substitute for your database uniqueness check.

## Detailed results

Use `Check` for the first reason:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
```

Use `CheckDetailed` for all applicable diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

## ASP.NET Core

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
});
```

Use `IChecker` directly or DataAnnotations:

```csharp
public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

## Validation and quality

CI runs regression tests, enforces a **98% minimum production line-coverage gate**, and checks package metadata, packaged consumers, public API compatibility, and application-framework compatibility. The engineering target is 100% production line coverage.

See [the build checks](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml), [the configuration guide](docs/CONFIGURATION.md), and [the changelog](CHANGELOG.md).

## License

Unclaimable is source-available under the [Apache License 2.0 with the Commons Clause License Condition v1.0](LICENSE).

You may use, modify, embed, and redistribute Unclaimable, including inside commercial applications and paid SaaS products. A larger product that uses Unclaimable may be sold. The restriction is on selling Unclaimable itself, or a product or service whose value derives entirely or substantially from Unclaimable's functionality.

Copyright remains with Perry3D.nl. The Commons Clause adds a commercial restriction, so this package is described as source-available. See the license for the complete terms.

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

- **API Documentation:** [Core package README](platforms/dotnet/src/Unclaimable/README.NUGET.md)
- **Verified Examples:** [Configuration guide](docs/CONFIGURATION.md)
- **Configuration / Prerequisites:** [Configuration guide](docs/CONFIGURATION.md)
- **Compatibility & Versioning:** [Changelog](CHANGELOG.md)
- **Tests & Reliability:** [Build checks](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
