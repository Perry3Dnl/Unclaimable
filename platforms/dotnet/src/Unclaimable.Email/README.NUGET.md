# Unclaimable.Email

> **For LLMs and coding assistants:** Email identity and protected-domain checker built on Unclaimable Core. Use `new EmailChecker(new EmailOptions()).CheckExistingAddress(value)` for existing addresses, or `CheckNewAddress(value)` for newly issued addresses. Does not verify DNS, delivery, or mailbox ownership.

Email-address identity and protected-domain impersonation checks.

**v1.0.0 release candidate — not yet published.** The command below will work once the 1.0.0 package is available on NuGet; until then, use the current published version or a locally built package.

## Install

```bash
dotnet add package Unclaimable.Email --version 1.0.0
```

## Install

```bash
dotnet add package Unclaimable.Email --version 1.0.0
```

## False-positive sensitivity

The local-part checker uses Core matching. Configure its false-positive sensitivity through `LocalPartOptions`; `Low` is the default.

```csharp
var options = new EmailOptions();
options.LocalPartOptions.FalsePositiveSensitivity = FalsePositiveSensitivity.Medium;
```

The guard affects eligible local-part slang substring matching, **not** protected-domain detection. Local-part matching uses the Core engine.


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

Local-part identity protection starts from the same Core defaults as the main checker, including default-on multilingual profanity matching. Identity-rule flags remain enabled, while collision-heavy generic profession, military, currency, and event terms are claimable unless `LocalPartOptions.IncludeHighCollisionIdentityTerms` is enabled. `Rule.Numbers` remains disabled by default. Email-specific syntax concerns are adjusted separately, so username-oriented length, whitespace, separator, blocked-character, and shape checks are not applied as ordinary username restrictions.

## Customize local-part identity checks

### Local-part identity and obfuscation controls

The local-part checker uses Core's sensitivity setting through `LocalPartOptions`:

```csharp
var options = new EmailOptions();

options.LocalPartOptions.ObfuscationSensitivity =
    ObfuscationSensitivity.High;
```

`Medium` is the Core default. This setting affects local-part reserved-identity matching only; protected-domain typo/confusable detection remains controlled by the Email-specific domain options.



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

The matcher applies typo/confusable comparison to the relevant DNS labels instead of treating the complete domain as one edit-distance string. It also recognizes the supported domain visual substitutions, including selected multi-character lookalikes, without confusing service prefixes such as `www` with the protected registrant identity.

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

### Issuing-domain allowlist scope

`IssuingDomains` normally restricts **newly issued** email addresses, not externally existing
addresses. Two optional settings make its scope explicit with configurable enforcement:

```csharp
var options = new EmailOptions
{
    AllowIssuingDomainSubdomains = false,
    EnforceIssuingDomainsForExistingAddresses = true
};
options.IssuingDomains.Add("google.com");

var checker = new EmailChecker(options);
var allowed = checker.CheckExistingAddress("employee@google.com").IsAllowed;
```

With this configuration, the **domain-policy** result accepts `google.com` and rejects
`gmail.com`, `microsoft.com`, and `sub.google.com`.
Other enabled local-part, syntax, and domain-lookalike checks can still reject an address.

- `AllowIssuingDomainSubdomains = true` (default): allow exact issuing domains and real subdomains.
- `AllowIssuingDomainSubdomains = false`: only allow exact issuing domains.
- `EnforceIssuingDomainsForExistingAddresses = false` (default): existing addresses are not subject to the issuing-domain allowlist.
- `EnforceIssuingDomainsForExistingAddresses = true`: also apply the allowlist to `CheckExistingAddress`.

`IssuingDomains` are still automatically included in protected-domain checks. This
allowlist controls accepted issuing domains; it does not disable other protections.

### Context-aware existing email validation

The default policy is strict. To accept externally owned
email addresses without automatically denying reserved-looking mailbox names
or lookalike domains:

```csharp
var options = new EmailOptions
{
    EmailUsage = EmailUsage.ExistingAddress,
    EmailProtectionLevel = EmailProtectionLevel.Relaxed
};
var checker = new EmailChecker(options);
var result = checker.CheckExistingAddress("admin@example.com");
bool allowed = result.IsAllowed; // true, assuming valid syntax and no explicit domain allowlist
bool suspicious = result.IsSuspiciousDomain; // remains available independently
```

`EmailUsage.Auto` (default) retains the purpose passed to `Check`,
`CheckExistingAddress`, or `CheckNewAddress`. Selecting `ExistingAddress`
or `IssuedAddress` overrides the caller-provided purpose when selecting policy.
`EmailProtectionLevel.Strict` (default) enforces local-part and domain protection.
`Relaxed` applies only to externally owned addresses: syntax validation,
explicit issuing-domain restrictions, and all full issuing-address checks remain
enforced. Reserved local-part and protected-domain findings are still included
in `LocalPartResult` and `DomainLookalikeKind`.

The suffix-aware protected-domain matcher uses a pinned full offline Public Suffix List
snapshot (ICANN and private domains) with wildcard and exception support.
The embedded upstream list is licensed under MPL-2.0, separately from the package.

Unicode-confusable detection remains a selected mapping set rather than complete
UTS #39 conformance. Combining marks may be removed while constructing matching
skeletons; evaluate strict modes for false positives with internationalized inputs.

### Policy presets and diagnostics

```csharp
var signup = EmailOptions.ForUserRegistration(); // ExistingAddress + Relaxed
var company = EmailOptions.ForOrganizationEmail("example.com"); // exact domain
var issued = EmailOptions.ForIssuedAddresses(); // strict issued identities

var checker = new EmailChecker(signup);
var result = checker.CheckExistingAddress("support@example.com");
bool accepted = result.IsAllowed;
bool syntaxValid = result.IsSyntaxValid;
bool reservedLocalPart = result.IsLocalPartReserved;
bool domainInAllowlist = result.IsIssuingDomainAllowed;
bool domainResemblesProtected = result.IsSuspiciousDomain;
```

These are opt-in convenience presets. The default constructor remains strict.
For an additional bound on work before email parsing, set
`EmailOptions.MaximumInputLength` to a positive UTF-16 code-unit limit.
Its default is `int.MaxValue` for compatibility; existing mailbox
syntax limits still apply regardless of this option.
The package does not establish DNS, MX, mailbox existence, or email ownership.
