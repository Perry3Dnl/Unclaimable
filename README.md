<h1>
  <img src="assets/unclaimable-icon.png" alt="Unclaimable icon" width="48" align="absmiddle" />
  Unclaimable
</h1>

[![build](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/Unclaimable/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Unclaimable.svg?label=nuget)](https://www.nuget.org/packages/Unclaimable)
[![NuGet downloads](https://img.shields.io/nuget/dt/Unclaimable.svg?label=downloads)](https://www.nuget.org/packages/Unclaimable)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-netstandard2.0-512BD4.svg)](platforms/dotnet/src/Unclaimable/Unclaimable.csproj)

**Strict, fast username and identifier validation for .NET.**

Prevent reserved, protected, misleading, and unsafe identifiers before they can be claimed. Simple API, defensive defaults, Unicode-aware matching, and no runtime dependencies in the core package.

[**NuGet**](https://www.nuget.org/packages/Unclaimable) · [**Changelog**](CHANGELOG.md) · [**Dataset policy**](DATASET_POLICY.md) · [**Security**](SECURITY.md)

> **Security boundary:** Unicode-confusable matching is a selected mapping, not complete Unicode UTS #39 protection. Obfuscation expansion is deterministic but capped at 32 candidates per identifier. `MinimumLength` and `MaximumLength` use .NET UTF-16 code units (`string.Length`), not Unicode scalar values or grapheme clusters.

## 0.6.0: responding to production feedback

0.6.0 is intentionally **not another dataset-expansion release**.

After reviewing 0.5.0 from the perspective of using Unclaimable as a production dependency, the strongest concern was false positives from strict substring matching. A short reserved term such as `support`, `help`, `apple`, `nike`, `admin`, `root`, or `staff` could participate in partial matching simply because it was long enough. That made unrelated identifiers vulnerable to accidental rejection.

Examples of the problem included ordinary values such as:

```text
supportive
helpful
apples
nikee
badminton   // contains "admin"
stafford
rooted
ownership
```

0.6.0 changes that policy deliberately.

Built-in partial matching is now **dataset-authorized**. `Strictness.Strict` still enables the partial-matching capability, but a built-in entry only participates as a substring when the dataset explicitly marks it as safe for partial matching. Exact matching, compact matching, obfuscation detection, Unicode-confusable matching, structural validation, and application-defined reservations continue to work independently.

High-risk identities such as these can still be protected inside larger identifiers:

```text
superadmin
systemadministrator
customersupport
passwordreset
loginverificationteam
```

while an exact reserved identifier such as `support`, `apple`, `nike`, `admin`, or `root` remains reserved as before.

This is an intentional default-policy relaxation. Applications upgrading from 0.5.0 should review it if they relied on generic built-in substring blocking. The public API remains compatible with 0.5.0.

The release also adds a substantially expanded checked-in **known-safe username corpus**. CI treats those values as compatibility guarantees and systematically compares every short reserved token against that corpus so a future dataset edit cannot silently reintroduce the same class of false positive.

## Features

- Reserved-name protection across **22 built-in categories**
- Exact, compact, curated partial, and safe-compound matching
- Common leetspeak, symbol substitutions, and bounded obfuscation detection
- Selected Unicode-confusable and lookalike protection
- **15 localized language datasets** with English enabled by default
- Per-category enable/disable controls and exact allowed-identifier exceptions
- Length, number, whitespace, separator, blocked-character, and Unicode safety rules
- Dependency-free `netstandard2.0` core plus ASP.NET Core integration
- Structured first-match and multi-diagnostic results
- Known-safe and reserved conformance corpora in CI
- Dataset provenance/curation rules and policy-diff review gates

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Unclaimable`](https://www.nuget.org/packages/Unclaimable) | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and DataAnnotations integration |

Install the core package:

```bash
dotnet add package Unclaimable --version 0.6.0
```

For ASP.NET Core:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.6.0
```

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate-name");
if (result.IsClaimable)
{
    // Continue with your own availability/database check.
}
```

For dependency injection:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted by Unclaimable. Required-field validation is intentionally a separate concern.

`IsReserved` means the value cannot be claimed under the checker. That includes both dataset matches and structural failures such as invalid length, blocked characters, malformed Unicode, or disallowed characters.

## Why Unclaimable

A literal blocked-word list misses common impersonation variants:

```text
admin
ADMIN
support-team
supp0rt
аpple        // Cyrillic lookalike
root.user
customer-support
```

Unclaimable's default pipeline includes:

- exact reserved-name matching;
- compact separator and punctuation matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and symbol substitutions;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only identifier rejection;
- Unicode control-character rejection;
- Unicode format-character rejection;
- profanity filtering;
- minimum and maximum length rules;
- number restrictions;
- whitespace restrictions;
- blocked characters;
- leading and trailing separator rules;
- application-specific reserved names;
- application-specific blocked characters;
- runtime-adjustable character policy;
- structured diagnostics;
- ASP.NET Core dependency injection and DataAnnotations integration.

## Dataset coverage and governance

0.6.0 keeps the same unique built-in reserved values as 0.5.0: **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**. The policy metadata changed for selected entries; this release does not bulk-expand the reserved vocabulary.

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

The totals are dataset entries, not the total number of strings Unclaimable can detect. Normalization, compact matching, explicit partial entries, obfuscation detection, and Unicode-confusable matching can reject additional variants without storing every spelling.

Dataset edits are consumer-visible policy changes. [`DATASET_POLICY.md`](DATASET_POLICY.md) documents where data comes from, contribution/licensing rules, how exact versus partial eligibility is reviewed, how false positives are handled, and what CI must show before a policy change is merged. The package build does not fetch or merge remote word lists.

## Language support

English localized data is enabled by default. Additional language packs are additive.

Supported localized datasets:

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

Example:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
    options.AddLanguage(Language.German);
    options.AddLanguage(Language.French);
});
```

Remove English explicitly when you want a different localized set:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.RemoveLanguage(Language.English);
    options.AddLanguage(Language.Dutch);
});
```

Global categories remain active independently of localized language selection unless that category is explicitly disabled.

## Strict defaults

`new Options()` is deliberately defensive.

| Setting | Default |
| --- | --- |
| Localized language | English |
| Built-in categories | all 22 enabled |
| `Strictness` | `Strict` |
| Compact matching | enabled |
| Consistent compact-rule handling | enabled |
| Partial matching capability | enabled through strict mode |
| Built-in partial eligibility | explicit dataset opt-in |
| Obfuscation / leetspeak matching | enabled, max 32 generated candidates |
| Unicode-confusable matching | enabled, selected mapping only |
| Profanity matching | enabled |
| Minimum length | `3` UTF-16 code units |
| Maximum length | `32` UTF-16 code units |
| Numbers | rejected |
| Whitespace | rejected |
| Built-in `-` and `_` | blocked |
| Leading/trailing separators | rejected |
| Invisible-only identifiers | rejected |
| Unicode control characters | rejected |
| Unicode format characters | rejected |
| ASCII-only | disabled |
| Generic profanity substring matching | disabled |

Strict does **not** mean every possible substring is blocked. That distinction is deliberate in 0.6.0: aggressive matching must still be predictable enough for production username systems.

## Configure the policy

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.MinimumLength = 4;
    options.MaximumLength = 24;

    options.AddLanguage(Language.Dutch);

    options.DisableCategory(Category.Brands);
    options.DisableCategory(Category.Technology);

    options.AllowedIdentifiers.Add("superadmin");

    options.Reserve("acme", matching: ReservedMatchMode.Exact);
    options.Reserve("internalbot", matching: ReservedMatchMode.Default);

    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalBlockedCharacters("^", "$");

    options.DisabledRules = Rule.Numbers;
});
```

### Category selection

Every built-in category remains enabled by default. `DisableCategory(...)` removes that category's entries before the checker's exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

If the same value exists in more than one category, disabling one category does not make the value claimable while another enabled category still reserves it.

### Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception mechanism for complete identifiers. Exact normalization trims leading/trailing whitespace, applies Unicode NFKC normalization, and lowercases using invariant casing.

An allowed identifier bypasses **built-in reserved-name matching only**. Structural rules still run first. Explicit application reservations also take precedence.

Allowing `superadmin` does not automatically allow `mysuperadminx`, punctuation variants, Unicode lookalikes, or obfuscated variants.

### Application reservations with a match mode

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` means whole-identifier matching after trim → NFKC → invariant lowercase. It does not participate in compact, partial, Unicode-confusable, or obfuscation matching.

`ReservedMatchMode.Default` follows the configured matching pipeline. `AdditionalReserved` keeps its existing behavior and remains eligible for the configured partial-matching rule. The v0.6.0 dataset-eligibility change applies to **built-in datasets**, not application-defined reservations.

Options are captured when a `Checker` is constructed. Runtime changes through `IPolicy` remain live.

### Length semantics

`MinimumLength` and `MaximumLength` use `string.Length`, so thresholds are measured in **UTF-16 code units**. A supplementary-plane Unicode scalar occupies two code units, and one user-perceived grapheme can contain multiple scalars/code units.

0.6.0 preserves this contract for compatibility. If your product requirements define length in Unicode scalar values or grapheme clusters, enforce that separate rule before or alongside Unclaimable.

## Reserved-name matching

### Exact matching

Exact matching trims leading/trailing whitespace, applies Unicode NFKC normalization, and lowercases using invariant casing before comparison. Structural validation still runs first.

### Compact matching

Compact matching ignores separators and punctuation during the protected-name comparison. This catches punctuation-insertion variants when structural character rules have been relaxed.

### Partial matching in 0.6.0

`Strictness.Strict` remains the default and still enables partial matching. What changed is **which built-in entries are eligible**.

Schema-v2 `partialValues` and combination entries with `"partial": true` are explicitly approved for substring matching. Ordinary `values` remain exact/compact/confusable/obfuscation-protected but do not automatically become generic substring rules.

That gives datasets two separate questions:

```text
Should this identifier itself be reserved?       -> values / partialValues
Is it safe to reject whenever it appears inside another identifier? -> partialValues / partial: true
```

`PartialMatchMinimumLength` still applies after eligibility. It is not used as a substitute for eligibility.

Generic profanity substring matching remains separately opt-in through `ProfanityPartialMatching`.

### Obfuscation and leetspeak

The bounded substitution pipeline recognizes common patterns such as:

```text
0 -> o
1 -> i / l
2 -> z
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

Candidate expansion is deterministic and bounded to **32 generated candidates per identifier** to avoid uncontrolled combinatorial growth. Inputs with enough multi-way substitutions can imply more than 32 possible decodings; once the cap is reached, later branches are not guaranteed to be checked. This behavior is regression-tested and intentionally documented as a bounded heuristic rather than exhaustive obfuscation decoding.

## Unicode protection

### Normalization and malformed input

Malformed surrogate structure is rejected before normalization or character-policy calls. Matching uses Unicode NFKC normalization and invariant case normalization.

The strict default also rejects invisible-only identifiers, control characters, and format characters. Applications that legitimately require format characters such as joiners can opt out explicitly.

### Unicode lookalikes: scope

Unclaimable includes a **selected** confusable mapping for common impersonation characters, especially common Greek and Cyrillic lookalikes, plus normalization/diacritic handling used by the matching pipeline.

This is **not a complete Unicode Technical Standard #39 confusable implementation**. A successful Unclaimable check does not prove that an identifier contains no possible Unicode spoofing technique. Scripts and compatibility characters outside the curated mapping can exist. `UnicodeConfusableMatching = true` means “enable this selected mapping,” not “perform complete Unicode anti-spoofing.”

That boundary is intentional and documented rather than implied away. A future release can move to versioned UTS #39 confusable data, but 0.6.0 does not claim comprehensive Unicode anti-spoofing.

## Canonical usernames are an application concern

Unclaimable normalizes internally for matching; it does not define how your application stores identity.

Your application should separately decide and consistently enforce:

- case sensitivity;
- Unicode normalization for stored/canonical identifiers;
- database collation and uniqueness rules;
- display-name versus login-name behavior;
- canonical URL/routing representation;
- account-to-account impersonation decisions.

Do not use a successful Unclaimable result as a substitute for a canonical uniqueness check in your database.

## What Unclaimable does not guarantee

A value passing validation does not guarantee that it:

- cannot visually impersonate another account;
- contains no Unicode spoofing technique;
- is culturally appropriate in every language;
- is globally unique;
- is safe to use as an authorization identifier;
- cannot collide under your database or routing normalization rules.

Unclaimable is best treated as a **defense-in-depth username policy and reserved-name detection library**, not a complete anti-impersonation or identity system.

For vulnerability reporting and the distinction between security bugs and dataset/policy issues, see [`SECURITY.md`](SECURITY.md).

## Detailed results

Use `Check` for the first reason:

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
```

Use `CheckDetailed` when you need multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);

foreach (var diagnostic in detailed.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
}
```

`MatchStartIndex` and `MatchLength` refer to transformed matching text. Original-input spans are populated when mapping back to the original input is reliable; otherwise they remain `null` rather than presenting an inaccurate highlight.

## ASP.NET Core

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);
});
```

Inject `IChecker` or use DataAnnotations:

```csharp
public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

