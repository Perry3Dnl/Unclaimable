<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  A lightweight, cross-platform library for detecting reserved, protected, impersonation-prone, and optionally profane usernames before they can be claimed.
</p>

> **Release status:** preparing the first public NuGet release, `0.1.0`. Until that release has been published on nuget.org, the install commands below will not resolve from the public feed.

Unclaimable answers a simple question: **should this username be claimable?**

It combines curated, runtime-neutral JSON datasets with a small matching engine for exact names, separator tricks, common leetspeak, selected Unicode lookalikes, optional embedded/partial matches, configurable character policies, and optional profanity filtering. The shared data is independent from the runtime implementation so future JavaScript, Python, Go, or other adapters can use the same source of truth.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | dependency-free runtime core and embedded shared datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core dependency injection and model validation |

After `0.1.0` is available on nuget.org:

```bash
dotnet add package Unclaimable --version 0.1.0
```

For ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.1.0
```

`Unclaimable.AspNetCore` depends on the matching version of the core `Unclaimable` package.

## What it catches

With the default configuration:

```text
admin             -> reserved role
customer-service  -> customer service
N1k3              -> nike
G00gle             -> google
аpple              -> apple   (first character is Cyrillic)
r00t               -> root
```

Stricter behavior is opt-in:

```text
PartialMatching = true
old-admin         -> admin
administrator2    -> administrator

AllowNumbers = false
ordinary2         -> rejected immediately by number policy

ProfanityMatching = true
sh1t              -> profanity: shit
```

Unclaimable does **not** perform broad edit-distance/fuzzy guessing. For example, `nikee` is not rejected merely because it is close to `nike`.

## Current datasets

Reserved values are stored as human-reviewable JSON under `data/<category>/reserved.json`.

| Category | Default | Purpose | Examples |
| --- | ---: | --- | --- |
| `roles` | on | privileged or trusted application identities | `admin`, `administrator`, `moderator`, `staff`, `root` |
| `support` | on | support, trust, billing, and official-channel identities | `support`, `helpdesk`, `security`, `official` |
| `system` | on | application, protocol, and system-owned identities | `system`, `api`, `auth`, `login`, `webmaster`, `noreply` |
| `technology` | on | broadly recognizable technology names | `apple`, `microsoft`, `linux`, `google`, `github`, `openai` |
| `brands` | on | broadly recognizable consumer and commercial brands | `nike`, `adidas`, `coca cola`, `disney`, `tesla`, `paypal` |
| `profanity` | **off** | common English profanity and vulgar insults | opt-in via `ProfanityMatching` |

The five always-on categories contain more than 700 curated values. The profanity category is deliberately separate and opt-in because profanity policy is application- and culture-dependent. Identity-targeting slurs are intentionally not mixed into the profanity dataset.

Each dataset follows `data/schema.json`:

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

Private product names, tenant names, internal bots, and application-specific terms should normally use `AdditionalReserved` instead of being added to the shared global datasets.

## Matching pipeline

`Check(...)`, `IsReserved(...)`, and `IsClaimable(...)` are fail-fast. The checker returns as soon as it knows the username is not allowed.

The effective order is:

1. **Cheap character policy checks** — number policy and/or printable-ASCII policy when enabled.
2. **Exact matching** — trim, Unicode NFKC normalization, invariant lowercase, dictionary lookup.
3. **Compact matching** — remove separators/punctuation while retaining letters/digits, then dictionary lookup.
4. **Partial matching** — optional embedded reserved-name detection.
5. **Unicode-confusable matching** — selected visual lookalikes and diacritic normalization.
6. **Obfuscation matching** — bounded common leetspeak/symbol substitutions.

Built-in values are loaded once and indexed in dictionaries. Exact and compact matches therefore use hashed dictionary lookups rather than scanning every reserved value.

### Exact matching

Case and surrounding whitespace do not matter:

```text
admin
ADMIN
 ADMIN 
