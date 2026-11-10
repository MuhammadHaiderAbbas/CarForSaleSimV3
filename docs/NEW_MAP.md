# Making a completely new map (without breaking the existing two)

This project ships two playable towns built from one engine:

| Town | Plan file | Scene (generated) | Marker |
|------|-----------|-------------------|--------|
| Master map (V2) | `Sim/World/TownPlan.cs` | `Gen/Scenes/DealershipTest.unity` | `TownMapMarker.townVersion = 2` |
| Final Map (V3) | `Sim/World/FinalMapPlan.cs` | `Gen/Scenes/FinalMap.unity` | `TownMapMarker.townVersion = 3` |

`CarSales.Sim.Town` (Sim/World/Town.cs) is the single switch. Everything else — roads, pavements,
traffic, minimap, GPS, world map, zone queries — asks `Town`, never TownPlan/FinalMapPlan directly.

## The rule that saves you

**A new map is a third plan + a third scene, NOT a fork of the project.** Do not copy scripts.
You add data and one builder menu; you reuse all systems unchanged.

## Route A — a new town inside the master map (no new scene at all)

For "same world, my roads and sites":

1. **Tools → Car Sales → World Builder** (editor window): draw roads, add sites (workshop, car wash,
   houses, apartments, dealership) — sites get Site/MapPlace components automatically.
2. Everything you place is stored in **`Assets/CarSales/Data/WorldLayout.asset`** (`LayoutPath` in
   CarSalesWorldBuilderTool.cs). `Build Everything` re-reads it and rebuilds your sites after its own.
3. Traffic uses `WorldLayout.Network()`; if you store no roads, the master plan is used.
4. `Layout_DefaultTown.asset` is generated documentation of the town — generated, never hand-edit.

Validity checks: World Builder → **Validate** flags sites in roads / overlapping / unreachable.

## Route B — a completely new map as a third town (recommended for a real new world)

Follow the Final Map's pattern — it is already a self-contained example of "new map in this project".

1. **Duplicate the geometry plan**: copy `Assets/CarSales/Sim/World/FinalMapPlan.cs` to
   `LagoonPlan.cs`; set `public const int Version = 4;` and rewrite `Zones/Sites/Roads/Ring/Roundabouts/
   LightNodes/Pavements/MapFrame/WidthOf/RoadClearance/BoxTouchesRoad/IsRoundabout/RoundaboutRadius`
   with your geometry. Keep class name different. Keep the same `PlanZone/PlanSite/PlanRoad` types.
2. **Teach `Town.cs` the third version** (mechanical, ~10 lines): change `Current = 4` default if wanted,
   `Set()` accepts 3..4, add `IsLagoon => Current >= 4`, and delegate every member:
   `IsLagoon ? LagoonPlan.X : (IsFinal ? FinalMapPlan.X : TownPlan.X)`.
   Update `TownMapMarker` tooltip. (Do not touch anything else in TownPlan/FinalMapPlan.)
3. **New scene Path**: `const string LagoonScenePath = Gen + "/Scenes/Lagoon.unity";`
4. **New builder menu**: copy the menu item at the top of CarSalesFinalMap.cs
   (`[MenuItem("Tools/Car Sales/Lagoon Map/Build Lagoon Map")]`). Its body can be exactly the Final Map's:
   `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`, `BuildSceneCore(mats, db, carDisplayPrefab,
   customerPrefab, actions, CarSales.Sim.LagoonPlan.Version, LagoonScenePath, finalMap: true)`
   — `BuildSceneCore` already: creates the scene, Sun, Town root (with `TownMapMarker v4` +
   `RoadNetwork`), `BuildTown(...)` (every zone/site/Roads/POIs from your plan), `BuildLayoutSites`,
   Systems, Player, EventSystem, UI, NavMesh bake, SaveAssets, `EditorBuildSettings` scene list
   (menu first, then your scene). Then `PointMainMenuAt("Lagoon")` like the Final Map does (optional —
   otherwise your menu still loads DealershipTest).
5. **Optional extras** (skip = same map content as master): new POIs are plain functions like
   `BuildFinalMapExtras` — add one, call it from your builder only (`if (lagoonMap) BuildLagoonExtras(env, m)`).
6. **Assets**: reuse everything under `Gen/` (materials, prefabs, database) — they are map-independent.
   New GLBs go in `Assets/CarSales/Sketchfab/…`; the manifest is shared; add a
   `final-map-overrides.json` style file if you need per-model tweaks.
