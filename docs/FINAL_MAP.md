# The Final Map (Phase 14)

The Final Map is the finished game town: the master-map town (version 2, unchanged — same roads,
same dealerships, same everything) **plus** the Phase 14 programme:

| POI | Where | What it is |
| --- | --- | --- |
| Tyres & Wheels | Dealer Road, opposite Parts Direct (zone 8) | The dedicated tyre shop (the master map dresses this spot as part of the parts complex; the Final Map builds a real one) |
| Patrol Station | Dealer Road, town centre end (zone 10) | Emergency-services POI for the police patrol feature |
| RentGo Rentals & Sales | Business Park Road, Southside Business Park (zone 15) | The rival rental & sales lot — static display cars (not in stock, not for sale, never for rent) |
| Northgate Offices & Offices West | Business Park Road, zone 15 | The south-side business park |
| Northgate Site (under construction) | By the northern ring (zone 14) | A real construction plot: cleared ground → machines → frame → finished building |
| Dockside Site (under construction) | Dockside, opposite the auction (zone 3) | The same construction staging, themed as a ship-repair yard |
| Hillside Villa (your third home) | Zone 1 | The exterior shell is the curated 7,742-face **hero villa** GLB (master map keeps the primitive villa) |
| Horizon Drive + Business Park Road | South-west of Depot Road | The new estate road and its link down to the ring road — both are in the traffic graph (real junctions, give-way) |
| Public park statues & new road signs | Zone 13 / Business Park Road | CC0 statue dressing and signage for the two new roads |

**The master map builds exactly as before** (`Town.Set(2)`, `Tools → Car Sales → 2. Build Everything`).
`Build Everything` disables the FinalMap.unity build-settings entry when it exists, and the
**Build Final Map** menu does the same in reverse — each build ships its own town. New games
default to the Final Map (`Town.Set(3)`; the scene carries a `TownMapMarker` with `townVersion = 3`,
and everything — traffic, the world map, GPS — reads it).

## Build it

1. `Tools → Car Sales → 1. Check & Install Packages` — installs the Unity packages the game needs
   (now including **GLTFast**, `com.unity.cloud.gltfast`, to load the Phase 14 models).
2. `Tools → Car Sales → 2. Build Everything` — once. Builds the master map (needed: it creates the
   materials, car database and prefabs the Final Map reuses).
3. *(Optional, for the real models)* download the Sketchfab set:

   ```
   SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py
   ```

   (~3.5 GB into `Assets/CarSales/Sketchfab/<category>/<uid>.glb` — git-ignored, never committed.
   Token: sketchfab.com → Settings → Password & API. `python3 tools/run_unity_tests.py` runs the
   edit-mode tests headlessly.)
4. `Tools → Car Sales → Final Map → Build Final Map`.

**Missing models are fine.** Every model is optional: anything not downloaded yet is replaced by a
primitive stand-in (a yellow `PlaceholderSlot` box — Asset Studio → Town can swap it later), and the
build still completes. The completion dialog and the Console (`[CarSales] Final Map build report:`)
list exactly what was placed and what was stood in for.

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
CC-BY) and **General Ship Repair** (Dockside, Katie Wolfe, CC-BY — kept with the
needsReview flag). Without them: a signed concrete-frame shell, then the same footprint as a
dark-glass block with bay markings.

## Map, GPS and minimap

Map icons and categories (M key / minimap) are resolved from the place name and glyph:
`X` Patrol Station (Radio icon, Services) · `P` Tyres & Wheels (TireRepair, Services) ·
`R` RentGo (Handshake, Services) · `O` offices (Apartment, Other) · `Y` construction plots
(Construction, Other). All five kinds can be clicked on the map to set the GPS or call a taxi.

## Assets, licences and attribution

- The GLBs (~3.5 GB) are **not in this repository**. What is committed: the manifest
  (`Assets/CarSales/FinalMap/final-map-assets.json`), the downloader (`tools/download_sketchfab_assets.py`),
  the regenerated attribution page (`docs/FINAL_MAP_ASSETS.md`) and this document.
- **80 models kept** — 72 × CC BY 4.0, 8 × CC0 — all downloadable, all recorded
  (author, licence, licence URL, face count, viewer URL) in `docs/FINAL_MAP_ASSETS.md`;
  see also `THIRD_PARTY_NOTICES.txt` §3. Nothing NC, nothing ND.
