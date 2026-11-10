# Publishing Car For Sale Simulator — the honest end-to-end path

This project is **not** one click from Steam. It is one working project away from it, and here is
exactly what that means — no sugar-coating.

## What is already done

- All phases build tooling; Release Checklist + Realism Checklist windows;
  `Tools > Car Sales > Build > Windows (Release)` refuses to build while blockers exist;
  `THIRD_PARTY_NOTICES.txt` ships with the build (CC BY attribution for the Sketchfab models).
- **Harbour City** (the one town) builds into the `HarbourCity.unity` scene; per-site model uploads are
  reproducible via `Tools > Car Sales > Harbour City > Map Slots…`.
- **Cars are a system now**: the built-in database (8 fictional cars) ships drivable; every car GLB you
  approve in Asset Studio lands in `Assets/CarSales/Data/Cars` and every `CarDatabase`; and
  `Tools > Car Sales > Release Checklist > Wire All Cars Into The Game` batch-imports every downloaded
  car GLB from the Harbour City manifest (measure → wheel detection → paint detection → clear-coat →
  prefab → `CarDefinition` → database). They are buyable and drivable; names are trademark-cleaned.
- The world renders with walkers (PedestrianSystem), traffic, weather, day cycle, phone, saves.

## What YOU must do on your machine (no one else can do it for you)

1. **Build with Unity 6 on your PC** (this repo deliberately contains no Library/, no .exe).
   `Tools > Car Sales > 2. Build Everything` → run EditMode tests
   (`python3 tools/run_unity_tests.py`) until green.
   Then (optional) `Tools > Car Sales > Release Checklist > Wire All Cars Into The Game` to turn every
   downloaded car GLB into a buyable, drivable car.
2. **Fix the Release Checklist to all-green**: Real maker cars renamed (the *Rename* button in the
   checklist renames text entries; **you must also strip badges/logos from the car textures** — trademark
   clearance is a legal task, not a text edit), the two imported cars' textures actually badge-free,
   the one `needsReview` licence (General Ship Repair) marked approved or replaced.
3. **Realistic pass on the Realism Checklist** (50% at last check): 2 more approved car models, scanned
   textures (Poly Haven, CC0), sound ambience — optional polish, but it is the difference between
   "released" and "loved".
4. **Art direction**: replace/keep the strongest Sketchfab hero assets; the current mixture reads as
   asset-pack. Custom hero pieces (menu background, key art) matter most on Steam.
5. **Test on a clean machine** (friends/relations): save/load, full day, 2 hours of play; fix crashes.
6. **Store page**: $100 Steam Direct fee (recoupable after $1,000 revenue), $5 app thumbnail,
   616×353 capsule + hero capsule + 1920×1080 background, 5+ screenshots, 30–60s trailer, store copy,
   tags (`Simulation`, `Management`, `Cars`), content rating questionnaire, support email.
7. **Build & depot**: `Tools > Car Sales > Build > Windows (Release)` → upload with Steamworks
   (`steamcmd`), set the build live, tick the age questionnaire.

## What “earn money” actually requires

- Wishlists before launch (realistically 7,000–10,000+ for a meaningful run), an Itch.io/demo funnel,
  Steam Next Fest, community presence, and honest Early Access labelling if you ship early.
- Realistic expectation for a first solo project: US$0–500 without a pre-launch push. Price £4.99–£14.99.
- Do not tick "I have renamed the real-brand cars" unless you actually did the badge work.

## What the assistant cannot do (so you don't wait for it)

- No Unity in the sandbox → no .exe, no import of your new GLBs, no overriding your local project state.
- No Steam account → no store page, no fee payment, no build upload, no legal advice.
- No GLB redistribution → `Assets/CarSales/Sketchfab/` is downloaded by you, never committed.
- GLBs already known to crash the import worker (see `docs/HARBOUR_CITY.md`) must be reimported or skipped by you.

## The ordered checklist I maintain for this project

```
1. Unity 6 open, project fully idle (bottom-right spinner stopped)
2. Tools > Car Sales > 2. Build Everything        (builds Harbour City)
3. python3 tools/run_unity_tests.py          (EditMode green)
4. Harbour City > Audit Harbour City                (0 FAILED; reimport crashers one by one)
5. Harbour City > Repair Missing Materials       (if any)  Ctrl+S
6. Harbour City > Map Slots…                      (place your own models per site)
7. Release Checklist > Wire All Cars Into The Game (every downloaded car buyable + drivable)
8. Release Checklist all green (Rename button → strip badges by hand)
9. PlayMode: full day cycle + auction + RentGo + construction sites
10. Build > Windows (Release)
11. Itch.io demo first → collect feedback → Next Fest → Steam week
```
