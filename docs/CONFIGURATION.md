# Configuring Unclaimable v1.0.0

This guide explains how to keep Unclaimable's strict defaults while making small, intentional exceptions for an application's naming rules.

If you only need a working example, start with **Copy-paste recipes**. The later sections explain exactly which checks are skipped, which checks still run, and how the deny-first pipeline behaves.

## Email and integration boundaries

### Existing email addresses versus application-issued addresses

`CheckExistingAddress` handles externally owned mailboxes. Its configured
`IssuingDomains` allowlist is **not** enforced by default, to retain 0.8.3 behavior.
`CheckNewAddress` handles mailboxes issued by the application, and enforces a
configured `IssuingDomains` allowlist. Setting `EmailUsage` overrides the
per-call purpose; leave `EmailUsage.Auto` to preserve existing callers.

To restrict **existing** emails to exactly one approved domain:

```csharp
using Unclaimable.Email;

var options = new EmailOptions
{
    EmailUsage = EmailUsage.ExistingAddress,
    EnforceIssuingDomainsForExistingAddresses = true,
    AllowIssuingDomainSubdomains = false
};
options.IssuingDomains.Add("example.com");
var checker = new EmailChecker(options);
bool allowed = checker.CheckExistingAddress("person@example.com").IsAllowed;
```

Set `AllowIssuingDomainSubdomains = true` to also permit real subdomains.
These controls check configured domain strings; they do **not** authenticate
ownership of the domain, a mailbox, or an enterprise account.

### Relaxed checks for third-party mailboxes

```csharp
var options = new EmailOptions
{
    EmailUsage = EmailUsage.ExistingAddress,
    EmailProtectionLevel = EmailProtectionLevel.Relaxed
};
var checker = new EmailChecker(options);
var result = checker.CheckExistingAddress("support@example.com");
bool allowed = result.IsAllowed;
bool localPartReserved = result.LocalPartResult?.IsReserved == true;
bool domainSuspicious = result.IsSuspiciousDomain;
```

With `Relaxed`, a syntactically valid externally owned address can pass despite
reserved local-part or protected-domain warnings. The warnings remain available.
Syntax failures and an explicitly enforced issuing-domain allowlist still deny.
`Strict` is the default, and issued addresses remain strict regardless of
`EmailProtectionLevel`. During login, validate ownership of the account and
use established authentication/session controls; string validation alone is
never authentication or authorization.

### Email syntax is not proof of deliverability

The package does **not** perform network DNS/MX queries, SMTP probes, mailbox
verification, organizational ownership verification, or account authentication.
Plausible domain syntax and similarity results must not be interpreted as proof
that an address exists or receives email. Use an email ownership verification
flow where needed.

### Null and required identifier fields

Core intentionally treats null as claimable to support optional inputs.
`checker.IsClaimable(null)` is not a required-field validation. Validate
requiredness explicitly:

```csharp
var checker = new Unclaimable.Checker();
bool validForRegistration =
    !string.IsNullOrWhiteSpace(candidate)
    && checker.IsClaimable(candidate);
```

Enforce uniqueness separately in the database, and do not equate
`IsClaimable` with permission to claim an identity.

### Unicode and resource boundaries

Confusable matching is a selective curated set, **not** full Unicode UTS #39
conformance. Combining marks can be collapsed when creating a detection skeleton.
Review internationalized legitimate names for false positives. The bundled
Public Suffix List is a pinned offline snapshot, so suffix policy may become
stale until an updated package is released.

For untrusted inputs, impose request-body and identifier-length limits at
application boundaries. Benchmark adversarial long strings, custom reservation
set growth, and High/Extreme obfuscation independently before claiming
denial-of-service resistance. No DoS vulnerability is established by the
presence of expensive matching paths alone.

## Explain a rejected identifier

Use `Check` for the first rejection reason. `MatchKind` describes how a value failed; `Category` identifies the protected dataset when there is a reserved-name match. They are independent: for example, an obfuscated match can belong to the profanity category.

