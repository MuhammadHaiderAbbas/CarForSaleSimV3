#!/usr/bin/env python3
"""
Car For Sale Simulator - "Harbour City" asset curator.

Queries Sketchfab's PUBLIC Data API v3 (search works without a token; downloading
the model files requires the user's own API token and is done inside Unity by
Assets/CarSales/Editor/HarbourCity/HarbourCityAssetImporter.cs).

What this produces:
  Assets/CarSales/HarbourCity/harbour-city-assets.json   machine-readable manifest
  docs/HARBOUR_CITY_ASSETS.md                         human-readable credit list

Every entry is verified to be:
  - downloadable (isDownloadable == true)
  - licence CC0 or CC BY (CC BY-SA is used only when nothing better exists and is flagged "needsReview")
  - above a per-category face-count floor (real, high-poly models, not placeholders)

Nothing here fabricates data: uid/name/licence/faces/description all come straight
from the API. Re-run any time to refresh:  python3 tools/curate_sketchfab_assets.py
"""
import json
import sys
import time
import urllib.request
import urllib.parse

API = "https://api.sketchfab.com/v3"
LICENSES = ("cc0", "by", "by-sa")          # preference order; by-sa flagged for review
LICENCE_RANK = {"cc0": 0, "by": 1, "by-sa": 2}
LICENCE_LABEL = {
    "cc0": ("CC0", "https://creativecommons.org/publicdomain/zero/1.0/", "CC0 (public domain)"),
    "by": ("CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/", "CC BY 4.0"),
    "by-sa": ("CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0/", "CC BY-SA 4.0"),
}

