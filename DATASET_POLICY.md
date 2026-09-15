# Dataset policy and provenance

Unclaimable's built-in reserved-name data is part of the package's public behavior. A dataset edit can make an identifier newly claimable or newly unclaimable without changing any C# API, so data changes are reviewed with the same care as code changes.

## Current provenance model

The datasets shipped by Unclaimable are maintained as version-controlled JSON under `data/` and are embedded into the package at build time. The build does **not** download, scrape, or merge remote word lists at runtime or during packaging.

The repository currently treats checked-in entries as project-curated data unless a dataset file, commit, or pull request explicitly records an external source. Names may correspond to public products, services, roles, protocol terms, operational identities, ordinary language, or profanity; inclusion is a policy decision and does not imply affiliation with any third party.

Do not add bulk third-party lists with unknown provenance. If external data is proposed, the pull request must identify the source, retrieval/version date, license or other redistribution basis, and any required attribution. Data with unclear redistribution rights must not be merged.

## Curation goals

A built-in value should have a clear reason to be protected by default. The main goals are to protect:

- platform/system identities and operational endpoints;
- support, authentication, moderation, security, governance, and other impersonation-sensitive roles;
- globally recognizable brands or technology identities where impersonation risk is material;
- selected profanity and abusive compounds according to the documented category scope;
- localized equivalents in the supported language datasets.

The datasets are not intended to be a general dictionary, a comprehensive trademark database, a complete content-moderation corpus, or a complete anti-impersonation system.

Ordinary personal names and plausible legitimate usernames should remain claimable whenever possible.

## Exact reservation versus partial matching

These are separate policy decisions.

- `values` reserves the complete normalized identifier and participates in the configured exact/compact/confusable/obfuscation pipeline.
- schema-v2 `partialValues` does the same **and** explicitly authorizes substring matching.
- schema-v2 combinations with `"partial": true` explicitly authorize each expanded root+suffix value for substring matching.

A term being important enough to reserve does not automatically make it safe as a substring rule. Short or ambiguous terms such as `admin`, `help`, `support`, `staff`, `root`, `apple`, and `nike` are examples of values that can create unrelated false positives when treated as generic substrings.

Short partial-safe values require heightened review because collision risk rises as a token gets shorter. CI deliberately does **not** use a magic token length as proof that a partial rule is safe: short partial-safe entries are surfaced for review and checked against the known-safe corpus. They are permitted when the policy rationale is strong and the regression evidence supports them. The configured runtime `PartialMatchMinimumLength` remains an independent lower bound on which eligible values can actually participate in partial matching.

## Review requirements for dataset changes

Every dataset pull request should answer the following questions:

1. **Why should this identifier be reserved by default?** State the impersonation, platform-safety, or policy rationale.
2. **Where did it come from?** Identify whether it was manually curated, derived from a public official term, or imported from an external dataset. External sources require provenance and licensing details.
3. **Which category and language own it?** Prefer the narrowest correct category and avoid unnecessary cross-category duplication.
4. **Should it be exact-only or partial-safe?** Partial matching requires a stronger justification because it can reject unrelated larger identifiers. Short partial-safe values require especially careful review.
5. **What are the likely false positives?** Check ordinary words, personal-name patterns, project/company-style identifiers, gaming handles, developer usernames, and multilingual identifiers.
6. **What regression data changes with it?** Add newly discovered legitimate identifiers to `conformance/safe-usernames.json` and add important blocked behavior to the reserved conformance/tests.
7. **What does the policy diff show?** Review newly blocked identifiers, newly allowed identifiers, partial-match additions/removals, category changes, and total dataset deltas in CI.

## Automated policy gates

CI runs dataset-specific checks in addition to the .NET unit suite.

The current-data guard:

- expands schema-v2 combinations exactly as the runtime loader does;
- checks for malformed/duplicate concrete entries within a dataset;
- surfaces short explicitly partial-safe entries for heightened review instead of assuming a fixed length threshold proves safety;
- compares **every short reserved token** against the full known-safe username corpus;
- fails if a partial-safe short token would collide with a known-safe identifier;
- reports guarded collisions where a short exact-only token appears inside a known-safe identifier, making regressions such as `admin` → `badminton` visible.

The dataset-diff step compares the pull request to its base and reports:

- newly blocked concrete identifiers;
- newly allowed concrete identifiers;
- newly added and removed partial-match entries;
- category changes for an existing value/language pair;
- total and partial-entry count deltas.

These reports do not replace review. They make the behavioral surface of a data edit explicit.

## False-positive handling

A credible false positive is treated as a policy bug, not merely as an application-specific inconvenience.

When a false positive is confirmed:

- add the legitimate identifier (or representative pattern) to the known-safe corpus;
- identify the exact matching path that caused the rejection;
- prefer narrowing partial eligibility or category/data policy over adding broad runtime exceptions;
- preserve protection for the complete high-risk identifier when possible;
- document intentional behavior changes in the changelog/release notes.

Application-specific exceptions remain available through `AllowedIdentifiers`, category controls, and custom options, but they are not a substitute for fixing an overbroad built-in default.

## Licensing and contributions

Repository contributions are made under the repository's license and contribution terms. A contributor must have the right to submit any data they add.

For externally sourced data, an acceptable contribution must preserve any required notices and must have terms that permit the repository to redistribute the relevant material in the package. Do not copy data from proprietary, confidential, access-controlled, or license-unclear sources.

When in doubt, contribute a small manually curated set with documented rationale instead of importing a large list.

## Scope and future changes

Dataset governance is versioned policy. Large vocabulary expansions, category reclassifications, or substantial partial-match changes should normally ship in a release that calls out those changes explicitly.

A future schema may carry per-entry provenance metadata. Until then, the dataset file plus its pull-request/commit history is the provenance record.