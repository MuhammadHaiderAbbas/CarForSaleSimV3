# Complete inventory — buildings, cars, people, everything

Use this when modelling replacements in Blender or laying out a new map.
Concept/mood references live in `Assets/CarSales/Art/Concepts/` (see end).

## 1. Buildings & structures (27 distinct kinds)

| # | Kind |Where built |Built-in mesh | Sketchfab GLB (if downloaded) |
|---|------|-------------|---------------|------------------------------|
|1| Dealership HQ (AUTO WORLD) | master+ | Dealer building + canopy | — |
|2| Downtown Motors dealership | master+ | same, spec variant | — |
|3| Luxury Lane dealership | master+ | same, premium palette | — |
|4| Auction Center (hall) | master+ | auction hall + ramp | *General Ship Repair* at Dockside only |
|5| Used Car Market office/lot | master+ | open-air office caravan | — |
|6| Service Centre / MOT | master+ | service unit | — |
|7| Mechanic Workshop | master+ | workshop shell | — |
|8| Car Wash hall | master+ | wash hall | *Elephant carWash* |
|9| Fuel Station forecourt | master+ | canopy + shop | — |
|10| Parts Direct store | master+ | parts store | — |
|11| Tyres & Wheels | FM | tyre workshop | — |
|12| Starter Trailer | master+ | trailer | — |
|13| Suburban House | master+ | house shell | GLB house swap (optional) |
|14| Hillside Villa | master+ | villa shell | *Modern luxury villa house* (zigurat, CC BY) — hero |
|15| Rental Apartments investment | master+ | block | — |
|16| Office Tower investment | master+ | tower | — |
|17| Parking Garage | master+ | garage block | — |
|18| Residential close houses (Maple/Birch ×2 streets) | master+ | rows of houses | — |
|19| Patrol Station | FM | slab + light bar | *Police station in Kobe, Japan* |
|20| RentGo sales office | FM | office block | *Car showroom* |
|21| Northgate Offices (business park) | FM | dark-glass slab | *Headquarters building, office building* |
|22| Offices West (business park) | FM | dark-glass slab | *Office building 2* |
|23| Northgate construction (3 stages) | FM | crane/hoardings/columns | *Building under construction* → `Office building 2` |
|24| Dockside construction (3 stages) | FM | crane/hoardings/columns | *Building under construction* → *General Ship Repair* |
|25| Town Centre skyline + apartments in zone 10 | master+ | towers | *residential complex modern apartment building* (unused by builder — available for your new map) |
|26| Industrial Estate sheds (zone 11) | master+ | sheds | — |
|27| Park (zone 13) + Home_Villa skyline pieces | master+ | trees, benches, fences | statues *Knabe mit Vögeln*, *Zwei Bären* (CC0) |

Unused GLB buildings awaiting your hand (safe to wire into a new map): *Classic Suburban House*, *modern house*,
*Modern House with full interior*, *residential complex… apartment building*, *Two Wheeler Showroom*,
*Flood Street - Fragment 1* (street scenery), *lamp shaft topped with a crowned eagle*, *road sign_OLD_4к*.

## 2. Cars (3 classes of stock + display)

### Built-in low-poly stock (always present; created by 2. Build Everything)

| id | Brand · Model | Class | Year | Price | Notes |
|----|---------------|-------|------|-------|-------|
|hatch_velo| Kestrel Velo | Hatch | 2016 | £12,000 | cheap & cheerful |
|hatch_metro| Kestrel Metro S | Hatch | 2019 | £15,500 | automatic |
|sedan_avenue| Orion Avenue | Sedan | 2014 | £18,000 | diesel auto |
|sedan_avenuegt| Orion Avenue GT | Sedan | 2020 | £26,000 | sporty sedan |
|suv_ridge| Titan Ridge | SUV/4×4 | 2017 | £32,000 | family |
|sports_stallion| Vortex Stallion R | Sports | 2018 | £55,000 | manual coupe |
|pickup_hauler| Titan Hauler | Pickup | 2015 | £28,000 | trade workhorse |
|lux_regent| Aurelia Regent | Luxury | 2019 | £70,000 | hybrid exec |

All are ~1500–2500 tris procedural bodies (`CarModels`). Display/lot cars always exist; player can buy/sell, service, auction, finance.

### Imported from Asset Studio (user-imported; in your project only)

- `imported dodge_dart_gt` → renamed to **Redbrook Dart GT** via Release Checklist → Rename (badge must still be stripped).
- `imported mercedes-benz_w211_e55__www.vecarz.com` → **Meridian E55** (same badge caveat).

### Sketchfab display GLBs in the Final Map (static display only, not for sale)

