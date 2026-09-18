# Unclaimable

Strict, fast username and identifier validation for .NET.

**Current release: 0.7.6**

Unclaimable helps decide whether a username, handle, slug, account name, tenant name, or similar identifier should be claimable. It combines curated reserved-name datasets with structural validation, configurable matching, identifier-shape checks, Unicode-aware protections, optional protected-identity lists, and ASP.NET Core integration.

## Install

```bash
dotnet add package Unclaimable --version 0.7.6
```

ASP.NET Core integration:

```bash
dotnet add package Unclaimable.AspNetCore --version 0.7.6
```

Email identity protection:

```bash
dotnet add package Unclaimable.Email --version 0.7.6
```

Large optional datasets:

```bash
dotnet add package Unclaimable.Extended --version 0.7.6
```

## What's new in 0.7.6

0.7.6 adds `Unclaimable.Extended`, a `netstandard2.0` sibling package containing 36,313 optional additional identifiers across companies, regional brands, finance, government, international organizations, sports, education, media, transport, healthcare, historical/public figures, celebrities, fiction, entertainment, professions, multilingual vocabulary, regional slang/profanity, crypto, platforms, and geography.

## Unclaimable.Extended

0.7.6 adds the optional `Unclaimable.Extended` package. It uses the Core matching engine and contributes a much larger identity snapshot without moving or removing anything that already ships in `Unclaimable`.

Installing the package alone does not change validation behavior. Enable it explicitly:

```csharp
using Unclaimable;
using Unclaimable.Extended;

var options = new Options();
options.UseExtendedData();

var checker = new Checker(options);
```

All Extended groups are enabled once you opt in. Disable only the groups your application does not need:

```csharp
options.UseExtendedData(extended =>
{
    extended.DisableCategory(ExtendedCategory.Celebrities);
    extended.DisableCategory(ExtendedCategory.Sports);
    extended.AllowedIdentifiers.Add("Aalborg University");
});
```

The first 0.7.6 snapshot contains **36,313 additional identifiers**:

| Extended group | Entries |
| --- | ---: |
| Companies | 12,500 |
| Education | 10,155 |
| Geography / administrative subdivisions | 3,722 |
| Transport, airports and operators | 3,446 |
| Sports clubs, teams and leagues | 2,740 |
| Financial institutions | 1,105 |
| Regional brands | 616 |
| Healthcare / pharma | 608 |
| Media organizations | 260 |
| Professions / titles | 159 |
| Celebrities | 120 |
| Crypto projects | 120 |
| Fictional characters / franchises | 114 |
| Platforms / services | 99 |
| Historical figures | 96 |
| Entertainment properties | 91 |
| Government / public bodies | 86 |
| Public figures | 74 |
| Multilingual reserved vocabulary | 72 |
| Regional slang / profanity | 69 |
| International organizations | 61 |

Extended entries use `ReservedMatchMode.WholeIdentifier`: exact, compact, selected Unicode-confusable, and obfuscation checks still apply, but these large datasets do not become arbitrary substring roots. Core entries are indexed first, so an identifier already protected by Core keeps its existing Core match/category when Extended is enabled.

Large imported sets are embedded as deterministic snapshots; the package performs no runtime data downloads. Source/provenance information is shipped in `data/SOURCES.md` inside the package.

### Email identity protection from 0.7.5

0.7.5 added `Unclaimable.Email`, a `netstandard2.0` sibling package that applies Unclaimable to an email local part and protects configured domains against common impersonation forms.

```csharp
using Unclaimable.Email;

var options = new EmailOptions();
options.ProtectedDomains.Add("lidl.nl");
options.IssuingDomains.Add("lidl.nl");

var checker = new EmailChecker(options);

var external = checker.CheckExistingAddress("admin@lidi.nl");
var created = checker.CheckNewAddress("bluegarden@lidl.nl");
```

Both existing and newly issued addresses run the local part through Unclaimable. Protected-domain diagnostics include typo/transposition, common Unicode and ASCII confusables, protected-label reuse, and embedded protected domains. Exact protected domains and their subdomains are accepted.

The package validates practical unquoted mailbox syntax and DNS/IDN domain shape. It does not perform DNS or MX lookups and does not prove that a mailbox exists.

#### Protected-domain checks and tested spoof forms

Protected-domain matching is deterministic and runs in this order:

