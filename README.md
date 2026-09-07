<p align="center">
  <img src="https://raw.githubusercontent.com/Perry3Dnl/Unclaimable/main/assets/unclaimable-icon.png" alt="Unclaimable icon" width="180" />
</p>

<h1 align="center">Unclaimable</h1>

<p align="center">
  Strict, fast username and identifier validation for .NET.
</p>

> **Release status:** preparing the first public NuGet release, `0.1.0`.

Unclaimable answers one question: **should this identifier be claimable?**

It combines curated reserved-name datasets with structural identifier rules, strict impersonation matching, bounded obfuscation detection, Unicode lookalike handling, localized profanity and trusted-role filtering, application-specific blocked values, and ASP.NET Core integration.

The default policy is intentionally strict. For most applications, configuration is optional:

```csharp
builder.Services.AddUnclaimable();
```

The default localized dataset is **English**. Dutch or German can be selected explicitly, and applications can opt into checking every supported language together.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| `Unclaimable` | `netstandard2.0` | dependency-free runtime core and embedded datasets |
| `Unclaimable.AspNetCore` | `net8.0` | ASP.NET Core dependency injection and model validation |
