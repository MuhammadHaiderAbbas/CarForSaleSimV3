# People in the world — what is there and how to add many more

## What ships out of the box

The pedestrian system now generates **stylised citizens on the pavements**, mixed every spawn:

| Archetype | Share | Height | Speed | Details |
|-----------|-------|--------|-------|---------|
| Man | 30% | 1.70–1.90 m | 1.1–1.6 m/s | menswear palette, 30% cap |
| Woman | 25% | 1.58–1.78 m | 1.1–1.6 m/s | womenswear palette, 30% hat |
| Senior | 13% | 1.50–1.72 m | 0.65–1.05 m/s | beige/grey palette, cane 45%, hat 40%, grey/white hair |
| Young | 14% | 1.60–1.85 m | 1.35–2.1 m/s | 25% jog at 2.6–3.6 m/s |
| Child | 10% | 1.00–1.35 m | 1.4–2.3 m/s | bright palette |
| Tall | 8% | 1.90–2.10 m | 1.15–1.55 m/s | long-line coat palette |

Body proportions scale with build width (slim 0.7×, average 1.0×, broad 1.25×), skin tone is picked from
six tones, hair from six colours (seniors get grey/white). With zero imported models every passer-by is a
unique combination — the world no longer shows one repeated "capsule man".

Importing real human models replaces the stylised bodies **automatically** (pick any mix):
`CharacterSetup` models are used with a random height scale (≈0.56–1.17 × the Blender/Mixamo base ~1.8 m).

## Getting a real human cast (free, ~20 minutes)

1. Go to [mixamo.com](https://www.mixamo.com) (Adobe account, free).
2. Download **8 characters** (FBX, with skin — e.g. "Michelle", "Sophie", "Max", "Wally", "Erika", "Bully",
   a teen rig, a senior rig — the site's characters are pre-rigged humans; scale tames height).
   Any eight distinct rigs work; don't worry about gender labels.
3. Still on mixamo.com, for **each** character download one idle clip and one walk clip (with *In Place* ticked).
4. In Unity: **Tools → Car Sales → Character Setup** → build the cast, tagging all 8 as
   **Customer/Pedestrian/Staff/Seller/Auction** (default roles). The game picks randomly per pedestrian.
   Tip: tag any workwear-styled model as *Staff* too, so shop staff look right.

   Children: Mixamo has no child rigs — the Child archetype (≈1.0–1.35 m) stays the stylised body;
   that is fine and often looks better at game distance.

## Rules of thumb

- 8+ distinct rigs → the crowd stops repeating for any realistic view distance.
- Keep faces simple; LOD helps perf (set up in Asset Studio when importing).
- Pedestrian count scales with `Settings → Traffic → Pedestrian density` and auto-thins via the
  Performance Governor on lower-end machines.

## Checking it works

Walk the town centre (`Zone 10`) at 09:00 and 17:30; with 8 rigs imported you should not see the same
character twice within a couple of minutes. The two lit junctions are the busiest spots.
