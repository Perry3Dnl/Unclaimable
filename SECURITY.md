# Security policy

Unclaimable is a defense-in-depth username and identifier policy library. Security reports are welcome, especially when an issue could let an attacker bypass documented protection, cause denial of service, or compromise the package/release process.

## Reporting a vulnerability

Please do **not** publish exploit details in a normal GitHub issue.

Use GitHub's private vulnerability reporting flow for this repository when it is available:

1. Open the repository's **Security** tab.
2. Choose **Report a vulnerability**.
3. Include the affected version, a minimal reproducer, expected behavior, actual behavior, and the practical security impact.

Direct advisory URL: `https://github.com/Perry3Dnl/Unclaimable/security/advisories/new`

If GitHub does not present the private reporting option, open a public issue containing **only** a request for private security contact. Do not include the vulnerability details, payload, proof of concept, or affected production identifiers in that public issue.

Please give the maintainer a reasonable opportunity to investigate and prepare a fix before public disclosure.

## Supported versions

Unclaimable is currently pre-1.0 and evolves by minor release line.

| Release line | Security support |
| --- | --- |
| Latest published minor | Supported for security fixes |
| Immediately previous published minor | Critical/high-impact fixes may be backported when practical |
| Older release lines | No guaranteed security fixes |
| Unreleased branches/commits | Best-effort development support only |

Users should upgrade to the latest stable package when a security fix is released.

## What is a security issue?

Examples that should normally be reported privately include:

- a practical bypass of behavior that the documentation explicitly guarantees, especially for protected system, support, authentication, security, or administrative identities;
- malformed or adversarial input that can reliably crash, hang, exhaust memory, or create disproportionate CPU work;
- a normalization, compact, partial, obfuscation, or Unicode-confusable bug that produces a concrete security bypass outside the documented limitations;
- a package integrity, CI, release, signing/provenance, or dependency-compromise issue;
- a vulnerability in the ASP.NET Core integration that changes validation or trust boundaries unexpectedly.

A report is much easier to evaluate when it includes the exact input, checker configuration, package version, result, and why the behavior is security-sensitive.

## What is normally a dataset or policy issue?

The following are usually appropriate for a normal public issue unless they reveal an exploitable vulnerability that should remain confidential:

- a legitimate username that is incorrectly blocked;
- a reserved term that should be added, removed, or moved to another category;
- a request to add or change a language dataset;
- disagreement about whether a term should be exact-only or partial-safe;
- ordinary false positives or false negatives within the documented policy boundaries;
- requests for broader Unicode coverage beyond the selected confusable mapping;
- behavior caused by the documented 32-candidate obfuscation expansion bound;
- requests for new matching heuristics or stricter application-specific policy.

Dataset and policy reports are still important. Built-in data directly affects application behavior, and confirmed false positives are treated as policy defects. See [`DATASET_POLICY.md`](DATASET_POLICY.md) for curation and review rules.

## Documented security boundaries

A successful Unclaimable check is not a proof that an identifier is globally safe or unique.

In particular:

- Unicode-confusable matching uses a selected mapping and is **not** a complete Unicode Technical Standard #39 implementation;
- obfuscation expansion is deliberately bounded and is not an exhaustive decoder of every ambiguous substitution combination;
- minimum and maximum identifier lengths use .NET UTF-16 code units (`string.Length`), not Unicode scalar values or grapheme clusters;
- Unclaimable does not replace database uniqueness, authorization checks, account-to-account impersonation logic, canonical username storage, rate limiting, or application-specific abuse controls.

Reports that demonstrate a bug **within** these documented contracts are welcome as security reports. Requests to broaden the contracts are normally feature or policy issues.

## Disclosure and fixes

When a report is accepted as a vulnerability, the maintainer will aim to:

- reproduce and assess the impact;
- determine affected release lines;
- prepare regression coverage with the fix;
- publish a patched package and release notes when appropriate;
- coordinate public disclosure after users have a reasonable upgrade path.

No bug-bounty or guaranteed response-time program is currently offered.