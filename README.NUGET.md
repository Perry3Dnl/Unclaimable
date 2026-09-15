# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current version: 0.6.0**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, compact matching, curated partial matching, obfuscation detection, selected Unicode lookalikes, localized filtering, category controls, exact exceptions, and application-specific rules.

> **Security boundary:** `UnicodeConfusableMatching` is a selected mapping, not complete Unicode UTS #39 protection. Obfuscation expansion is deterministic but capped at 32 candidates per identifier. `MinimumLength` and `MaximumLength` use .NET UTF-16 code units (`string.Length`), not Unicode scalar values or grapheme clusters.

## Why 0.6.0 exists

0.6.0 is a response to production-oriented feedback rather than a bulk feature or dataset release.

The main issue found in 0.5.0 was false positives from generic built-in substring matching. Short protected terms could reject unrelated words merely because the protected text appeared inside them. Examples included values such as `supportive`, `helpful`, `apples`, `nikee`, `badminton`, `stafford`, and `rooted`.

0.6.0 changes that behavior deliberately:

- `Strictness.Strict` still enables partial matching;
- built-in entries must now be explicitly marked as safe for partial matching before they enter the substring index;
- exact, compact, obfuscation, Unicode-confusable, and structural protections remain independent;
- application-defined `AdditionalReserved` and `Reserve(..., Default)` keep their existing configured matching behavior;
- the built-in reserved vocabulary is not bulk-expanded in this release;
- CI now includes a substantially expanded checked-in known-safe username corpus to prevent this class of regression from returning unnoticed.

Exact reserved identifiers such as `support`, `apple`, `nike`, `admin`, and `root` remain reserved. Curated high-risk compounds such as `superadmin`, `systemadministrator`, `customersupport`, `passwordreset`, and generated authentication-service identities can still participate in partial matching.

This is an intentional default-policy relaxation. Consumers upgrading from 0.5.0 should review it if they relied on generic built-in substring blocking.

## Install

```bash
dotnet add package Unclaimable --version 0.6.0
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.6.0
```

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

`null` is rejected as `MatchKind.MissingValue`. `[ClaimableUsername]` applies the same rule, so a separate `[Required]` attribute is not needed merely to prevent a null identifier.

## Default protection

The default policy includes:

- missing (`null`) identifier rejection;
- all 22 built-in categories;
- English localized data;
- exact reserved-name matching;
- compact punctuation/separator matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and bounded obfuscation detection;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only, control-character, and format-character protection;
- length `3` through `32`, measured in UTF-16 code units;
- numbers rejected;
- whitespace rejected;
- built-in `-` and `_` blocked;
- leading and trailing separator restrictions.

`IsReserved` therefore means “this value cannot be claimed under this checker,” not only “this literal exists in a reserved-name dataset.”

## Dataset coverage and governance

0.6.0 keeps the same built-in vocabulary cardinality as 0.5.0: **10,731 filter entries across 22 categories**, representing **10,633 unique values within those categories**. Selected entries changed matching-policy metadata rather than adding another large vocabulary batch.

Matching rules can reject additional variants without storing every possible spelling.

Dataset edits are treated as consumer-visible policy changes. The repository documents provenance/curation requirements, requires external data to have clear redistribution rights, checks every short reserved token against the known-safe corpus, and reports dataset behavior deltas in CI. See the repository's `DATASET_POLICY.md` for the complete contribution and review policy.

## Language support

English is enabled by default. Additional localized datasets are available for Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

```csharp
var options = new Options();
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);

var checker = new Checker(options);
```

## Configuration

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AddLanguage(Language.Dutch);

    options.MinimumLength = 4;
    options.MaximumLength = 24;

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

Every built-in category remains enabled unless explicitly disabled. Category selection applies before exact, compact, partial, Unicode-confusable, and obfuscation indexes are built.

`AllowedIdentifiers` is an exact normalized exception for built-in matching. Structural validation still runs first, and explicit application reservations still win.

`ReservedMatchMode.Exact` performs whole-identifier matching after trim → NFKC → invariant lowercase. `ReservedMatchMode.Default` uses the configured matching pipeline. `AdditionalReserved` remains compatible with its existing behavior.

### Length semantics

`MinimumLength` and `MaximumLength` intentionally use `string.Length`, so thresholds are measured in **UTF-16 code units**. A supplementary-plane Unicode scalar occupies two code units, and a user-perceived grapheme can contain multiple scalars/code units.

0.6.0 preserves this behavior for compatibility. Applications whose product requirements are expressed in Unicode scalar values or grapheme clusters should apply that separate length rule before or alongside Unclaimable. Changing Unclaimable's length unit would require a separately reviewed compatibility change.

## Partial matching in 0.6.0

Built-in dataset entries now separate two decisions:

```text
Reserve this complete identifier?                 yes/no
Reject it whenever it appears inside another one? explicit opt-in
```

Schema-v2 `partialValues` and generated combinations with `"partial": true` are eligible for built-in substring matching. Ordinary `values` are still protected through exact and the other configured matching paths but are not automatically substring rules.

`PartialMatchMinimumLength` still applies after eligibility. It no longer acts as the only protection against false positives.

Generic profanity substring matching remains separately opt-in through `ProfanityPartialMatching`.

## Obfuscation boundary

Common substitutions such as `0 -> o`, `1 -> i/l`, `3 -> e`, `4 -> a`, `@ -> a`, and `$ -> s` are expanded deterministically.

Expansion is deliberately capped at **32 candidates per identifier** to bound combinatorial work. When an input contains enough multi-way ambiguous substitutions to exceed that cap, later candidate branches are not guaranteed to be checked. The cap is deterministic and regression-tested, but the obfuscation matcher should be understood as a bounded heuristic rather than an exhaustive decoder of every possible substitution combination.

## Unicode scope

Matching uses Unicode NFKC normalization and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is **not a complete Unicode Technical Standard #39 confusable implementation**. Passing Unclaimable does not prove that an identifier contains no possible Unicode spoofing technique. `UnicodeConfusableMatching = true` therefore means “enable Unclaimable's selected confusable mapping,” not “perform complete Unicode anti-spoofing.”

Applications should separately define their canonical username model, including case sensitivity, storage normalization, database uniqueness/collation, display-name behavior, and URL/routing normalization.

Unclaimable is a defense-in-depth policy and reserved-name library, not a complete anti-impersonation or identity system.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.OriginalMatchStartIndex);
Console.WriteLine(result.OriginalMatchLength);
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

`[Required]` can still be useful for your application's normal DataAnnotations semantics and message conventions, but `[ClaimableUsername]` now rejects `null` on its own as well.

## Release quality

0.6.0 now includes a 250+ known-safe regression corpus spanning personal-name patterns, ordinary compounds, project/company-style identifiers, gaming/developer handles, multilingual names, and Unicode identifiers. CI also performs systematic short-token collision checks, emits dataset behavior diffs for review, validates the documented 32-candidate obfuscation boundary and UTF-16 length semantics, validates NuGet public API compatibility against published 0.5.0, pins GitHub Actions to immutable commit SHAs, and uses Dependabot to maintain those pins.

The repository also includes `SECURITY.md` for private vulnerability reporting and `DATASET_POLICY.md` for provenance, licensing, curation, false-positive handling, and dataset-review requirements.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable