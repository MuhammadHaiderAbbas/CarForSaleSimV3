# Final Map — asset placement sheet

*Generated from `Assets/CarSales/FinalMap/final-map-assets.json` + placement table in `tools/asset_map_docs.py` — re-run `python3 tools/asset_map_docs.py` after any manifest or builder change.*

Site numbers refer to the exact map render: **docs/images/final-map.png** (site list is also printed on the image).
Companion docs: FINAL_MAP_ASSETS.md (full catalogue + attribution), FINAL_MAP.md (audit & repair), NEW_MAP.md (linkage), CATALOG.md (everything in the project).

**20 of 80 curated models are placed by the current builder.** The rest are deliberate headroom — see the checklist below and “How to place more”.

## Read this first

1. `SKETCHFAB_TOKEN=… python3 tools/download_sketchfab_assets.py` — fetches GLBs into `Assets/CarSales/Sketchfab/<category>/<uid>.glb` (never committed, ~3.5 GB).
2. `python3 tools/preflight_assets.py` — verifies every download before Unity (size, GLB magic, JSON chunk, triangle/image counts, Draco/meshopt flags, external references, brand strings for the Release Checklist). Report: `docs/ASSET_PREFLIGHT.md`.
3. Open Unity → **Tools → Car Sales → Final Map → Build Final Map** — places the models listed here; every miss falls back to a yellow stand-in with a PlaceholderSlot.
4. **Tools → Car Sales → Final Map → Audit Final Map** names any model Unity cannot read; see docs/FINAL_MAP.md.

## A. Buildings & POI shells placed by the Final Map

| Map site | Model (manifest key) | Licence | Footage |
|---|---|---|---|
| Site 3 · Hillside Villa (H, zone 1) | **Modern luxury villa house building home** (`building_house_01`) | BY | Hero override: replaces the primitive villa shell (Home_Villa/Level1); hides VillaBase/VillaUpper/Glass1/Glass2/RoofTrim. Yaw 0°, ~17×11 m footprint. |
| Site 22 · Northgate Offices (O, zone 15) | **Headquarters building, office building** (`building_office_03`) | BY | Named in BuildOffices ('NorthgateOffices'). ~23×16 m footprint, sign 'NORTHGATE OFFICES'. |
| Site 23 · Offices West (O, zone 15) + Site 24 · Northgate Site finished shell (Y, zone 14) | **Office building 2** (`building_office_04`) | BY | Used twice: BuildOffices ('OfficesWest') ~19×14 m; construction stage 3 'finalKey' ~20×15 m (swaps in after StageCount builds). |
| Site 21 · RentGo sales office (R, zone 15) | **Car showroom** (`building_showroom_02`) | BY | Name match 'Car showroom' in BuildRentGo. ~13×9 m; a blue-striped RENTGO sign goes on the roof. |
| Site 18 · Patrol Station (X, zone 10) | **Police station in Kobe, Japan** (`building_patrol_02`) | BY | Category match 'building_patrol' in BuildPatrolStation. ~19×15 m; sign 'PATROL STATION' + blue light bar. |
| Sites 24 & 25 · construction Stage 2 'Draft' shell (Y) | **Building under construction** (`building_construction_02`) | BY | Name match 'Building under construction' in BuildConstructionPlot. ~21×14 m; columns/slabs primitive otherwise. |
| Site 25 · Dockside Site finished shell (Y, zone 3) | **General Ship Repair** (`building_auction_02`) | BY | Name match 'General Ship Repair' as DocksideSite's finalKey. ~20×15 m. Manifest flags needs review — eyeball in Asset Studio before shipping. |

## B. Construction stage 1 — machines & scaffolding

| Map site | Model | Licence | Detail |
|---|---|---|---|
| Sites 24 & 25 · Stage 1 'Site' machines | **Excavator** (`construction_machine_01`) | BY | Excavator — placed deterministically from manifest key order. |
| Sites 24 & 25 · Stage 1 'Site' machines | **Telescopic Forklift T30104** (`construction_machine_02`) | BY | Telescopic Forklift T30104. |
| Sites 24 & 25 · Stage 1 'Site' machines | **Truck** (`construction_machine_03`) | BY | Truck. |
| Sites 24 & 25 · Stage 1 'Site' machines | **Mining excavator, Mining machine** (`construction_machine_04`) | BY | Mining excavator, Mining machine. (_05, _08, _09 are backups; _06 'tower crane' is deliberately excluded — the map uses its own primitive crane.) |
| Sites 24 & 25 · Stage 1 scaffolding | **Pergula 3D scaffolding** (`construction_prop_01`) | BY | Name match 'Pergula 3D scaffolding' in BuildConstructionPlot. |

Placement is deterministic: `PickFromCategory("construction_machine", 6)` in manifest key order, the tower-crane entry excluded, first 4 placed — the primitive red crane is always present on top.

## C. Cars placed as static show models

