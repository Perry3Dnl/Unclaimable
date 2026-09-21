# Unclaimable.Email

Email-address identity and protected-domain impersonation checks.

**Package version: 0.8.0**

## Install

```bash
dotnet add package Unclaimable.Email --version 0.8.0
```

## Cross-platform app compatibility

`Unclaimable.Email` targets `netstandard2.0` and is compile-checked in .NET MAUI, Blazor WebAssembly, WPF, Windows Forms, Console, Worker Service, Avalonia, and Uno Platform consumers.

It contains no UI-framework dependency, so the same email checker can be used from client, desktop, mobile, or server application code.

## Quick start

```csharp
using Unclaimable.Email;

var options = new EmailOptions();
options.ProtectedDomains.Add("lidl.nl");
options.IssuingDomains.Add("lidl.nl");

var checker = new EmailChecker(options);

var external = checker.CheckExistingAddress("admin@lidi.nl");
var created = checker.CheckNewAddress("bluegarden@lidl.nl");
```

The email local part is checked with an email-adapted Unclaimable policy. Domain checks are handled separately.

In 0.8.0, local-part identity protection starts from the same default protected identity rules as the main checker: country, city, celebrity, nationality, currency, religion, landmark, event, award, fictional-character, franchise, profession, and military rules are enabled by default, while `Rule.Numbers` remains disabled. Email-specific syntax concerns are adjusted separately, so username-oriented length, whitespace, separator, blocked-character, and shape checks are not applied as ordinary username restrictions.

## Customize local-part identity checks

Email syntax and Core identity checks are separate. `EmailOptions.LocalPartOptions` exposes the Core `Options` used for the local part.

Use the same narrow exception APIs when an email naming convention needs them:

```csharp
var options = new EmailOptions();

options.LocalPartOptions.AllowIdentifierForRule(
    "Charlotte",
    Rule.PopularCityNames);

options.LocalPartOptions.AllowedIdentifiers.Add(
    "supportive");

options.LocalPartOptions.Reserve(
    "billingdesk",
    ReservedMatchMode.Exact);
```

Username-specific shape and separator rules are already relaxed by the Email package because valid email local parts have different syntax requirements. Protected identity data, reserved-name matching, and application reservations still apply.

A Core exception changes only the local-part identity checker. It does not disable protected-domain typo, confusable, or label-reuse detection.

## Existing vs newly issued addresses

Use `CheckExistingAddress(...)` for addresses that already exist outside your application.

Use `CheckNewAddress(...)` when your application is issuing a new address. If `IssuingDomains` is configured, a new address must use one of those domains or a real subdomain.

```csharp
var existing = checker.CheckExistingAddress("bluegarden@lidi.nl");
var issued = checker.CheckNewAddress("bluegarden@lidl.nl");
```

## Protected domains

```csharp
var options = new EmailOptions();

options.ProtectedDomains.Add("example.com");
options.ProtectedDomains.Add("example.org");
```

Exact protected domains and their real subdomains are accepted. Lookalike or misleading variants can be rejected.

Protected-domain matching checks:

1. DNS/IDN normalization to lowercase ASCII.
2. Exact protected domains and legitimate subdomains.
3. Embedded protected domains such as `example.com.attacker.net`.
4. Selected Unicode and ASCII confusables.
5. Bounded Damerau-Levenshtein typo distance, including adjacent transpositions.
6. Protected registrant-label reuse on other TLDs or lure labels.

The default maximum typo distance is `1`.

```csharp
options.MaximumDomainEditDistance = 2;
```

Supported values are 0 through 2.

## Issuing domains

```csharp
options.IssuingDomains.Add("example.com");
```

Issuing domains are automatically treated as protected domains.

## Detection controls

```csharp
options.DetectUnicodeLookalikes = true;
options.DetectTypographicalLookalikes = true;
options.DetectProtectedLabelReuse = true;
```

## Result diagnostics

`EmailResult` exposes the primary `EmailFailureKind`, the local-part Unclaimable result, `DomainLookalikeKind`, and the matched protected domain.

A local-part rejection remains the primary failure when both the local part and domain are suspicious, while the domain diagnostic is still retained.

For example, `admin@lidi.nl` can report a reserved local part while also reporting the `lidl.nl` typo.

## Syntax scope

The package validates practical unquoted mailbox local parts plus DNS/IDN domain shape.

It does **not** perform DNS or MX lookups and does not prove that a domain or mailbox exists.