```

all resolve to the same reserved value.

### Compact matching

Enabled by default. Separator and punctuation variants can resolve to the same stored value:

```text
customer service
customer-service
customer_service
customer.service
```

### Partial matching

Disabled by default. Enable it when your application also wants to reject reserved names embedded inside a larger username:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true
});

checker.IsReserved("old-admin");      // true
checker.IsReserved("administrator2"); // true
```

`PartialMatchMinimumLength` defaults to `4`. This intentionally prevents very short reserved values such as `api` from matching inside ordinary longer words by default.

Partial matching is deterministic substring matching, not fuzzy/edit-distance matching.

### Obfuscation / leetspeak matching

Enabled by default. Common substitutions include:

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

Candidate expansion is capped so ambiguous substitutions cannot grow into an uncontrolled search.

### Unicode-confusable matching

Enabled by default. Unclaimable includes a deliberately bounded mapping for common visual impersonation characters, including selected Cyrillic and Greek lookalikes plus diacritic normalization.

For example:

```text
аpple
^ Cyrillic U+0430
```

can match reserved `apple`.

This is intentionally **not** a complete implementation of every Unicode confusable defined by Unicode security standards.

### Number policy

Numbers are allowed by default. To reject Unicode decimal digits:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AllowNumbers = false
});
```

The number check happens before the slower reserved-name matching pipeline. A failure includes the offending character and its zero-based index in `UnclaimableResult`.

### ASCII-only input

`AsciiOnly` is off by default so applications can support international usernames.

When enabled, characters outside printable ASCII (`U+0020` through `U+007E`) are rejected with `UnclaimableMatchKind.InvalidCharacters`:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AsciiOnly = true
});
```

`AsciiOnly` is a character-set policy, not a complete username-format policy. It does not decide your minimum/maximum length, whitespace rules, or which printable ASCII punctuation your application permits.

### Profanity matching

Profanity filtering is disabled by default:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    ProfanityMatching = true
});
```

When enabled, profanity uses the normal exact, compact, obfuscation, and Unicode-confusable pipeline.

Generic `PartialMatching` does **not** automatically perform profanity substring matching. To deliberately enable that stricter behavior, both options are required:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true,
    ProfanityMatching = true,
    ProfanityPartialMatching = true
});
```

Keeping `ProfanityPartialMatching` separate helps avoid substring false positives such as innocent words that happen to contain a short vulgar fragment.

## Core .NET API

For the fastest yes/no path:

```csharp
using Unclaimable;

if (UnclaimableChecker.Default.IsClaimable(userName))
{
    // Username passed Unclaimable's configured policy.
}
```

The public checker contract is:

```csharp
public interface IUnclaimableChecker
{
    bool IsReserved(string? value);
    bool IsClaimable(string? value);
    UnclaimableResult Check(string? value);
    UnclaimableDetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
```

### `Check(...)`: structured fail-fast result

Use `Check` when code needs to know *why the first rejection happened* without collecting every possible diagnostic:

```csharp
var result = UnclaimableChecker.Default.Check("N1k3");

Console.WriteLine(result.IsReserved);   // true
Console.WriteLine(result.MatchedValue); // nike
Console.WriteLine(result.Category);     // brands
Console.WriteLine(result.MatchKind);    // Obfuscated
```

`UnclaimableResult` exposes:

| Property | Meaning |
| --- | --- |
| `IsReserved` / `IsClaimable` | rejection/acceptance state |
| `Input` / `InputLength` | original input and length |
| `MatchedValue` | stored reserved value that matched, when applicable |
| `Category` | `roles`, `support`, `system`, `technology`, `brands`, `profanity`, or `custom` |
| `MatchKind` | matching/policy reason |
| `OffendingCharacterIndex` | zero-based invalid-character/digit index, when applicable |
| `OffendingCharacter` | offending character, when applicable |
| `MatchStartIndex` | start of a reserved match when available |
| `MatchLength` | length of the reserved match when available |

