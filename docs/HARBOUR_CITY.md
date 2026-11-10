# Harbour City — the one town of Car For Sale Simulator (V4)

Harbour City is a fictional port town on the south coast (Portsmouth-inspired): the old docks and the
auction quarter in the south-west, the high street (Harbour Boulevard) running the width of town, the
homes up by Northern Avenue, the public park in the west, and the new Southside Business Park on the
south-east fringe. It is the **only** town in the game — the old master map (V2) and the Final Map (V3)
were removed; their builders, plans, scenes and diagrams were deleted along with them.

- Plan: `Assets/CarSales/Sim/World/HarbourPlan.cs` (16 zones — two numbered 14, zone 15 is the business
  park; 25 sites; ~2.5 km test-drive loop; 8 roundabouts; traffic lights at the two Guildhall Street
  crossroads and Forecourt Road × Harbour Boulevard).
- Scene (generated): `Assets/CarSales/Generated/Scenes/HarbourCity.unity` — built by
  **Tools → Car Sales → 2. Build Everything** (or **Tools → Car Sales → Harbour City → Build Harbour City**
  to rebuild just the scene). `MainMenuUI.gameScene` is baked as `HarbourCity`.
- Diagram: `docs/images/harbour-city.png` (exact render of the plan via `tools/render_plan_map.py`).
- Plan validation: `python3 tools/plan_check.py` — same maths as the EditMode tests, run it after any
  plan edit.

## The town at a glance

| Area | Roads | Sites |
| --- | --- | --- |
| Player start — homes up by Northern Avenue (zone 1) | Northern Avenue | Starter Trailer · Suburban House · Hillside Villa (hero villa GLB) |
| AUTO WORLD — the dealership HQ (zone 2) | Harbour Boulevard | AUTO WORLD (full dealership: sales, service, MOD shop) |
| Docks quarter (zone 3) | Dockside Road | Auction Center · Used Car Market · Dockside Site (under construction) |
| Services north (zones 4–5) | Northern Avenue, Guildhall Street | AUTO WORLD Service Centre · Mechanic Workshop |
| Harbour Boulevard east (zones 6–7) | Harbour Boulevard | Car Wash · Fuel Station |
| TYRES & WHEELS + Parts Direct (zone 8) | Harbour Boulevard | the two parts shops, south side of the boulevard |
| Sellers' terrace + town centre (zones 9–10) | Harbour Boulevard, Station Road, Guildhall Street | Maple Close · Birch Close · Rental Apartments · Office Tower · Parking Garage · Patrol Station |
| Industrial estate (zone 11) | ring road | — |
| Test-drive loop (zone 12) | the ring | signs every few hundred metres |
| Public park (zone 13) | Park Road | statues + lake |
| Northgate (zone 14, by the ring) | Northern Avenue | Downtown Motors · Northgate Site (under construction) |
| Luxury Lane (zone 14, south) | Harbour Boulevard | Luxury Lane |
| Southside Business Park (zone 15) | Horizon Drive · Business Park Road | RentGo Rentals & Sales · Northgate Offices · Offices West |

## Build it

1. `Tools → Car Sales → 1. Check & Install Packages` — installs the Unity packages the game needs
   (including **GLTFast**, `com.unity.cloud.gltfast`, to load the Sketchfab models).
2. `Tools → Car Sales → 2. Build Everything` — once. Creates the materials, car database and prefabs,
   and builds Harbour City itself (the scene carries a `TownMapMarker` with `townVersion = 4`, and
   everything — traffic, pedestrians, the world map, GPS, saves — reads `CarSales.Sim.Town`, which now
   only delegates to `HarbourPlan`).
3. *(Optional, for the real models)* download the Sketchfab set:

   ```
   SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py
   ```

   (~3.5 GB into `Assets/CarSales/Sketchfab/<category>/<uid>.glb` — git-ignored, never committed.
   Token: sketchfab.com → Settings → Password & API.)
4. *(Optional)* rebuild the scene only: **Tools → Car Sales → Harbour City → Build Harbour City**.

**Missing models are fine.** Every model is optional: anything not downloaded yet is replaced by a
primitive stand-in (a yellow `PlaceholderSlot` box — Asset Studio → Town can swap it later), and the
build still completes. The completion dialog and the Console (`[CarSales] Harbour City build report:`)
list exactly what was placed and what was stood in for.

## Your own models per site (Map Slots)

**Tools → Car Sales → Harbour City → Map Slots…** lists all 25 sites in plan order. Each row has a
file picker, a live preview thumbnail, a yaw and a scale slider. Pick any prefab/.glb/.fbx per site and
press **Apply** (writes `Assets/CarSales/HarbourCity/harbour-map-slots.json`) — or **Apply + Build
Harbour City** to see it immediately.