- **Hero villa**: *Modern luxury villa house building home* by zigurat architecture studio —
  7,742 faces, CC BY 4.0 — <https://sketchfab.com/3d-models/modern-luxury-villa-house-building-home-4958b4ac23eb445aaa6115278676d5df>.
  Referenced at build time (`building_house_01` in the manifest) and placed by
  **Tools → Car Sales → Final Map → Build Final Map** as the Hillside Villa exterior.
- `needsReview` models (eyeball before shipping: load in Unity, check scale/materials, swap or drop):
  **General Ship Repair** (kept — it *is* the Dockside plot's finished building), Two Wheeler Showroom
  (unused by the builder), four hatchbacks (unused — the builder takes the first by key), and three
  street props (unused). If any are unsuitable, replace them in `Assets/CarSales/FinalMap/final-map-assets.json`
  and re-run `tools/manifest_docs.py`.
- Deterministic paths: `Assets/CarSales/Sketchfab/<category>/<uid>.glb` — exactly where the builder looks;
  every `importPath` in the manifest follows it.

## Verifying a build

Edit-mode tests (`Assets/CarSales/Tests/EditMode/Phase14Tests.cs`) lock the geometry: zones 1–15,
every site off-roads and reachable, no overlapping sites, Horizon Drive meets the ring road,
Business Park Road serves the three park sites, pavements don't cross roads, the lane graph stays
connected with lights only at the two lit junctions, the construction staging ladder, and the
manifest contract (licences, sizes, face budget). Note: tests are written but **could not be executed
in this environment** — run `python3 tools/run_unity_tests.py` (or Unity Test Runner → EditMode) once
Unity is available, and check the editor Console on the first build for missing-model stand-ins.

## Housekeeping

- `.gitignore` excludes `/Assets/CarSales/Sketchfab/` (the downloads) — commit everything else.
- Not committed and not needed in git: the downloaded GLBs, Unity's `Library/`, `GeneratedArt/`.
- If you fork/redistribute the game **with** the models, keep `docs/FINAL_MAP_ASSETS.md` and
  `THIRD_PARTY_NOTICES.txt` in the distribution (CC BY requires the attribution).

## Repairing a build with bad / missing assets

If the first build shows placeholder-ish or broken models, work the list in this order:

1. **Read the audit.** Run **Tools → Car Sales → Final Map → Audit Final Map**. It loads every GLB
   the manifest knows about, force-reimports the ones Unity skipped, and writes `final-map-audit.md`
   to the project root with a verdict per asset: `OK` · `MISSING FILE` (download it) · `FAILED TO IMPORT`
   (the **asset import worker crashed** — Unity 6 enforces a per-import memory ceiling on big GLBs;
   close other apps, reimport that single GLB via right-click → *Reimport*, and if it keeps crashing,
   delete just that one file so the Final Map falls back to its stand-in instead of hanging the editor) ·
   `IMPORTED WITH MISSING MATERIALS` · `NO RENDERERS`.
2. **Fix materials in the open scene.** **Tools → Car Sales → Final Map → Repair Missing Materials
   (Open Scene)** replaces every empty material slot and every material whose shader Unity could not
   locate with a URP/Lit grey (`Assets/CarSales/Gen/Materials/ImportRecovery.mat`) so the scene is
   presentable while broken imports are fixed. Save (Ctrl+S) afterwards.
3. **Fix orientation / scale per asset.** Edit `Assets/CarSales/FinalMap/final-map-overrides.json`
   (format below) and re-run **Build Final Map**; the builder applies the override when placing.
   - `yawOffset` — degrees added to the placement yaw (e.g. `180` turns a building around).
   - `footprintScale` — multiplies the footprint the model is scaled to (e.g. `1.5` = 50% larger).
   - `skip` — `true` means "don't place this GLB" (the stand-in is used instead). Use this for GLBs
     that crash the import worker or are the wrong model entirely.
   - Each entry matches by `uid` (preferred, from the manifest / docs/FINAL_MAP_ASSETS.md) or by `key`.
   - Delete the `example-not-a-real-uid` entry before shipping.
4. **Rebuild** (**Tools → Car Sales → Final Map → Build Final Map**) and re-audit. Repeat until the
   report reads the way you want it.

### final-map-overrides.json format

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

### Asset import worker crashes ("AssetImportWorkerHW1 has crashed", 20:10 / 20:11 in the Console)

Not a project bug — Unity ran out of memory importing the biggest GLBs (max ~87 MB / ~1.5M faces).
Mitigations: close Chrome/other heavy apps before Unity, import big GLBs one at a time, or `skip: true`
them in the overrides file. Unity logs the exact file it was importing when the worker died
(`Editor.log`, next to `Importing ...`) — check it to identify the culprit.