1. Parse and IDN-normalize the domain to lowercase ASCII.
2. Accept an exact configured protected domain or a real subdomain of it.
3. Reject an embedded protected domain such as `google.com.attacker.com`.
4. Decode IDN/punycode back to Unicode and compare a confusable skeleton. This covers selected Greek/Cyrillic lookalikes and common ASCII substitutions such as `0/o`, `2/z`, `3/e`, `4/a`, `5/s`, `6|9/g`, `7/t`, `8/b`, and `1/l`.
5. Apply bounded Damerau-Levenshtein typo matching. The default maximum distance is `1`, so one insertion, deletion, substitution, or adjacent transposition is suspicious.
6. Reject reuse of the protected registrant label on another TLD, hyphenated lure domain, or unrelated domain hierarchy.

The v0.7.5 regression suite exercises McDonald's, Nike, Google, Amazon, Visa, and Nvidia. Representative outcomes:

| Protected domain | Candidate domain | Result | Why |
| --- | --- | --- | --- |
| `mcdonalds.com` | `mcdonalds.com` | allowed | exact configured domain |
| `mcdonalds.com` | `mail.mcdonalds.com` | allowed | genuine subdomain |
| `mcdonalds.com` | `mcdonald.com` | `Typographical` | one deletion |
| `mcdonalds.com` | `mcdnoalds.com` | `Typographical` | adjacent transposition |
| `mcdonalds.com` | `mcd0nalds.com` | `Confusable` | ASCII `0/o` |
| `mcdonalds.com` | `mcdоnalds.com` | `Confusable` | Cyrillic `о` for Latin `o` |
| `mcdonalds.com` | `xn--mcdnalds-pbh.com` | `Confusable` | punycode decodes to the Unicode homograph |
| `mcdonalds.com` | `mcdonalds.net` | `ProtectedLabelReuse` | protected label on another TLD |
| `mcdonalds.com` | `mcdonalds-login.com` | `ProtectedLabelReuse` | protected label reused in a lure label |
| `mcdonalds.com` | `mcdonalds.com.attacker.com` | `EmbeddedProtectedDomain` | real domain text embedded before an attacker-controlled suffix |
| `nike.com` | `nkie.com` | `Typographical` | adjacent transposition |
| `nike.com` | `nik3.com` | `Confusable` | ASCII `3/e` |
| `nike.com` | `nіke.com` | `Confusable` | Cyrillic `і` for Latin `i` |
| `google.com` | `gogle.com` | `Typographical` | one deletion |
| `google.com` | `gooogle.com` | `Typographical` | one insertion |
| `google.com` | `googel.com` | `Typographical` | adjacent transposition |
| `google.com` | `g00gle.com` | `Confusable` | repeated ASCII `0/o` |
| `google.com` | `xn--gogle-rce.com` | `Confusable` | punycode Unicode homograph |
| `google.com` | `google.co` | `Typographical` | TLD deletion |
| `amazon.com` | `Amazon.com` | allowed | domain comparison is case-insensitive after normalization |
| `amazon.com` | `amazone.com` | `Typographical` | explicit one-character insertion case |
| `amazon.com` | `amaz0n.com` | `Confusable` | ASCII `0/o` |
| `amazon.com` | `ama2on.com` | `Confusable` | ASCII `2/z` |
| `visa.com` | `vsia.com` | `Typographical` | adjacent transposition |
| `visa.com` | `vi5a.com` | `Confusable` | ASCII `5/s` |
| `visa.com` | `vіsa.com` | `Confusable` | Cyrillic `і` |
| `nvidia.com` | `nvidai.com` | `Typographical` | adjacent transposition |
| `nvidia.com` | `nvidi4.com` | `Confusable` | ASCII `4/a` |
| `nvidia.com` | `nvіdia.com` | `Confusable` | Cyrillic `і` |
| `nvidia.com` | `nvidia.net` | `ProtectedLabelReuse` | protected label on another TLD |

The test suite also covers substitutions, extra/missing letters, uppercase domain input, nested real subdomains, hyphen-prefix and hyphen-suffix lure domains, multiple protected brands in the same checker, and distance-2 typo matching when `MaximumDomainEditDistance = 2`.

A local-part rejection remains the primary `EmailFailureKind` when both sides fail, but the domain result is still retained. For example, `admin@lidi.nl` can report `ReservedLocalPart` while `DomainLookalikeKind` still reports the `lidl.nl` typo.

The Unicode/confusable and leetspeak mapping table used for domain skeletons is shared from the `Unclaimable` core package. `Unclaimable.Email` adds domain-specific IDN/punycode normalization and protected-domain policy on top of that shared base, so generic confusable fixes are made once in Core.

The email-domain test suite is also data-driven. It loads the built-in core and extended `brands` and `technology` datasets and derives protected registrant labels from them, rather than relying only on a fixed list of example companies. In the current v0.7.5 dataset this produces **913 distinct protected labels** from 972 raw entries.

