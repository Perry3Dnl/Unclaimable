<p align="center">
  <img src="https://raw.githubusercontent.com/ImtehQ/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  A lightweight, cross-platform library for detecting reserved, protected, and impersonation-prone usernames before they can be claimed.
</p>

> **Status:** active development. The .NET packages can already be built and packed locally, but **Unclaimable is not published to NuGet yet**.

Unclaimable answers a simple question: **should this username be claimable?**

It combines curated, runtime-neutral JSON datasets with a small matching engine that can detect direct reserved names, separator tricks, common leetspeak, and selected Unicode lookalikes. The shared data is intentionally independent from the runtime implementation so future JavaScript, Python, Go, or other adapters can use the same source of truth.

## What it catches

```text
admin             -> reserved role
customer-service  -> customer service
N1k3              -> nike
G00gle             -> google
аpple              -> apple   (first character is Cyrillic)
r00t               -> root
```

It is deliberately **not** a broad fuzzy-name or substring blocker. Names such as `administrator2`, `supportive`, `apples`, and `nikee` remain claimable unless an application explicitly reserves them.

## Current datasets

Reserved values are stored as human-reviewable JSON under `data/<category>/reserved.json`.

| Category | Purpose | Examples |
| --- | --- | --- |
| `roles` | privileged or trusted application identities | `admin`, `administrator`, `moderator`, `staff`, `root` |
| `support` | names that could impersonate support or official channels | `support`, `helpdesk`, `security`, `official` |
| `system` | application, protocol, and system-owned identities | `system`, `api`, `auth`, `login`, `webmaster`, `noreply` |
| `technology` | broadly recognizable technology names | `apple`, `microsoft`, `linux`, `google`, `github`, `openai` |
| `brands` | broadly recognizable consumer brands | `nike`, `adidas`, `coca cola`, `disney`, `tesla`, `paypal` |

Each file follows `data/schema.json`:

```json
{
  "schema": 1,
  "category": "roles",
  "description": "Names that imply privileged or trusted application roles.",
  "values": [
    "admin",
    "administrator",
    "moderator"
  ]
}
```

New categories can be added without changing the consumer API. Private product names, tenant names, internal bots, and project-specific terms should normally **not** be added to the global data files; use `AdditionalReserved` instead.

## Matching pipeline

The .NET checker stops as soon as it finds a match. The current order is:

1. **ASCII policy check** — only when `AsciiOnly` is enabled.
2. **Exact matching** — trim, Unicode NFKC normalization, invariant lowercase, then dictionary lookup.
3. **Compact matching** — remove separators and punctuation while retaining letters/digits, then dictionary lookup.
4. **Unicode-confusable matching** — normalize selected visually confusable characters and diacritics, then compare again.
5. **Obfuscation matching** — try a bounded set of common leetspeak/symbol substitutions.

Built-in values are loaded once and indexed in dictionaries, so normal exact/compact checks use average O(1) lookups after initialization.

### Exact matching

Case and surrounding whitespace do not matter:

```text
admin
ADMIN
 ADMIN 
```

all match the same reserved value.

### Compact matching

Enabled by default. Separator/punctuation variants can resolve to the same stored value:

```text
customer service
customer-service
customer_service
customer.service
```

### Obfuscation / leetspeak matching

Enabled by default. Common substitutions are recognized, including mappings such as:

```text
0 -> o
1 -> i / l
2 -> z
3 -> e
4 -> a
5 -> s
6 -> g
7 -> t
8 -> b
9 -> g
@ -> a
$ -> s
! -> i / l
| -> i / l
+ -> t
```

Examples:

```text
N1ke       -> nike
N1k3       -> nike
M1crosoft  -> microsoft
G00gle     -> google
@pple      -> apple
app1e      -> apple
r00t       -> root
c0ca-c0la  -> coca cola
```

Candidate expansion is capped, so ambiguous substitutions such as `1 -> i/l` cannot grow into an uncontrolled search.

### Unicode-confusable matching

Enabled by default. Unclaimable includes a deliberately small curated mapping for common visual impersonation characters, including selected Cyrillic and Greek lookalikes plus diacritic normalization.

For example, this value does **not** contain a normal Latin `a`:

```text
аpple
^ Cyrillic U+0430
```

but it can still match the reserved `apple` entry.

This is intentionally **not a complete implementation of every Unicode confusable defined by Unicode security standards**. The goal is useful, bounded protection without pretending that visual-identity detection is perfect.

### ASCII-only input

`AsciiOnly` is **off by default** so applications can support international usernames.

When enabled, input containing characters outside printable ASCII (`U+0020` through `U+007E`) is treated as unclaimable and returns `UnclaimableMatchKind.InvalidCharacters`.

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AsciiOnly = true
});
```

`AsciiOnly` is a character-set policy, not a complete username-format validator. It does not replace your application's rules for length, whitespace, punctuation, prefixes, or which ASCII symbols are allowed.

## .NET compatibility

The .NET implementation is split into two packages so the core remains small and widely compatible.

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | dependency-free core checker and embedded shared datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core dependency injection and model validation |

The `netstandard2.0` core can be consumed by modern .NET and compatible .NET Framework / .NET Standard implementations. Applications that do not need ASP.NET Core integration only need the core project/package.

## Core .NET usage

For a simple boolean check:

```csharp
using Unclaimable;

