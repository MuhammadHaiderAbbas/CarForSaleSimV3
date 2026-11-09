# Realism Guide — making CarForSaleSimV2 look real

This is everything you need to do to take the game from "generated town" to "looks real", in the order that makes the
biggest difference first. Inside Unity, **Tools → Car Sales → Realism → Realism Checklist** shows the same steps with
live progress and a button for each one.

---

## 0. The honest picture first

GTA V took hundreds of artists several years and a budget in the hundreds of millions. What makes any game look
real comes from four things:

| What | Who makes it | Status |
|---|---|---|
| **Lighting**: sun, shadows, night lights, reflections, fog, colour grading | Code and settings | **Done in Phase 13** (automatic) |
| **Surfaces**: real textures on roads, walls and floors | Generated textures, or scanned photos | **Done** (generated). Better with scanned (Step 4) |
| **3D models**: cars, buildings, trees, people, furniture | 3D artists. You download or buy them | **Pipelines done.** The models are up to you (Steps 3, 5, 6) |
| **Life**: animation, sound, people moving | Mixamo, recordings, code | Code done; you add the files (Steps 5, 8) |

Code can't draw a realistic BMW or a human face: those come from real 3D models. Phase 13 makes the game
**ready** for them. Everything that can be done in code is done, and each model you add drops straight in. A
£10 Steam game that looks really good is reachable with **free** assets and these tools. It won't be GTA V, and
nobody expects that at £10.

---

## 1. What you get as soon as you update

Pull the update, open Unity, wait for it to compile, then run **Tools → Car Sales → 2. Build Everything**.

- **Real textures everywhere**:
  - Every surface has a texture: asphalt with stones in it, paving slabs with grout, red brick, painted render,
    grass, polished porcelain and marble floors, wood grain, roof tiles, metal cladding and workshop resin floors.
  - Backdrop buildings get **real building fronts** (windows, frames, curtains) and rooftop clutter (air-con units,
    lift housings, water tanks).
  - Textures show at their real size: a brick is a brick on any wall.
- **Graphics levels Low / Medium / High / Ultra**:
  - Soft, long shadows, **ambient occlusion** (dark contact shadows in corners), HDR colour grading and texture
    streaming.
  - **Forward+** lighting, so every street lamp and headlight can really light the scene.
  - Players pick the level in Settings → Graphics.
- **Day and night**:
  - Office and flat windows light up after dusk (different rooms in each building).
  - Street lamps really light the street near you.
  - Your car has working headlights.
  - Lamp heads and signs glow at night and not by day.
  - Night colour grading: cooler and darker, with lights blooming.
- **Weather**:
  - Roads, pavements and roofs get **darker and shiny when wet** and dry off slowly after the rain.
  - Rain and grey skies look flatter and greyer.
  - The air has haze: misty at dawn, golden at sunset, deep blue at night.
- **Car paint**: real clear-coat lacquer on every car, including cars you import.
- **Interiors**:
  - **Showrooms** have a reception counter, a customer lounge (sofa, armchairs, coffee table, rug, lamp, wall art),
    sales desks with computers and chairs, a coffee point with a water cooler, and a brochure stand.
  - The **workshops** have tool chests, workbenches with tool boards, tyre racks, oil drums and strip lights.
  - The **parts shop** has racking full of stock.
- **Walk-in homes**:
  - Press **E** at your front door to go **inside**: a furnished home (studio, family home or villa) with a
    kitchen, lounge, dining area and bedroom.
  - Sleep in your **bed** (E) and wake up beside it. Press E on the inside of the front door to go out.
- **Every piece of furniture can be swapped** for a real model (Step 7).

Check after the first build:

1. **Edit → Project Settings → Quality** lists Low, Medium, High, Ultra.
2. Press Play. Walk around in the day, then press **F2** (debug: one hour later) until it's dark, and look at the
   windows, street lamps and your car's headlights. Press **F4** (debug: next weather) to see rain and fog.
3. Walk into AUTO WORLD and into your trailer.

If anything looks broken (pink or black surfaces), use **Tools → Car Sales → Realism → Restore Original Graphics**
and send me a screenshot plus the Console errors.

---

## 2. Step by step (do these in order)

### Step 1 — Graphics levels (automatic)

Build Everything does this the first time. To redo it: **Tools → Car Sales → Realism → Apply Realistic Graphics**.
It copies your URP settings into `Assets/CarSales/Data/Rendering` (your originals are not changed) and points the
quality levels at the copies. **Restore Original Graphics** undoes it.

### Step 2 — A real sky (10 minutes, free)

A photographed sky lights the whole town and reflects in every car.

1. **Tools → Car Sales → Asset Studio → Online → Poly Haven → Skies.**
2. Search "partly cloudy" or "puresky" (pure-sky versions have no ground in them, which suits a town).
3. Download at **4K**. It is CC0: free for anything, no credit needed.
4. Run **Build Everything** again; the newest sky is used.

### Step 3 — Real cars (the biggest win: 30–60 minutes per car)

