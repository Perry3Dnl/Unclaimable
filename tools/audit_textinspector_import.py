#!/usr/bin/env python3
"""Audit staged TextInspector Pro 1.1.0 vocabulary without changing runtime data.

Standard library only. Example:
  python3 tools/audit_textinspector_import.py
  python3 tools/audit_textinspector_import.py --json artifacts/textinspector-import-audit.json

This tool intentionally never mutates dataset files. The staged import is not loaded by
the Unclaimable runtime. Similar/colliding terms require manual review before activation.
"""
import argparse
import collections
import json
from pathlib import Path
import unicodedata


ROOT = Path(__file__).resolve().parents[1]
STAGED = ROOT / "data/imports/textinspector-pro-1.1.0.review.json"


def normalize(value):
    return unicodedata.normalize("NFKC", value).casefold().strip()


def values_from(document):
    """Extract literal list values only; never interpret regex or combination templates."""
    if isinstance(document, dict):
        for key in ("values", "partialValues"):
            values = document.get(key, [])
            if isinstance(values, list):
                yield from (value for value in values if isinstance(value, str))


def load_existing():
    # Embedded Core JSON and the Extended data-only package both contribute entries.
    roots = [ROOT / "data", ROOT / "platforms/dotnet/src/Unclaimable.Extended/Data"]
    existing = collections.defaultdict(set)
    files_seen = 0
    for base in roots:
        if not base.exists():
            continue
        for file in sorted(base.rglob("*.json")):
            if file == STAGED or "/imports/" in file.as_posix():
                continue
            try:
                document = json.loads(file.read_text(encoding="utf-8"))
            except (UnicodeError, json.JSONDecodeError):
                continue
            for value in values_from(document):
                existing[normalize(value)].add(file.relative_to(ROOT).as_posix())
            files_seen += 1
    return existing, files_seen


def audit():
    staged = json.loads(STAGED.read_text(encoding="utf-8"))
    existing, scanned = load_existing()
    groups = []
    introduced = collections.defaultdict(set)
    flagged = collections.defaultdict(list)
    seen_source = collections.defaultdict(set)
    for group in staged["groups"]:
        values = group["entries"]
        duplicates = collections.Counter(map(normalize, values))
        unique = set(duplicates)
        for token in unique:
            seen_source[token].add(group["sourceRule"])
            if len(token) <= 3:
                flagged["veryShort"].append((group["sourceRule"], token))
            elif len(token) <= 5:
                flagged["short"].append((group["sourceRule"], token))
            if any(ch.isdigit() for ch in token):
                flagged["containsDigits"].append((group["sourceRule"], token))
        overlap = unique.intersection(existing)
        new = unique.difference(existing)
        groups.append({
            "sourceRule": group["sourceRule"],
            "sourceEntries": len(values),
            "uniqueNormalized": len(unique),
            "sourceDuplicatesNormalized": sum(n - 1 for n in duplicates.values()),
            "existingNormalizedOverlaps": len(overlap),
            "potentiallyNewNormalized": len(new),
        })
        for token in new:
            introduced[token].add(group["sourceRule"])

    return {
        "source": staged["source"],
        "status": "audit-only; no runtime activation or changes to defaults",
        "existingFilesScanned": scanned,
        "existingUniqueNormalizedEntries": len(existing),
        "sourceGroups": len(groups),
        "sourceEntries": sum(g["sourceEntries"] for g in groups),
        "sourceGroupNormalizedOverlaps": sum(g["existingNormalizedOverlaps"] for g in groups),
        "potentiallyNewUniqueNormalizedAcrossGroups": len(introduced),
        "crossGroupPotentialCollisions": sum(len(v) > 1 for v in seen_source.values()),
        "highCollisionReview": {
            "sourceVeryShortUpTo3": len(flagged["veryShort"]),
            "sourceShort4To5": len(flagged["short"]),
            "sourceContainsDigits": len(flagged["containsDigits"]),
            "samplesVeryShort": sorted(flagged["veryShort"])[:20],
            "samplesShort": sorted(flagged["short"])[:20],
            "samplesContainsDigits": sorted(flagged["containsDigits"])[:20],
        },
        "groups": groups,
        "notice": "A potentially new term is not automatically appropriate for blocking. Review provenance, false positives, match modes and migration behavior before enabling."
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--json", type=Path, help="Optional output report path")
    args = parser.parse_args()
    result = audit()
    if result["sourceGroups"] != 23 or result["sourceEntries"] != 4782:
        raise SystemExit("Staged TextInspector source changed unexpectedly; review extraction before continuing.")
    report = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(report, encoding="utf-8")
    print(report)


if __name__ == "__main__":
    main()