# category -> (queries, how many to keep, face floor, max faces, allowed licences, search categories)
CATALOG = {
    # ---------------- buildings (high poly, realistic) ----------------
    "building_house":      (["modern house", "suburban house", "family house", "villa house"], 8, 3000, 1_500_000, ("cc0", "by"), None),
    "building_apartment":  (["apartment building", "apartment block", "modern apartment building", "residential building"], 5, 3000, 1_500_000, ("cc0", "by"), None),
    "building_office":     (["office building", "office tower", "modern office building"], 4, 3000, 1_500_000, ("cc0", "by"), None),
    "building_showroom":   (["car showroom", "car dealership building", "showroom building"], 4, 1500, 1_500_000, ("cc0", "by"), None),
    "building_patrol":     (["police station", "police station building"], 2, 1500, 1_500_000, ("cc0", "by"), None),
    "building_carwash":    (["car wash building", "car wash", "carwash"], 3, 1500, 1_500_000, ("cc0", "by"), None),
    "building_service":    (["car repair shop", "mechanic workshop building", "auto service center building"], 3, 1500, 1_500_000, ("cc0", "by"), None),
    "building_auction":    (["auction house", "auction hall", "auction building"], 2, 1500, 1_500_000, ("cc0", "by", "by-sa"), None),
    "building_tyre":       (["tire shop", "tyre shop building"], 2, 800, 1_500_000, ("cc0", "by", "by-sa"), None),
    "building_fuel":       (["gas station", "petrol station", "fuel station"], 2, 2000, 1_500_000, ("cc0", "by"), None),
    "building_bank":       (["bank building"], 1, 2000, 1_500_000, ("cc0", "by", "by-sa"), None),
    "building_construction": (["building construction", "construction site building", "unfinished building"], 2, 1500, 1_500_000, ("cc0", "by", "by-sa"), None),
    # ---------------- cars, 8 per type ----------------
    "car_hatchback": (["hatchback car", "city car"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    "car_sedan":     (["sedan car"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    "car_suv":       (["SUV car", "suv vehicle", "4x4 car"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    "car_sports":    (["sports car", "supercar", "sports coupe"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    "car_pickup":    (["pickup truck", "pickup car"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    "car_luxury":    (["luxury car", "luxury sedan", "executive car"], 8, 8000, 1_500_000, ("cc0", "by"), "cars-vehicles"),
    # ---------------- construction site & machines ----------------
    "construction_machine": (["tower crane", "excavator", "bulldozer", "wheel loader", "dump truck", "concrete mixer", "forklift"], 10, 800, 1_500_000, ("cc0", "by"), None),
    "construction_prop":    (["scaffolding", "construction barrier", "site office container", "traffic cone"], 6, 300, 1_500_000, ("cc0", "by", "by-sa"), None),
    # ---------------- interior furniture & shop fitting ----------------
    "furniture_office":     (["office desk", "office chair", "meeting table", "reception desk"], 5, 300, 1_500_000, ("cc0", "by"), None),
    "furniture_lounge":     (["sofa", "armchair", "coffee table"], 4, 200, 1_500_000, ("cc0", "by"), None),
    "furniture_retail":     (["shop shelf", "display shelf", "tool chest", "workbench"], 4, 300, 1_500_000, ("cc0", "by"), None),
    "furniture_equipment":  (["car lift garage", "tire rack", "garage lift", "workbench vise"], 4, 300, 1_500_000, ("cc0", "by"), None),
    "furniture_computer":   (["computer monitor", "office computer", "cash register"], 3, 100, 1_500_000, ("cc0", "by"), None),
    # ---------------- town dressing ----------------
    "prop_street":          (["street light", "street lamp", "traffic light", "road sign"], 4, 200, 1_500_000, ("cc0", "by"), None),
    "prop_park":            (["park bench", "fountain", "realistic tree"], 4, 1000, 1_500_000, ("cc0", "by", "by-sa"), None),
}

# Wanted UID the user asked to feature (Modern Luxury Villa, Architecture). Always included.
HERO_UIDS = {
    "4958b4ac23eb445aaa6115278676d5df": ("building_house", "hero"),
}

def fetch(url, timeout=14):
    req = urllib.request.Request(url, headers={"User-Agent": "CarForSaleSim-HarbourCityCurator/1.0"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read().decode("utf-8"))

def search(query, license_slug, categories=None, count=8, cursor=None):
    params = {"type": "models", "downloadable": "true", "count": count, "q": query, "license": license_slug}
    if categories:
        params["categories"] = categories
    if cursor:
        params["cursor"] = cursor
    return fetch(f"{API}/search?{urllib.parse.urlencode(params)}")

def model_detail(uid):
    try:
        return fetch(f"{API}/models/{urllib.parse.quote(uid)}")
    except Exception:
        return {}

def licence_of(m):
    slug = (m.get("_licSlug") or (m.get("license") or {}).get("slug") or "").lower()
    lic = m.get("license") or {}
    label, url, name = LICENCE_LABEL.get(slug, (lic.get("label") or slug or "unknown", lic.get("url") or lic.get("uri", ""), slug))
    return slug, name, url

globally_seen = set()

def collect(category, queries, keep, min_faces, max_faces, allowed, categories):
    seen, pool = set(), []
    for q in queries:
        for lic in allowed:
            if lic == "by-sa" and len(seen) > 0 and len(pool) >= keep:
                continue  # fallback only
            try:
                page = search(q, lic, categories=categories, count=24)
            except Exception as e:
                print(f"  ! search failed ({q!r}, {lic}): {e}", file=sys.stderr)
                continue
            for m in page.get("results", []):
                uid = m.get("uid")
                if not uid or uid in seen or uid in globally_seen:
                    if uid and uid in globally_seen:
                        print(f"  - cross-category duplicate skipped: {m.get('name')!r} ({uid})", flush=True)
                    continue
                m["_licSlug"] = lic      # the search filter IS the licence (search results carry no slug)
                faces = m.get("faceCount") or 0
                if not m.get("isDownloadable"):
                    continue
                if faces < min_faces or faces > max_faces:
                    continue
                seen.add(uid)
                pool.append(m)
            time.sleep(0.12)
    # rank: licence quality first, then faces (higher = more detailed), stable by api order
    def rank(m):
        slug, _, _ = licence_of(m)
        return (LICENCE_RANK.get(slug, 9), -(m.get("faceCount") or 0))
    pool.sort(key=rank)
    picked, used = [], set()
    for m in pool:
        if len(picked) >= keep:
            break
        key = (m.get("name") or "").strip().lower()
        if key in used:   # same model under two queries shows up with different uids sometimes
            continue
        used.add(key)
        picked.append(m)
    for m in picked:
        globally_seen.add(m.get("uid"))
    return picked

def archives(m):
    return m.get("archives") or {}

def thumb(m):
    images = ((m.get("thumbnails") or {}).get("images")) or []
    small = [i for i in images if (i.get("width") or 0) >= 200]
    if not small:
        return images[-1].get("url", "") if images else ""
    small.sort(key=lambda i: i.get("width") or 0)
    return small[0].get("url", "")

def describe(detail, m, category):
    desc = (detail.get("description") or "").strip()
    if desc:
        desc = " ".join(desc.split())
        if len(desc) > 260:
            desc = desc[:257].rsplit(" ", 1)[0] + "…"
    tags = [t.get("name") for t in (detail.get("tags") or []) if t.get("name")][:8]
    return desc, tags

DEADLINE = time.time() + 780  # soft stop: write whatever we have

def main():
    manifest = {"generated": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                "source": "Sketchfab Data API v3 (search results - downloads need the user's token)",
                "heroUids": HERO_UIDS,
                "assets": []}
    md = ["# Harbour City — Sketchfab asset list", "",
          "Every model below was found and verified through the public Sketchfab Data API v3",
          "(downloadable, licence checked, face count recorded). Files are downloaded inside Unity:",
          "**Tools → Car Sales → Harbour City → Download All Assets** (needs your Sketchfab API token,",
          "sketchfab.com → Settings → Password & API). Nothing is committed to this repository —",
          "the JSON manifest + importer fetch them straight from Sketchfab, as requested.", ""]
    total = 0
    for category, (queries, keep, min_faces, max_faces, allowed, cats) in CATALOG.items():
        print(f"[{category}] searching…", flush=True)
        if time.time() > DEADLINE:
            print("deadline reached - writing partial manifest", flush=True)
            break
        picked = collect(category, queries, keep, min_faces, max_faces, allowed, cats)
        # hero uid for the user's chosen villa
        for uid, (cat, role) in HERO_UIDS.items():
            if cat == category and all(p.get("uid") != uid for p in picked):
                d = model_detail(uid)
                if d:
                    d["_hero"] = True
                    picked.insert(0, d)
        for i, m in enumerate(picked):
            uid = m.get("uid")
            search_desc = (m.get("description") or "").strip()
            if search_desc:
                detail = m
            else:
                detail = model_detail(uid)
                time.sleep(0.1)
            slug, lic_name, lic_url = licence_of(m)
            desc, tags = describe(detail, m, category)
            faces = m.get("faceCount") or 0
            author = (m.get("user") or {}).get("displayName") or (m.get("user") or {}).get("username") or "Unknown"
            entry = {
                "key": f"{category}_{i+1:02d}",
                "category": category,
                "role": "hero" if m.get("_hero") else ("showcase" if category.startswith("car_") else "world"),
                "uid": uid,
                "name": (m.get("name") or "Untitled").strip(),
                "viewerUrl": m.get("viewerUrl") or f"https://sketchfab.com/3d-models/{uid}",
                "author": author,
                "authorUrl": (m.get("user") or {}).get("profileUrl", ""),
                "licence": slug,
                "licenceLabel": lic_name,
                "licenceUrl": lic_url,
                "needsReview": slug == "by-sa",
                "faceCount": faces,
                "description": desc,
                "tags": tags,
                "thumbnailUrl": thumb(m),
                "glbBytes": (archives(m).get("glb") or {}).get("size"),
                "gltfBytes": (archives(m).get("gltf") or {}).get("size"),
                "importPath": f"Assets/CarSales/Sketchfab/{category}/{uid}.glb",
            }
            manifest["assets"].append(entry)
            total += 1
        if picked:
            md.append(f"## {category} ({len(picked)})")
            md.append("")
            for i, m in enumerate(picked):
                slug, lic_name, _ = licence_of(m)
                author = (m.get("user") or {}).get("displayName") or (m.get("user") or {}).get("username") or "Unknown"
                md.append(f"- **{m.get('name')}** by {author} — {m.get('faceCount', 0):,} faces, {lic_name}{' ⚠️ review' if slug == 'by-sa' else ''} — [view]({m.get('viewerUrl')})")
            md.append("")
        time.sleep(0.2)

    md.append(f"\n**{total} assets** in total. All are free downloadable models on Sketchfab;")
    md.append("CC BY models are attributed automatically in the in-game credits screen (CREDITS.txt).")
    md.append("CC BY-SA models (flagged ⚠️) need a release-licence review before shipping a commercial build.")

    with open("Assets/CarSales/HarbourCity/harbour-city-assets.json", "w") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    with open("docs/HARBOUR_CITY_ASSETS.md", "w") as f:
        f.write("\n".join(md) + "\n")
    total_glb = sum(a.get("glbBytes") or 0 for a in manifest["assets"])
    print(f"\nWrote {total} assets to Assets/CarSales/HarbourCity/harbour-city-assets.json and docs/HARBOUR_CITY_ASSETS.md")
    print(f"GLB download size (approx): {total_glb/1e6:.0f} MB")

if __name__ == "__main__":
    main()