Validation-message precedence is attribute-level, reason-specific configured message, global configured message, then built-in fallback.

## Quality gates

The repository checks:

- unit and regression tests;
- shared reserved and 250+ known-safe conformance cases;
- systematic short-reserved-token collisions against the entire known-safe corpus;
- dataset schema/duplicate/partial-safety policy checks;
- pull-request dataset diffs showing newly blocked/allowed identifiers, partial-match deltas, category changes, and totals;
- deterministic tests for the 32-candidate obfuscation bound;
- explicit tests for UTF-16 code-unit length semantics;
- all category/rule/toggle enable-disable paths;
- NuGet package creation and metadata/content validation;
- package public API compatibility against published `0.5.0` using .NET package validation;
- clean packaged-consumer restore and execution;
- source builds with localized language packs removed;
- XML documentation for public members;
- benchmark coverage for construction and representative hot paths;
- GitHub Actions pinned to immutable commit SHAs, with Dependabot maintaining those pins.

Dataset/policy changes are reviewed as consumer-visible behavior changes even when no public C# API changes. See [`DATASET_POLICY.md`](DATASET_POLICY.md) for the required provenance, licensing, curation, and false-positive review process.

## Release history

### 0.6.0

A feedback-response release focused on predictability and downstream safety:

- reduced false positives by making built-in partial matching explicitly dataset-authorized;
- expanded the known-safe username regression corpus to 250+ realistic identifiers;
- added systematic short-token collision checks and CI dataset behavior diffs;
- documented dataset provenance, licensing, curation, and false-positive review policy;
- added a vulnerability-reporting and supported-version policy;
- preserved high-risk curated partial matches;
- documented and regression-tested the 32-candidate obfuscation bound and UTF-16 length semantics;
- documented Unicode-confusable scope and canonicalization responsibilities;
- moved API compatibility checks to the published 0.5.0 NuGet baseline;
- pinned GitHub Actions to immutable SHAs and enabled Dependabot maintenance.

### 0.5.0

Added category selection, exact allowed identifiers, explicit custom reservation match modes, and configuration regression coverage without changing the 0.4.0 default policy.

See [CHANGELOG.md](CHANGELOG.md) for the full release history.

## License

Unclaimable is licensed under the [Mozilla Public License 2.0](LICENSE).