```csharp
var result = checker.Check(candidate);
if (result.IsReserved)
{
    logger.LogInformation("Username denied: {Kind}, {Category}",
        result.MatchKind, result.Category);

    // Give users a general message; keep matched identities and raw input out of logs.
    return "Choose another username.";
}
```

Use `CheckDetailed(candidate, includeMessages: true)` when a UI or audit view needs every structural and matching diagnostic. Each item exposes `Kind`, `Category`, and an optional `Message`. `MatchStartIndex` and `MatchLength` refer to the transformed matching text; `OriginalMatchStartIndex` and `OriginalMatchLength` identify the original UTF-16 input where mapping is reliable. Do not use transformed offsets to highlight the original string.

```csharp
var detailed = checker.CheckDetailed(candidate, includeMessages: true);
foreach (var diagnostic in detailed.Diagnostics)
{
    logger.LogInformation("Username check: {Kind}, {Category}",
        diagnostic.Kind, diagnostic.Category);
}
```

`IsReserved` and `IsClaimable` are convenient for decisions when no reason is needed. All these methods accept null as claimable; validate required fields and uniqueness separately.

## Copy-paste recipes

### Use the default policy

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check(candidate);
if (result.IsClaimable)
{
    // Continue with your own uniqueness/database check.
}
```

The 0.8.3 default keeps the deny-first pipeline, multilingual profanity behavior, and `ObfuscationSensitivity.Medium`, while reducing false positives from collision-heavy optional identity vocabulary. Every enabled check is a deny check: passing one check never clears the identifier. The value is claimable only when no enabled check rejects it.

### High-collision identity vocabulary

The protected identity rule flags remain enabled by default, but 0.8.3 filters collision-heavy generic terms from the default reservation set. This includes common profession and military terms, short currency codes, selected ambiguous currency words, and short event abbreviations.

Applications that intentionally want the broader identity lists can opt back in:

```csharp
var options = new Options
{
    IncludeHighCollisionIdentityTerms = true
};
```

This is a vocabulary switch, not a rule switch. For example, `Rule.Professions` can remain enabled while its collision-heavy generic terms are omitted from the default checker.

### Add an application-specific reserved name

Use `Reserve` when your application owns a name that users must not claim.

```csharp
var options = new Options();

options.Reserve("billingdesk", ReservedMatchMode.Exact);
options.Reserve("internalbot", ReservedMatchMode.Default);

var checker = new Checker(options);
```

Use `Exact` when only the complete identifier should be reserved. Use `Default` when the reservation should participate in the configured matching pipeline, including partial matching where applicable.

### Allow one built-in reserved identifier

Use `AllowedIdentifiers` when a complete identifier from the built-in reserved-name datasets is legitimate in your application.

```csharp
var options = new Options();

options.AllowedIdentifiers.Add("supportive");

var checker = new Checker(options);
```

This is not a universal bypass. Structural checks, pattern checks, protected-identity rules, and explicit application reservations still run.

### Ignore one rule for one complete identifier

Use `AllowIdentifierForRule` when the value should skip one specific `Rule` but still be checked by everything else.

```csharp
var options = new Options();

options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);

var checker = new Checker(options);
```

If another rule, pattern, reserved-name entry, or application reservation rejects `Charlotte`, it is still denied.

Multiple rules can be supplied:

```csharp
options.AllowIdentifierForRule(
    "legacy_user7",
    Rule.BlockedCharacters | Rule.Numbers);
```

### Ignore one pattern for one complete identifier

Use `AllowIdentifierForPattern` for an exact exception to a shape/pattern check.

```csharp
var options = new Options();

options.AllowIdentifierForPattern(
    "ababab",
    Pattern.Repeated);

var checker = new Checker(options);
```

Only `Pattern.Repeated` is skipped for that identifier. Other patterns and all rules continue normally.

### Allow a character without disabling the whole character rule

The default character policy blocks `-` and `_`. If an application allows underscores, keep the rule enabled and permit only that character:

```csharp
var options = new Options()
    .AllowCharacters("_");

