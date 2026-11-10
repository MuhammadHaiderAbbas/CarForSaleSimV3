#!/usr/bin/env python3
"""
Preflight check for the Harbour City's Sketchfab GLBs — run this AFTER

    SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py

and BEFORE opening Unity, so you know exactly which models are complete,
which are corrupt/missing, and which carry licence or brand notes for the
Release Checklist. Pure Python, no third-party packages, no network.

    python3 tools/preflight_assets.py            # writes docs/ASSET_PREFLIGHT.md + prints a summary
    python3 tools/preflight_assets.py --quiet    # just the summary

Exit code 0 = every manifest GLB present and parseable (warnings allowed).
Exit code 1 = at least one file missing, corrupt or truncated.
"""
import json
import re
import struct
import sys
from collections import Counter

MANIFEST = "Assets/CarSales/HarbourCity/harbour-city-assets.json"
REPORT = "docs/ASSET_PREFLIGHT.md"

# Brand strings worth flagging for docs/RELEASE.md (Release Checklist → Rename).
BRANDS = [
    "ferrari", "lamborghini", "maserati", "rolls-royce", "rolls royce", "porsche",
    "aston martin", "mercedes", "bmw", "jaguar", "chevrolet", "corvette", "kia",
    "toyota", "ford", "oldsmobile", "vaz", "zhiguli", "lexus", "honda", "nissan",
]

def parse_glb(path):
    """Return (info dict, error or None)."""
    info = {"size": 0, "version": None, "json_ok": False}
    try:
        data = open(path, "rb").read()
    except OSError as e:
        return info, f"unreadable: {e}"
    info["size"] = len(data)
    if len(data) < 28:
        return info, "file too small to be a GLB"
    magic, version, length = struct.unpack_from("<4sII", data, 0)
    if magic != b"glTF":
        return info, "not a GLB (magic bytes are not 'glTF') — did a .gltf get saved here?"
    info["version"] = version
    if length != len(data):
        return info, f"truncated: header says {length} bytes, file is {len(data)}"
    offset, gltf = 12, None
    bin_bytes = 0
    while offset + 8 <= len(data):
        clen, ctype = struct.unpack_from("<I4s", data, offset)
        offset += 8
        chunk = data[offset:offset + clen]
        offset += clen
        if ctype == b"JSON":
            try:
                gltf = json.loads(chunk)
            except json.JSONDecodeError as e:
                return info, f"glTF JSON chunk does not parse: {e}"
        elif ctype == b"BIN\x00":
            bin_bytes = clen
    if gltf is None:
        return info, "no JSON chunk found"
    info["json_ok"] = True

    meshes = gltf.get("meshes", [])
    prims = sum(len(ms.get("primitives", [])) for ms in meshes)
    accessors = gltf.get("accessors", [])
    tris = 0
    for ms in meshes:
        for p in ms.get("primitives", []):
            if p.get("mode", 4) != 4:      # TRIANGLES only
                continue
            idx = p.get("indices")
            if idx is not None and 0 <= idx < len(accessors):
                tris += accessors[idx].get("count", 0) // 3
            else:  # no index buffer: count the position accessor
                pos = (p.get("attributes") or {}).get("POSITION")
                if pos is not None and 0 <= pos < len(accessors):
                    tris += accessors[pos].get("count", 0) // 3
    images = gltf.get("images", [])
    embedded = sum(1 for im in images if "bufferView" in im or str(im.get("uri", "")).startswith("data:"))
    external_imgs = len(images) - embedded
    external_buf = any("uri" in b for b in gltf.get("buffers", []))
    ext_used = gltf.get("extensionsUsed", [])
    ext_req = gltf.get("extensionsRequired", [])
    # For the brand scan, names + copyright are what show up in the importer/inspector.
    blob = " ".join(
        [gltf.get("asset", {}).get("generator", ""), gltf.get("asset", {}).get("copyright", "")]
        + [n.get("name", "") for n in gltf.get("nodes", [])]
        + [m.get("name", "") for m in gltf.get("materials", [])]
    ).lower()
    hits = sorted({b for b in BRANDS if b in blob})

    info.update(
        meshes=len(meshes), primitives=prims, triangles=tris,
        materials=len(gltf.get("materials", [])), textures=len(gltf.get("textures", [])),
        images=len(images), images_embedded=embedded, images_external=external_imgs,
        animations=len(gltf.get("animations", [])), nodes=len(gltf.get("nodes", [])),
        external_buffer=external_buf,
        extensions_used=ext_used, extensions_required=ext_req,
        generator=gltf.get("asset", {}).get("generator", ""),
        draco="KHR_draco_mesh_compression" in ext_used or "KHR_draco_mesh_compression" in ext_req,
        meshopt="EXT_meshopt_compression" in ext_used or "EXT_meshopt_compression" in ext_req,
        brand_hits=hits,
    )
    return info, None