1. Get a free Sketchfab account. Find your API token under Settings → Password & API.
2. **Asset Studio → Online → Sketchfab**: paste your links (the six cars) and press **Look up**. It shows each
   model's licence and whether it can be downloaded.
3. **Download** each one (glTF), then set it up in the Asset Studio:
   - check the size;
   - choose the **paint materials** (the body panels);
   - choose the **four wheels**;
   - **Approve** as **Vehicle**.

   The paint gets the clear-coat lacquer automatically.
4. **Real brands (Mercedes, BMW, Toyota) — important before you sell:**
   - Car makers own their names, logos and often the car designs. Games pay for licences to use them.
   - Without a licence, Steam can take the game down.
   - So give each car a **fictional name** in the Asset Studio (the game already uses fictional brands: Kestrel,
     Orion, Titan, Vortex).
   - Remove the **badges and logos** from the model (in Blender: select the badge mesh and delete it). The Release
     Checklist warns about real brand names.
5. **Finding more cars on Sketchfab:**
   - Filter by **Downloadable** and licence **CC Attribution** or **CC0**.
   - Avoid **NonCommercial**, **NoDerivs** and **Editorial** licences: they are not allowed in a paid game.
   - The Asset Studio records each licence and puts the credit in the game's Credits screen and
     `THIRD_PARTY_NOTICES.txt`.
6. **Size:** there is no triangle limit, but aim for under about 150,000 triangles per car so a lot full of cars
   stays smooth. Very heavy models (1M+) work but cost frame rate.

### Step 4 — Scanned textures (about 1 hour, free)

The generated textures look good from a distance. Photo-scanned ones look better close up.

1. **Asset Studio → Online → Poly Haven → Textures** (all CC0). Good searches:

   | Surface | Search for |
   |---|---|
   | Roads | asphalt |
   | Pavements | paving, pavement |
   | Concrete | concrete |
   | Brick | red brick |
   | Painted walls | plaster |
   | Grass | grass |
   | Showroom floor | marble, tiles |
   | Wood | wood floor, planks |

   Download at **2K** (4K only for the asphalt and floors).
2. **Tools → Car Sales → Realism → Surface Library.**
   - Drag each downloaded material onto its surface.
   - Set **Metres per copy** to the real size from the texture's Poly Haven page (shown as its dimensions, often
     2–4 m).
   - Press **Apply to the town now**.
3. For textures from **ambientCG** (also CC0) or elsewhere:
   - make a URP Lit material;
   - put the colour map in Base Map, the normal map in Normal Map, and the ambient-occlusion map in Occlusion;
   - drop that material into the Surface Library.

### Step 5 — People (1–2 hours, free)

1. Go to **mixamo.com** and sign in with a free Adobe account. Mixamo characters and animations are royalty-free
   for commercial games.
2. Open **Tools → Car Sales → Character Setup**. It lists exactly which animations to download (idle, walk, talk,
   wave, bidding and so on).
3. Pick a realistic character and download it as FBX for Unity, **with skin**.
4. Download each listed animation as FBX, **without skin**. The window finds them automatically.
5. The game builds an AI cast from your character: different builds, skin tones, hair and clothes. **Add 2–4 more
   Mixamo characters** for more varied faces.

Want better faces?

- **Reallusion Character Creator** (paid) makes much more realistic people that work with Mixamo animations.
- **MetaHuman** (Epic) has been allowed in other engines since mid-2025. Read the current licence first. Getting
  MetaHumans into Unity takes extra work, and they are very heavy for a town full of people.

### Step 6 — Town models: buildings, trees, lamps, benches (a few evenings)

1. **Where to get them:**
   - **Sketchfab** (Downloadable + CC licence; check each one) for buildings, houses, shops and street furniture.
   - **Poly Haven Models** (CC0) for props, plants and furniture.
   - **Unity Asset Store**: search "free" plus "city", "trees" or "street props".
   - Avoid cartoon or low-poly packs (Kenney, Quaternius and similar): they would clash with the realistic look.
2. **Importing:**
   - **Asset Studio → Import** (drag the file in).
   - Choose **Building** for buildings, **Prop** for trees, lamps and benches.
   - Check the size and Approve.
3. **Using them:** **Asset Studio → Town**. Tick models under Buildings, Trees, Street lamps and Benches (tick
   several for variety), then **Apply to the town scene**. Build Everything re-applies your choices every time.
4. **Trees matter a lot.**
   - The generated trees are the most obviously fake thing left.
   - Two or three good tree models (one big street tree, one small ornamental tree, one bush) change the whole town.
   - Keep each tree under about 30,000 triangles.

### Step 7 — Furniture models (optional)

The built-in furniture is detailed, but scanned furniture finishes the interiors.

1. Import furniture as **Interior** in the Asset Studio.
2. Go to **Asset Studio → Town** and tick the models under Sofa, Chair, Table, Plant, Storage, Bed, Appliance,
   Decor or Workshop kit.
3. Press **Apply**. Each model is fitted to the size of the piece it replaces.

### Step 8 — Sound (1–2 hours, free)