- car_hatchback ×5: *Hatchback car model*, *Car winter*, *Car_ Design_riyadh City*, *Low Poly City-Pack*, *City*
- car_sedan ×8: *BMW M3 Sedan*, *Ford Focus Sedan*, *Generic Sedan Car*, *Red Sedan Car*, *Sedan Car*, *Soviet Car VAZ-2103*, *Oldsmobile Cutlass Supreme '71*, *Generic passenger car pack*
- car_suv ×8: *Suv Car*, *Mid Size Off Road SUV*, *Kia SUV*, *Generic wrecked SUV*, *Chevy Colorado Z71*, *4x4 toyota Bj*, *4x4*, *3D SUV Car*
- car_luxury ×8: *Maserati Ghibli hybrid*, *Jaguar XJ220 (1992)*, *Rolls-Royce Silver Shadow*, *Mercedes C-class W202 (1997)*, *Realistic Classic Sedan*, *Mercedes EQE AMG*, *Aston Martin Valkyrie AMR Pro*, *Porsche Panamera 4S*
- car_sports ×8: *Furtivo Supercar*, *BMW X6M F16*, *Supercar*, *Chevrolet Corvette C7*, *Gerry Anderson Supercar*, *Mercedes GT Coupe AMG*, *lamborghini|car|supercar|italia|cycles*, *BG Thrill Ferrari*
- car_pickup ×5: *Pickup Truck*, *American pickup truck*, *Ford Model A Pickup*, *Sunny's pickup truck*, *C10 Monster Truck*

Licences: mostly CC-BY / CC0 — attribute (already in THIRD_PARTY_NOTICES.txt). **Trademark risk:** the
re-brand = rename + remove badges (see RELEASE.md). Do not sell with maker logos on the 3D paint.

## 3. People (out of the box, no downloads needed)

| Archetype | Share | Height | Width | Speed | Details |
|-----------|-------|--------|-------|-------|---------|
| Man | 30% | 1.70–1.90 | 0.85–1.25 | 1.1–1.6 | menswear, 30% flat cap |
| Woman | 25% | 1.58–1.78 | 0.75–1.05 | 1.1–1.6 | womenswear, 30% hat |
| Senior | 13% | 1.50–1.72 | 0.95–1.25 | 0.65–1.05 | greys, 40% hat, 45% cane |
| Young | 14% | 1.60–1.85 | 0.70–1.00 | 1.35–2.1 | 25% jog 2.6–3.6 |
| Child | 10% | 1.00–1.35 | 0.70–0.95 | 1.4–2.3 | bright |
| Tall | 8% | 1.90–2.10 | 0.95–1.20 | 1.15–1.55 | long-line |

Skin: 6 tones · Hair: 6 colours (seniors grey/white) · Eyes: none on primitive version; Mixamo models replace the body entirely.
Mixamo cast guidance: docs/PEOPLE.md (8 characters, idle+walk, all tagged Customer/Pedestrian/Staff).

## 4. Props & furniture (manifest)

- Street/prop: *Flood Street - Fragment 1*, *The lamp shaft topped with a crowned eagle*, *road sign_OLD_4к*, scaffolding (*Pergula 3D*) — Northgate/Dockside; *Tube & Fitting* — spare.
- Park: *Knabe mit Vögeln*, *Zwei Bären* (CC0).
- Workshop equipment: *Regular truck tires*, *Workbench* (equipment slots by Tyres & Wheels & the Workshop).
- Office furniture: *Side Chair*, *Litewall Counter Table*, *office chair*, *Vibe Counter Panel Table* (retail/office desks — service counters, RentGo).
- Lounge: *Armchair*, *Armchair With Slip Seat*, *White Victorian Armchair* (villa/lounge; Armchair is 1.43M faces — hero only).
- Cash: *Cash register with a counting machine* (the till — computer/desk).
- Construction machines (8): *Excavator*, *Telescopic Forklift T30104*, *Truck*, *Mining excavator*, *Bulldozer scan 01*, *Clarkair Bulldozer*, *Bulldozer Mining machine*, *Construction site tower crane* (the tower crane GLB; if it crashes the import worker, the primitive crane in code is used).

## 5. Everything else in a scene (system objects, not art)

- Lights: directional sun (ACES, 5600K), baked probes (reflection), LampGlow lamp heads (URP emissive).
- `MapPlace` glyphs: D dealership · A auction · U used market · S service · W workshop · C car wash · F fuel · P parts/tyres · $ investment/parking · H house · G park (zone 13) · T town centre · I industrial · M street · X patrol · R RentGo · O office · Y construction (6 letters reserved, pick any).
- `Site` / `MapPlace` pair = how MapUI/GPS/minimap know a location exists.
- `PlaceholderSlot` kinds (Asset Studio re-skins): Building, Tree, StreetLamp, Bench, Sofa, Chair, Table, Plant, Storage, Bed, Appliance, Decor, WorkshopKit.
- NavMesh surfaces (dealership footprints + homes), `InteractLayer=20` boxes on counters/services, `StudioLayer=21` (renderer-only) and `MinimapLayer=22` (hidden from main camera).
- `ConstructionSite` component per plot with roots `[Ground, Site, Draft, Final]` — toggled by game day via `BuildingManager`.
- `RoadNetwork` on Town root; `TownMapMarker` v2/v3 (+ your v4 for a new map).
- UI: every screen is a window under the `CarSalesUI` canvas (ComputerUI desktop, PhoneUI, MapUI, DealershipUI…), built by `UIFactory`.

