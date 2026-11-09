using CarSales.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 13c: furnished interiors. A kit of detailed furniture built from many parts (sofas with cushions and arms,
    /// desks with monitors, office chairs with five-star bases, beds with duvets, kitchens with hobs and wall units...).
    /// Every piece sits in a PlaceholderSlot, so Asset Studio > Town can swap it for a real model (Interior assets).
    /// Showrooms, the workshops and the parts shop are refurnished, and every home gets a walk-in interior.
    /// Kit convention: a piece stands on its local origin and its front (the side you use it from) faces +Z.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        // ------------------------------------------------------------------ KIT BASICS
        static Transform Piece(Transform g, string name, PlaceholderKind kind, Vector3 pos, float yaw, Vector3 footprint)
        {
            var t = Slot(g, name, pos, kind, footprint, 0f);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        /// <summary>One part of a piece: a primitive that casts shadows (and has a collider when <paramref name="solid"/>).</summary>
        static GameObject Part(Transform t, string name, Vector3 centre, Vector3 size, Material mat, bool solid = false,
                               PrimitiveType type = PrimitiveType.Cube, Vector3? euler = null, bool shadows = true)
        {
            var go = Prim(type, name, t, centre, size, mat, solid);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.isStatic = true;
            go.GetComponent<Renderer>().shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static void SmallLight(Transform t, Vector3 pos, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(t, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(1f, 0.84f, 0.62f);
            l.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ SEATING
        static Transform Sofa(Transform g, Vector3 pos, float yaw, float width, int seats, Material fabric, Material accent, Mats m)
        {
            const float d = 0.95f, arm = 0.18f;
            var t = Piece(g, seats == 1 ? "Armchair" : "Sofa", PlaceholderKind.Sofa, pos, yaw, new Vector3(width, 0.88f, d));
            Part(t, "Base", new Vector3(0f, 0.27f, 0f), new Vector3(width, 0.3f, d), fabric, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Leg", new Vector3(sx * (width / 2f - 0.1f), 0.06f, sz * (d / 2f - 0.1f)), new Vector3(0.06f, 0.12f, 0.06f), m.frame);
            float inner = width - 2f * arm, cw = inner / seats;
            for (int i = 0; i < seats; i++)
            {
                float x = -inner / 2f + (i + 0.5f) * cw;
                Part(t, "SeatCushion", new Vector3(x, 0.5f, 0.07f), new Vector3(cw - 0.02f, 0.16f, d - 0.3f), fabric);
                Part(t, "BackCushion", new Vector3(x, 0.74f, -d / 2f + 0.3f), new Vector3(cw - 0.03f, 0.42f, 0.16f), fabric, euler: new Vector3(-10f, 0f, 0f));
            }
            Part(t, "Back", new Vector3(0f, 0.62f, -d / 2f + 0.12f), new Vector3(width, 0.6f, 0.24f), fabric, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                Part(t, "Arm", new Vector3(sx * (width / 2f - arm / 2f), 0.39f, 0f), new Vector3(arm, 0.54f, d), fabric, solid: true);
            if (seats > 1 && accent)
                foreach (var sx in new[] { -1f, 1f })
                    Part(t, "Pillow", new Vector3(sx * (inner / 2f - 0.26f), 0.74f, -d / 2f + 0.44f), new Vector3(0.42f, 0.38f, 0.12f), accent,
                         euler: new Vector3(-14f, sx * 18f, 0f));
            return t;
        }

        static Transform OfficeChair(Transform g, Vector3 pos, float yaw, Material seat, Mats m)
        {
            var t = Piece(g, "OfficeChair", PlaceholderKind.Chair, pos, yaw, new Vector3(0.62f, 1.15f, 0.62f));
            Part(t, "Seat", new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.09f, 0.48f), seat, solid: true);
            Part(t, "Back", new Vector3(0f, 0.9f, -0.25f), new Vector3(0.46f, 0.58f, 0.07f), seat, euler: new Vector3(-8f, 0f, 0f));
            Part(t, "Column", new Vector3(0f, 0.3f, 0f), new Vector3(0.06f, 0.15f, 0.06f), m.chrome, type: PrimitiveType.Cylinder);
            Part(t, "BackSpine", new Vector3(0f, 0.66f, -0.27f), new Vector3(0.05f, 0.3f, 0.03f), m.blackGlass);
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f, r = a * Mathf.Deg2Rad;
                Part(t, "BaseArm", new Vector3(Mathf.Sin(r) * 0.15f, 0.08f, Mathf.Cos(r) * 0.15f), new Vector3(0.05f, 0.035f, 0.3f), m.blackGlass, euler: new Vector3(0f, a, 0f));
                Part(t, "Castor", new Vector3(Mathf.Sin(r) * 0.3f, 0.03f, Mathf.Cos(r) * 0.3f), new Vector3(0.06f, 0.06f, 0.06f), m.blackGlass, type: PrimitiveType.Sphere, shadows: false);
            }
            foreach (var sx in new[] { -1f, 1f })
            {
                Part(t, "ArmPost", new Vector3(sx * 0.28f, 0.6f, -0.02f), new Vector3(0.04f, 0.2f, 0.04f), m.blackGlass);
                Part(t, "ArmPad", new Vector3(sx * 0.28f, 0.71f, 0f), new Vector3(0.07f, 0.03f, 0.26f), m.blackGlass);
            }
            return t;
        }

        static Transform DiningChair(Transform g, Vector3 pos, float yaw, Material wood, Material seat)
        {
            var t = Piece(g, "Chair", PlaceholderKind.Chair, pos, yaw, new Vector3(0.46f, 0.95f, 0.46f));
            Part(t, "Seat", new Vector3(0f, 0.46f, 0f), new Vector3(0.44f, 0.05f, 0.44f), seat ? seat : wood, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Leg", new Vector3(sx * 0.19f, 0.22f, sz * 0.19f), new Vector3(0.04f, 0.44f, 0.04f), wood);
            foreach (var sx in new[] { -1f, 1f })
                Part(t, "BackPost", new Vector3(sx * 0.19f, 0.72f, -0.2f), new Vector3(0.04f, 0.5f, 0.04f), wood);
            Part(t, "BackRest", new Vector3(0f, 0.84f, -0.2f), new Vector3(0.42f, 0.2f, 0.03f), wood);
            return t;
        }

        // ------------------------------------------------------------------ TABLES & DESKS
        static Transform CoffeeTable(Transform g, Vector3 pos, float yaw, float w, float d, Mats m)
        {
            var t = Piece(g, "CoffeeTable", PlaceholderKind.Table, pos, yaw, new Vector3(w, 0.42f, d));
            Part(t, "Top", new Vector3(0f, 0.4f, 0f), new Vector3(w, 0.04f, d), m.desk, solid: true);
            Part(t, "Shelf", new Vector3(0f, 0.12f, 0f), new Vector3(w - 0.1f, 0.025f, d - 0.1f), m.desk);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Leg", new Vector3(sx * (w / 2f - 0.05f), 0.2f, sz * (d / 2f - 0.05f)), new Vector3(0.04f, 0.38f, 0.04f), m.blackGlass);
            Part(t, "Books", new Vector3(-w * 0.22f, 0.45f, 0.02f), new Vector3(0.28f, 0.06f, 0.2f), m.art[1]);
            Part(t, "Book", new Vector3(-w * 0.22f, 0.49f, 0.02f), new Vector3(0.24f, 0.025f, 0.17f), m.art[2], euler: new Vector3(0f, 12f, 0f));
            Part(t, "Bowl", new Vector3(w * 0.2f, 0.45f, 0f), new Vector3(0.24f, 0.03f, 0.24f), m.pot, type: PrimitiveType.Cylinder);
            return t;
        }

        static Transform DiningTable(Transform g, Vector3 pos, float yaw, float w, float d, Mats m)
        {
            var t = Piece(g, "DiningTable", PlaceholderKind.Table, pos, yaw, new Vector3(w, 0.76f, d));
            Part(t, "Top", new Vector3(0f, 0.75f, 0f), new Vector3(w, 0.05f, d), m.desk, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Leg", new Vector3(sx * (w / 2f - 0.08f), 0.36f, sz * (d / 2f - 0.08f)), new Vector3(0.07f, 0.72f, 0.07f), m.desk);
            Part(t, "Runner", new Vector3(0f, 0.778f, 0f), new Vector3(w * 0.6f, 0.005f, 0.3f), m.linen, shadows: false);
            Part(t, "Vase", new Vector3(0f, 0.86f, 0f), new Vector3(0.12f, 0.08f, 0.12f), m.pot, type: PrimitiveType.Cylinder);
            Part(t, "Flowers", new Vector3(0f, 1.0f, 0f), new Vector3(0.22f, 0.18f, 0.22f), m.leaves, type: PrimitiveType.Sphere);
            return t;
        }

        /// <summary>A work desk: top, side panels, drawers, monitor on a stand, keyboard, mouse, lamp and papers.</summary>
        static Transform Desk(Transform g, Vector3 pos, float yaw, float w, Mats m, bool computer = true)
        {
            const float d = 0.78f;
            var t = Piece(g, "Desk", PlaceholderKind.Table, pos, yaw, new Vector3(w, 0.76f, d));
            Part(t, "Top", new Vector3(0f, 0.74f, 0f), new Vector3(w, 0.04f, d), m.desk, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                Part(t, "Side", new Vector3(sx * (w / 2f - 0.03f), 0.36f, 0f), new Vector3(0.04f, 0.72f, d - 0.04f), m.frame);
            Part(t, "Modesty", new Vector3(0f, 0.52f, -d / 2f + 0.05f), new Vector3(w - 0.1f, 0.36f, 0.02f), m.frame);
            Part(t, "Drawers", new Vector3(w / 2f - 0.3f, 0.36f, 0.02f), new Vector3(0.42f, 0.62f, d - 0.12f), m.desk, solid: true);
            for (int i = 0; i < 3; i++)
            {
                Part(t, "DrawerLine", new Vector3(w / 2f - 0.3f, 0.2f + i * 0.2f, d / 2f - 0.039f), new Vector3(0.4f, 0.006f, 0.004f), m.blackGlass, shadows: false);
                Part(t, "Handle", new Vector3(w / 2f - 0.3f, 0.27f + i * 0.2f, d / 2f - 0.03f), new Vector3(0.12f, 0.015f, 0.015f), m.chrome, shadows: false);
            }
            if (!computer) return t;
            Part(t, "StandBase", new Vector3(0f, 0.768f, -0.17f), new Vector3(0.22f, 0.015f, 0.18f), m.blackGlass);
            Part(t, "StandNeck", new Vector3(0f, 0.9f, -0.21f), new Vector3(0.04f, 0.26f, 0.03f), m.blackGlass);
            Part(t, "Monitor", new Vector3(0f, 1.08f, -0.19f), new Vector3(0.62f, 0.37f, 0.03f), m.blackGlass);
            Part(t, "Screen", new Vector3(0f, 1.08f, -0.174f), new Vector3(0.58f, 0.33f, 0.004f), m.screen, shadows: false);
            Part(t, "Keyboard", new Vector3(0f, 0.768f, 0.12f), new Vector3(0.44f, 0.018f, 0.14f), m.blackGlass);
            Part(t, "Mouse", new Vector3(0.32f, 0.772f, 0.13f), new Vector3(0.06f, 0.025f, 0.1f), m.blackGlass);
            Part(t, "Papers", new Vector3(-w / 2f + 0.3f, 0.763f, 0.05f), new Vector3(0.21f, 0.006f, 0.3f), m.flagWhite, euler: new Vector3(0f, -8f, 0f));
            Part(t, "Mug", new Vector3(-0.42f, 0.81f, -0.05f), new Vector3(0.08f, 0.05f, 0.08f), m.pot, type: PrimitiveType.Cylinder);
            Part(t, "LampBase", new Vector3(-w / 2f + 0.15f, 0.77f, -0.25f), new Vector3(0.14f, 0.01f, 0.14f), m.blackGlass, type: PrimitiveType.Cylinder);
            Part(t, "LampArm", new Vector3(-w / 2f + 0.17f, 0.98f, -0.2f), new Vector3(0.02f, 0.42f, 0.02f), m.chrome, euler: new Vector3(18f, 0f, 0f));
            Part(t, "LampHead", new Vector3(-w / 2f + 0.17f, 1.17f, -0.1f), new Vector3(0.14f, 0.05f, 0.08f), m.lampShade, shadows: false);
            return t;
        }

        // ------------------------------------------------------------------ STORAGE
        static Transform Bookcase(Transform g, Vector3 pos, float yaw, float w, float h, Mats m, int seed)
        {
            const float d = 0.36f;
            var t = Piece(g, "Bookcase", PlaceholderKind.Storage, pos, yaw, new Vector3(w, h, d));
            foreach (var sx in new[] { -1f, 1f }) Part(t, "Side", new Vector3(sx * (w / 2f - 0.015f), h / 2f, 0f), new Vector3(0.03f, h, d), m.desk, solid: true);
            Part(t, "Back", new Vector3(0f, h / 2f, -d / 2f + 0.005f), new Vector3(w, h, 0.01f), m.desk);
            int shelves = Mathf.Max(3, Mathf.RoundToInt(h / 0.38f));
            float gap = (h - 0.06f) / shelves;
            var rng = new System.Random(seed);
            Material[] spines = { m.art[0], m.art[1], m.art[2], m.flagWhite, m.signBoard, m.fabricAccent, m.desk };
            for (int s = 0; s <= shelves; s++)
            {
                float y = 0.03f + s * gap;
                Part(t, "Shelf", new Vector3(0f, y, 0f), new Vector3(w - 0.04f, 0.025f, d - 0.02f), m.desk);
                if (s == shelves) break;
                float x = -w / 2f + 0.06f;
                while (x < w / 2f - 0.12f)
                {
                    float bw = 0.06f + (float)rng.NextDouble() * 0.16f, bh = Mathf.Min(gap - 0.06f, 0.18f + (float)rng.NextDouble() * 0.12f);
                    if (rng.NextDouble() < 0.18) { x += 0.1f; continue; }   // a gap / an ornament space
                    Part(t, "Books", new Vector3(x + bw / 2f, y + 0.013f + bh / 2f, 0.02f), new Vector3(bw, bh, d - 0.12f), spines[rng.Next(spines.Length)], shadows: false);
                    x += bw + 0.01f;
                }
            }
            return t;
        }

        /// <summary>Heavy-duty warehouse racking (orange uprights, steel shelves, stock boxes).</summary>
        static Transform Racking(Transform g, Vector3 pos, float yaw, float w, float h, Mats m, int seed)
        {
            const float d = 0.9f;
            var t = Piece(g, "Racking", PlaceholderKind.Storage, pos, yaw, new Vector3(w, h, d));
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Upright", new Vector3(sx * (w / 2f - 0.04f), h / 2f, sz * (d / 2f - 0.04f)), new Vector3(0.07f, h, 0.07f), m.racking, solid: true);
            var rng = new System.Random(seed);
            Material[] boxes = { m.toolRed, m.accentPanel, m.timber, m.flagWhite, m.barrel };
            for (int s = 0; s < 4; s++)
            {
                float y = 0.12f + s * (h - 0.2f) / 3.5f;
                Part(t, "Beam", new Vector3(0f, y, d / 2f - 0.04f), new Vector3(w, 0.08f, 0.05f), m.barrel);
                Part(t, "Deck", new Vector3(0f, y + 0.05f, 0f), new Vector3(w - 0.1f, 0.02f, d - 0.08f), m.metal);
                float x = -w / 2f + 0.1f;
                while (x < w / 2f - 0.3f)
                {
                    float bw = 0.3f + (float)rng.NextDouble() * 0.35f, bh = 0.2f + (float)rng.NextDouble() * 0.28f;
                    Part(t, "Stock", new Vector3(x + bw / 2f, y + 0.06f + bh / 2f, 0.05f), new Vector3(bw, bh, d - 0.25f), boxes[rng.Next(boxes.Length)], shadows: false);
                    x += bw + 0.05f;
                }
            }
            return t;
        }

        static Transform Wardrobe(Transform g, Vector3 pos, float yaw, float w, Mats m)
        {
            const float h = 2.1f, d = 0.6f;
            var t = Piece(g, "Wardrobe", PlaceholderKind.Storage, pos, yaw, new Vector3(w, h, d));
            Part(t, "Body", new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), m.gloss, solid: true);
            int doors = Mathf.Max(2, Mathf.RoundToInt(w / 0.6f));
            for (int i = 1; i < doors; i++)
                Part(t, "DoorLine", new Vector3(-w / 2f + i * w / doors, h / 2f, d / 2f + 0.001f), new Vector3(0.006f, h - 0.06f, 0.004f), m.blackGlass, shadows: false);
            for (int i = 0; i < doors; i++)
                Part(t, "Handle", new Vector3(-w / 2f + (i + (i % 2 == 0 ? 0.85f : 0.15f)) * w / doors, 1.05f, d / 2f + 0.015f), new Vector3(0.02f, 0.3f, 0.02f), m.chrome, shadows: false);
            return t;
        }

        // ------------------------------------------------------------------ BEDROOM
        static Transform Bed(Transform g, Vector3 pos, float yaw, float w, float l, Mats m)
        {
            var t = Piece(g, "Bed", PlaceholderKind.Bed, pos, yaw, new Vector3(w + 0.2f, 1.25f, l + 0.15f));
            Part(t, "Frame", new Vector3(0f, 0.15f, 0f), new Vector3(w + 0.12f, 0.3f, l + 0.08f), m.timber, solid: true);
            Part(t, "Mattress", new Vector3(0f, 0.41f, 0f), new Vector3(w, 0.22f, l), m.linen, solid: true);
            Part(t, "Duvet", new Vector3(0f, 0.56f, l * 0.15f), new Vector3(w + 0.06f, 0.1f, l * 0.68f), m.fabricLight);
            foreach (var sx in new[] { -1f, 1f })
                Part(t, "DuvetSide", new Vector3(sx * (w / 2f + 0.04f), 0.4f, l * 0.15f), new Vector3(0.03f, 0.3f, l * 0.68f), m.fabricLight);
            Part(t, "Throw", new Vector3(0f, 0.62f, l / 2f - 0.32f), new Vector3(w + 0.1f, 0.04f, 0.45f), m.fabricAccent);
            foreach (var sx in new[] { -1f, 1f })
                Part(t, "Pillow", new Vector3(sx * w / 4.2f, 0.6f, -l / 2f + 0.3f), new Vector3(w / 2f - 0.12f, 0.15f, 0.42f), m.linen, euler: new Vector3(-12f, 0f, 0f));
            Part(t, "Headboard", new Vector3(0f, 0.68f, -l / 2f - 0.05f), new Vector3(w + 0.2f, 1.1f, 0.1f), m.fabricAccent, solid: true);
            return t;
        }

        static Transform Nightstand(Transform g, Vector3 pos, float yaw, Mats m)
        {
            var t = Piece(g, "Nightstand", PlaceholderKind.Table, pos, yaw, new Vector3(0.5f, 0.85f, 0.4f));
            Part(t, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.4f), m.desk, solid: true);
            Part(t, "DrawerLine", new Vector3(0f, 0.3f, 0.201f), new Vector3(0.46f, 0.006f, 0.004f), m.blackGlass, shadows: false);
            Part(t, "LampBase", new Vector3(0.08f, 0.58f, -0.04f), new Vector3(0.1f, 0.08f, 0.1f), m.pot, type: PrimitiveType.Cylinder);
            Part(t, "LampShade", new Vector3(0.08f, 0.76f, -0.04f), new Vector3(0.26f, 0.1f, 0.26f), m.lampShade, type: PrimitiveType.Cylinder, shadows: false);
            SmallLight(t, new Vector3(0.08f, 0.8f, -0.04f), 3.5f, 0.7f);
            return t;
        }

        // ------------------------------------------------------------------ LIVING ROOM
        static void Rug(Transform g, Vector3 pos, float w, float d, Material mat) =>
            Part(g, "Rug", pos + new Vector3(0f, 0.006f, 0f), new Vector3(w, 0.012f, d), mat, shadows: false);

        static Transform TvWall(Transform g, Vector3 pos, float yaw, float w, Mats m)
        {
            var t = Piece(g, "TvUnit", PlaceholderKind.Appliance, pos, yaw, new Vector3(w, 1.8f, 0.45f));
            Part(t, "Unit", new Vector3(0f, 0.24f, 0f), new Vector3(w, 0.42f, 0.42f), m.gloss, solid: true);
            for (int i = 1; i < 3; i++) Part(t, "DoorLine", new Vector3(-w / 2f + i * w / 3f, 0.24f, 0.211f), new Vector3(0.005f, 0.38f, 0.004f), m.blackGlass, shadows: false);
            float tvW = Mathf.Min(1.65f, w * 0.85f), tvH = tvW * 0.5625f;
            Part(t, "TvBezel", new Vector3(0f, 1.3f, -0.17f), new Vector3(tvW, tvH, 0.04f), m.blackGlass);
            Part(t, "TvScreen", new Vector3(0f, 1.3f, -0.149f), new Vector3(tvW - 0.04f, tvH - 0.04f, 0.004f), m.screen, shadows: false);
            Part(t, "Soundbar", new Vector3(0f, 0.5f, 0.02f), new Vector3(tvW * 0.6f, 0.07f, 0.1f), m.blackGlass);
            Part(t, "Plant", new Vector3(w / 2f - 0.15f, 0.58f, 0f), new Vector3(0.18f, 0.26f, 0.18f), m.leaves, type: PrimitiveType.Sphere);
            return t;
        }

        static Transform WallArt(Transform g, Vector3 pos, float yaw, float w, float h, Material canvas, Mats m)
        {
            var t = Piece(g, "WallArt", PlaceholderKind.Decor, pos, yaw, new Vector3(w + 0.06f, h + 0.06f, 0.04f));
            Part(t, "Frame", Vector3.zero, new Vector3(w + 0.06f, h + 0.06f, 0.03f), m.blackGlass, shadows: false);
            Part(t, "Canvas", new Vector3(0f, 0f, 0.017f), new Vector3(w, h, 0.004f), canvas, shadows: false);
            Part(t, "Stripe", new Vector3(-w * 0.15f, -h * 0.1f, 0.02f), new Vector3(w * 0.5f, h * 0.12f, 0.003f), m.flagWhite, shadows: false, euler: new Vector3(0f, 0f, 18f));
            return t;
        }

        static Transform FloorLamp(Transform g, Vector3 pos, Mats m)
        {
            var t = Piece(g, "FloorLamp", PlaceholderKind.Decor, pos, 0f, new Vector3(0.45f, 1.75f, 0.45f));
            Part(t, "Base", new Vector3(0f, 0.015f, 0f), new Vector3(0.3f, 0.015f, 0.3f), m.blackGlass, type: PrimitiveType.Cylinder);
            Part(t, "Pole", new Vector3(0f, 0.8f, 0f), new Vector3(0.025f, 0.78f, 0.025f), m.chrome, type: PrimitiveType.Cylinder);
            Part(t, "Shade", new Vector3(0f, 1.62f, 0f), new Vector3(0.42f, 0.13f, 0.42f), m.lampShade, type: PrimitiveType.Cylinder, shadows: false);
            SmallLight(t, new Vector3(0f, 1.55f, 0f), 4.5f, 0.9f);
            return t;
        }

        /// <summary>A potted plant: tapered pot, soil, stems and layered leaf clusters (replaces the old ball on a box).</summary>
        static Transform PottedPlant(Transform g, Vector3 pos, float scale, Mats m)
        {
            var t = Piece(g, "Plant", PlaceholderKind.Plant, pos, (pos.x * 37f + pos.z * 11f) % 360f, new Vector3(0.9f, 1.6f, 0.9f) * scale);
            Part(t, "Pot", new Vector3(0f, 0.22f, 0f) * scale, new Vector3(0.46f, 0.22f, 0.46f) * scale, m.pot, solid: true, type: PrimitiveType.Cylinder);
            Part(t, "PotRim", new Vector3(0f, 0.45f, 0f) * scale, new Vector3(0.5f, 0.02f, 0.5f) * scale, m.pot, type: PrimitiveType.Cylinder);
            Part(t, "Soil", new Vector3(0f, 0.44f, 0f) * scale, new Vector3(0.42f, 0.01f, 0.42f) * scale, m.fieldSoil, type: PrimitiveType.Cylinder, shadows: false);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Part(t, "Stem", new Vector3(Mathf.Sin(a) * 0.06f, 0.75f, Mathf.Cos(a) * 0.06f) * scale, new Vector3(0.025f, 0.32f, 0.025f) * scale, m.trunk,
                     type: PrimitiveType.Cylinder, euler: new Vector3(Mathf.Cos(a) * 10f, 0f, -Mathf.Sin(a) * 10f));
            }
            var leaves = new[] { new Vector3(0f, 1.25f, 0f), new Vector3(0.18f, 1.05f, 0.1f), new Vector3(-0.16f, 1.12f, -0.12f), new Vector3(0.05f, 1.45f, -0.08f), new Vector3(-0.1f, 0.85f, 0.16f) };
            for (int i = 0; i < leaves.Length; i++)
                Part(t, "Leaves", leaves[i] * scale, new Vector3(0.5f, 0.32f, 0.44f) * scale * (1f - i * 0.08f), m.leaves, type: PrimitiveType.Sphere,
                     euler: new Vector3(i * 17f, i * 53f, i * 11f));
            return t;
        }

        // ------------------------------------------------------------------ KITCHEN
        /// <summary>A run of kitchen units along local X: base units, worktop, sink, hob, oven, wall units, splashback, extractor.</summary>
        static void KitchenRun(Transform g, Vector3 pos, float yaw, float length, Mats m, bool wallUnits = true)
        {
            var t = Child("Kitchen", g, pos);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Part(t, "Units", new Vector3(0f, 0.47f, 0f), new Vector3(length, 0.78f, 0.6f), m.gloss, solid: true);
            Part(t, "Plinth", new Vector3(0f, 0.05f, -0.03f), new Vector3(length, 0.1f, 0.54f), m.blackGlass);
            Part(t, "Worktop", new Vector3(0f, 0.88f, 0.01f), new Vector3(length + 0.02f, 0.04f, 0.64f), m.worktop, solid: true);
            int doors = Mathf.Max(2, Mathf.RoundToInt(length / 0.6f));
            for (int i = 1; i < doors; i++)
                Part(t, "DoorLine", new Vector3(-length / 2f + i * length / doors, 0.47f, 0.301f), new Vector3(0.005f, 0.74f, 0.004f), m.blackGlass, shadows: false);
            for (int i = 0; i < doors; i++)
                Part(t, "Handle", new Vector3(-length / 2f + (i + 0.5f) * length / doors, 0.8f, 0.315f), new Vector3(0.3f, 0.015f, 0.02f), m.chrome, shadows: false);
            float sinkX = -length / 2f + Mathf.Min(1.1f, length * 0.3f), hobX = length / 2f - 0.8f;
            Part(t, "Sink", new Vector3(sinkX, 0.902f, 0f), new Vector3(0.7f, 0.006f, 0.45f), m.chrome, shadows: false);
            Part(t, "Basin", new Vector3(sinkX, 0.905f, 0f), new Vector3(0.48f, 0.004f, 0.34f), m.blackGlass, shadows: false);
            Part(t, "Tap", new Vector3(sinkX, 1.05f, -0.24f), new Vector3(0.03f, 0.15f, 0.03f), m.chrome, type: PrimitiveType.Cylinder);
            Part(t, "Spout", new Vector3(sinkX, 1.2f, -0.16f), new Vector3(0.025f, 0.025f, 0.18f), m.chrome);
            Part(t, "Hob", new Vector3(hobX, 0.903f, 0f), new Vector3(0.6f, 0.006f, 0.5f), m.blackGlass, shadows: false);
            foreach (var (dx, dz) in new[] { (-0.15f, -0.12f), (0.15f, -0.12f), (-0.15f, 0.12f), (0.15f, 0.12f) })
                Part(t, "Ring", new Vector3(hobX + dx, 0.907f, dz), new Vector3(0.17f, 0.001f, 0.17f), m.signBoard, type: PrimitiveType.Cylinder, shadows: false);
            Part(t, "OvenDoor", new Vector3(hobX, 0.46f, 0.303f), new Vector3(0.58f, 0.55f, 0.006f), m.blackGlass, shadows: false);
            Part(t, "OvenHandle", new Vector3(hobX, 0.76f, 0.32f), new Vector3(0.5f, 0.02f, 0.02f), m.chrome, shadows: false);
            if (!wallUnits) return;   // an island: no splashback or wall units
            Part(t, "Splashback", new Vector3(0f, 1.2f, -0.295f), new Vector3(length, 0.6f, 0.01f), m.tiles, shadows: false);
            Part(t, "WallUnits", new Vector3(sinkX < 0 ? -0.45f : 0f, 1.88f, -0.13f), new Vector3(length - 1f, 0.7f, 0.34f), m.gloss, solid: true);
            Part(t, "Hood", new Vector3(hobX, 1.72f, -0.08f), new Vector3(0.62f, 0.12f, 0.45f), m.chrome);
            Part(t, "Chimney", new Vector3(hobX, 2.1f, -0.2f), new Vector3(0.26f, 0.65f, 0.2f), m.chrome);
            Part(t, "UnderLight", new Vector3(sinkX < 0 ? -0.45f : 0f, 1.52f, -0.06f), new Vector3(length - 1.1f, 0.01f, 0.03f), m.lampShade, shadows: false);
        }

        static Transform Fridge(Transform g, Vector3 pos, float yaw, Mats m)
        {
            var t = Piece(g, "Fridge", PlaceholderKind.Appliance, pos, yaw, new Vector3(0.72f, 1.95f, 0.68f));
            Part(t, "Body", new Vector3(0f, 0.975f, 0f), new Vector3(0.7f, 1.95f, 0.66f), m.metal, solid: true);
            Part(t, "DoorLine", new Vector3(0f, 1.25f, 0.331f), new Vector3(0.68f, 0.006f, 0.004f), m.blackGlass, shadows: false);
            Part(t, "Handle", new Vector3(0.28f, 1.45f, 0.35f), new Vector3(0.025f, 0.45f, 0.03f), m.chrome, shadows: false);
            Part(t, "Handle2", new Vector3(0.28f, 0.85f, 0.35f), new Vector3(0.025f, 0.35f, 0.03f), m.chrome, shadows: false);
            return t;
        }

        // ------------------------------------------------------------------ SHOWROOM EXTRAS
        static Transform ReceptionCounter(Transform g, Vector3 pos, float yaw, float w, Mats m, bool premium)
        {
            var t = Piece(g, "Reception", PlaceholderKind.Table, pos, yaw, new Vector3(w, 1.15f, 0.9f));
            Part(t, "Body", new Vector3(0f, 0.53f, 0f), new Vector3(w, 1.06f, 0.7f), premium ? m.blackGlass : m.wall, solid: true);
            Part(t, "Top", new Vector3(0f, 1.1f, 0.08f), new Vector3(w + 0.1f, 0.05f, 0.85f), premium ? m.goldTrim : m.worktop);
            Part(t, "DeskTop", new Vector3(0f, 0.74f, -0.25f), new Vector3(w - 0.1f, 0.04f, 0.45f), m.desk);
            Part(t, "Led", new Vector3(0f, 0.08f, 0.352f), new Vector3(w - 0.1f, 0.03f, 0.01f), m.ledStrip, shadows: false);
            Part(t, "Monitor", new Vector3(w * 0.25f, 0.98f, -0.3f), new Vector3(0.5f, 0.32f, 0.03f), m.blackGlass, euler: new Vector3(0f, 180f, 0f));
            Part(t, "Bell", new Vector3(-w * 0.3f, 1.14f, 0.3f), new Vector3(0.07f, 0.03f, 0.07f), m.chrome, type: PrimitiveType.Sphere);
            return t;
        }

        static void CoffeeStation(Transform g, Vector3 pos, float yaw, Mats m)
        {
            // The cabinet sits left of centre and the water cooler right of it, so the pair is centred on pos.
            pos += Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.45f, 0f, 0f);
            var t = Piece(g, "CoffeeStation", PlaceholderKind.Appliance, pos, yaw, new Vector3(1.4f, 1.3f, 0.55f));
            Part(t, "Cabinet", new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 0.55f), m.gloss, solid: true);
            Part(t, "Top", new Vector3(0f, 0.92f, 0f), new Vector3(1.44f, 0.04f, 0.58f), m.worktop);
            Part(t, "Machine", new Vector3(-0.3f, 1.15f, -0.04f), new Vector3(0.34f, 0.42f, 0.38f), m.chrome);
            Part(t, "MachineFront", new Vector3(-0.3f, 1.17f, 0.152f), new Vector3(0.28f, 0.2f, 0.004f), m.blackGlass, shadows: false);
            for (int i = 0; i < 3; i++)
                Part(t, "Cup", new Vector3(0.15f + i * 0.13f, 0.98f, 0.05f), new Vector3(0.08f, 0.04f, 0.08f), m.pot, type: PrimitiveType.Cylinder);
            Part(t, "Tray", new Vector3(0.35f, 0.945f, -0.12f), new Vector3(0.4f, 0.01f, 0.25f), m.desk);
            var cooler = Piece(g, "WaterCooler", PlaceholderKind.Appliance, pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.2f, 0f, 0f), yaw, new Vector3(0.34f, 1.45f, 0.34f));
            Part(cooler, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.32f, 1f, 0.32f), m.flagWhite, solid: true);
            Part(cooler, "Bottle", new Vector3(0f, 1.2f, 0f), new Vector3(0.27f, 0.2f, 0.27f), m.windowGlass, type: PrimitiveType.Cylinder, shadows: false);
            Part(cooler, "Taps", new Vector3(0f, 0.85f, 0.17f), new Vector3(0.12f, 0.05f, 0.03f), m.flagBlue);
        }

        static void BrochureStand(Transform g, Vector3 pos, float yaw, Mats m)
        {
            var t = Piece(g, "Brochures", PlaceholderKind.Decor, pos, yaw, new Vector3(0.5f, 1.4f, 0.4f));
            Part(t, "Post", new Vector3(0f, 0.6f, 0f), new Vector3(0.04f, 1.2f, 0.04f), m.chrome);
            Part(t, "Foot", new Vector3(0f, 0.01f, 0f), new Vector3(0.4f, 0.02f, 0.35f), m.blackGlass);
            Part(t, "Rack", new Vector3(0f, 1.15f, 0.05f), new Vector3(0.48f, 0.4f, 0.03f), m.chrome, euler: new Vector3(-18f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                Part(t, "Brochure", new Vector3(-0.15f + i * 0.15f, 1.18f, 0.08f), new Vector3(0.13f, 0.22f, 0.01f), m.art[i], euler: new Vector3(-18f, 0f, 0f), shadows: false);
        }

        // ------------------------------------------------------------------ WORKSHOP KIT
        static void ToolChest(Transform g, Vector3 pos, float yaw, Mats m)
        {
            var t = Piece(g, "ToolChest", PlaceholderKind.WorkshopKit, pos, yaw, new Vector3(1.6f, 1.15f, 0.65f));
            Part(t, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 1.0f, 0.6f), m.toolRed, solid: true);
            Part(t, "Top", new Vector3(0f, 1.12f, 0f), new Vector3(1.62f, 0.04f, 0.62f), m.blackGlass);
            for (int i = 0; i < 7; i++)
            {
                float y = 0.2f + i * 0.13f;
                Part(t, "Drawer", new Vector3(0f, y, 0.301f), new Vector3(1.54f, 0.005f, 0.004f), m.blackGlass, shadows: false);
                Part(t, "Handle", new Vector3(0f, y + 0.07f, 0.315f), new Vector3(1.3f, 0.015f, 0.02f), m.chrome, shadows: false);
            }
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Castor", new Vector3(sx * 0.7f, 0.05f, sz * 0.22f), new Vector3(0.09f, 0.09f, 0.09f), m.blackGlass, type: PrimitiveType.Sphere, shadows: false);
            Part(t, "Spanner", new Vector3(-0.4f, 1.15f, 0.05f), new Vector3(0.25f, 0.01f, 0.04f), m.chrome, euler: new Vector3(0f, 25f, 0f), shadows: false);
        }

        static void Workbench(Transform g, Vector3 pos, float yaw, float w, Mats m)
        {
            var t = Piece(g, "Workbench", PlaceholderKind.WorkshopKit, pos, yaw, new Vector3(w, 0.95f, 0.8f));
            Part(t, "Top", new Vector3(0f, 0.9f, 0f), new Vector3(w, 0.06f, 0.8f), m.timber, solid: true);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Part(t, "Leg", new Vector3(sx * (w / 2f - 0.06f), 0.44f, sz * 0.32f), new Vector3(0.06f, 0.88f, 0.06f), m.metal);
            Part(t, "Shelf", new Vector3(0f, 0.18f, 0f), new Vector3(w - 0.1f, 0.03f, 0.7f), m.metal);
            Part(t, "Vice", new Vector3(w / 2f - 0.25f, 1.0f, 0.3f), new Vector3(0.22f, 0.14f, 0.2f), m.flagBlue);
            Part(t, "Pegboard", new Vector3(0f, 1.55f, -0.38f), new Vector3(w, 1.0f, 0.02f), m.timber);
            for (int i = 0; i < 7; i++)
                Part(t, "Tool", new Vector3(-w / 2f + 0.3f + i * (w - 0.6f) / 6f, 1.55f + (i % 2) * 0.12f, -0.36f), new Vector3(0.05f, 0.32f - (i % 3) * 0.06f, 0.02f), i % 3 == 0 ? m.toolRed : m.chrome, shadows: false);
            Part(t, "PartsBin", new Vector3(-w / 2f + 0.35f, 0.99f, 0.1f), new Vector3(0.4f, 0.12f, 0.3f), m.barrel);
        }

        static void TyreRack(Transform g, Vector3 pos, float yaw, float w, Mats m)
        {
            var t = Piece(g, "TyreRack", PlaceholderKind.WorkshopKit, pos, yaw, new Vector3(w, 1.7f, 0.5f));
            foreach (var sx in new[] { -1f, 1f }) Part(t, "Post", new Vector3(sx * w / 2f, 0.85f, 0f), new Vector3(0.05f, 1.7f, 0.45f), m.racking, solid: true);
            for (int r = 0; r < 2; r++)
            {
                float y = 0.15f + r * 0.85f;
                Part(t, "Rail", new Vector3(0f, y, 0f), new Vector3(w, 0.04f, 0.4f), m.metal);
                int n = Mathf.FloorToInt((w - 0.1f) / 0.27f);
                for (int i = 0; i < n; i++)
                    Part(t, "Tyre", new Vector3(-w / 2f + 0.18f + i * 0.27f, y + 0.33f, 0f), new Vector3(0.64f, 0.11f, 0.64f), m.tire,
                         type: PrimitiveType.Cylinder, euler: new Vector3(0f, 0f, 90f));
            }
        }

        static void TubeLights(Transform g, float y, float x0, float x1, float z0, float z1, int across, int deep, Mats m)
        {
            for (int i = 0; i < across; i++)
                for (int j = 0; j < deep; j++)
                {
                    float x = across == 1 ? (x0 + x1) / 2f : Mathf.Lerp(x0, x1, i / (float)(across - 1));
                    float z = deep == 1 ? (z0 + z1) / 2f : Mathf.Lerp(z0, z1, j / (float)(deep - 1));
                    Part(g, "TubeHousing", new Vector3(x, y + 0.04f, z), new Vector3(0.18f, 0.06f, 1.5f), m.metal, shadows: false);
                    Part(g, "Tube", new Vector3(x, y, z), new Vector3(0.1f, 0.03f, 1.42f), m.ceilingLight, shadows: false);
                }
        }

        // ------------------------------------------------------------------ FURNISHING
        /// <summary>Showroom furniture per building level, from <see cref="InteriorPlan.Showroom"/> (tested to keep clear of the car platforms).</summary>
        static void FurnishShowroom(Transform g, Mats m, int level, bool premium)
        {
            foreach (var item in InteriorPlan.Showroom(level)) PlaceItem(g, item, m, premium, level * 7);
        }

        /// <summary>Builds one planned piece of furniture with the kit. Returns its transform.</summary>
        static Transform PlaceItem(Transform g, FurnitureItem i, Mats m, bool premium, int seed)
        {
            var pos = new Vector3(i.X, i.Y, i.Z);
            Material SofaFabric(int v) => v == 1 ? m.fabricLight : v == 2 ? m.leather : v == 3 ? m.fabricAccent : premium ? m.leather : m.sofa;
            switch (i.Type)
            {
                case FurnitureType.Sofa: return Sofa(g, pos, i.Yaw, i.W, Mathf.Max(1, i.Seats), SofaFabric(i.Variant), premium ? m.goldTrim : (i.Variant == 2 ? m.fabricLight : m.fabricAccent), m);
                case FurnitureType.Armchair: return Sofa(g, pos, i.Yaw, 0.95f, 1, SofaFabric(i.Variant), null, m);
                case FurnitureType.CoffeeTable: return CoffeeTable(g, pos, i.Yaw, i.W, i.D, m);
                case FurnitureType.TvWall: return TvWall(g, pos, i.Yaw, i.W, m);
                case FurnitureType.DiningTable: return DiningTable(g, pos, i.Yaw, i.W, i.D, m);
                case FurnitureType.DiningChair:
                    return i.Variant == 3 ? DiningChair(g, pos, i.Yaw, m.chrome, premium ? m.leather : m.sofa)
                         : DiningChair(g, pos, i.Yaw, m.desk, i.Variant == 1 ? m.fabricLight : i.Variant == 2 ? m.leather : null);
                case FurnitureType.Bed: return Bed(g, pos, i.Yaw, i.W, i.D, m);
                case FurnitureType.Nightstand: return Nightstand(g, pos, i.Yaw, m);
                case FurnitureType.Wardrobe: return Wardrobe(g, pos, i.Yaw, i.W, m);
                case FurnitureType.Kitchen: KitchenRun(g, pos, i.Yaw, i.W, m); return null;
                case FurnitureType.KitchenIsland: KitchenRun(g, pos, i.Yaw, i.W, m, wallUnits: false); return null;
                case FurnitureType.Fridge: return Fridge(g, pos, i.Yaw, m);
                case FurnitureType.Desk: return Desk(g, pos, i.Yaw, i.W, m);
                case FurnitureType.OfficeChair: return OfficeChair(g, pos, i.Yaw, i.Variant == 2 ? m.leather : m.blackGlass, m);
                case FurnitureType.Bookcase: return Bookcase(g, pos, i.Yaw, i.W, i.H > 0f ? i.H : 2.2f, m, seed);
                case FurnitureType.FloorLamp: return FloorLamp(g, pos, m);
                case FurnitureType.Plant: return PottedPlant(g, pos, i.W > 0f ? i.W : 1f, m);
                case FurnitureType.Rug: Rug(g, pos, i.W, i.D, i.Variant == 1 ? m.fabricLight : m.carpet); return null;
                case FurnitureType.WallArt: return WallArt(g, pos, i.Yaw, i.W, i.H > 0f ? i.H : 0.8f, m.art[Mathf.Abs(i.Variant) % m.art.Length], m);
                case FurnitureType.Reception: return ReceptionCounter(g, pos, i.Yaw, i.W, m, premium);
                case FurnitureType.CoffeeStation: CoffeeStation(g, pos, i.Yaw, m); return null;
                case FurnitureType.Brochures: BrochureStand(g, pos, i.Yaw, m); return null;
                default: return null;   // windows are built by the room
            }
        }

        static void FurnishServiceCentre(Transform g, Mats m)
        {
            ToolChest(g, new Vector3(-10.95f, 0f, 13.5f), 90f, m);
            Workbench(g, new Vector3(0f, 0f, 15.2f), 180f, 3f, m);
            Racking(g, new Vector3(10.95f, 0f, 13.5f), -90f, 3.6f, 2.4f, m, 3);
            foreach (float z in new[] { 10.4f, 11.2f })
                Part(g, "OilDrum", new Vector3(-11.3f, 0.45f, z), new Vector3(0.58f, 0.45f, 0.58f), m.barrel, solid: true, type: PrimitiveType.Cylinder);
            TubeLights(g, 5.3f, -8f, 8f, 3f, 13f, 3, 3, m);
            WallArt(g, new Vector3(-11.83f, 2.6f, 4f), 90f, 1.2f, 0.85f, m.art[2], m);
        }

        static void FurnishWorkshop(Transform g, Mats m)
        {
            ToolChest(g, new Vector3(7.25f, 0f, 11.5f), -90f, m);
            Workbench(g, new Vector3(0f, 0f, 13.4f), 180f, 4f, m);
            TyreRack(g, new Vector3(-5f, 0f, 13.45f), 180f, 1.8f, m);
            TubeLights(g, 4.75f, -5f, 5f, 3f, 11f, 3, 2, m);
            WallArt(g, new Vector3(-7.83f, 2.5f, 6f), 90f, 1.1f, 0.8f, m.art[0], m);
        }

        static void FurnishPartsShop(Transform g, Mats m)
        {
            foreach (float x in new[] { -4.5f, 4.5f }) Racking(g, new Vector3(x, 0f, 8.1f), 180f, 2.4f, 2.6f, m, x < 0 ? 5 : 7);
            TyreRack(g, new Vector3(-4.9f, 0f, 3.5f), 90f, 1.6f, m);
            WallArt(g, new Vector3(5.83f, 2.2f, 4.5f), -90f, 1.2f, 0.8f, m.art[1], m);
            TubeLights(g, 3.75f, -3f, 3f, 2.5f, 6.5f, 2, 2, m);
        }

        // ------------------------------------------------------------------ WALK-IN HOMES
        /// <summary>Depth below the house where its interior is built (out of sight under the ground).</summary>
        const float InteriorDepth = 40f;

        /// <summary>
        /// Builds the furnished inside of a home (tier 0 studio, 1 family home, 2 villa) under <paramref name="home"/> and
        /// links it to the front door. <paramref name="outside"/> is where you come out (the existing wake point).
        /// </summary>
        static HomeInterior BuildHomeInterior(Transform home, Mats m, string propertyId, int tier, Transform outside)
        {
            var plan = InteriorPlan.Home(tier);
            var size = new Vector3(plan.SizeX, plan.Height, plan.SizeZ);
            var root = Child("Interior", home, new Vector3(0f, -InteriorDepth, 0f));
            float hx = size.x / 2f, hz = size.z / 2f, h = size.y;
            float doorX = plan.DoorX;
            var floor = tier == 2 ? m.marble : m.desk;

            // Shell: floor, ceiling, walls (door in the south wall), skirting.
            Solid(root, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(size.x + 0.4f, 0.1f, size.z + 0.4f), floor);
            Solid(root, "Ceiling", new Vector3(0f, h + 0.05f, 0f), new Vector3(size.x + 0.4f, 0.1f, size.z + 0.4f), m.wall);
            Solid(root, "Wall_N", new Vector3(0f, h / 2f, hz + 0.1f), new Vector3(size.x + 0.4f, h, 0.2f), m.wall);
            Solid(root, "Wall_W", new Vector3(-hx - 0.1f, h / 2f, 0f), new Vector3(0.2f, h, size.z), m.wall);
            Solid(root, "Wall_E", new Vector3(hx + 0.1f, h / 2f, 0f), new Vector3(0.2f, h, size.z), m.wall);
            Solid(root, "Wall_S", new Vector3(0f, h / 2f, -hz - 0.1f), new Vector3(size.x + 0.4f, h, 0.2f), m.wall);
            foreach (var (c, s) in new[] { (new Vector3(0f, 0.05f, hz - 0.01f), new Vector3(size.x, 0.1f, 0.02f)), (new Vector3(0f, 0.05f, -hz + 0.01f), new Vector3(size.x, 0.1f, 0.02f)),
                                           (new Vector3(-hx + 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.1f, size.z)), (new Vector3(hx - 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.1f, size.z)) })
                Part(root, "Skirting", c, s, m.flagWhite, shadows: false);

            // The door: a panel on the inside of the south wall you press E on to go out.
            var door = Part(root, "FrontDoor", new Vector3(doorX, 1.05f, -hz + 0.04f), new Vector3(0.95f, 2.1f, 0.06f), m.desk, solid: true);
            door.layer = InteractLayer;
            Part(root, "DoorFrame", new Vector3(doorX, 1.1f, -hz + 0.02f), new Vector3(1.1f, 2.2f, 0.03f), m.flagWhite, shadows: false);
            Part(root, "DoorHandle", new Vector3(doorX + 0.36f, 1.0f, -hz + 0.09f), new Vector3(0.12f, 0.02f, 0.03f), m.chrome, shadows: false);
            Part(root, "DoorMat", new Vector3(doorX, 0.008f, -hz + 0.6f), new Vector3(0.9f, 0.012f, 0.6f), m.carpet, shadows: false);

            // Frosted windows (daylight by day, dark at night) with frames and sills.
            void Window(Vector3 c, float yaw, float w, float wh)
            {
                var t = Child("Window", root, c);
                t.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Part(t, "Pane", Vector3.zero, new Vector3(w, wh, 0.02f), m.daylight, shadows: false);
                Part(t, "FrameTop", new Vector3(0f, wh / 2f + 0.03f, 0.02f), new Vector3(w + 0.1f, 0.06f, 0.05f), m.flagWhite, shadows: false);
                Part(t, "FrameBottom", new Vector3(0f, -wh / 2f - 0.03f, 0.04f), new Vector3(w + 0.16f, 0.06f, 0.12f), m.flagWhite, shadows: false);
                foreach (var sx in new[] { -1f, 1f }) Part(t, "FrameSide", new Vector3(sx * (w / 2f + 0.03f), 0f, 0.02f), new Vector3(0.06f, wh, 0.05f), m.flagWhite, shadows: false);
                Part(t, "Mullion", new Vector3(0f, 0f, 0.02f), new Vector3(0.04f, wh, 0.04f), m.flagWhite, shadows: false);
            }

            // Ceiling downlights and a couple of real lights per room.
            void Downlights(float x0, float x1, float z0, float z1, int nx, int nz)
            {
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < nz; j++)
                    {
                        float x = nx == 1 ? (x0 + x1) / 2f : Mathf.Lerp(x0, x1, i / (float)(nx - 1));
                        float z = nz == 1 ? (z0 + z1) / 2f : Mathf.Lerp(z0, z1, j / (float)(nz - 1));
                        Part(root, "Downlight", new Vector3(x, h - 0.005f, z), new Vector3(0.14f, 0.005f, 0.14f), m.ceilingLight, type: PrimitiveType.Cylinder, shadows: false);
                    }
            }

            // Furniture and windows from the plan (tested: nothing overlaps, the door can reach the bed).
            Transform bedT = null;
            foreach (var item in plan.Items)
            {
                if (item.Type == FurnitureType.Window) { Window(new Vector3(item.X, item.Y, item.Z), item.Yaw, item.W, item.H); continue; }
                var placed = PlaceItem(root, item, m, false, 11 + tier);
                if (item.Type == FurnitureType.Bed) bedT = placed;
            }
            foreach (var (px, g0, g1) in plan.Partitions)
            {
                Solid(root, "Partition_S", new Vector3(px, h / 2f, (-hz + g0) / 2f), new Vector3(0.12f, h, g0 + hz), m.wall);
                Solid(root, "Partition_N", new Vector3(px, h / 2f, (g1 + hz) / 2f), new Vector3(0.12f, h, hz - g1), m.wall);
                Solid(root, "Partition_Top", new Vector3(px, (2.15f + h) / 2f, (g0 + g1) / 2f), new Vector3(0.12f, h - 2.15f, g1 - g0), m.wall);
            }
            if (tier == 2)
                for (int i = -1; i <= 1; i++)
                {
                    Part(root, "PendantCable", new Vector3(-3f + i * 0.7f, h - 0.4f, 0.9f), new Vector3(0.01f, 0.8f, 0.01f), m.blackGlass, shadows: false);
                    Part(root, "Pendant", new Vector3(-3f + i * 0.7f, h - 0.9f, 0.9f), new Vector3(0.28f, 0.16f, 0.28f), m.lampShade, type: PrimitiveType.Sphere, shadows: false);
                }
            // Ceiling downlights in a grid and a few real lights (warm, no shadows).
            int nx = Mathf.Max(2, Mathf.RoundToInt(size.x / 3f)), nz = Mathf.Max(2, Mathf.RoundToInt(size.z / 3f));
            Downlights(-hx + 1.2f, hx - 1.2f, -hz + 1.2f, hz - 1.2f, nx, nz);
            for (int i = 0; i < (tier == 0 ? 2 : 3); i++)
            {
                float x = Mathf.Lerp(-hx * 0.55f, hx * 0.55f, tier == 0 ? i : i / 2f);
                PointLight(root, new Vector3(x, h - 0.35f, 0f), tier == 2 ? 9f : 7f, 1.15f, new Color(1f, 0.88f, 0.72f));
            }
            AddReflectionProbe(root, "InteriorReflections", new Vector3(0f, h / 2f, 0f), new Vector3(size.x, h, size.z), 0.9f);

            // Gameplay: interior component, entry / wake points, the door and the bed.
            var interior = root.gameObject.AddComponent<HomeInterior>();
            interior.propertyId = propertyId;
            interior.size = size;
            var entry = Child("EntryPoint", root, new Vector3(plan.EntryX, 0.05f, plan.EntryZ));
            interior.entryPoint = entry;
            var wake = Child("WakePoint", root, new Vector3(plan.Wake.x, 0.05f, plan.Wake.z));
            wake.LookAt(new Vector3(root.position.x, wake.position.y, root.position.z));
            interior.wakePoint = wake;
            interior.exitPoint = outside;
            door.AddComponent<InteriorDoor>().home = interior;
            if (!bedT) return interior;
            // Not inside the bed's slot: a replacement bed model would switch the slot's colliders off.
            var sleep = new GameObject("BedSleep") { layer = InteractLayer };
            sleep.transform.SetParent(root, false);
            sleep.transform.SetPositionAndRotation(bedT.position + Vector3.up * 0.6f, bedT.rotation);
            var box = sleep.AddComponent<BoxCollider>();
            box.size = new Vector3(tier == 2 ? 2.1f : 1.8f, 0.5f, 2.2f);
            box.isTrigger = false;
            sleep.AddComponent<InteriorBed>().home = interior;
            return interior;
        }
    }
}
