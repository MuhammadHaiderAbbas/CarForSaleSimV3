# How to work on the Harbour City map

Harbour City is the only town in Car For Sale Simulator (V4). The old master map (V2) and the
Final Map (V3) were deleted — this is the one page that explains how the map itself is put together,
how to change it safely, and how to build it.

| What | Where |
| --- | --- |
| The plan (all geometry, data-only) | `Assets/CarSales/Sim/World/HarbourPlan.cs` — `Ring`, `Roads`, `Roundabouts`, `LightNodes`, `Zones`, `Sites`, `MapFrame` |
| Plan validity checker (run after every edit) | `python3 tools/plan_check.py` |
| The map drawing | `docs/images/harbour-city.png` — `python3 tools/render_plan_map.py` |
| Plan docs (POIs, assets) | `docs/HARBOUR_CITY.md` · `docs/HARBOUR_CITY_ASSETS.md` · `docs/images/harbour-city.png` |
| The one scene | Generated: `Gen/Scenes/HarbourCity.unity` — **Tools → Car Sales → 2. Build Everything** |
| The town facade everything reads | `CarSales.Sim.Town` (`Sim/World/Town.cs`) — delegates to `HarbourPlan` |
| The marker in the scene root | `TownMapMarker.townVersion = 4` |

## The rules that keep it consistent

1. **Junctions are shared vertices.** A road must meet another road at the exact same `P(x, z)`
   coordinate (or at a roundabout centre). The traffic graph, pavements, lamps and the world map all
   key off those shared vertices — a "near miss" is a disconnected town, and
   `plan_check.py` / `Phase12Tests` will fail.
2. **No dead ends.** Every lane must have a `next` in the traffic graph; terminal road ends only exist
   where a road meets the ring.
3. **Sites never touch the roads** (footprint check with margin), their **entrance is within 14 m** of
   a road, no two footprints overlap, and every site's position sits in its zone (pad 40 m). Horizon
   Drive meets the ring and Business Park Road serves the three business-park fronts.
   `plan_check.py` proves all of it without Unity.
4. **Zone numbering is contractual.** Zones 1–15, two numbered 14 (the expansion plots) and zone 15 =
   Southside Business Park — the tests assert this. Zone 12 is the ring road itself and its name must
   contain "Test-Drive"; zone 2 contains "Dealership".
5. **Site ids are contractual.** The 25 site ids match `PropertyManager` / scene references and
   `Phase12Tests` lists them — never rename an id, only positions. The two construction plots'
   display names ("Northgate Site (under construction)" / "Dockside Site (under construction)") are
   likewise asserted.
6. **The plan is data, not code.** No scene object is hand-placed; everything derives from
   `HarbourPlan` at build time. Change the plan → run `python3 tools/plan_check.py` → rebuild
   (`2. Build Everything`) → done.

## Editing the map

1. Edit `Assets/CarSales/Sim/World/HarbourPlan.cs` (move a site, re-route a road, add a zone).
2. `python3 tools/plan_check.py` — must print `ALL CHECKS PASSED`.
3. `python3 tools/render_plan_map.py` — refresh `docs/images/harbour-city.png`.
4. In Unity: **Tools → Car Sales → Harbour City → Build Harbour City** (or Build Everything) and
   play. The NavMesh rebakes automatically with the scene.
5. Run the EditMode tests (`python3 tools/run_unity_tests.py`) when Unity is available.

## World Builder extras

The World Builder window (**Tools → Car Sales → World Builder**) can add your own sites and roads on
top of the plan; they live in `Assets/CarSales/Data/WorldLayout.asset` and are rebuilt after the plan
world. Not part of the plan — the checks above don't apply to them (the window has its own Validate
button).

## If you ever want a second, separate world

Harbour City is intentionally self-contained, but the engine allows another plan: add a
`XyzPlan.cs` next to `HarbourPlan.cs` (same `PlanRoad`/`PlanZone`/`PlanSite` types, pick the next free
version number), teach `Town.cs` to delegate to it, add a scene path + builder menu beside Harbour
City's, mirror `Phase14Tests` against your new plan, and point `MainMenuUI.gameScene` at the new
scene. Keep the plan data-only and lean on `plan_check.py` as your first line of verification.