Real recordings sell the scene as much as graphics do.

1. Go to **freesound.org** and **filter by licence: Creative Commons 0** (CC0).
2. Search for these:

   | Ambience | Search for |
   |---|---|
   | Street | city traffic ambience |
   | Showroom | office ambience, shop ambience |
   | Workshop | garage ambience, workshop tools |
   | Auction hall | crowd murmur, auction |
   | Rain | rain loop |

3. Put them in **Assets/CarSales/Data/AudioLibrary.asset** (one slot each).
4. **Music:** use tracks you have a commercial licence for (bought, or CC0). Never use songs off YouTube.

### Step 9 — Polish in Unity (optional, about 1 hour)

- **Occlusion culling** (smoother frame rate in town):
  1. Open the game scene.
  2. Open **Window → Rendering → Occlusion Culling → Bake**. Everything the builder makes is already marked static.
  3. Re-bake after each Build Everything.
- **Sun direction:** if your HDRI sky's sun sits somewhere else, rotate the sky material (Rotation) so the sky's
  sun matches the shadows.
- **Tuning the look:** select **RealismDirector** in the game scene. Settings:
  - window brightness at night;
  - how many street-lamp lights follow you;
  - headlight strength;
  - lamp colour.

  To change colours or brightness for good, tell me what looks off (a screenshot helps) and I'll change the
  built-in values, because Build Everything regenerates the scene and the post-processing profiles.

### Step 10 — Performance check (about 1 hour)

1. Press Play on each graphics level. In Settings, turn on the **frame-rate overlay**.
2. Aim for **60 fps on High at 1080p** on a mid-range PC, and a playable 30+ on Low on an old laptop.
3. If it's slow:
   - lower **Lamp Lights** on the RealismDirector;
   - use lighter car models;
   - bake occlusion culling;
   - keep imported textures at 2K.

   The game also thins traffic and pedestrians on its own when the frame rate drops.
4. On your Steam page, write minimum and recommended specs based on what you tested.

### Step 11 — Screenshots and trailer for Steam

- Shoot at **golden hour** (around 19:30) and at **night in the rain**: wet roads, lit windows and headlights look
  best.
- Use Ultra, hide the HUD, and take several angles of the showroom and the town.
- For a trailer, install the free **Unity Recorder** package (Window → Package Manager).

### Step 12 — Release checks

**Tools → Car Sales → Release Checklist** must pass: licences, brands, credits, name and version. Then run
**Tools → Car Sales → Build → Windows (Release)**.

---

## 3. Shopping list (all of it can be free)

| Need | Free source | Licence | Notes |
|---|---|---|---|
| Sky | Poly Haven HDRIs | CC0 | Downloads from inside the Asset Studio |
| Textures | Poly Haven, ambientCG | CC0 | Surface Library |
| Cars | Sketchfab | CC-BY (credit) or CC0 | Rename and de-badge real brands |
| People | Mixamo | Free for commercial games | Character Setup |
| Buildings, trees, props | Sketchfab, Poly Haven Models, Asset Store free packs | Check each | Asset Studio → Town |
| Furniture | Poly Haven Models, Sketchfab | CC0 / CC-BY | Import as Interior |
| Sound | freesound.org (CC0 filter) | CC0 | Audio Library |
| Music | Bought or CC0 tracks | Commercial use | Never YouTube rips |

Your £100 covers the **Steam Direct fee** (US$100 per game). Everything above is free, so you don't need to buy
assets to launch.

## 4. Licence rules for a paid game

- **CC0**: use it for anything; no credit needed.
- **CC-BY** (Attribution): allowed. You must credit the author; the Asset Studio does this for you.
- **CC-BY-NC** (NonCommercial): **not allowed** in a game you sell.
- **CC-BY-ND** (NoDerivs): avoid. Rescaling or re-materialling a model can count as changing it.
- **Sketchfab "Editorial"**: not for commercial use.
- **Real brands and car designs**: need a licence from the car maker. Use fictional names and remove logos.

## 5. Troubleshooting

| Problem | Fix |
|---|---|
| Pink surfaces | URP isn't the active pipeline. Run Apply Realistic Graphics, or Restore and tell me the errors |
| Everything looks too dark or too bright | Check Project Settings → Player → Color Space is **Linear** (Apply sets it) |
| Blurry textures | Choose a higher graphics level; Low halves texture resolution on purpose |
| Low frame rate | Medium level, fewer lamp lights, lighter models, bake occlusion culling |
| Can't get out of the house | Look at the inside of the front door (by the door mat) and press E |
| Imported car paint not glossy | Its paint material must be **extracted**: Asset Studio does this when you approve. Re-approve the car |
| Surface Library change not showing | Press **Apply to the town now**, then Play |

## 6. What I could not do from here

- **Download models**: this environment can't reach Sketchfab or Mixamo, and the licences need your account.
- **See the result**: I can't run Unity. All values are a careful first pass checked by 375 automated tests, but
  colours and brightness are a matter of taste. Send screenshots and I'll tune them.