At build time the uploaded model **replaces the site's generated building** — the ground, lot lines,
apron, fences, signs, lamps and trees stay. The model is fitted into the site's footprint and stood on
the ground at its centre. Everything is keyed by site id, so re-coordinating the plan never breaks a
slot.

## The Phase 14 POI programme

| POI | Where | What it is |
| --- | --- | --- |
| Tyres & Wheels | Harbour Boulevard, opposite Parts Direct (zone 8) | The dedicated tyre shop (real one, not dressed-up prims) |
| Patrol Station | town centre (zone 10) | Emergency-services POI for the police patrol feature |
| RentGo Rentals & Sales | Business Park Road, Southside Business Park (zone 15) | The rival rental & sales lot — static display cars (not in stock, not for sale, never for rent) |
| Northgate Offices & Offices West | Business Park Road, zone 15 | The south-side business park |
| Northgate Site (under construction) | Northern Avenue end (zone 14) | A real construction plot: cleared ground → machines → frame → finished building |
| Dockside Site (under construction) | Dockside, zone 3 | The same construction staging, themed as a ship-repair yard |
| Hillside Villa (your third home) | Zone 1 | The exterior shell is the curated 7,742-face **hero villa** GLB |
| Horizon Drive + Business Park Road | South of the boulevard | The new estate road and its link down to the ring road — both in the traffic graph (real junctions) |
| Public park statues & new road signs | Zone 13 / Business Park Road | CC0 statue dressing and signage for the two new roads |

## Construction plots, properly staged

`NorthgateSite` and `DocksideSite` carry a `ConstructionSite` component
(`Assets/CarSales/Scripts/World/ConstructionSite.cs`) and four build stages —
`[Ground, Site, Draft, Final]`, advanced deterministically by the in-game day:

- `ConstructionStages.StageAt(startDay, daysPerStage, today)` — pure function, nothing extra is saved;
- the site messages the player on every stage advance;
- `Complete()` / `SetStage(int)` can force a stage (e.g. a story beat);
- NorthgateSite starts on day 1 (2 days per stage), DocksideSite on day 2 (3 days per stage) —
  both plots are fully built in the scene; only the *active* stage is visible.

The Final buildings are real models when downloaded: **Office building 2** (Northgate, Nikitos & 3130,
CC-BY) and **General Ship Repair** (Dockside, Katie Wolfe, CC-BY — kept with the needsReview flag).
Without them: a signed concrete-frame shell, then the same footprint as a dark-glass block with bay
markings.

## Map, GPS and minimap

Map icons and categories (M key / minimap) are resolved from the place name and glyph:
`X` Patrol Station (Radio icon, Services) · `P` Tyres & Wheels (TireRepair, Services) ·
`R` RentGo (Handshake, Services) · `O` offices (Apartment, Other) · `Y` construction plots
(Construction, Other). All five kinds can be clicked on the map to set the GPS or call a taxi.

## Assets, licences and attribution

- The GLBs (~3.5 GB) are **not in this repository**. What is committed: the manifest
  (`Assets/CarSales/HarbourCity/harbour-city-assets.json`), the downloader (`tools/download_sketchfab_assets.py`),
  the regenerated attribution page (`docs/HARBOUR_CITY_ASSETS.md`) and this document.
- **80 models kept** — 72 × CC BY 4.0, 8 × CC0 — all downloadable, all recorded
  (author, licence, licence URL, face count, viewer URL) in `docs/HARBOUR_CITY_ASSETS.md`;
  see also `THIRD_PARTY_NOTICES.txt` §3. Nothing NC, nothing ND.
- **Hero villa**: *Modern luxury villa house building home* by zigurat architecture studio —
  7,742 faces, CC BY 4.0. Referenced at build time (`building_house_01` in the manifest) and placed by
  the builder as the Hillside Villa exterior.
- `needsReview` models (eyeball before shipping): **General Ship Repair** (kept — it *is* the Dockside
  plot's finished building), Two Wheeler Showroom (unused), four hatchbacks (unused — the builder takes
  the first by key), and three street props (unused). If any are unsuitable, replace them in
  `Assets/CarSales/HarbourCity/harbour-city-assets.json` and re-run `tools/manifest_docs.py`.
- Deterministic paths: `Assets/CarSales/Sketchfab/<category>/<uid>.glb` — exactly where the builder looks;
  every `importPath` in the manifest follows it.

## Cars

