# Matching rules and options

This page defines how every `UnclaimableOptions` rule changes username filtering. The behavior is deterministic: Unclaimable does not use edit distance, probabilistic classification, or broad fuzzy matching.

## Presets

`Strictness` supplies defaults for four configurable matching rules. Exact matching is always enabled.

| `UnclaimableStrictness` | Compact | Obfuscation | Unicode confusables | Partial | Intended use |
| --- | ---: | ---: | ---: | ---: | --- |
| `Basic` | on | off | off | off | low false-positive risk; literal and separator variants only |
| `Standard` | on | on | on | off | balanced signup protection; the default |
| `Strict` | on | on | on | on | stronger impersonation protection, including embedded names |

The following options are independent of `Strictness`: `PartialMatchMinimumLength`, `ProfanityMatching`, `ProfanityPartialMatching`, `AllowNumbers`, `AsciiOnly`, and `AdditionalReserved`.

## Presets and explicit overrides

The preset is evaluated unless a matching property was explicitly set. An explicit `true` or `false` always wins, even when `Strictness` is changed later.

```csharp
var options = new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict,
    PartialMatching = false,       // explicit exception to Strict
    ObfuscationMatching = false    // another explicit exception
};
```

Use `ResetMatchingRuleOverrides()` to forget explicit values for `CompactMatching`, `PartialMatching`, `ObfuscationMatching`, and `UnicodeConfusableMatching`. Those properties then follow the current preset again.

`EnabledRules` returns an `UnclaimableRule` flags value representing the final effective configuration. It always contains `Exact` and may contain `Compact`, `Partial`, `Obfuscation`, `UnicodeConfusables`, `Profanity`, `ProfanityPartial`, `RejectNumbers`, and `AsciiOnly`.

## Fail-fast order

`Check`, `IsReserved`, and `IsClaimable` stop at the first rejection in this order:

1. `AllowNumbers = false` rejects the first Unicode decimal digit.
2. `AsciiOnly = true` rejects the first character outside printable ASCII.
3. Exact matching checks trimmed, Unicode NFKC-normalized, invariant-lowercase input.
4. Compact matching removes non-letter/non-digit characters before lookup.
5. Partial matching searches for an eligible reserved value inside the exact or compact input.
6. Unicode-confusable matching maps the supported lookalikes and repeats the enabled lookup rules.
7. Obfuscation matching expands a bounded set of supported leetspeak/symbol substitutions and repeats the enabled lookup rules.

`CheckDetailed` collects every character-policy failure and also reports the first reserved-name match. It does not change matching semantics.

## Rule reference

| Option/rule | Effect | Example when enabled | Important interaction |
| --- | --- | --- | --- |
| Exact | trims, applies NFKC, lowercases, then performs a dictionary lookup | ` ADMIN ` → `admin` | always enabled |
| `CompactMatching` | removes separators and punctuation before lookup | `customer-service` → `customer service` | controls compact checks used by later Unicode/obfuscation passes too |
| `PartialMatching` | finds a reserved value inside a longer exact or compact value | `old-admin` → `admin` | governed by `PartialMatchMinimumLength` |
| `PartialMatchMinimumLength` | excludes short reserved values from substring searches | default `4` prevents `api` matching `rapid` | does not affect exact or compact lookup |
| `UnicodeConfusableMatching` | normalizes diacritics and selected visual Unicode lookalikes | Cyrillic `аpple` → `apple` | intentionally bounded, not the full Unicode confusables table |
| `ObfuscationMatching` | checks a bounded set of leetspeak/symbol candidates | `N1k3` → `nike` | candidate generation is capped to prevent combinatorial growth |
| `ProfanityMatching` | loads the opt-in profanity dataset into exact, compact, Unicode, and obfuscation indexes | `sh1t` → `shit` | off by default |
| `ProfanityPartialMatching` | includes profanity entries in substring searches | a longer value containing a configured profanity can be rejected | requires both profanity and partial matching; intentionally separate to limit Scunthorpe-style false positives |
| `AllowNumbers = false` | rejects Unicode decimal digits before name normalization | `user2` → `NumbersNotAllowed` | also rejects non-ASCII decimal digits |
| `AsciiOnly = true` | accepts only U+0020 through U+007E | `josé` → `InvalidCharacters` | runs before reserved-name matching; this is not a complete format validator |
| `AdditionalReserved` | adds application-specific entries under category `custom` | `ExampleBrand` → exact custom match | entries participate in every enabled matching rule |

## Profanity interaction

`ProfanityMatching` is intentionally not part of any strictness preset. Enabling strict mode does not enable profanity. Enabling both strict mode and profanity still does not permit profanity substring matching unless `ProfanityPartialMatching` is also enabled.

```csharp
var options = new UnclaimableOptions
{
    Strictness = UnclaimableStrictness.Strict,
    ProfanityMatching = true,
    ProfanityPartialMatching = false
};
```

This configuration catches exact, compact, Unicode-confusable, and obfuscated profanity, while avoiding substring matches inside otherwise ordinary words.

## What these rules do not validate

Unclaimable does not replace application rules for length, allowed punctuation, separator placement, database uniqueness, user-to-user impersonation, rate limiting, or comprehensive multilingual moderation.