var checker = new Checker(options);
```

This is narrower than:

```csharp
options.DisableRule(Rule.BlockedCharacters);
```

because other blocked characters are still rejected.

### Allow one letter to repeat

Direct runs of three or more identical Unicode text elements are rejected by `Pattern.Repeated` by default.

If a naming convention intentionally uses repeated `T` characters:

```csharp
var options = new Options()
    .AllowRepeatedCharacters("T");

var checker = new Checker(options);
```

This permits direct `T` runs such as `TTT` while keeping other direct runs and cyclic repetition protected.

```text
TTT       -> repetition check can pass
AAAA      -> RepeatedPattern
ababab    -> RepeatedPattern
```

### Team-prefix example

A team requires names such as `TTT_user7`.

```csharp
var options = new Options()
    .AllowCharacters("_")
    .AllowRepeatedCharacters("T");

var checker = new Checker(options);

checker.Check("TTT_user7");   // can pass these two checks
checker.Check("AAA_user7");   // RepeatedPattern
checker.Check("TTT-user7");   // '-' remains blocked
```

"Can pass" is intentional wording: the rest of Unclaimable still runs. For example, if the application enables `Rule.Numbers`, `TTT_user7` is rejected because the digit rule still applies.

### Allow the exception only for one exact team identifier

If repeated `T` and underscore should not be generally allowed, scope the exception to one complete identifier instead:

```csharp
var options = new Options();

options.AllowIdentifierForRule(
    "TTT_user7",
    Rule.BlockedCharacters);

options.AllowIdentifierForPattern(
    "TTT_user7",
    Pattern.Repeated);

var checker = new Checker(options);
```

Other usernames still use the normal blocked-character and repetition defaults.

## The deny-first model

Unclaimable does not have a general "passed" state halfway through validation.

Conceptually:

```text
structural rules
    -> deny if matched

pattern checks
    -> deny if matched

application reservations
    -> deny if matched

built-in reserved data
    -> deny if matched

protected identity rules
    -> deny if matched

no deny found
    -> claimable
```

A scoped exception changes one step from "check this" to "skip this check for this identifier." It does not jump to the end of the pipeline.

That is why this remains safe:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.AllowIdentifierForPattern(
    "ADMIN",
    Pattern.UppercaseOnly);

var result = new Checker(options).Check("ADMIN");
```

The uppercase-only pattern is skipped, but `ADMIN` still resolves to the reserved `admin` identity and is denied.

## Choosing the right customization API

| Goal | API | Scope |
| --- | --- | --- |
| Add a name users must not claim | `Reserve(...)` / `AdditionalReserved` | application-defined deny |
| Allow one complete built-in reserved identifier | `AllowedIdentifiers` | exact built-in dataset exception |
| Skip one `Rule` for one identifier | `AllowIdentifierForRule(...)` | exact identifier + selected rule(s) |
| Skip one `Pattern` for one identifier | `AllowIdentifierForPattern(...)` | exact identifier + selected pattern(s) |
| Permit one character globally | `AllowCharacters(...)` | startup character policy |
| Permit direct repetition of one character | `AllowRepeatedCharacters(...)` | direct-run portion of `Pattern.Repeated` |
| Disable a rule application-wide | `DisableRule(...)` | all identifiers |
| Disable a pattern application-wide | `DisablePattern(...)` | all identifiers |
| Disable a built-in reserved category | `DisableCategory(...)` | all entries in that category |
| Allow one Extended identity | `ExtendedOptions.AllowedIdentifiers` | one Extended registration |

Prefer the narrowest API that expresses the application's actual rule.

## Obfuscation sensitivity in 0.8.2

`Options.ObfuscationSensitivity` controls how aggressively the reserved-name matcher treats visual and typo-style evasions:

```csharp
var options = new Options
{
    ObfuscationSensitivity = ObfuscationSensitivity.Medium
};
```

| Level | Behavior |
| --- | --- |
| `Low` | closest to 0.8.1: classic leetspeak and selected confusables, without fuzzy edit matching |
| `Medium` | default: high-confidence visual/composed obfuscation and guarded one-edit repair for curated safe roots |
| `High` | broader visual forms plus one-edit insertion, deletion, substitution, adjacent transposition, and selected repeated-run/multi-character evasions |
| `Extreme` | broadest visual forms and up to two bounded edits for longer protected values |

