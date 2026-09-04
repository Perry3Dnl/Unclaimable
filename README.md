<p align="center">
  <img src="https://raw.githubusercontent.com/ImtehQ/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  A lightweight, cross-platform library for detecting reserved, protected, and impersonation-prone usernames before they can be claimed.
</p>

> **Status:** active development. The .NET packages can already be built and packed locally, but **Unclaimable is not published to NuGet yet**.

Unclaimable answers a simple question: **should this username be claimable?**

It combines curated, runtime-neutral JSON datasets with a small matching engine that can detect direct reserved names, separator tricks, common leetspeak, selected Unicode lookalikes, optional partial-name impersonation, and configurable character policies. The shared data is intentionally independent from the runtime implementation so future JavaScript, Python, Go, or other adapters can use the same source of truth.

## What it catches

```text
admin             -> reserved role
customer-service  -> customer service
N1k3              -> nike
G00gle             -> google
аpple              -> apple   (first character is Cyrillic)
r00t               -> root
```

By default, Unclaimable deliberately avoids broad substring blocking, so names such as `administrator2`, `supportive`, `apples`, and `nikee` remain claimable. Applications that want stricter protection can enable `PartialMatching`, which also rejects names such as `old-admin`, `administrator2`, and `nikee`.

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

The normal boolean and `Check(...)` APIs are deliberately fail-fast. Cheap policy checks are performed before more expensive normalization and impersonation checks:

1. **Number policy** — only when `AllowNumbers` is `false`.
2. **ASCII policy** — only when `AsciiOnly` is enabled.
3. **Exact matching** — trim, Unicode NFKC normalization, invariant lowercase, then dictionary lookup.
4. **Compact matching** — remove separators and punctuation while retaining letters/digits, then dictionary lookup.
5. **Partial matching** — optional substring protection against embedded reserved names.
6. **Unicode-confusable matching** — normalize selected visually confusable characters and diacritics, then compare again.
7. **Obfuscation matching** — try a bounded set of common leetspeak/symbol substitutions.

As soon as one check fails, the fail-fast APIs return. Built-in values are loaded once and indexed in dictionaries, so normal exact/compact checks use average O(1) lookups after initialization.

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

### Partial matching

`PartialMatching` is **off by default** because substring blocking is intentionally stricter and can create more false positives.

Enable it when usernames must not contain protected names anywhere inside a larger value:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true
});
```

Examples:

```text
administrator2  -> administrator
old-admin       -> admin
admin-old       -> admin
supportive      -> support
apples          -> apple
nikee           -> nike
old-N1k3        -> nike
```

To reduce accidental matches, reserved values shorter than `PartialMatchMinimumLength` are ignored for partial matching. The default minimum is `4`, so an exact reserved value such as `api` is still blocked, while a normal word such as `rapid` does not become blocked merely because it contains `api`.

Applications that deliberately want more aggressive matching can lower the threshold:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    PartialMatching = true,
    PartialMatchMinimumLength = 3
});
```

### Number policy

Numbers are allowed by default. Set `AllowNumbers = false` to reject Unicode decimal digits before reserved-name normalization or other slower checks:

```csharp
var checker = new UnclaimableChecker(new UnclaimableOptions
{
    AllowNumbers = false
});
```

```text
ordinary-user  -> continues to normal matching
user123        -> rejected immediately at '1'
old-admin2     -> rejected immediately at '2'
```

The fail-fast result exposes the offending character and its zero-based position in the original input.

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

For the cheapest possible answer, use the boolean APIs:

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

For a fail-fast structured result:

```csharp
var result = UnclaimableChecker.Default.Check("N1k3");

Console.WriteLine(result.IsReserved);   // true
Console.WriteLine(result.MatchedValue); // nike
Console.WriteLine(result.Category);     // brands
Console.WriteLine(result.MatchKind);    // Obfuscated
```

For a slower validation pass that collects policy diagnostics as well as the reserved-name result:

```csharp
var detailed = checker.CheckDetailed(userName);
```

This returns machine-readable diagnostics with no user-facing prose. When feedback text is useful, request messages explicitly:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

For example, with `AllowNumbers = false` and `PartialMatching = true`, checking `old-admin2` can report both:

```text
NumbersNotAllowed -> character "2", index 9
Partial           -> matched "admin", start 4, length 5
```

The public checker contract remains compact:

```csharp
public interface IUnclaimableChecker
{
    bool IsReserved(string? value);
    bool IsClaimable(string? value);
    UnclaimableResult Check(string? value);
    UnclaimableDetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
```

## Result information

`Check(...)` returns a fail-fast `UnclaimableResult` with structured information such as:

| Property | Meaning |
| --- | --- |
| `IsReserved` | whether the input was rejected by Unclaimable |
| `IsClaimable` | inverse of `IsReserved` |
| `Input` | original input |
| `InputLength` | original string length |
| `MatchedValue` | stored reserved value that matched, when applicable |
| `Category` | dataset category such as `roles`, `technology`, `brands`, or `custom` |
| `MatchKind` | why the input was rejected/matched |
| `OffendingCharacterIndex` | original zero-based input index for character-policy failures |
| `OffendingCharacter` | offending input character/text element for character-policy failures |
| `MatchStartIndex` | start of a reserved-name match when available |
| `MatchLength` | length of the reserved-name match when available |

`CheckDetailed(...)` returns `UnclaimableDetailedResult`, which has the original input, input length, overall claimable/reserved state, and a collection of `UnclaimableDiagnostic` entries. Each diagnostic is developer-friendly structured data; `Message` remains `null` unless `includeMessages: true` is requested.

