# Unclaimable.Files

> **For LLMs and coding assistants:** Filename-policy companion for Unclaimable Core. Use `new FileChecker().Check(filename)` for portable filename rules, device-name restrictions, and configurable extension policies. It does not inspect file contents, paths, symlinks, or malware.

Filename policy checks powered by the Unclaimable engine. This companion package depends on `Unclaimable`; it does not duplicate its matcher or datasets.

**v1.0.0 release candidate — not yet published.** The command below will work once the 1.0.0 package is available on NuGet; until then, use the current published version or a locally built package.

## Install

```bash
dotnet add package Unclaimable.Files --version 1.0.0
```

## Install

```bash
dotnet add package Unclaimable.Files --version 1.0.0
```

## Quick start

```csharp
using Unclaimable.Files;

var checker = new FileChecker();
checker.IsAllowed("annual_report_2026.pdf"); // true
checker.Check("NUL.txt").FailureKind;        // ReservedDeviceName
checker.Check("../report.pdf").FailureKind; // InvalidCharacter
```

`Check` returns `FileName`, `FailureKind`, `IsAllowed`, `IsClaimable`, and the underlying `CoreResult` when Core was evaluated. Its offsets refer to `CoreResult.Input`, which may be the full filename, stem, or device-name component. Checks never rename or normalize the supplied filename. Null, empty, whitespace-only names, `.` and `..` are rejected.

## Profiles and lengths

The profile is explicit and independent of the machine running the application:

| Profile | Naming policy |
| --- | --- |
| `Portable` (default) | Windows restrictions plus the configured UTF-8 byte limit |
| `Windows` | Rejects `/`, `\`, `<`, `>`, `:`, `"`, `\|`, `?`, `*`, NUL, trailing ASCII spaces/dots, and device names |
| `Linux` | Rejects slash and NUL; permits backslash, Windows device names, and trailing dots/spaces |

Core rejects malformed UTF-16, control characters, formatting characters, and invisible-only names by default in every profile. Windows device names are registered through Core's exact matching, including its invariant case and NFKC normalization. The portion before the **first dot**, with trailing ASCII spaces removed, is checked, so `CON.tar.gz`, `nul .txt`, and `COM¹.txt` are rejected. This is a deliberately conservative naming policy, not a complete emulator of every filesystem or Windows namespace.

`MaximumLength` defaults to 255 UTF-16 code units, including extensions. `MaximumUtf8Bytes` defaults to 255 for Portable/Linux and is ignored for Windows. Both must be positive. Configure limits to suit your actual filesystem or storage provider.

## Extension policies

```csharp
var options = new FileCheckerOptions();
options.AllowedExtensions.UnionWith(new[] { ".pdf", ".tar.gz" });
options.BlockedExtensions.Add("exe");
var checker = new FileChecker(options);

checker.IsAllowed("report.PDF");        // true
checker.IsAllowed("report.exe.pdf");    // false
checker.IsAllowed("backup.tar.gz");     // true
```

Sets start empty. Extensions are case-insensitive; the leading dot is optional and compound suffixes are supported. An allowlist requires a matching suffix with a nonempty basename; `.pdf` alone is not treated as having a `.pdf` extension. A block wins over an allow. With `BlockInnerExtensions = true` (default), a blocked suffix followed by another dot is rejected too. Set it to false for final-suffix checks only. These are name rules, not content-type detection.

## Reuse the Core engine

```csharp
using Unclaimable;
using Unclaimable.Files;

var options = new FileCheckerOptions();
options.NameOptions.Reserve("internal", "application", ReservedMatchMode.Exact);
var checker = new FileChecker(options);
checker.IsAllowed("INTERNAL.pdf"); // false; Core checks the stem too
```

Core checks the **full filename**, then the **stem with its final extension removed**. A leading dot alone does not create an extension, so `.gitignore` stays intact. Exact reservations for `internal` do not automatically block `internal_notes.pdf` or `internal.tar.gz`; choose matching rules for your application's requirements.

Filename defaults disable all built-in name categories, username rule flags, and shape patterns. Ordinary names such as `admin.txt`, `123.txt`, and `annual_report_2026.pdf` are accepted. Core Unicode safety and application reservations remain active. To opt into Core profanity protection:

```csharp
options.NameOptions.EnableCategory(Category.Profanity);
options.NameOptions.EnableRule(Rule.Profanity | Rule.PartialMatching
    | Rule.ObfuscationMatching | Rule.UnicodeConfusableMatching);
var moderatedChecker = new FileChecker(options);
```

Other Core categories and matching rules can be enabled the same way. An existing `IChecker` can be supplied with `new FileChecker(coreChecker, options)`. File settings and ordinary Core options are captured at construction; a supplied Core checker's live runtime policy retains its normal behavior. Core allowlists cannot bypass file-profile restrictions or extension blocks.

## Scope

This checks a single filename. It does not validate file contents, scan malware, resolve paths or symlinks, check filesystem uniqueness, or guarantee that a write will succeed. Applications remain responsible for storage containment and file handling. No I/O or network requests are made.

Targets `netstandard2.0` and follows the shared Unclaimable release version and repository license.