Medium intentionally requires real obfuscation evidence before its edit allowance can complete a match. This keeps plain typo-like values such as `pieml` and `piemle` claimable at the default level while still blocking composed evasions such as leetspeak plus a missing character.

The sensitivity value is captured when the `Checker` is constructed, like the other normal `Options` values.

To disable the complete obfuscation layer rather than reducing sensitivity:

```csharp
options.DisableRule(Rule.ObfuscationMatching);
```

That rule remains authoritative over all four sensitivity levels.

## 0.8.1 multilingual profanity behavior

`Category.Profanity` and `Rule.Profanity` remain enabled by default. The expanded explicit, sexual, and anatomical terms are stored in their normal per-language profanity datasets.

`MultilingualProfanityMatching` is enabled by default. This causes profanity datasets from all supported languages to participate even when those languages are not selected in `Options.Languages`:

```csharp
var options = new Options(); // English selected; multilingual profanity enabled by default.
var checker = new Checker(options);

// English profanity.
checker.Check("penis");

// Dutch profanity is also protected by multilingual mode.
checker.Check("piemel");
checker.Check("godverdomme");
```

To make profanity follow only the selected language packs, disable multilingual profanity matching:

```csharp
var options = new Options
{
    MultilingualProfanityMatching = false
};

// English is selected by default, so English profanity still applies.
// Dutch profanity is not loaded yet.
options.AddLanguage(Language.Dutch); // Now Dutch profanity applies too.
```

This switch affects only profanity language selection. Other localized categories still follow `Options.Languages` normally.

By default, multilingual profanity matching uses all supported languages. Restrict only the additional profanity languages with:

```csharp
options.SetMultilingualProfanityLanguages(
    Language.Dutch,
    Language.German);
```

Languages already selected through `Options.Languages` remain active independently. Calling `SetMultilingualProfanityLanguages()` with no arguments restores the default all-language multilingual profanity set.

The entries use the normal exact, compact, obfuscation/leetspeak, and selected Unicode-confusable pipeline:

```text
penis       -> Profanity
p3n1s       -> Profanity / Obfuscated
boobs       -> Profanity
b00bs       -> Profanity / Obfuscated
vagina      -> Profanity
v4g1n4      -> Profanity / Obfuscated
myp3n1sname -> Profanity / Partial (after obfuscation)
```

Disable the entire profanity category when an application intentionally permits these identifiers:

```csharp
options.DisableCategory(Category.Profanity);
```

Or disable profanity matching through the rule:

```csharp
options.DisableRule(Rule.Profanity);
```

Forty-two low-collision expanded explicit terms are deliberately marked as curated partial roots, so wrapping those terms inside a larger identifier is blocked by default. Collision-prone short terms remain exact-only. `ProfanityPartialMatching` remains opt-in for broad substring matching of ordinary profanity entries.
### Profanity coverage in 0.8.1

The profanity filters now contain the following distinct values per language. These are per-language counts, so the same spelling appearing in more than one language is counted once in each relevant filter.

| Language | Unique profanity values |
| --- | ---: |
| English | 311 |
| Dutch | 130 |
| German | 138 |
| French | 55 |
| Spanish | 55 |
| Italian | 50 |
| Portuguese | 59 |
| Polish | 60 |
| Turkish | 51 |
| Indonesian | 45 |
| Czech | 50 |
| Vietnamese | 59 |
| Hungarian | 53 |
| Swedish | 58 |
| Romanian | 56 |

The eight previously smaller filters received **300 additional exact-only entries** in 0.8.1. Exact-only expansion improves whole-identifier coverage without turning short or common profanity terms into broad substring rules.

## 0.8.0 default behavior

The 0.8.0 baseline is stricter than 0.7.8.

### Rules enabled by default

All built-in rules are enabled except `Rule.Numbers`.

That includes the protected identity lists:

```csharp
Rule.CountryNames
Rule.PopularCityNames
Rule.CelebrityNames
Rule.Nationalities
Rule.Currencies
Rule.Religions
Rule.Landmarks
Rule.Events
Rule.Awards
Rule.FictionalCharacters
Rule.Franchises
Rule.Professions
Rule.Military
```