Current match kinds:

```text
None
Exact
Compact
Obfuscated
UnicodeConfusable
InvalidCharacters
Partial
NumbersNotAllowed
```

### `CheckDetailed(...)`: collect diagnostics

Use the detailed path when validation UI, logging, or diagnostics benefit from more than the first failure:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true,
    AllowNumbers = false
});

var result = checker.CheckDetailed("old-admin2");

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(diagnostic.Kind);
}
```

For built-in human-readable diagnostic text:

```csharp
var result = checker.CheckDetailed("old-admin2", includeMessages: true);
```

Messages are omitted by default so callers that only need machine-readable diagnostics do not request unnecessary user-facing strings.

`UnclaimableDiagnostic` exposes the reason kind, matched value/category, offending character/index, match start/length, and optional message.

## Configuration

Typical application configuration:

```csharp
var options = new UnclaimableOptions
{
    CompactMatching = true,
    PartialMatching = false,
    PartialMatchMinimumLength = 4,
    ProfanityMatching = false,
    ProfanityPartialMatching = false,
    ObfuscationMatching = true,
    UnicodeConfusableMatching = true,
    AllowNumbers = true,
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
| `PartialMatching` | `false` | catch embedded reserved values |
| `PartialMatchMinimumLength` | `4` | minimum compact reserved-name length eligible for partial matching |
| `ProfanityMatching` | `false` | include the built-in profanity dataset |
| `ProfanityPartialMatching` | `false` | allow profanity entries to participate in partial matching |
| `ObfuscationMatching` | `true` | catch common leetspeak/symbol substitutions |
| `UnicodeConfusableMatching` | `true` | catch selected visual Unicode lookalikes |
| `AllowNumbers` | `true` | allow Unicode decimal digits |
| `AsciiOnly` | `false` | restrict input to printable ASCII when enabled |
| `ValidationMessage` | `null` | ASP.NET Core global validation message; built-in message is used when null |
| `AdditionalReserved` | empty | application-specific reserved values |

## Application-specific reserved names

Use `AdditionalReserved` for names that matter to your own application but should not pollute the global datasets:

```csharp
var options = new UnclaimableOptions();
options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);

checker.IsReserved("ExampleBrand");            // true
checker.Check("examplebrand").Category;        // custom
```

Custom entries use the same configured matching pipeline as normal reserved entries.

## ASP.NET Core

Register once in `Program.cs`:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.PartialMatching = true;
    options.AllowNumbers = false;
    options.ProfanityMatching = true;

    options.ValidationMessage = "{FieldName} is not available. Please choose another one.";
    options.AdditionalReserved.Add("examplebrand");
});
```

Inject the checker anywhere:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use model validation in Razor Pages/MVC:

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

### Validation messages

The application-wide ASP.NET validation message is configured once:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.ValidationMessage = "{FieldName} is not available.";
});
```

`{FieldName}` is replaced with the validation display name.

Message precedence is:

1. attribute-specific `ErrorMessage`;
2. `options.ValidationMessage`;
3. built-in `"{FieldName} is reserved and cannot be claimed."`.

A field can therefore override the global message when needed:

```csharp
[ClaimableUsername(ErrorMessage = "Please choose another display name.")]
public string UserName { get; set; } = string.Empty;
```

`[Required]` remains a separate concern; `ClaimableUsernameAttribute` intentionally treats `null` as valid so normal DataAnnotations required-field validation can handle it.

## What Unclaimable intentionally does not do

Unclaimable focuses on reserved-name, impersonation, and configurable basic username-policy checks. Applications should still own rules such as:

- minimum and maximum length;
- application-specific whitespace and punctuation rules;
- leading/trailing separator rules;
- uniqueness in the application's database;
- account-specific impersonation decisions;
- rate limiting and abuse controls;
- comprehensive multilingual content moderation.