| Map site | Model | Licence |
|---|---|---|
| Site 21 · RentGo display bay (bay 1) | **Hatchback car model** (`car_hatchback_02`) | BY |
| Site 21 · RentGo bay 2 + Site 22 · Northgate Offices company car | **BMW M3 Sedan topaz blue car** (`car_sedan_01`) | BY |
| Site 21 · RentGo bay 3 + Site 23 · Offices West company car | **Suv Car** (`car_suv_01`) | BY |
| Site 21 · RentGo display bay (bay 4) | **Furtivo Supercar (Black Diamond)** (`car_sports_01`) | BY |
| Site 21 · RentGo display bay (bay 5) | **Maserati-ghibli-hybrid** (`car_luxury_01`) | BY |
| Site 21 · RentGo far lot | **Pickup Truck** (`car_pickup_01`) | BY |

These are display cars only — no CarVisual, no stock, no trade-in. The fleet classes in the game economy are unchanged.

## D. Park props

| Map site | Model | Licence |
|---|---|---|
| Zone 13 · The Public Park — statue 1 (west of Park Road ring) | **Knabe mit Vögeln** (`prop_park_03`) | CC0 |
| Zone 13 · The Public Park — statue 2 | **Zwei Bären** (`prop_park_04`) | CC0 |

## E. Full checklist — all 80 curated assets

| key | model | role | licence | place on the map |
|---|---|---|---|---|
| `building_apartment_05` | residential complex modern apartment building | world | BY | — *not placed.* Spare. Zone 10 'Rental Apartments' is primitive; wire it in if you extend the Dockside/Buildings programme. |
| `building_auction_02` | General Ship Repair | world | BY | Site 25 · Dockside Site finished shell (Y, zone 3) |
| `building_carwash_02` | Elephant carWash | world | BY | — *not placed.* Car Wash (Site 9) is built from primitives by design. Swap with Asset Studio → Town if you prefer this model. |
| `building_construction_02` | Building under construction | world | BY | Sites 24 & 25 · construction Stage 2 'Draft' shell (Y) |
| `building_house_01` | Modern luxury villa house building home | hero | BY | Site 3 · Hillside Villa (H, zone 1) |
| `building_house_06` | Classic Suburban House with Separate Garage | world | BY | — *not placed.* Spare for the residential zone (Sites 2/3); the master map keeps primitives there. |
| `building_house_07` | modern house | world | BY | — *not placed.* Spare for the residential zone. |
| `building_house_08` | Modern House with full interior | world | BY | — *not placed.* Spare for the residential zone (has an interior — useful if homes become walkable). |
| `building_office_03` | Headquarters building, office building | world | BY | Site 22 · Northgate Offices (O, zone 15) |
| `building_office_04` | Office building 2 | world | BY | Site 23 · Offices West (O, zone 15) + Site 24 · Northgate Site finished shell (Y, zone 14) |
| `building_patrol_02` | Police station in Kobe, Japan | world | BY | Site 18 · Patrol Station (X, zone 10) |
| `building_showroom_01` | Two Wheeler Showroom | world | BY | — *not placed.* Two Wheeler Showroom — manifest flags needs review; not auto-placed. |
| `building_showroom_02` | Car showroom | world | BY | Site 21 · RentGo sales office (R, zone 15) |
| `car_hatchback_02` | Hatchback car model | showcase | BY | Site 21 · RentGo display bay (bay 1) |
| `car_hatchback_03` | Car winter | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_hatchback_06` | Car_ Design_riyadh City | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_hatchback_07` | Low Poly City-Pack | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_hatchback_08` | City | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_01` | Maserati-ghibli-hybrid | showcase | BY | Site 21 · RentGo display bay (bay 5) |
| `car_luxury_02` | 1992 Jaguar XJ220 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_03` | Rolls-Royce Silver Shadow 1965 1980 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_04` | 1997 Mercedes C-class W202 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_05` | Realistic Classic Sedan – Unity & Unreal | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_06` | Mercedes Benz EQE AMG | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_07` | Aston martin valkyrie amr pro 2022 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_luxury_08` | Porsche Panamera 4S | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_pickup_01` | Pickup Truck | showcase | BY | Site 21 · RentGo far lot |
| `car_pickup_02` | American pickup truck | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_pickup_05` | Ford Model A Pickup | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_pickup_07` | Sunny's pickup truck | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_pickup_08` | C10-Monster Truck and Pickup | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_01` | BMW M3 Sedan topaz blue car | showcase | BY | Site 21 · RentGo bay 2 + Site 22 · Northgate Offices company car |
| `car_sedan_02` | Ford Focus Sedan | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_03` | Generic Sedan Car | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_04` | Red Sedan Car | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_05` | Sedan Car | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_06` | Soviet Car. VAZ-2103 Zhiguli | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_07` | Oldsmobile Cutlass Supreme Sedan '71 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sedan_08` | Generic passenger car pack | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_01` | Furtivo Supercar (Black Diamond) | showcase | BY | Site 21 · RentGo display bay (bay 4) |
| `car_sports_02` | BMW X6M (F16) | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_03` | Supercar | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_04` | Chevrolet Corvette (C7) | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_05` | Gerry Anderson's Supercar | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_06` | Mercedes Benz GT Coupe AMG | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_07` | lamborghini\|car\|supercar\|hypercar\|italia\|cycles | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_sports_08` | BG Thrill - 3D Ferrari Supercar | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_01` | Suv Car | showcase | BY | Site 21 · RentGo bay 3 + Site 23 · Offices West company car |
| `car_suv_02` | Mid Size Off Road SUV | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_03` | Kia SUV | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_04` | Generic SUV Snow covered wrecked | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_05` | Chevy Colorado Z71 4x4 2015 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_06` | 4x4 toyota Bj | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_07` | 4x4 | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `car_suv_08` | 3D SUV Car | showcase | BY | — *not placed.* Showcase catalogue — the Final Map places only the first key per class (RentGo + company cars). The other cars are imported into the game economy only via your own wiring. |
| `construction_machine_01` | Excavator | world | BY | Sites 24 & 25 · Stage 1 'Site' machines |
| `construction_machine_02` | Telescopic Forklift T30104 | world | BY | Sites 24 & 25 · Stage 1 'Site' machines |
| `construction_machine_03` | Truck | world | BY | Sites 24 & 25 · Stage 1 'Site' machines |
| `construction_machine_04` | Mining excavator, Mining machine | world | BY | Sites 24 & 25 · Stage 1 'Site' machines |
| `construction_machine_05` | Bulldozer scan 01 | world | BY | — *not placed.* Backup machine (deterministic order: only the first 4 non-crane are placed). |
| `construction_machine_06` | Construction site, building site, tower crane | world | BY | — *not placed.* Deliberately excluded — the map uses its own primitive tower crane (two masts + jib). |
| `construction_machine_08` | Clarkair Bulldozer | world | BY | — *not placed.* Backup machine. |
| `construction_machine_09` | Bulldozer, Mining machine | world | BY | — *not placed.* Backup machine. |
| `construction_prop_01` | Pergula 3D scaffolding | world | BY | Sites 24 & 25 · Stage 1 scaffolding |
| `construction_prop_04` | Tube & Fitting suspended scaffolding | world | BY | — *not placed.* Suspended scaffolding — not placed; the ground scaffold (_01) is used. |
| `furniture_computer_01` | Cash register with a counting machine | world | CC0 | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_equipment_02` | Regular truck tires | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_equipment_04` | Workbench | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_lounge_02` | Armchair | world | CC0 | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_lounge_03` | Armchair With Slip Seat | world | CC0 | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_lounge_04` | White Victorian Armchair | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_office_02` | Side Chair | world | CC0 | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_office_03` | Litewall Counter Table | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_office_04` | office chair | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `furniture_office_05` | Vibe Counter Panel Table | world | BY | — *not placed.* Interiors are primitive by design (furnished via Asset Studio / Phase 13 props if you swap). |
| `prop_park_03` | Knabe mit Vögeln | world | CC0 | Zone 13 · The Public Park — statue 1 (west of Park Road ring) |
| `prop_park_04` | Zwei Bären | world | CC0 | Zone 13 · The Public Park — statue 2 |
| `prop_street_01` | Flood Street - Fragment 1 | world | CC0 | — *not placed.* 'Flood Street — Fragment 1' — a Roman fragment, not street furniture; kept as a catalogue piece. |
| `prop_street_02` | The lamp shaft topped with a crowned eagle | world | CC0 | — *not placed.* Crowned-eagle lamp shaft — kept as catalogue; the map builds its own street lamps. |
| `prop_street_04` | road sign_OLD_4к | world | BY | — *not placed.* 'road sign_OLD_4к' — cyrillic sign, off-theme; kept as catalogue. |