`Rule.Numbers` stays disabled so ordinary mixed alphanumeric identifiers can remain claimable.

```text
player7     -> numbers alone do not reject it
1234567     -> Pattern.NumericOnly still rejects it
```

### Pattern defaults

Enabled:

```csharp
Pattern.NumericOnly
Pattern.Repeated
Pattern.SymbolOnly
Pattern.AsciiArt
```

Opt-in:

```csharp
Pattern.UppercaseOnly
```

### Repeated-pattern defaults

There are two layers.

Direct runs:

```text
aa       -> below the direct-run limit
aaa      -> RepeatedPattern
dddd     -> RepeatedPattern
```

Cyclic repetition uses `RepeatedPatternMinimumLength`, default `6`:

```text
abab     -> below cyclic threshold
ababab   -> RepeatedPattern
abcabc   -> RepeatedPattern
hahaha   -> RepeatedPattern
```

Change the cyclic threshold when needed:

```csharp
var options = new Options
{
    RepeatedPatternMinimumLength = 8
};
```

Direct three-character runs remain protected independently. Use `AllowRepeatedCharacters(...)` if a particular repeated character is intentional.

### Sensitive partial roots

0.8.0 intentionally treats selected support and privileged-role roots as substring protection:

```text
supportive  -> support -> denied
helpful     -> help    -> denied
badminton   -> admin   -> denied
stafford    -> staff   -> denied
rooted      -> root    -> denied
ownership   -> owner   -> denied
```

These are explicit curated partial roots. Unclaimable does not automatically make every dataset value a generic substring rule.

## Rules, patterns, categories, and datasets are different controls

### Rules

Rules control validation or protected-identity features.

```csharp
options.DisableRule(Rule.PopularCityNames);
options.EnableRule(Rule.Numbers);
```

A rule exception is narrower:

```csharp
options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);
```

### Patterns

Patterns describe suspicious identifier shapes.

```csharp
options.DisablePattern(Pattern.AsciiArt);
options.EnablePattern(Pattern.UppercaseOnly);
```

A pattern exception is narrower:

```csharp
options.AllowIdentifierForPattern(
    "ababab",
    Pattern.Repeated);
```

### Categories

Categories group built-in reserved-name data.

```csharp
options.DisableCategory(Category.Brands);
```

Use `AllowedIdentifiers` when only one complete built-in identifier should be allowed instead of disabling the category.

### Application reservations

Application reservations are explicit denies owned by the consuming application.

```csharp
options.Reserve("internal", ReservedMatchMode.Exact);
options.Reserve("staffportal", "application", ReservedMatchMode.Default);
```

These remain authoritative. Scoped exceptions to unrelated rules or patterns do not remove an application reservation.

## Matching modes for application reservations

### Exact

```csharp
options.Reserve("acme", ReservedMatchMode.Exact);
```

Protects the complete normalized identifier without broadening it to arbitrary compounds.

### Default

```csharp
options.Reserve("internalbot", ReservedMatchMode.Default);
```

Participates in the normal configured matching pipeline, including partial matching where applicable.

### WholeIdentifier

```csharp
options.Reserve(
    "Example Identity",
    "partner",
    ReservedMatchMode.WholeIdentifier);
```

Participates in exact, compact, configured obfuscation, and selected Unicode-confusable matching without becoming a generic partial root.

## Character policy: startup vs runtime

Startup:

```csharp
var options = new Options()
    .AllowCharacters("_");
```

Runtime:

```csharp
var policy = new Policy(
    options.ConfiguredBlockedCharacters,
    options.ConfiguredAllowedCharacters);

var checker = new Checker(options, policy);

policy.BlockCharacter("_");
policy.AllowCharacter("_");
```

The `Checker` captures normal `Options` values at construction time. The supplied `IPolicy` remains live, so runtime character-policy changes affect existing checkers that reference it.

In ASP.NET Core, the registered singleton `IPolicy` is already shared with the registered checker.