- The built-in database holds 8 fictional cars (Kestrel/Orion/Titan/Vortex/Aurelia) — no real makes.
- Imported cars (Asset Studio → approve as Vehicle, or **Tools → Car Sales → Release Checklist →
  Wire All Cars Into The Game**) become `CarDefinition`s in `Assets/CarSales/Data/Cars`, are registered
  in the CarDatabase on the next build, and are buyable and drivable — the wheel/handling system
  auto-detects wheels on models without a rig.
- **Rename Trademarked Cars** (Release Checklist) rewrites display names to fictional makes.
  Badge stripping on the 3D textures is manual and still required before selling.

## Verifying a build

- `python3 tools/plan_check.py` — the plan geometry, exactly as the tests see it (zones, sites off the
  roads, entrances within 14 m, no overlapping sites, Business Park Road serving the three park sites,
  Horizon Drive meeting the ring, one connected network, no dead ends).
- Edit-mode tests (`Assets/CarSales/Tests/EditMode/Phase12Tests.cs`, `Phase14Tests.cs`) lock the same
  invariants plus the construction staging ladder and the manifest contract (licences, sizes, faces).
  Note: tests are written but **could not be executed in this environment** — run
  `python3 tools/run_unity_tests.py` (or Unity Test Runner → EditMode) once Unity is available.

## Housekeeping

- `.gitignore` excludes `/Assets/CarSales/Sketchfab/` (the downloads) — commit everything else.
- Not committed and not needed in git: the downloaded GLBs, Unity's `Library/`, `GeneratedArt/`.
- If you fork/redistribute the game **with** the models, keep `docs/HARBOUR_CITY_ASSETS.md` and
  `THIRD_PARTY_NOTICES.txt` in the distribution (CC BY requires the attribution).

## Repairing a build with bad / missing assets

1. **Read the audit.** Run **Tools → Car Sales → Harbour City → Audit Harbour City**. It loads every GLB
   the manifest knows about, force-reimports the ones Unity skipped, and writes `harbour-city-audit.md`
   to the project root with a verdict per asset: `OK` · `MISSING FILE` (download it) · `FAILED TO IMPORT`
   (the **asset import worker crashed** — close other apps, reimport that single GLB via right-click →
   *Reimport*, and if it keeps crashing, delete just that one file so Harbour City falls back to its
   stand-in instead of hanging the editor) · `IMPORTED WITH MISSING MATERIALS` · `NO RENDERERS`.
2. **Fix materials in the open scene.** **Tools → Car Sales → Harbour City → Repair Missing Materials
   (Open Scene)** replaces every empty material slot and every material whose shader Unity could not
   locate with a URP/Lit grey (`Assets/CarSales/Gen/Materials/ImportRecovery.mat`) so the scene is
   presentable while broken imports are fixed. Save (Ctrl+S) afterwards.
3. **Fix orientation / scale per asset.** Edit `Assets/CarSales/HarbourCity/harbour-city-overrides.json`
   (format below) and re-run **Build Harbour City**; the builder applies the override when placing.
   - `yawOffset` — degrees added to the placement yaw (e.g. `180` turns a building around).
   - `footprintScale` — multiplies the footprint the model is scaled to (e.g. `1.5` = 50% larger).
   - `skip` — `true` means "don't place this GLB" (the stand-in is used instead).
   - Each entry matches by `uid` (preferred, from the manifest / docs/HARBOUR_CITY_ASSETS.md) or by `key`.
   - Delete the `example-not-a-real-uid` entry before shipping.
4. **Rebuild** (**Tools → Car Sales → Harbour City → Build Harbour City**) and re-audit. Repeat until the
   report reads the way you want it.

### harbour-city-overrides.json format

```json
{
  "assets": [
    { "uid": "4958b4ac23eb445aaa6115278676d5df", "yawOffset": 0,   "footprintScale": 1.0 },
    { "uid": "<uid from the manifest>",          "yawOffset": 90,  "footprintScale": 1.25, "skip": false },
    { "key": "car_sedan_01",                     "yawOffset": 180, "footprintScale": 0.9 }
  ]
}
```

All of the above is per-model and judged by eye in the scene view — Unity cannot know which way an
arbitrary GLB faces, so there is no automatic fix for orientation; the override table is the mechanism.

### Asset import worker crashes ("AssetImportWorkerHW1 has crashed")

Not a project bug — Unity ran out of memory importing the biggest GLBs (max ~87 MB / ~1.5M faces).
Mitigations: close Chrome/other heavy apps before Unity, import big GLBs one at a time, or `skip: true`
them in the overrides file. Unity logs the exact file it was importing when the worker died
(`Editor.log`, next to `Importing ...`) — check it to identify the culprit.