if (UnclaimableChecker.Default.IsReserved(userName))
{
    // Reject the username.
}

if (UnclaimableChecker.Default.IsClaimable(userName))
{
    // Username passed Unclaimable's checks.
}
```

For detailed information:

```csharp
var result = UnclaimableChecker.Default.Check("N1k3");

Console.WriteLine(result.IsReserved);   // true
Console.WriteLine(result.MatchedValue); // nike
Console.WriteLine(result.Category);     // brands
Console.WriteLine(result.MatchKind);    // Obfuscated
```

The public checker contract is intentionally small:

```csharp
public interface IUnclaimableChecker
{
    bool IsReserved(string? value);
    bool IsClaimable(string? value);
    UnclaimableResult Check(string? value);
}
```

## Result information

`Check(...)` returns an `UnclaimableResult` with:

| Property | Meaning |
| --- | --- |
| `IsReserved` | whether the input was rejected by Unclaimable |
| `Input` | the original input |
| `MatchedValue` | the stored reserved value that matched, when applicable |
| `Category` | dataset category such as `roles`, `technology`, `brands`, or `custom` |
| `MatchKind` | how the input was rejected/matched |

Current `UnclaimableMatchKind` values are:

```text
None
Exact
Compact
Obfuscated
UnicodeConfusable
InvalidCharacters
```

For `InvalidCharacters`, there is no reserved-name match, so `MatchedValue` and `Category` are null.

## Configuration

```csharp
var options = new UnclaimableOptions
{
    CompactMatching = true,
    ObfuscationMatching = true,
    UnicodeConfusableMatching = true,
    AsciiOnly = false
};

options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);
```

Defaults:

| Option | Default | Purpose |
| --- | ---: | --- |
| `CompactMatching` | `true` | catch separator/punctuation variants |
| `ObfuscationMatching` | `true` | catch common leetspeak/symbol substitutions |
| `UnicodeConfusableMatching` | `true` | catch selected visual Unicode lookalikes |
| `AsciiOnly` | `false` | reject characters outside printable ASCII when enabled |
| `AdditionalReserved` | empty | add application-specific reserved names |

## Application-specific reserved names

Use `AdditionalReserved` for names that matter to your own application but should not pollute the global dataset:

```csharp
var options = new UnclaimableOptions();
options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);

checker.IsReserved("ExampleBrand"); // true
checker.Check("examplebrand").Category; // custom
```

Custom entries go through the same compact, obfuscation, and Unicode-confusable matching pipeline as built-in names.

## ASP.NET Core

Register the checker once:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.CompactMatching = true;
    options.ObfuscationMatching = true;
    options.UnicodeConfusableMatching = true;
    options.AsciiOnly = false;

    options.AdditionalReserved.Add("examplebrand");
});
```

Inject the interface anywhere:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use the ASP.NET Core validation attribute in Razor Pages / MVC:

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

`ClaimableUsernameAttribute` resolves `IUnclaimableChecker` from dependency injection, so the application's configured options and `AdditionalReserved` values are respected automatically.

`[Required]` should still be used when the field itself is mandatory; `ClaimableUsernameAttribute` intentionally treats `null` as a separate validation concern.

## What Unclaimable does not validate

Unclaimable is focused on **reserved-name and impersonation checks**. Your application should still validate its own username policy, including things such as:

- minimum and maximum length;
- whether whitespace is allowed;
- which punctuation is allowed;
- leading/trailing separators;
- profanity or content moderation;
- uniqueness in your own database;
- account-specific impersonation rules;
- rate limiting and abuse controls.

Unclaimable also deliberately avoids broad substring and edit-distance matching because those strategies can create large numbers of false positives.

## Repository layout

```text
.github/
  workflows/
    dotnet.yml

assets/
  unclaimable-icon.png

conformance/
  cases.json

data/
  schema.json
  brands/reserved.json
  roles/reserved.json
  support/reserved.json
  system/reserved.json
  technology/reserved.json

platforms/
  dotnet/
    src/
      Unclaimable/
      Unclaimable.AspNetCore/
    tests/
      Unclaimable.Tests/
```

`conformance/cases.json` contains shared behavioral test vectors so future runtime adapters can implement the same default matching semantics.

## Development

Run the .NET tests:

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj
```

Build local NuGet packages:

```bash
dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj --configuration Release --output artifacts

dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj --configuration Release --output artifacts
```

GitHub Actions runs the test-and-pack workflow for relevant changes to the datasets, conformance cases, .NET implementation, README, and package assets.

## NuGet status

The projects already contain package metadata, repository information, README inclusion, and the Unclaimable package icon so the generated `.nupkg` files can be validated before release.

**There is intentionally no NuGet publishing/release workflow yet.** Do not assume `dotnet add package Unclaimable` is available until the first package release has actually been published and verified.

## Design principles

- Keep the core small and dependency-free.
- Keep reserved-name data runtime-neutral and reviewable.
- Prefer exact, deterministic matching over broad fuzzy guesses.
- Detect common impersonation tricks without making legitimate usernames unusable.
- Keep private/project-specific names out of the global dataset.
- Keep platform implementations aligned through shared conformance cases.

## Future adapters

The JSON datasets are not .NET-specific. Additional adapters can live beside .NET under `platforms/` while using the same data and conformance cases.
