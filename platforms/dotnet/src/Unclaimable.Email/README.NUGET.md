# Unclaimable.Email

Email-address identity and protected-domain impersonation checks.

**Package version: 0.7.7**

## Install

```bash
dotnet add package Unclaimable.Email --version 0.7.7
```

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