For every derived label, the suite verifies exact-domain and real-subdomain acceptance plus generated deletion, insertion, substitution, adjacent-transposition, alternate-TLD, hyphen-lure, and embedded-domain attacks. Where the label contains supported lookalike characters, the same generated suite also checks ASCII confusables, Unicode homographs, and their punycode representations. Current coverage includes ASCII-confusable generation for **901** labels, Unicode/punycode generation for **912** labels, and transposition generation for all **913** labels. Future compatible additions to those datasets automatically become new email-domain behavior tests.


All first-party packages use version `0.7.6`.

## Protected identity lists from 0.7.4

0.7.4 adds ten opt-in protected-identity lists. All are disabled by default:

```csharp
Rule.Nationalities
Rule.Currencies
Rule.Religions
Rule.Landmarks
Rule.Events
Rule.Awards
Rule.FictionalCharacters
Rule.Franchises
Rule.Professions
Rule.Military
```

Enable only the lists your application needs:

```csharp
builder.Services.AddUnclaimable(options =>
{
    options.EnableRule(
        Rule.Nationalities |
        Rule.Currencies |
        Rule.FictionalCharacters |
        Rule.Franchises);
});
```

Representative protected identities include `dutch`, `euro`, `bitcoin`, `christianity`, `eiffeltower`, `olympics`, `nobelprize`, `darthvader`, `starwars`, `doctor`, and `airforce`.

These lists use whole-identifier protection. They support normalized exact matching, case-insensitive matching, compact separator/punctuation forms, configured obfuscation/leetspeak matching, and selected Unicode-confusable matching. They do **not** become generic substring roots, so values such as `doctorwho`, `starwarsfan`, or `americanfootball` are not rejected merely because they contain a protected identity.

## Celebrity-name protection from 0.7.3

0.7.3 added the opt-in `Rule.CelebrityNames` list with 100 protected high-profile identity forms, including values such as `trump`, `donaldtrump`, `taylorswift`, `cristianoronaldo`, `messi`, `elonmusk`, and `mrbeast`.

```csharp
options.EnableRule(Rule.CelebrityNames);
```

Celebrity matching follows the same whole-identifier contract as the 0.7.4 identity lists. `trump`, `TRUMP`, compact forms, supported obfuscations, and selected confusable forms can be rejected without turning `trump` into a broad substring rule that blocks unrelated words such as `trumpet`.

## Practical defaults from 0.7.2

Ordinary mixed alphanumeric usernames are allowed by default:

```text
user7       -> can be claimable
john2026    -> can be claimable
player123   -> can be claimable
```

`Rule.Numbers` is disabled by default, while `Pattern.NumericOnly` remains enabled. Therefore:

```text
123456789   -> rejected as NumericOnly
```

Applications that want to reject every decimal digit can enable that rule explicitly:

```csharp
options.EnableRule(Rule.Numbers);
```

0.7.2 also added two opt-in geography rules:

```csharp
Rule.CountryNames
Rule.PopularCityNames
```

They are whole-identifier checks. Enabling them can reject `france`, `United Kingdom`, `amsterdam`, or `New York` without turning those names into generic substring filters.

## Reserved-vocabulary expansion from 0.7.1

The 0.7.1 work expanded exact reserved-name coverage across authentication, automation, commerce, communications, community, developer, finance, governance, identity, infrastructure, legal, moderation, official, operations, security, and system vocabulary.

Representative exact values include `member`, `vote`, `active`, `authentication`, `announcement`, `apikey`, `treasury`, `username`, `loadbalancer`, `banned`, `verified`, `operations`, `buyer`, `legalhold`, and `phishing`.

Generic values remain exact rather than broad substring roots: `vote` does not block `devote`, `member` does not block `rememberme`, and `active` does not block `hyperactive`.

## Quick start

```csharp
using Unclaimable;

var checker = new Checker();

var result = checker.Check("candidate7");
if (result.IsClaimable)
{
    // Continue with your own database/availability check.
}
```

ASP.NET Core:

```csharp
builder.Services.AddUnclaimable();
```

`null` is accepted by Unclaimable so required-field validation can remain a separate concern, for example through `[Required]`.

## Default core protection in 0.7.6

The default policy includes:

- all 23 built-in reserved-name categories;
- English localized data;
- exact reserved-name matching;
- compact punctuation/separator matching;
- dataset-authorized partial matching;
- curated profanity compounds;
- common leetspeak and bounded obfuscation detection;
- selected Unicode-confusable lookalikes;
- malformed UTF-16 rejection;
- invisible-only, control-character, and format-character protection;
- minimum length `3` and maximum length `32`;
- whitespace restrictions;
- built-in `-` and `_` blocking;
- leading and trailing separator restrictions;
- numeric-only, repeated-pattern, symbol-only, and conservative ASCII-art checks.