def main():
    quiet = "--quiet" in sys.argv
    with open(MANIFEST, encoding="utf-8") as f:
        manifest = json.load(f)
    assets = manifest["assets"]

    rows, missing, corrupt, warnings = [], [], [], []
    for a in assets:
        path = a["importPath"]
        if not __import__("os").path.exists(path):
            missing.append(a)
            rows.append((a, None, "MISSING"))
            continue
        info, err = parse_glb(path)
        if err:
            corrupt.append((a, err))
            rows.append((a, info, "CORRUPT: " + err))
            continue
        note = []
        if info["draco"]:
            note.append("Draco-compressed (needs glTFast with Draco, or re-export without it)")
        if info["meshopt"]:
            note.append("meshopt-compressed")
        if info["images_external"] or info["external_buffer"]:
            note.append("references EXTERNAL files (GLB should be self-contained)")
        if info["brand_hits"]:
            note.append("brand strings: " + ", ".join(info["brand_hits"]))
        if a.get("needsReview"):
            note.append("flagged needs review in the manifest")
        status = "ok" if not note else "warn"
        if note:
            warnings.append((a, note))
        rows.append((a, info, status + (": " + "; ".join(note) if note else "")))

    ok = len(assets) - len(missing) - len(corrupt)
    lines = [
        "# Harbour City — GLB preflight report",
        "",
        "*Generated by `python3 tools/preflight_assets.py` after `tools/download_sketchfab_assets.py`. Re-run any time.*",
        "",
        f"**{ok} / {len(assets)} models present and parseable** · {len(missing)} missing · {len(corrupt)} corrupt · {len(warnings)} with warnings",
        "",
    ]
    if missing:
        lines += ["## Missing (re-run the downloader)", ""]
        lines += [f"- `{a['key']}` — {a['name']}" for a in missing]
        lines += ["", "```", "SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py", "```", ""]
    if corrupt:
        lines += ["## Corrupt (delete the file and re-run the downloader)", ""]
        lines += [f"- `{a['key']}` — {err}" for a, err in corrupt]
        lines.append("")
    if warnings:
        lines += ["## Warnings (import may need attention — not fatal)", ""]
        for a, note in warnings:
            lines.append(f"- **{a['key']}** — {'; '.join(note)}")
        lines.append("")
    lines += [
        "## Per-model detail", "",
        "| key | status | MB | tris | nodes | materials | textures | notes |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for a, info, status in rows:
        if info is None:
            lines.append(f"| {a['key']} | MISSING | – | – | – | – | – | `{a['importPath']}` not found |")
        elif not info.get("json_ok"):
            lines.append(f"| {a['key']} | CORRUPT | {info['size']/1e6:.1f} | – | – | – | – | {status[9:]} |")
        else:
            note = status.split(": ", 1)[1] if ": " in status else ""
            lines.append(
                f"| {a['key']} | {'ok' if status == 'ok' else 'warn'} | {info['size']/1e6:.1f} | {info['triangles']:,} "
                f"| {info['nodes']} | {info['materials']} | {info['textures']} | {note} |"
            )
    lines += [
        "",
        "## What happens next",
        "",
        "1. Zero missing/corrupt? Open Unity → **Tools → Car Sales → Harbour City → Build Harbour City**.",
        "   Models that fail to import anyway are caught by **Tools → Car Sales → Harbour City → Audit Harbour City**",
        "   (see docs/HARBOUR_CITY.md) — the audit names the model and the concrete fix.",
        "2. Draco/meshopt rows: the project auto-installs `com.unity.cloud.gltfast`, which reads Draco GLBs;",
        "   if one stays pink or empty, open it in Blender and re-export as plain glTF.",
        "3. Brand strings feed the Release Checklist rename list in docs/RELEASE.md — do those renames before any public build.",
        "4. Licences for everything here are CC0 or CC BY — keep THIRD_PARTY_NOTICES.txt with any release.",
        "",
    ]
    with open(REPORT, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

    if not quiet:
        print(f"{ok}/{len(assets)} present and parseable · {len(missing)} missing · {len(corrupt)} corrupt · {len(warnings)} warnings")
        print(f"Report written to {REPORT}")
    return 1 if (missing or corrupt) else 0

if __name__ == "__main__":
    sys.exit(main())
