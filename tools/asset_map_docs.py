#!/usr/bin/env python3
"""
Regenerates docs/ASSET_MAP.md — the sheet that maps every manifest asset to
the exact map site (number on docs/images/harbour-city.png), glyph and zone,
or says plainly that it is not placed and why.

    python3 tools/asset_map_docs.py

The placement column is maintained by hand in PLACEMENT below — it mirrors
Assets/CarSales/Editor/CarSalesHarbourMap.cs. If you change the builder,
change PLACEMENT too (and docs/HARBOUR_CITY.md / docs/HARBOUR_CITY_PLAN.md if the sites move).
"""
import json
import collections

MANIFEST = "Assets/CarSales/HarbourCity/harbour-city-assets.json"
DOC = "docs/ASSET_MAP.md"

# key -> (map site # / area, one-line detail). Everything not listed is intentionally unplaced.
PLACEMENT = {
    "building_house_01": ("Site 3 · Hillside Villa (H, zone 1)", "Hero override: replaces the primitive villa shell (Home_Villa/Level1); hides VillaBase/VillaUpper/Glass1/Glass2/RoofTrim. Yaw 0°, ~17×11 m footprint."),
    "building_office_03": ("Site 22 · Northgate Offices (O, zone 15)", "Named in BuildOffices ('NorthgateOffices'). ~23×16 m footprint, sign 'NORTHGATE OFFICES'."),
    "building_office_04": ("Site 23 · Offices West (O, zone 15) + Site 24 · Northgate Site finished shell (Y, zone 14)", "Used twice: BuildOffices ('OfficesWest') ~19×14 m; construction stage 3 'finalKey' ~20×15 m (swaps in after StageCount builds)."),
    "building_showroom_02": ("Site 21 · RentGo sales office (R, zone 15)", "Name match 'Car showroom' in BuildRentGo. ~13×9 m; a blue-striped RENTGO sign goes on the roof."),
    "building_patrol_02": ("Site 18 · Patrol Station (X, zone 10)", "Category match 'building_patrol' in BuildPatrolStation. ~19×15 m; sign 'PATROL STATION' + blue light bar."),
    "building_construction_02": ("Sites 24 & 25 · construction Stage 2 'Draft' shell (Y)", "Name match 'Building under construction' in BuildConstructionPlot. ~21×14 m; columns/slabs primitive otherwise."),
    "building_auction_02": ("Site 25 · Dockside Site finished shell (Y, zone 3)", "Name match 'General Ship Repair' as DocksideSite's finalKey. ~20×15 m. Manifest flags needs review — eyeball in Asset Studio before shipping."),
    "car_hatchback_02": ("Site 21 · RentGo display bay (bay 1)", "First hatchback by key order (code: PickFromCategory('car_hatchback', 1)). Static, for show — never for sale, never in stock."),
    "car_sedan_01": ("Site 21 · RentGo bay 2 + Site 22 · Northgate Offices company car", "First sedan by key order; used in both spots."),
    "car_suv_01": ("Site 21 · RentGo bay 3 + Site 23 · Offices West company car", "First SUV by key order; used in both spots."),
    "car_sports_01": ("Site 21 · RentGo display bay (bay 4)", "First sports car by key order."),
    "car_luxury_01": ("Site 21 · RentGo display bay (bay 5)", "First luxury car by key order."),
    "car_pickup_01": ("Site 21 · RentGo far lot", "First pickup by key order; parked at the far end of the lot."),
    "construction_machine_01": ("Sites 24 & 25 · Stage 1 'Site' machines", "Excavator — placed deterministically from manifest key order."),
    "construction_machine_02": ("Sites 24 & 25 · Stage 1 'Site' machines", "Telescopic Forklift T30104."),
    "construction_machine_03": ("Sites 24 & 25 · Stage 1 'Site' machines", "Truck."),
    "construction_machine_04": ("Sites 24 & 25 · Stage 1 'Site' machines", "Mining excavator, Mining machine. (_05, _08, _09 are backups; _06 'tower crane' is deliberately excluded — the map uses its own primitive crane.)"),
    "construction_prop_01": ("Sites 24 & 25 · Stage 1 scaffolding", "Name match 'Pergula 3D scaffolding' in BuildConstructionPlot."),
    "prop_park_03": ("Zone 13 · The Public Park — statue 1 (west of Park Road ring)", "Rijksmuseum-style CC0 scan ('Knabe mit Vögeln'). ~2.4×3.4 m."),
    "prop_park_04": ("Zone 13 · The Public Park — statue 2", "CC0 scan ('Zwei Bären'). ~2.4×3.4 m."),
}

