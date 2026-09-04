# Unclaimable

A lightweight, cross-platform library for detecting reserved, protected, and impersonation-prone usernames before they can be claimed.

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

## .NET

The core package targets .NET 8 and later and has no third-party runtime dependencies.

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
    options.AdditionalReserved.Add("thesugarlook");
    options.AdditionalReserved.Add("sugarlook");
    options.AdditionalReserved.Add("sitecheckuser");
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
options.AdditionalReserved.Add("mybrand");
options.AdditionalReserved.Add("internalbot");

var checker = new UnclaimableChecker(options);
```

This keeps the global dataset useful to everyone without turning it into a collection of unrelated product names.

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