The optional profanity dataset is a practical English baseline, not a comprehensive moderation engine, and it intentionally does not combine identity-targeting slurs into the same category.

Unclaimable also avoids broad edit-distance/fuzzy matching because it can create large numbers of false positives.

## Repository layout

```text
.github/
  workflows/
    dotnet.yml
    release.yml

CHANGELOG.md
Directory.Build.props
LICENSE
README.md

assets/
  unclaimable-icon.png

conformance/
  cases.json

data/
  schema.json
  brands/reserved.json
  profanity/reserved.json
  roles/reserved.json
  support/reserved.json
  system/reserved.json
  technology/reserved.json

platforms/
  dotnet/
    scripts/
      Validate-Packages.ps1
    smoke/
      Unclaimable.ConsumerSmoke/
    src/
      Unclaimable/
      Unclaimable.AspNetCore/
    tests/
      Unclaimable.Tests/
```

`conformance/cases.json` contains shared behavioral test vectors so future runtime adapters can implement the same default matching semantics.

## Development

Run tests:

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj
```

Build local NuGet packages:

```bash
dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj --configuration Release --output artifacts

dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj --configuration Release --output artifacts
```

The package version is defined once as `UnclaimableVersion` in `Directory.Build.props`, so both packages share the same release version.

Release builds generate:

```text
Unclaimable.<version>.nupkg
Unclaimable.<version>.snupkg
Unclaimable.AspNetCore.<version>.nupkg
Unclaimable.AspNetCore.<version>.snupkg
```

The `.snupkg` files contain portable PDBs with Source Link information. `Microsoft.SourceLink.GitHub` is a private build dependency and is not exposed to package consumers.

Validate generated package contents locally:

```powershell
./platforms/dotnet/scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

CI validates package IDs/version, MPL-2.0 metadata, copyright, README/icon inclusion, assemblies/XML docs, canonical repository metadata, symbol packages, Source Link dependency isolation, and the ASP.NET Core dependency on the matching core version.

A separate consumer smoke application restores the generated `.nupkg` files from a temporary local feed instead of using project references. This catches packaging problems that source-project tests cannot.

## Release process

Normal CI performs:

```text
unit + conformance tests
        ↓
pack .nupkg + .snupkg
        ↓
validate package contents/metadata
        ↓
restore packages into clean consumer project
        ↓
run consumer smoke test
```

The release workflow repeats the full validation pipeline. Publishing occurs **only** for a `v<version>` tag, and the tag must:

- match `UnclaimableVersion` exactly (for example `v0.1.0`);
- point at the current `main` commit.

NuGet publishing uses GitHub Actions OIDC / NuGet Trusted Publishing. The repository does not store a long-lived NuGet API key. The publishing job uses the protected `release` environment and obtains a short-lived API key immediately before pushing the packages.

Before the first release, the package owner must configure the matching nuget.org Trusted Publishing policy and the `NUGET_USER` GitHub Actions secret.

## Design principles

- Keep the runtime core small and dependency-free.
- Keep reserved-name data runtime-neutral and human-reviewable.
- Prefer deterministic matching over broad fuzzy guesses.
- Make aggressive/false-positive-prone policies opt-in.
- Fail cheaply and early when a configured character policy can decide the result.
- Keep private/project-specific names out of the shared datasets.
- Keep platform implementations aligned through shared conformance cases.
- Publish with short-lived credentials rather than permanent package-feed secrets.

## License

Unclaimable is licensed under the **Mozilla Public License 2.0 (MPL-2.0)**.

Copyright (c) 2026 Perry3D.nl.

The MPL-2.0 permits Unclaimable to be used as a dependency in proprietary and commercial applications while keeping changes to MPL-covered Unclaimable source files under the MPL when those changes are distributed. See the root `LICENSE` file for the project notice and official MPL-2.0 terms.

## Future adapters

The JSON datasets are not .NET-specific. Additional adapters can live beside .NET under `platforms/` while using the same shared data and conformance cases.