7. **Build order**: `2. Build Everything` (generates prefabs/assets) → your menu → open scene → Play.
8. **Docs/tests**: mirror `Phase14Tests.cs` for your zone count, off-road sites, road clearance,
   pavements on pavements, one connected network; and note the master map must stay untouched
   (Deltas: `Town.Set(2)` in a test scene).

Checklist before shipping a new map: it is reachable (network connected), no site inside a road,
spawn point on pavement, NavMesh baked, EditorBuildSettings has menu+scene, `PointMainMenuAt` correct,
Phase tests green.

## Route C — replacing the menu background / wiring the menu

Not a map, but people ask: the menu scene is `Gen/Scenes/MainMenu.unity`; `MainMenuUI.gameScene` decides
which town Play loads. `PointMainMenuAt` flips it (registry: `Build MainMenuUI → gameScene`).

---

# Linkage — what references what inside a built scene

A scene here is three roots + a light. Everything below is created by code (`BuildSceneCore`),
and the names are your debugging handles. Starred items are the ones that break most often.

## Roots

| Object | Carries | Wired to |
|--------|---------|----------|
| `Directional Light` | `Light`, `RenderSettings.sun` | quality tier; `DayNightLighting` (car sales) animates it; night profile takes over in rain |
| `EventSystem` | `EventSystem` + `InputSystemUIInputModule` | UI input everywhere (actions assigned at build) |
| `Town` (Env) | `TownMapMarker` (v2/v3/v4 → `Town.Set` in Awake), `RoadNetwork` (segments/lightNodes — used by TrafficSystem, RoadGraph, GPS) | the world itself; `MapUI` reads it; minimap reads `MapPlace` markers under it |
| `CarSalesSystems` | ~30 components + the UI canvas; **also saved as prefab** `Gen/Prefabs/CarSalesSystems.prefab` and re-linked on scene build | the whole game brain; a new build **replaces** this object |
| `Player` | `CharacterController`, `CameraPivot` → `Main Camera` (tagged MainCamera) | controls must not change except `CameraPivot` child layout |
| `Player/CameraPivot/Main Camera` | `Camera`, `AudioListener`, `SetUpLooks(cam)` (see CarSalesLooks.cs: reflection probes / flags) | `PlayerInteractor` (cam ref), `SimpleFirstPersonController` (pivot), cullingMask hides StudioLayer + MinimapLayer |
| `Player` | `MenuInputBlocker` (disables the FirstPersonController while any menu is open) | expects the UI to keep its focus model |

## `Town` children (all built by code)

- `Roads` (created by `BuildPlanRoads(Group(env,"Roads"), m)`): meshes + colliders; the
  `RoadNetwork` component lives on the **`Town` root** (`RoadNetwork.Instance` in `RoadNetwork.cs`),
  not on this group.
- Site groups — one per site in the plan (e.g. `AUTO WORLD`, `Used Car Market`, `TyreShop`,
  `Home_Villa`, `NorthgateSite…`). Inside a typical site group:
  - `MapPlace`* — name, glyph, colour, `arrivalPoint` (invisible marker where the map/GPS sends you)
    — also becomes the `Site` component via `TagDefaultSites`/site builders (Id, displayName,
    function, entrance). **The minimap, GPS, "field map" UI and the Save system's property registry all
    key off these two components.**
  - `PlaceholderSlot`* — present on every primitive stand-in the Asset Studio Town tab can re-skin;
    `PlaceholderReplacer.ApplyToOpenScene` swaps them with your imported models on build.
  - `NavMeshSurface`* — one per dealership/home footprint (children of the site); baked by
    `BuildSceneCore` to `Gen/Scenes/NavMesh_N.asset` (error → surfaces stay null and Unity bakes at runtime).
  - Interactable volumes (`InteractBox`: the counters/shops/service points) — layer `InteractLayer`;
    `PlayerInteractor` raycasts only this layer.
  - GLB dressing (Final Map extras) — not referenced by gameplay code at all (purely visual).
- POI groups from the code builders (`FinalMap/*.cs`) — e.g. RentGo lot cars, Patrol Station, statues:
  they get a `MapPlace` glyph so they appear on the map. Same names are the ids in `PlanSite`.
- `Zone13_ParkExtras`, `BusinessParkSigns` — decorative only, no behaviour.

## `CarSalesSystems` children — the job of each system (top-down)

