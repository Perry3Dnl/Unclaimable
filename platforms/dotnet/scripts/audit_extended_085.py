#!/usr/bin/env python3
"""Release dataset audit for Extended JSON; stdlib only."""
import collections
import json
import pathlib
import sys
import unicodedata

root = pathlib.Path(__file__).resolve().parents[3]
source = root / "platforms/dotnet/src/Unclaimable.Extended/Data"
files = sorted(source.glob("*.json"))
errors = []
warnings = []
raw = []
by_category = collections.Counter()
normalized = collections.defaultdict(list)
seen_per_file = collections.defaultdict(list)
for file in files:
    try:
        document = json.loads(file.read_text(encoding="utf-8"))
    except (ValueError, UnicodeError) as exc:
        errors.append(f"{file.name}: invalid JSON: {exc}")
        continue
    category = document.get("category")
    values = document.get("values")
    if document.get("schema") != 1 or not isinstance(category, str) or not category:
        errors.append(f"{file.name}: invalid schema/category")
    if not isinstance(values, list):
        errors.append(f"{file.name}: values is not a list")
        continue
    for idx, value in enumerate(values):
        loc = f"{file.name}:{idx + 1}"
        if not isinstance(value, str) or not value.strip():
            errors.append(f"{loc}: empty or non-string value")
            continue
        if value != value.strip():
            warnings.append(f"{loc}: leading/trailing whitespace")
        if any(unicodedata.category(ch) == "Cc" for ch in value):
            errors.append(f"{loc}: contains control character")
        key = unicodedata.normalize("NFKC", value.strip()).lower()
        normalized[key].append((category, loc, value))
        seen_per_file[(file.name, key)].append(loc)
        by_category[category] += 1
        raw.append((category, value))
duplicates = {k: v for k, v in seen_per_file.items() if len(v) > 1}
cross_category = {k: v for k, v in normalized.items() if len({x[0] for x in v}) > 1}
print(f"EXTENDED_AUDIT files={len(files)} raw_entries={len(raw)} normalized_unique={len(normalized)}")
print(f"EXTENDED_AUDIT duplicate_keys_within_file={len(duplicates)} cross_category_keys={len(cross_category)}")
print("COUNTS", json.dumps(dict(sorted(by_category.items())), ensure_ascii=False))
for (filename,key),locs in list(sorted(duplicates.items()))[:30]:
    print("DUPLICATE",filename,repr(key),locs[:8])
for key,vals in list(sorted(cross_category.items()))[:30]:
    print("CROSS_CATEGORY",repr(key), sorted(set(x[0] for x in vals)))
for message in warnings[:30]:
    print("WARNING", message)
for message in errors[:60]:
    print("ERROR", message)
print(f"TOTAL errors={len(errors)} warnings={len(warnings)}")
if errors:
    sys.exit(1)
