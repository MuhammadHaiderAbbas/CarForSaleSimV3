#!/usr/bin/env python3
"""
Downloads every verified asset in Assets/CarSales/FinalMap/final-map-assets.json straight from Sketchfab.

Needs YOUR Sketchfab API token (sketchfab.com -> Settings -> Password & API):

    SKETCHFAB_TOKEN=xxxxxxxx python3 tools/download_sketchfab_assets.py

Files land in Assets/CarSales/Sketchfab/<category>/<uid>.glb (the layout the Final Map importer expects).
Assets bigger than SKIP_OVER_MB (default 95) are skipped on purpose - GitHub rejects files that large.
(The manifest keeps only curated models at or under that cap, so this is a safety net; skipped files simply
build as primitive stand-ins.)

The token is used for these requests only; it is never written to the repository.
"""
import json
import os
import sys
import time
import urllib.request
import urllib.error

API = "https://api.sketchfab.com/v3"
MANIFEST = "Assets/CarSales/FinalMap/final-map-assets.json"
OUT_ROOT = "Assets/CarSales/Sketchfab"
REPORT = "Assets/CarSales/FinalMap/download-report.json"
SKIP_OVER_MB = float(os.environ.get("SKIP_OVER_MB", "95"))
TOKEN = os.environ.get("SKETCHFAB_TOKEN", "").strip()

def api_get(url, headers=None, timeout=30):
    req = urllib.request.Request(url, headers=headers or {})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.status, r.read(), dict(r.headers)

def main():
    if not TOKEN:
        print("Set SKETCHFAB_TOKEN=<your token> first.", file=sys.stderr)
        return 2
    with open(MANIFEST, encoding="utf-8") as _f:
        manifest = json.load(_f)
    assets = manifest["assets"]
    print(f"{len(assets)} assets in manifest; downloading into {OUT_ROOT}/ (skipping > {SKIP_OVER_MB:.0f} MB)")

    report, ok, skipped, failed = [], 0, 0, 0
    total_bytes = 0
    for i, a in enumerate(assets):
        uid = a["uid"]
        out_dir = os.path.join(OUT_ROOT, a["category"])
        os.makedirs(out_dir, exist_ok=True)
        out_glb = os.path.join(out_dir, uid + ".glb")
        entry = {"key": a["key"], "uid": uid, "name": a["name"], "category": a["category"]}

        if os.path.exists(out_glb) and os.path.getsize(out_glb) > 1024:
            entry["status"] = "already_present"
            entry["bytes"] = os.path.getsize(out_glb)
            ok += 1
            total_bytes += entry["bytes"]
            report.append(entry)
            continue

        # 1. Ask Sketchfab for the download URL (needs the token).
        try:
            status, body, _ = api_get(f"{API}/models/{uid}/download", headers={"Authorization": f"Token {TOKEN}"})
        except urllib.error.HTTPError as e:
            entry["status"] = f"download_refused_http_{e.code}"
            failed += 1
            report.append(entry)
            print(f"[{i+1}/{len(assets)}] FAIL {a['name']!r}: HTTP {e.code}")
            continue
        except Exception as e:
            entry["status"] = f"download_refused_{type(e).__name__}"
            failed += 1
            report.append(entry)
            print(f"[{i+1}/{len(assets)}] FAIL {a['name']!r}: {e}")
            continue

        try:
            dl = json.loads(body)
        except json.JSONDecodeError:
            entry["status"] = "download_refused_bad_json"
            failed += 1
            report.append(entry)
            continue

        glb = (dl.get("glb") or {}).get("url")
        gltf = (dl.get("gltf") or {}).get("url")
        if not glb and not gltf:
            entry["status"] = "no_downloadable_glb_or_gltf"
            failed += 1
            report.append(entry)
            print(f"[{i+1}/{len(assets)}] SKIP {a['name']!r}: Sketchfab offers no GLB/glTF")
            continue

        url = glb or gltf
        target = out_glb if glb else os.path.join(out_dir, uid + ".gltf.zip")

        # 2. Stream it, honouring the size cap.
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "CarForSaleSim-FinalMapDownloader/1.0"})
            with urllib.request.urlopen(req, timeout=180) as r:
                length = int(r.headers.get("Content-Length") or 0)
                if length and length / 1e6 > SKIP_OVER_MB:
                    entry["status"] = "skipped_too_large"
                    entry["bytes"] = length
                    skipped += 1
                    report.append(entry)
                    print(f"[{i+1}/{len(assets)}] SKIP {a['name']!r}: {length/1e6:.0f} MB exceeds the {SKIP_OVER_MB:.0f} MB cap")
                    continue
                tmp = target + ".part"
                with open(tmp, "wb") as f:
                    while True:
                        chunk = r.read(1 << 20)
                        if not chunk:
                            break
                        f.write(chunk)
                os.replace(tmp, target)
        except Exception as e:
            entry["status"] = f"fetch_failed_{type(e).__name__}"
            failed += 1
            report.append(entry)
            if os.path.exists(target + ".part"):
                os.remove(target + ".part")
            print(f"[{i+1}/{len(assets)}] FAIL {a['name']!r}: {e}")
            continue

        size = os.path.getsize(target)
        if size < 10 * 1024:
            os.remove(target)
            entry["status"] = "fetch_failed_too_small_to_be_a_model"
            failed += 1
            print(f"[{i+1}/{len(assets)}] FAIL {a['name']!r}: {size} bytes - not a real model")
            report.append(entry)
            continue

        # 3. GLB sanity check (magic "glTF").
        if target.endswith(".glb"):
            with open(target, "rb") as f:
                magic = f.read(4)
            if magic != b"glTF":
                os.remove(target)
                entry["status"] = "fetch_failed_not_a_glb"
                failed += 1
                print(f"[{i+1}/{len(assets)}] FAIL {a['name']!r}: not a GLB file")
                report.append(entry)
                continue

        entry["status"] = "ok"
        entry["file"] = target.replace(os.sep, "/")
        entry["bytes"] = size
        ok += 1
        total_bytes += size
        report.append(entry)
        print(f"[{i+1}/{len(assets)}] OK   {a['name']!r:60s} {size/1e6:7.1f} MB")
        time.sleep(0.15)

    with open(REPORT, "w", encoding="utf-8") as f:
        json.dump({"downloaded": ok, "skippedTooLarge": skipped, "failed": failed,
                   "totalBytes": total_bytes, "assets": report}, f, indent=1)
    print(f"\nDone: {ok} downloaded ({total_bytes/1e9:.2f} GB), {skipped} skipped (too large), {failed} failed. Report: {REPORT}")
    return 0 if failed == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