## ASP.NET Core

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AllowCharacters("_");
    options.AllowRepeatedCharacters("T");

    options.AllowIdentifierForRule(
        "Charlotte",
        Rule.PopularCityNames);

    options.Reserve(
        "internalbot",
        ReservedMatchMode.Exact);
});
```

Inject `IChecker`:

```csharp
public sealed class AccountService
{
    private readonly IChecker _checker;

    public AccountService(IChecker checker)
    {
        _checker = checker;
    }

    public bool CanClaim(string value) =>
        _checker.IsClaimable(value);
}
```

Or use DataAnnotations:

```csharp
public sealed class SignupModel
{
    [Required]
    [ClaimableUsername]
    public string UserName { get; set; } = string.Empty;
}
```

## Email local-part configuration

`Unclaimable.Email` has its own email syntax policy, but the local-part checker exposes the Core `Options` object through `LocalPartOptions`.

```csharp
var options = new EmailOptions();

options.LocalPartOptions.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);

options.LocalPartOptions.AllowedIdentifiers.Add(
    "examplelocalpart");

var checker = new EmailChecker(options);
```

Email syntax validation remains separate from Core username structural rules.

## Extended data

Extended is opt-in:

```csharp
var options = new Options();

options.UseExtendedData(extended =>
{
    extended.DisableCategory(
        ExtendedCategory.Sports);

    extended.AllowedIdentifiers.Add(
        "Aalborg University");
});
```

Extended entries are registered as explicit whole-identifier reservations. Use `ExtendedOptions.AllowedIdentifiers` while registering Extended data to omit one Extended identity. A Core rule/pattern exception does not erase an explicit Extended reservation after it has been registered.

## Diagnostics

For the first deny reason:

```csharp
var result = checker.Check(candidate);

Console.WriteLine(result.IsReserved);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
Console.WriteLine(result.Category);
```

For all applicable diagnostics:

```csharp
var detailed = checker.CheckDetailed(
    candidate,
    includeMessages: true);

foreach (var diagnostic in detailed.Diagnostics)
{
    Console.WriteLine(
        $"{diagnostic.Kind}: {diagnostic.Message}");
}
```

Use `Check` for normal fail-fast decisions. Use `CheckDetailed` when a UI, audit trail, test, or administration tool needs more context.

## Migration checklist for 0.8.0

Before upgrading an application from 0.7.8:

1. Run its real username/identifier regression corpus against 0.8.0.
2. Review identifiers that now hit protected identity rules because those lists are enabled by default.
3. Review values containing the new curated partial roots: `support`, `help`, `admin`, `staff`, `root`, and `owner`.
4. Review the repeated-pattern policy: direct runs of three are denied; cyclic repetition defaults to six text elements.
5. Prefer scoped exceptions over disabling an entire rule or pattern.
6. Keep application-owned names in `Reserve(...)` / `AdditionalReserved`.
7. For Extended, configure exceptions inside `UseExtendedData(...)`.
8. Re-run tests after every policy change; an exception to one check can expose a later deny reason, which is expected deny-first behavior.

## Common mistakes

### Treating an exception as a global allow

This is incorrect reasoning:

```text
I exempted Pattern.Repeated, therefore the username is allowed.
```

The correct reasoning is:

```text
Pattern.Repeated is skipped for this identifier.
Now continue every other check.
```

### Disabling an entire rule for one legacy identifier

Avoid:

```csharp
options.DisableRule(Rule.PopularCityNames);
```

when the actual requirement is only:

```csharp
options.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);
```

### Allowing every blocked character to permit one separator

Avoid disabling `Rule.BlockedCharacters` when the application only needs underscores:

```csharp
options.AllowCharacters("_");
```

### Raising the cyclic repeat threshold to permit a repeated prefix letter

`RepeatedPatternMinimumLength` controls cyclic repetition. Direct runs are intentionally separate. For a repeated team-prefix character, use:

```csharp
options.AllowRepeatedCharacters("T");
```

## Configuration principle

Start strict, then make the smallest exception that expresses the application's actual naming convention.

That preserves the value of the rest of the deny pipeline and makes configuration changes easier to review, test, and audit.