Current `UnclaimableMatchKind` values are:

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

For character-policy failures there is no reserved-name match, so `MatchedValue` and `Category` are null.

## Configuration

```csharp
var options = new UnclaimableOptions
{
    CompactMatching = true,
    PartialMatching = false,
    PartialMatchMinimumLength = 4,
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
| `PartialMatching` | `false` | reject larger usernames containing reserved values |
| `PartialMatchMinimumLength` | `4` | avoid short partials producing excessive false positives |
| `ObfuscationMatching` | `true` | catch common leetspeak/symbol substitutions |
| `UnicodeConfusableMatching` | `true` | catch selected visual Unicode lookalikes |
| `AllowNumbers` | `true` | permit decimal digits; disable for a cheap fail-fast digit policy |
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

Custom entries go through the same configured compact, partial, obfuscation, and Unicode-confusable matching pipeline as built-in names.

## ASP.NET Core

Register the checker once:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.CompactMatching = true;
    options.PartialMatching = true;
    options.PartialMatchMinimumLength = 4;
    options.ObfuscationMatching = true;
    options.UnicodeConfusableMatching = true;
    options.AllowNumbers = false;
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

`ClaimableUsernameAttribute` resolves `IUnclaimableChecker` from dependency injection, so all configured options and `AdditionalReserved` values are respected automatically.

`[Required]` should still be used when the field itself is mandatory; `ClaimableUsernameAttribute` intentionally treats `null` as a separate validation concern.

## What Unclaimable does not validate

Unclaimable is focused on **reserved-name, impersonation, and explicitly configured character-policy checks**. Your application should still validate its own remaining username policy, including things such as:

- minimum and maximum length;
- whether whitespace is allowed;
- which punctuation is allowed;
- leading/trailing separators;
- profanity or broader content moderation;
- uniqueness in your own database;
- account-specific impersonation rules;
- rate limiting and abuse controls.

Unclaimable still deliberately avoids edit-distance fuzzy matching because it can create large numbers of false positives. Partial matching is available as an explicit opt-in instead of silently changing the conservative default behavior.

## Repository layout

```text
.github/
  workflows/
    dotnet.yml

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

Run the .NET tests:

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj
```

Build local NuGet packages:

```bash
dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj --configuration Release --output artifacts

dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj --configuration Release --output artifacts
```

The package version is defined once as `UnclaimableVersion` in `Directory.Build.props`, so the core and ASP.NET Core packages use the same release version.

Release builds generate both normal NuGet packages and symbol packages:

```text
Unclaimable.<version>.nupkg
Unclaimable.<version>.snupkg
Unclaimable.AspNetCore.<version>.nupkg
Unclaimable.AspNetCore.<version>.snupkg
```

The symbol packages contain portable PDBs with Source Link information pointing back to this GitHub repository. `Microsoft.SourceLink.GitHub` is a private build dependency and is not exposed as a dependency to package consumers.

Validate the generated package contents and metadata locally with PowerShell:

```powershell
./platforms/dotnet/scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

The validator checks package IDs and versions, MPL-2.0/copyright metadata, README and icon inclusion, DLL/XML documentation files, repository URL and commit metadata, symbol PDBs, Source Link dependency isolation, and the ASP.NET Core package's dependency on the matching core package version.

A separate consumer smoke project restores the generated `.nupkg` files instead of referencing the source projects. This verifies that a real consuming application can use the core checker, Unicode/obfuscation matching, optional partial matching, the number and ASCII policies, detailed diagnostics, ASP.NET Core dependency injection, `AdditionalReserved`, and `[ClaimableUsername]` directly from the packed artifacts.

GitHub Actions performs the full sequence automatically:

```text
unit/conformance tests
        ↓
pack .nupkg + .snupkg
        ↓
validate package contents/metadata
        ↓
restore packages into clean consumer project
        ↓
run consumer smoke test
        ↓
upload package artifacts for inspection
```

## NuGet status

The projects now contain centralized version metadata, repository information, README and icon inclusion, MPL-2.0 license metadata, Source Link information, portable symbols, package-content validation, and an installed-package consumer smoke test.

CI uploads the resulting `.nupkg` and `.snupkg` files as temporary GitHub Actions artifacts for inspection.

**There is intentionally no NuGet publishing/release workflow yet.** Do not assume `dotnet add package Unclaimable` is available until the first package release has actually been published and verified.

## Design principles

- Keep the core small and dependency-free at runtime.
- Fail fast on cheap policy checks before running more expensive matching passes.
- Keep reserved-name data runtime-neutral and reviewable.
- Prefer exact, deterministic matching over broad fuzzy guesses.
- Make stricter substring matching explicit and configurable.
- Detect common impersonation tricks without making legitimate usernames unusable.
- Keep private/project-specific names out of the global dataset.
- Keep platform implementations aligned through shared conformance cases.

## License

Unclaimable is licensed under the **Mozilla Public License 2.0 (MPL-2.0)**.

Copyright (c) 2026 Perry3D.nl.

The MPL-2.0 permits Unclaimable to be used as a dependency in proprietary and commercial applications while keeping changes to MPL-covered Unclaimable source files under the MPL when those changes are distributed. See the root `LICENSE` file for the project notice and the official MPL-2.0 terms.

## Future adapters

The JSON datasets are not .NET-specific. Additional adapters can live beside .NET under `platforms/` while using the same data and conformance cases.
