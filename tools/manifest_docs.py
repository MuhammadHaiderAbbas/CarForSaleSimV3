#!/usr/bin/env python3
"""
Regenerates docs/FINAL_MAP_ASSETS.md from Assets/CarSales/FinalMap/final-map-assets.json so the
document always matches the manifest (kept assets, attribution, exclusions and why).

    python3 tools/manifest_docs.py
"""
import json
import collections
import datetime
import sys

MANIFEST = "Assets/CarSales/FinalMap/final-map-assets.json"
DOC = "docs/FINAL_MAP_ASSETS.md"

manifest = json.load(open(MANIFEST))
assets = manifest["assets"]
by_category = collections.OrderedDict()
for a in sorted(assets, key=lambda x: (x["category"], x["key"])):
    by_category.setdefault(a["category"], []).append(a)

total_bytes = sum(a.get("glbBytes") or 0 for a in assets)
lines = []
lines.append("# Final Map — Sketchfab asset list")
lines.append("")
lines.append(f"*Generated from `{MANIFEST}` — do not edit by hand; edit the manifest and re-run `tools/manifest_docs.py`.*")
lines.append("")
lines.append(f"Every model below was found and verified through the public Sketchfab Data API v3 (downloadable,")
lines.append(f"licence checked, face count recorded). **Files are not committed to this repository** (the full set is")
lines.append(f"about {total_bytes / 1e9:.1f} GB). Fetch them with:")
lines.append("")
lines.append("```")
lines.append("SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py")
lines.append("```")
lines.append("")
lines.append("(Token: sketchfab.com → Settings → Password & API. The files land in `Assets/CarSales/Sketchfab/`.)")
lines.append("After downloading, open Unity and run **Tools → Car Sales → Final Map → Build Final Map**.")
lines.append("")
lines.append(f"**{len(assets)} assets kept** · licences: " + ", ".join(f"{k} ×{v}" for k, v in sorted(manifest.get("licenceCount", {}).items())))
lines.append("")
lines.append("Car model names use real makes, consistent with the game's existing real-car stance (acknowledged in the")
lines.append("Release Checklist); dealership buildings with real-brand signage were excluded instead (see exclusions).")
lines.append("Entries flagged **needs review** should be eyeballed in the Asset Studio before shipping.")
lines.append("")
for cat, items in by_category.items():
    lines.append(f"## {cat} ({len(items)})")
    lines.append("")
    for a in items:
        faces = a.get("faceCount") or 0
        face_s = f"{faces:,.0f} faces" if faces else "faces n/a"
        size_s = f", ~{(a.get('glbBytes') or 0) / 1e6:.0f} MB" if a.get("glbBytes") else ""
        licence = a.get("licenceLabel") or a.get("licence") or "?"
        review = " — **needs review**" if a.get("needsReview") else ""
        author = a.get("author") or "unknown"
        lines.append(f"- **{a['name']}** by {author} — {face_s}{size_s}, {licence} — [view]({a.get('viewerUrl', '')}){review}")
    lines.append("")

excl = manifest.get("exclusions", [])
if excl:
    lines.append(f"## Excluded ({len(excl)})")
    lines.append("")
    lines.append("Checked and rejected — kept here for the record (licence, relevance or the 95 MB Sketchfab/git cap):")
    lines.append("")
    for a in sorted(excl, key=lambda x: (x["category"], x["name"])):
        lines.append(f"- ~~{a['name']}~~ ({a['category']}) — {a['reason']}")
    lines.append("")

notes = manifest.get("notes", [])
if notes:
    lines.append("## Notes")
    lines.append("")
    for n in notes:
        lines.append(f"- {n}")
    lines.append("")

open(DOC, "w", encoding="utf-8").write("\n".join(lines))
print(f"Wrote {DOC}: {len(assets)} assets, {len(excl)} exclusions")
sys.exit(0)