## 6. New-map & new-Blender-assets workflow

1. **New map**: follow docs/NEW_MAP.md — Route B for a full third town.
   Minimum viable new town: Plan file (Version=4) → Town.cs IsLagoon branch → Menu item → `BuildSceneCore` → menu wiring.
2. **New assets from Blender for the game** (match this art direction: low-poly stylish, 1500–8000 tris for world, ≤2k for cars you repeat; hero pieces may go higher):
   - Export **GLB**, no lights/cameras, apply all transforms, +Y up (this game is Unity: Z-up off; front of buildings = +Z or -Z — the builder fits by footprint, check `yaw`).
   - 1 material set per asset, URP/Lit-compatible (glTFast converts automatically) — solid colours or ≤2k textures; bake AO if you can.
   - Scale to **metres**. Buildings: footprint fit is automatic — you set the height; cars: roughly 4.4 m long sedan footprint for stock cars; characters: 1.8 m tall.
   - UVs simple; no missing materials.
   - Put the GLB in `Assets/CarSales/Sketchfab/<category>/<uid>.glb` layout only if replacing manifest entries; otherwise import via **Tools → Car Sales → Asset Studio → Online/Import** (vehicles) or **World Builder** (buildings/street) — that records licences + spawns a CarDefinition for stock.
   - Do not ship real maker badges; keep the Release Checklist green.
3. **Naming convention the code understands**: manifest `importPath` pattern `Assets/CarSales/Sketchfab/<category>/<uid>.glb`; categories listed in §2/§4 (add a new one by editing the manifest — the audit tool reads it).
4. **Face/size budget** (measured from this manifest): world cars ≤15k, hero ≤1.5M; buildings 1k–20k typical (hero tower ≤1.3M); prop ≤5k; machine ≤1.5M hero only.
   GLB ≤ ~90 MB (import worker), textures ≤2048 px for safety.
5. **Colour palette** (from the built materials): warm flag-red signs, brass/gold Trim, anthracite roofs, cream walls, teal glass, wet asphalt — match the concepts for coherence.

## 7. Reference images (in repo, CC-safe)

`Assets/CarSales/Art/Concepts/` — AI mood renders (targets, not literal screenshots):
main_menu_hero, showroom_interior, used_car_market, auction_hall, service_centre, car_wash,
fuel_station, mechanic_workshop, hillside_villa, patrol_station
`Assets/CarSales/Art/Concepts/ref_*.jpg` — Blender-ready references (straight-on elevations and sheets):

| File | Use |
|------|-----|
| ref_dealership_elevation.jpg | model the dealership facade: glass band, parapet, roller shutter |
| ref_villa_elevation.jpg | model the modern villa: cladding, oak slats, cantilever |
| ref_fuel_forecourt.jpg | fuel canopy canopy heights, island spacing, shop behind |
| ref_hatchback_3view.jpg | generic hatchback side/front/rear/three-quarter proportions |
| ref_tower_crane.jpg | lattice mast, jib, counterweights, hook — for the construction sites |
| ref_roundabout_aerial.jpg | ring-road + roundabout geometry for new-map layouts |

`docs/images/final-map.png` — **the final map, rendered exactly from the plan file** (roads, zones, sites, glyphs, lights, legend; regenerate with `python3 tools/render_plan_map.py`).
`docs/images/master-map.png` — the same render for the master map (V2).
`docs/images/final-map-aerial.jpg` — cinematic aerial mood shot of the same town (AI render, not to scale).
`docs/images/town-plan.png` — the original master plan drawing (historical reference).
`docs/ASSET_MAP.md` — every curated Sketchfab GLB → exact map site (number on final-map.png), glyph, zone; generated by `python3 tools/asset_map_docs.py`.
`docs/ASSET_PREFLIGHT.md` — per-model download check (size, triangles, Draco, brand strings); generated by `python3 tools/preflight_assets.py` after the Sketchfab download.

## 8. One-glance "everything in the project" table

| Class | Count | Examples/Notes |
|-------|-------|----------------|
|Zones| 14 master / 15 FM | see TownPlan.cs / FinalMapPlan.cs |
|Sites| 19 master / 25 FM | ids in the plan files |
|Built-in cars| 8 | §2 |
|Imported car assets| 2 (yours) + 44 Sketchfab | §2 |
|Buildings kinds| 27 | §1 |
|People archetypes| 6 (+ any Mixamo cast) | §3, docs/PEOPLE.md |
|Props/furniture GLBs| 22 | §4 |
|Map glyphs| 17 in use | §5 |
|Placeholder kinds| 13 | §5 |
|Systems (headless)| ~30 | §5, docs/NEW_MAP.md |
|Editor tools| ~40 menus | wizard + Asset Studio + Realism/Release checklists |
|EditMode test suite| 1 (Phase14Tests, untested here) | `tools/run_unity_tests.py` |