# Why specific curated assets are never placed (so nobody hunts for them in the scene).
NOT_PLACED_WHY = {
    "building_showroom_01": "Two Wheeler Showroom — manifest flags needs review; not auto-placed.",
    "building_carwash_02": "Car Wash (Site 9) is built from primitives by design. Swap with Asset Studio → Town if you prefer this model.",
    "building_apartment_05": "Spare. Zone 10 'Rental Apartments' is primitive; wire it in if you extend the Dockside/Buildings programme.",
    "building_house_06": "Spare for the residential zone (Sites 2/3); Harbour City keeps primitives there.",
    "building_house_07": "Spare for the residential zone.",
    "building_house_08": "Spare for the residential zone (has an interior — useful if homes become walkable).",
    "construction_machine_05": "Backup machine (deterministic order: only the first 4 non-crane are placed).",
    "construction_machine_06": "Deliberately excluded — the map uses its own primitive tower crane (two masts + jib).",
    "construction_machine_08": "Backup machine.",
    "construction_machine_09": "Backup machine.",
    "construction_prop_04": "Suspended scaffolding — not placed; the ground scaffold (_01) is used.",
    "prop_street_01": "'Flood Street — Fragment 1' — a Roman fragment, not street furniture; kept as a catalogue piece.",
    "prop_street_02": "Crowned-eagle lamp shaft — kept as catalogue; the map builds its own street lamps.",
    "prop_street_04": "'road sign_OLD_4к' — cyrillic sign, off-theme; kept as catalogue.",
}
# furniture_* and remaining car_* are handled by blanket rules below.

def blanket_reason(a):
    if a["category"].startswith("furniture_"):
        return "Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap)."
    if a["category"].startswith("car_"):
        return "Showcase catalogue — the Harbour City places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring."
    return "Catalogue / headroom — not auto-placed."

with open(MANIFEST, encoding="utf-8") as _f:
    manifest = json.load(_f)
assets = manifest["assets"]
by_cat = collections.OrderedDict()
for a in sorted(assets, key=lambda x: (x["category"], x["key"])):
    by_cat.setdefault(a["category"], []).append(a)

placed = [a for a in assets if a["key"] in PLACEMENT]
lines = []
A = lines.append
A("# Harbour City — asset placement sheet")
A("")
A("*Generated from `" + MANIFEST + "` + placement table in `tools/asset_map_docs.py` — re-run `python3 tools/asset_map_docs.py` after any manifest or builder change.*")
A("")
A("Site numbers refer to the exact map render: **docs/images/harbour-city.png** (site list is also printed on the image).")
A("Companion docs: HARBOUR_CITY_ASSETS.md (full catalogue + attribution), HARBOUR_CITY.md (audit & repair), HARBOUR_CITY_PLAN.md (linkage), CATALOG.md (everything in the project).")
A("")
A(f"**{len(placed)} of {len(assets)} curated models are placed by the current builder.** The rest are deliberate headroom — see the checklist below and “How to place more”.")
A("")
A("## Read this first")
A("")
A("1. `SKETCHFAB_TOKEN=… python3 tools/download_sketchfab_assets.py` — fetches GLBs into `Assets/CarSales/Sketchfab/<category>/<uid>.glb` (never committed, ~3.5 GB).")
A("2. `python3 tools/preflight_assets.py` — verifies every download before Unity (size, GLB magic, JSON chunk, triangle/image counts, Draco/meshopt flags, external references, brand strings for the Release Checklist). Report: `docs/ASSET_PREFLIGHT.md`.")
A("3. Open Unity → **Tools → Car Sales → Harbour City → Build Harbour City** — places the models listed here; every miss falls back to a yellow stand-in with a PlaceholderSlot.")
A("4. **Tools → Car Sales → Harbour City → Audit Harbour City** names any model Unity cannot read; see docs/HARBOUR_CITY.md.")
A("")
A("## A. Buildings & POI shells placed by the Harbour City")
A("")
A("| Map site | Model (manifest key) | Licence | Footage |")
A("|---|---|---|---|")
for key in ["building_house_01","building_office_03","building_office_04","building_showroom_02","building_patrol_02","building_construction_02","building_auction_02"]:
    a = next(x for x in assets if x["key"] == key)
    where, detail = PLACEMENT[key]
    A(f"| {where} | **{a['name']}** (`{key}`) | {a['licence'].upper()} | {detail} |")
