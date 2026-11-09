# Making the game look like the concept renders (Assets/CarSales/Art/Concepts/)

The engine already contains most of the cinematic machinery — this document is the checklist
that closes the gap between "good URP town" and "the mood of the images". Work top to bottom;
nothing here requires new code.

Total: 3.5 GB of curated GLBs are the raw ingredients. **High-poly only where the eye lands
(hero spots); the world stays mid-poly or it will not run at 60 fps.** Do NOT try to make
everything "highpoly" — the frame budget is the enemy, not the artist's ambition.

## 1) Sky and light (the single biggest jump — do this first)

The images live and die on sky. Concrete steps:

1. **Tools → Car Sales → Asset Studio → Online → Poly Haven**: fetch a sunset/dusk HDRI
   (e.g. *kloofendal_48d_partly_cloudy_puresky* or *limpopo_golf_course_4k*, CC0). Approve it as
   **Sky**. The next build applies it automatically (`ApplyImportedSky` in CarSalesLooks.cs).
2. Keep **Ultra** graphics for the money shots, but tune on **High** (Low–Ultra switch exists):
   shadows on, ambient occlusion on, real lamps/headlights on.
3. The ACES + bloom + filmic grading stack is already created by the builder
   (`Gen/Settings/PostProcessing.asset`): ACES tonemap, bloom 0.35, contrast +8, saturation +6,
   cool shadows / warm highlights, film grain 0.1 — i.e. the dusk/gold-teal balance in the images.

## 2) Surfaces (the difference between plastic and real at 1 m)

1. Poly Haven → **Textures**: grab scanned asphalt (aerial/chip), brick, concrete pavement,
   grass, gravel — 1–2k versions are fine.
2. **Tools → Car Sales → Realism → Surfaces**: drop each in, Apply. This replaces the procedural
   tiles everywhere they are used (roads, pavements, walls).
3. Wet-road look for night rain shots: enable rain in-game (weather system) — wet shader response
   is already wired; the night profile (cool, desaturated, bloom 1.1) does the rest.

## 3) Hero assets (high-poly, used sparingly)

Use the audit (**Tools → Car Sales → Final Map → Audit Final Map**) to place the heavyweights
selectively. The manifest's biggest faces:

| Faces | Size | Asset | Where it goes |
|-------|------|-------|---------------|
| 1.49M | 68 MB | Furtivo Supercar | Garage/trophy car only — keep OFF the lots |
| 1.48M | 57 MB | Classic Suburban House | Home_House swap (one instance) |
| 1.43M | 36 MB | White Victorian Armchair | Interior dressing — one spot |
| 1.42M | 54 MB | Maserati-ghibli-hybrid | Gallery display / hero stock |
| 1.41M | 46 MB | Pergula scaffolding | Northgate/Dockside construction (already planned) |
| 1.41M | 52 MB | Excavator | Construction sites (already planned) |
| ~1M | ~80 MB | Headquarters / Building under construction / General Ship Repair | Northgate finished / draft / Dockside finished (already placed) |

**Rule:** high-poly cars = hero/trophy or single hero placements. If a 1.4M-face car appears
twelve times on the Used Car Market, the game drops to single digits of FPS. For lots and traffic,
the procedural mid-poly cars win.

## 4) Interiors and furniture (the "showroom" from the images)

`furniture_office` (HQ suites), `furniture_lounge` (villa/showroom), `furniture_equipment`
(service lifts), `furniture_computer` (desks) — all in the manifest and placed by the Audit-driven
build; missing ones → primitive stand-ins. For inspection close-ups, approve 2–3 furniture pieces
in Asset Studio > Interiors.

## 5) What the images imply that you cannot fake with assets

- Wet reflections + warm key light + cool fill at dusk — that is **Ultra quality + the rain toggle + HDRI**, not a filter.
- Depth/haze on the horizon — fog/colour grading; keep Ultra, don't stack extra fog in Volume profiles.
- Flag poles, banners, tyre stacks, string lights — these exist in the Final Map builder
  (Tyre shop stacks, RentGo string lights, patrol light bar); the GLB props (KPI street props) can add more
  via **Tools → Car Sales → World Builder** on a per-site basis.

## 6) Importing the heavy GLBs without killing Unity (repeat of docs/FINAL_MAP.md)

1. Close other apps first. 2. Import one at a time. 3. If the import worker crashes on a GLB,
   open it in free Blender → scale textures to 2048 px → re-export → replace the file → reimport.
4. If it still crashes, mark that asset `"skip": true` in `final-map-overrides.json` — the Final Map
   uses a stand-in instead (the build never hard-fails).

## 7) Screenshot procedure (match the images for store/marketing)

16:9, Ultra, dusk or night, camera at ~1.5 m eye height (game height), no UI (F11 hides it in builds;
in editor use the Game view chrome), no text plates (numbers/signs readable = trademark risk).
The concept images in `Assets/CarSales/Art/Concepts/` are AI-generated mood/targets — they are
**not** literal renders of the scene; treat them as lighting/palette/mood references.
