# Configuring Unclaimable 0.8.0

This guide explains how to keep Unclaimable's strict defaults while making small, intentional exceptions for an application's naming rules.

If you only need a working example, start with **Copy-paste recipes**. The later sections explain exactly which checks are skipped, which checks still run, and how the deny-first pipeline behaves.

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

The 0.8.0 default is intentionally strict. Every enabled check is a deny check: passing one check never clears the identifier. The value is claimable only when no enabled check rejects it.

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