A("")
A("## B. Construction stage 1 — machines & scaffolding")
A("")
A("| Map site | Model | Licence | Detail |")
A("|---|---|---|---|")
for key in ["construction_machine_01","construction_machine_02","construction_machine_03","construction_machine_04","construction_prop_01"]:
    a = next(x for x in assets if x["key"] == key)
    where, detail = PLACEMENT[key]
    A(f"| {where} | **{a['name']}** (`{key}`) | {a['licence'].upper()} | {detail} |")
A("")
A("Placement is deterministic: `PickFromCategory(\"construction_machine\", 6)` in manifest key order, the tower-crane entry excluded, first 4 placed — the primitive red crane is always present on top.")
A("")
A("## C. Cars placed as static show models")
A("")
A("| Map site | Model | Licence |")
A("|---|---|---|")
for key in ["car_hatchback_02","car_sedan_01","car_suv_01","car_sports_01","car_luxury_01","car_pickup_01"]:
    a = next(x for x in assets if x["key"] == key)
    where, _ = PLACEMENT[key]
    A(f"| {where} | **{a['name']}** (`{key}`) | {a['licence'].upper()} |")
A("")
A("These are display cars only — no CarVisual, no stock, no trade-in. The fleet classes in the game economy are unchanged.")
A("")
A("## D. Park props")
A("")
A("| Map site | Model | Licence |")
A("|---|---|---|")
for key in ["prop_park_03","prop_park_04"]:
    a = next(x for x in assets if x["key"] == key)
    where, detail = PLACEMENT[key]
    A(f"| {where} | **{a['name']}** (`{key}`) | {a['licence'].upper()} |")
A("")
A("## E. Full checklist — all " + str(len(assets)) + " curated assets")
A("")
A("| key | model | role | licence | place on the map |")
A("|---|---|---|---|---|")
for cat, group in by_cat.items():
    for a in group:
        key = a["key"]; name = a["name"].replace("|", "\\|")
        if key in PLACEMENT:
            where = PLACEMENT[key][0]
        elif key in NOT_PLACED_WHY:
            where = "— *not placed.* " + NOT_PLACED_WHY[key]
        else:
            where = "— *not placed.* " + blanket_reason(a)
        A(f"| `{key}` | {name} | {a.get('role','')} | {a['licence'].upper()} | {where} |")
A("")
A("## F. How placement works, and how to change it")
A("")
A("- The builder (`Assets/CarSales/Editor/CarSalesHarbourMap.cs`) resolves each placement with `FindAsset` — it matches on **key**, then **name**, then **category** (first manifest entry by key order). Missing/undownloadable models fall back to a labelled yellow stand-in (`PlaceholderSlot`), so a partial download never blocks the build.")
A("- `TryPlaceGlb` scales each model to a footprint (keeps aspect ratio), stands it on the ground and adds `MeshCollider`s + static flags. Buildings keep their authored front; cars/machines may turn 90° to fit.")
A("- Per-model tweaks without code: `Assets/CarSales/HarbourCity/harbour-city-overrides.json` — `{ \"uid\": \"…\", \"yawOffset\": 90, \"footprintScale\": 1.1, \"skip\": true }`.")
A("- To place more: extend `BuildHarbourCityExtras` (or the specific `Build*`) in `CarSalesHarbourMap.cs` with `PlaceGlbOrStandIn(g, \"<key>\", localCentre, footprint, yaw, allowFlip, kind, m, \"what\")`. Keep the stand-in fallback — it is what makes partial downloads safe.")
A("- After any change: `python3 tools/asset_map_docs.py` to refresh this sheet, and `python3 tools/render_plan_map.py` if sites or roads moved.")
A("")
A("## G. Honest size note")
A("")
A("The download is ~3.5 GB but today only " + str(len(placed)) + " models appear in the scene (many are the same model in two spots). The remaining GLBs are the showcase fleet and catalogue props — importing them all is pointless today, but they are the pool you draw from when you extend the map, and none of them is committed to git.")
A("")
with open(DOC, "w", encoding="utf-8") as f:
    f.write("\n".join(lines) + "\n")
print(f"wrote {DOC} — {len(assets)} assets, {len(placed)} placed")
