<p align="center">
  <img src="assets/unclaimable-icon.png" alt="Unclaimable" width="160" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  A lightweight, cross-platform library for detecting reserved, protected, and impersonation-prone usernames before they can be claimed.
</p>

Unclaimable keeps the **data** independent from the **runtime implementation**. The same JSON lists can therefore be consumed by .NET, JavaScript, Python, Go, or another adapter without duplicating the source of truth.

## Design goals

- Small runtime surface and no unnecessary dependencies.
- Shared, human-reviewable JSON datasets.
- Fast lookups using normalized in-memory dictionaries.
- Conservative defaults to avoid over-blocking legitimate usernames.
- Application-specific reserved names without polluting the global dataset.
- Platform adapters that feel native to their ecosystem.

## Repository layout

```text
assets/
  unclaimable-icon.png

data/
  schema.json
  roles/reserved.json
  support/reserved.json
  system/reserved.json

platforms/
  dotnet/
    src/
      Unclaimable/
      Unclaimable.AspNetCore/
    tests/
      Unclaimable.Tests/
```

Each `reserved.json` file follows the contract in `data/schema.json`. New categories can be added as another folder without changing the consumers.

## Matching

The .NET implementation performs two intentionally small normalization passes:

1. **Exact normalization** — trim, Unicode compatibility normalization (NFKC), and invariant lowercase.
2. **Compact normalization** — optionally remove separators and punctuation while retaining letters and digits.

That means values such as `customer service`, `customer-service`, `customer_service`, and `customer.service` can resolve to the same reserved entry.

Unclaimable deliberately does not perform broad fuzzy matching. Applications can layer stricter anti-impersonation or Unicode-confusable policies on top when their threat model requires it.

## .NET compatibility

The packages are deliberately split so the small core can support a much wider range of applications without taking a dependency on ASP.NET Core.

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | Dependency-free core checker and shared datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core DI and model-validation integration |

The `netstandard2.0` core can be consumed by modern .NET as well as compatible .NET Framework and other .NET Standard implementations. Applications that do not need the ASP.NET Core integration can reference only the core package.

## .NET

```csharp
using Unclaimable;

if (UnclaimableChecker.Default.IsReserved(userName))
{
    // Reject the username.
}
```

For more information about a match:

```csharp
var result = UnclaimableChecker.Default.Check(userName);

if (result.IsReserved)
{
    Console.WriteLine($"Matched '{result.MatchedValue}' from '{result.Category}'.");
}
```

### ASP.NET Core

Register the checker once:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.AdditionalReserved.Add("examplebrand");
    options.AdditionalReserved.Add("internalbot");
});
```

Then inject it where needed:

```csharp
public sealed class UsernameService(IUnclaimableChecker checker)
{
    public bool CanRegister(string userName) => checker.IsClaimable(userName);
}
```

Or use the ASP.NET Core validation attribute on a Razor Pages / MVC model:

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

`ClaimableUsernameAttribute` uses the checker registered with dependency injection, so application-specific names are respected automatically.

## Application-specific names

Brand, project, tenant, and internal account names should normally **not** be added to the shared data files. Reserve them in the application instead:

```csharp
var options = new UnclaimableOptions();
options.AdditionalReserved.Add("examplebrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);
```

This keeps the global dataset useful to everyone without turning it into a collection of unrelated product names.

## NuGet packaging status

The projects already contain package metadata, README inclusion, and the Unclaimable package icon so local `.nupkg` files can be validated before the first public release. There is intentionally **no NuGet publishing workflow yet**.

## Development

Run the .NET tests with:

```bash
dotnet test platforms/dotnet/tests/Unclaimable.Tests/Unclaimable.Tests.csproj
```

Create local NuGet packages with:

```bash
dotnet pack platforms/dotnet/src/Unclaimable/Unclaimable.csproj -c Release
dotnet pack platforms/dotnet/src/Unclaimable.AspNetCore/Unclaimable.AspNetCore.csproj -c Release
```

## Future adapters

The JSON datasets are intentionally runtime-neutral. Additional adapters can live beside .NET under `platforms/` while keeping matching semantics and test vectors aligned across implementations.