The following protections are disabled by default and must be explicitly enabled when wanted:

- `Rule.Numbers`;
- `Rule.CountryNames`;
- `Rule.PopularCityNames`;
- `Rule.CelebrityNames`;
- `Rule.Nationalities`;
- `Rule.Currencies`;
- `Rule.Religions`;
- `Rule.Landmarks`;
- `Rule.Events`;
- `Rule.Awards`;
- `Rule.FictionalCharacters`;
- `Rule.Franchises`;
- `Rule.Professions`;
- `Rule.Military`;
- `Pattern.UppercaseOnly`;
- generic profanity substring matching;
- ASCII-only input restriction.

Passing or disabling one deny rule never positively clears an identifier through the rest of the pipeline.

## Rule configuration

Use the incremental helpers rather than replacing the full legacy rule mask:

```csharp
var options = new Options();

options.EnableRule(Rule.CountryNames | Rule.CelebrityNames);
options.DisableRule(Rule.Whitespace);
options.EnableRule(Rule.Numbers);
```

`Options.EnabledOptionalRules` exposes currently enabled opt-in rules.

`DisabledRules` remains for compatibility as a full mask. Assigning it replaces the mask; prefer `EnableRule(...)` and `DisableRule(...)` for incremental configuration.

## Pattern configuration

Pattern checks are configured independently from rule flags:

```csharp
var options = new Options();

options.EnablePattern(Pattern.UppercaseOnly);
options.DisablePattern(Pattern.AsciiArt);
options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);
```

Default pattern state:

```text
NumericOnly    on
Repeated       on
SymbolOnly     on
AsciiArt       on
UppercaseOnly  off
```

## Category selection

Every built-in category is enabled by default:

```csharp
options.DisableCategory(Category.Brands);
options.DisableCategory(Category.Technology);
```

Disabled categories are excluded before exact, compact, partial, Unicode-confusable, and obfuscation indexes are constructed.

## Exact allowed identifiers

`AllowedIdentifiers` is a narrow exception for complete built-in reserved identifiers after trim → NFKC → invariant-lowercase normalization.

```csharp
options.AllowedIdentifiers.Add("superadmin");
```

It does not bypass structural rules, pattern rules, opt-in geography/protected-identity rules, or explicit application reservations.

## Application reservations

```csharp
options.Reserve("acme", matching: ReservedMatchMode.Exact);
options.Reserve("internalbot", matching: ReservedMatchMode.Default);
```

`ReservedMatchMode.Exact` performs whole-identifier matching. `ReservedMatchMode.Default` participates in the configured matching pipeline.

## Language support

English is enabled by default. Additional localized datasets are available for Dutch, German, French, Spanish, Italian, Portuguese, Polish, Turkish, Indonesian, Czech, Vietnamese, Hungarian, Swedish, and Romanian.

```csharp
options.AddLanguage(Language.Dutch);
options.AddLanguage(Language.German);
```

## Unicode scope

Unclaimable uses Unicode NFKC normalization and selected confusable mappings for common impersonation characters, especially common Greek and Cyrillic lookalikes.

This is **not** a complete Unicode Technical Standard #39 implementation. Passing Unclaimable does not prove that no visual spoofing technique exists.

Applications should separately define canonical username storage, case sensitivity, database uniqueness/collation, display-name behavior, and URL/routing normalization.

## Detailed results

```csharp
var result = checker.Check(userName);

Console.WriteLine(result.IsClaimable);
Console.WriteLine(result.Category);
Console.WriteLine(result.MatchKind);
Console.WriteLine(result.MatchedValue);
```

For multiple diagnostics:

```csharp
var detailed = checker.CheckDetailed(userName, includeMessages: true);
```

## Release quality

The 0.7.5 release line is validated with **3,560 passing tests**, package-content validation, packaged-consumer smoke tests, public-API compatibility checks, source builds without localized language packs, and a production line-coverage gate covering `Unclaimable`, `Unclaimable.AspNetCore`, `Unclaimable.Email`, and `Unclaimable.Extended`.

Latest measured production line coverage: **98.26%** (`2,147 / 2,185`); branch coverage: **83.55%** (`1,366 / 1,635`). The engineering target is **100%** and CI enforces a **98% minimum**.

Full documentation and source:

https://github.com/Perry3Dnl/Unclaimable