| Component | Referenced by / reads | Notes |
|-----------|----------------------|-------|
| `VehicleRegistry` | ALL car placement code; `carDisplayPrefab` → `Gen/Prefabs/CarDisplay.prefab` | spawns physical cars (market, auction, private sellers, your lot) |
| `CarSalesBootstrap` | `database` → `Gen/Resources/CarSalesDatabase` (same asset everywhere) | first frame: load data, load save, go |
| `DayCycleManager` | `toggleShopAction` | owns Day/Hour, Sunday hours, holidays |
| `DayNightLighting` | `RenderSettings.sun` | rotates sun, darkens at night |
| `SaveManager`, `ProgressManager`, `MessageManager`, `BankManager`, `UpgradeManager`, `InventoryManager`, `MarketplaceManager`, `EconomyManager` (+ `SourcingManager`, `MarketManager`, `StaffManager`, `CrmManager`) | gameplay loop; save data via `SaveData` (3 slots + backups) | headless logic — no scene refs |
| `TowService` | Tow truck spawns | gated by GameSettings |
| `AuctionManager`, `RentalManager`, `BuildingManager`, `BusinessManager`, `WorldManager`, `SourcingManager`, `WorkshopManager`, `PropertyManager` | phase-specific gameplay (auctions, Phase14 rentals, building upgrades, business KPIs, weather/events, workshop) | Phase 14 cables: `RentalManager` ⇄ `RentGo` site; `BuildingManager` ⇄ the two construction sites' `ConstructionSite` components (stage roots [Ground/Site/Draft/Final]) |
| `AmbienceManager` | AudioLibrary / ambience | loops by location/activity |
| `AudioManager` | most UI and world sounds | ambient mixing |
| `ThumbnailService` | renders car thumbnails for the phone/market UI | |
| `TestDriveManager` | `moveAction`, `brakeAction`(jump), `exitAction`(interact), `vehicleAction`, `cameraAction`, `gearUpAction`, `gearDownAction` | driving |
| `DebugCheats` | dev keys | harmless, off in release? (still compiled; inputs only in debug builds) |
| `UIManager` (child `CarSalesUI`, Screen Space Overlay) | `closeAction`, `inventoryAction`, `mapAction`, `phoneAction`; `UIFactory` builds all screens at runtime | the entire HUD/menu system; `MapUI`, `PhoneUI`, `ComputerUI`, `DealershipUI` are windows under this canvas |

## Prefab linkage (created once by `2. Build Everything`, reused by every map)

| Prefab | Root components | Used for |
|--------|-----------------|----------|
| `Gen/Prefabs/CarDisplay.prefab` | `CarDisplay` (+`visualRoot`, `browsePoint`, `priceTag`(billboarded, inactive by default), BoxCollider, NavMeshObstacle (carving)) | every physical car in the world — initialised via `CarDisplay.Initialize*(instance)`; `Definition.displayPrefab` is instantiated under `visualRoot` and gets `CarVisual` (paint via MaterialPropertyBlock) + `CarDamageVisual` |
| `Gen/Prefabs/CarModels/Model_*.prefab` | `CarVisual` (+ primitive meshes, wheels/lights/brakes) | visual bodies for the procedural cars — swapped for GLB models in Asset Studio |
| `Gen/Prefabs/Customer.prefab` | `CustomerAI` (+`NavMeshAgent`, `bodyRenderer`, `bubble` Text3D), colliders shared | customers walking into the lot (spawned via `CustomerSpawner`/`PedestrianSystem` walk-ins) |
| `Gen/Prefabs/CarSalesSystems.prefab` | the whole Systems root as a prefab asset | **update flow:** edit prefab → existing scenes get it on next `Build Everything`; never hand-edit a scene copy long-term |

## Layers that matter (scripts hide/mask by layer)

`StudioLayer` and `MinimapLayer` are created by the builder; Main Cameras cull them out;
`InteractLayer` is what `PlayerInteractor` sees (set: `1 << InteractLayer`). NavMesh surfaces use
`PhysicsColliders` geometry so sign text and glow planes never become obstacles.

## What does NOT link to anything (safe to ignore)

- `HugeSky` volumes, reflection probes, marker quads, statues/monuments, road paint, signs — no code reads them.
- `TownMapMarker.townVersion` — read once (Awake) by `Town.Set`; after that it is decoration.
- The `Library/` folder and `.unity` scene YAML — you cannot safely rewrite those by hand; never.

## Mental model (one line)

`Data plan (Plan/JSON)` → `Builders (editor code)` → `Scene objects with components` →
`Systems (headless) + UI (canvas)` → `Save data`. If you respect that ladder, you can build a whole new world.