## F. How placement works, and how to change it

- The builder (`Assets/CarSales/Editor/CarSalesFinalMap.cs`) resolves each placement with `FindAsset` — it matches on **key**, then **name**, then **category** (first manifest entry by key order). Missing/undownloadable models fall back to a labelled yellow stand-in (`PlaceholderSlot`), so a partial download never blocks the build.
- `TryPlaceGlb` scales each model to a footprint (keeps aspect ratio), stands it on the ground and adds `MeshCollider`s + static flags. Buildings keep their authored front; cars/machines may turn 90° to fit.
- Per-model tweaks without code: `Assets/CarSales/FinalMap/final-map-overrides.json` — `{ "uid": "…", "yawOffset": 90, "footprintScale": 1.1, "skip": true }`.
- To place more: extend `BuildFinalMapExtras` (or the specific `Build*`) in `CarSalesFinalMap.cs` with `PlaceGlbOrStandIn(g, "<key>", localCentre, footprint, yaw, allowFlip, kind, m, "what")`. Keep the stand-in fallback — it is what makes partial downloads safe.
- After any change: `python3 tools/asset_map_docs.py` to refresh this sheet, and `python3 tools/render_plan_map.py` if sites or roads moved.

## G. Honest size note

The download is ~3.5 GB but today only 20 models appear in the scene (many are the same model in two spots). The remaining GLBs are the showcase fleet and catalogue props — importing them all is pointless today, but they are the pool you draw from when you extend the map, and none of them is committed to git.

