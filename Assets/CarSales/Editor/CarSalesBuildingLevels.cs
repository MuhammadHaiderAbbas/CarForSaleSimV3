using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 11b: three complete looks per building. Level 1 is the original building; levels 2 and 3 are bigger,
    /// completely different buildings built here and switched on by BuildingLevelVisual when you rebuild. Everything
    /// that works (spots, desks, bays, doors) stays in the same place so all levels play the same way.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static Transform[] LevelRoots(Transform g, string buildingId, Vector3 siteCentre, Vector3 siteSize)
        {
            var roots = new Transform[3];
            for (int i = 0; i < 3; i++) roots[i] = Child("Level" + (i + 1), g, Vector3.zero);
            var v = g.gameObject.AddComponent<BuildingLevelVisual>();
            v.buildingId = buildingId;
            v.levels = roots;
            v.siteCentre = siteCentre;
            v.siteSize = siteSize;
            return roots;
        }

        /// <summary>Levels 2 and 3 start hidden (Build Everything bakes the NavMesh for level 1).</summary>
        static void HideUpperLevels(Transform[] roots) { roots[1].gameObject.SetActive(false); roots[2].gameObject.SetActive(false); }

        /// <summary>A wall along X at z with a door gap (doorW wide, doorH high) centred on doorX.</summary>
        static void WallWithDoor(Transform g, string name, float x0, float x1, float z, float h, float thick, float doorX, float doorW, float doorH, Material mat)
        {
            float a = doorX - doorW / 2f, b = doorX + doorW / 2f;
            if (a > x0) Solid(g, name + "_L", new Vector3((x0 + a) / 2f, h / 2f, z), new Vector3(a - x0, h, thick), mat);
            if (x1 > b) Solid(g, name + "_R", new Vector3((b + x1) / 2f, h / 2f, z), new Vector3(x1 - b, h, thick), mat);
            if (h > doorH) Solid(g, name + "_Top", new Vector3(doorX, (doorH + h) / 2f, z), new Vector3(doorW, h - doorH, thick), mat);
        }

        static void Glass(Transform g, string name, Vector3 pos, Vector3 size, Material glass) =>
            Solid(g, name, pos, size, glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        static void PitchedRoof(Transform g, string name, Vector3 centre, float width, float depth, float rise, Material mat)
        {
            // Phase 13: real roof tiles - clay for the red roofs, slate for the grey ones.
            if (mats != null && mats.roofTiles && mat == mats.flagRed) mat = mats.roofTiles;
            else if (mats != null && mats.roofSlate && mat == mats.roof) mat = mats.roofSlate;
            // Two tilted slabs meeting at the ridge (runs along X).
            float half = depth / 2f, len = Mathf.Sqrt(half * half + rise * rise) + 0.3f;
            float ang = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            var a = Deco(g, name + "_Front", PrimitiveType.Cube, centre + new Vector3(0f, rise / 2f, -half / 2f), new Vector3(width + 0.6f, 0.18f, len), mat);
            a.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
            var b = Deco(g, name + "_Back", PrimitiveType.Cube, centre + new Vector3(0f, rise / 2f, half / 2f), new Vector3(width + 0.6f, 0.18f, len), mat);
            b.transform.localRotation = Quaternion.Euler(ang, 0f, 0f);
            a.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            b.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        }

        static void WindowRow(Transform g, float z, float y, float x0, float x1, float w, float h, float gap, Material glass)
        {
            for (float x = x0 + w / 2f; x <= x1 - w / 2f + 0.01f; x += w + gap)
                Deco(g, "Window", PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(w, h, 0.06f), glass);
        }

        // ------------------------------------------------------------------ DEALERSHIP
        /// <summary>Level 2: a two-storey showroom - wider, taller, glass ground floor, clad upper floor, canopy, totem sign.</summary>
        static void BuildShowroomL2(Transform g, Mats m, string name, bool premium)
        {
            Solid(g, "Floor", new Vector3(0f, 0.02f, 18f), new Vector3(32f, 0.04f, 16f), premium ? (m.marble ?? m.platform) : m.floor);
            Solid(g, "Wall_North", new Vector3(0f, 4f, 26f), new Vector3(32.3f, 8f, 0.3f), m.wall);
            Solid(g, "Wall_West", new Vector3(-16f, 4f, 18f), new Vector3(0.3f, 8f, 16f), m.wall);
            Solid(g, "Wall_East", new Vector3(16f, 4f, 18f), new Vector3(0.3f, 8f, 16f), m.wall);
            Solid(g, "Roof", new Vector3(0f, 8.15f, 18f), new Vector3(32.8f, 0.3f, 16.8f), m.roof);
            // Ground floor: glass between frames, door in the middle.
            foreach (float x in new[] { -16f, -11f, -6f, -3f, 3f, 6f, 11f, 16f }) Solid(g, "FramePost", new Vector3(x, 2.6f, 10f), new Vector3(0.24f, 5.2f, 0.34f), m.frame);
            foreach (var (x, w) in new[] { (-13.5f, 4.8f), (-8.5f, 4.8f), (-4.5f, 2.8f), (4.5f, 2.8f), (8.5f, 4.8f), (13.5f, 4.8f) })
                Glass(g, "GlassPane", new Vector3(x, 2.6f, 10f), new Vector3(w, 5.2f, 0.06f), m.windowGlass);
            Solid(g, "DoorHeader", new Vector3(0f, 4.6f, 10f), new Vector3(6f, 1.2f, 0.34f), m.frame);
            // Upper floor band with strip windows and the sign.
            Solid(g, "UpperBand", new Vector3(0f, 6.6f, 10f), new Vector3(32.3f, 2.8f, 0.4f), premium ? m.signBoard : m.cladding);
            WindowRow(g, 9.78f, 6.6f, -15.5f, -6f, 2.6f, 1.2f, 0.6f, m.windowsGlow);
            WindowRow(g, 9.78f, 6.6f, 6f, 15.5f, 2.6f, 1.2f, 0.6f, m.windowsGlow);
            Deco(g, "SignPanel", PrimitiveType.Cube, new Vector3(0f, 6.6f, 9.7f), new Vector3(11f, 2f, 0.2f), premium ? m.goldTrim : m.accentPanel);
            SignText(g, new Vector3(0f, 6.75f, 9.58f), name.ToUpper(), 16f, Color.white, new Vector2(10f, 1.4f));
            SignText(g, new Vector3(0f, 5.95f, 9.58f), premium ? "PREMIUM CARS" : "QUALITY USED CARS", 5f, new Color(0.6f, 0.85f, 1f), new Vector2(9f, 0.5f));
            Deco(g, "LedStrip", PrimitiveType.Cube, new Vector3(0f, 5.25f, 9.75f), new Vector3(32.3f, 0.08f, 0.06f), m.ledStrip);
            // Entrance canopy.
            Solid(g, "Canopy", new Vector3(0f, 4.6f, 8.2f), new Vector3(8f, 0.25f, 3.6f), m.frame);
            foreach (float x in new[] { -3.8f, 3.8f }) Solid(g, "CanopyPost", new Vector3(x, 2.3f, 6.6f), new Vector3(0.18f, 4.6f, 0.18f), m.metal);
            // Inside: four platforms, reception, lounge, offices upstairs (balcony).
            foreach (float x in new[] { -12f, -6f, 6f, 12f })
            {
                Solid(g, "DisplayPlatform", new Vector3(x, 0.07f, 17f), new Vector3(5.6f, 0.05f, 5.6f), m.platform);
                Deco(g, "PlatformGlow", PrimitiveType.Cylinder, new Vector3(x, 0.045f, 17f), new Vector3(5.9f, 0.02f, 5.9f), premium ? m.lampGlow : m.ringGlow);
            }
            Solid(g, "Mezzanine", new Vector3(0f, 4.5f, 24.5f), new Vector3(31.6f, 0.25f, 2.8f), m.wall);
            Deco(g, "Railing", PrimitiveType.Cube, new Vector3(0f, 5.1f, 23.15f), new Vector3(31.6f, 1f, 0.05f), m.windowGlass);
            for (int ix = -2; ix <= 2; ix++) for (int iz = 0; iz < 3; iz++)
                Deco(g, "CeilingLight", PrimitiveType.Cube, new Vector3(ix * 6.4f, 7.97f, 12.5f + iz * 4.5f), new Vector3(3f, 0.04f, 0.8f), m.ceilingLight);
            PointLight(g, new Vector3(-8f, 6.5f, 16f), 18f, 1.8f, new Color(1f, 0.96f, 0.9f));
            PointLight(g, new Vector3(8f, 6.5f, 18f), 18f, 1.8f, new Color(1f, 0.96f, 0.9f));
            AddReflectionProbe(g, "ShowroomReflections", new Vector3(0f, 4f, 18f), new Vector3(32f, 8.2f, 16.2f));
            // Totem sign by the road and roof flags.
            Solid(g, "Totem", new Vector3(-17f, 3f, -20f), new Vector3(1.2f, 6f, 0.6f), premium ? m.signBoard : m.accentPanel);
            SignText(g, new Vector3(-17f, 4.6f, -20.32f), name.ToUpper(), 6f, Color.white, new Vector2(1.1f, 2.5f));
            Deco(g, "TotemLed", PrimitiveType.Cube, new Vector3(-17f, 1.4f, -20.32f), new Vector3(1.1f, 1.4f, 0.04f), m.ledStrip);
            foreach (float x in new[] { -12f, -4f, 4f, 12f })
            {
                Solid(g, "RoofFlagPole", new Vector3(x, 9.6f, 10.5f), new Vector3(0.08f, 2.6f, 0.08f), m.metal);
                Deco(g, "RoofFlag", PrimitiveType.Cube, new Vector3(x + 0.5f, 10.4f, 10.5f), new Vector3(1f, 0.6f, 0.03f), premium ? m.goldTrim : m.flagBlue);
            }
            foreach (var p in new[] { new Vector3(-15f, 0f, 25f), new Vector3(15f, 0f, 11f), new Vector3(-15f, 0f, 11f), new Vector3(15f, 0f, 25f) }) Plant(g, p, m, 1.1f);
            FurnishShowroom(g, m, 2, premium);   // phase 13c
        }

        /// <summary>Level 3: a flagship glass box - full-height glazing, white blade frame, mezzanine, six platforms, rooftop letters.</summary>
        static void BuildShowroomL3(Transform g, Mats m, string name, bool premium)
        {
            const float H = 10f;
            Solid(g, "Floor", new Vector3(0f, 0.02f, 18f), new Vector3(36f, 0.04f, 18f), m.platform);
            Deco(g, "FloorInlay", PrimitiveType.Cube, new Vector3(0f, 0.045f, 17f), new Vector3(30f, 0.01f, 0.3f), m.ledStrip);
            Solid(g, "Wall_North", new Vector3(0f, H / 2f, 27f), new Vector3(36.3f, H, 0.3f), m.cladding);
            // Glass sides and front between slim frames.
            for (float z = 10f; z <= 26.01f; z += 4f) { Solid(g, "SideFrameW", new Vector3(-18f, H / 2f, z), new Vector3(0.25f, H, 0.25f), m.frame); Solid(g, "SideFrameE", new Vector3(18f, H / 2f, z), new Vector3(0.25f, H, 0.25f), m.frame); }
            Glass(g, "SideGlassW", new Vector3(-18f, H / 2f, 18f), new Vector3(0.06f, H, 18f), m.darkGlass);
            Glass(g, "SideGlassE", new Vector3(18f, H / 2f, 18f), new Vector3(0.06f, H, 18f), m.darkGlass);
            foreach (float x in new[] { -18f, -12f, -6f, -3f, 3f, 6f, 12f, 18f }) Solid(g, "FrontFrame", new Vector3(x, H / 2f, 9f), new Vector3(0.22f, H, 0.3f), m.frame);
            foreach (var (x, w) in new[] { (-15f, 5.8f), (-9f, 5.8f), (-4.5f, 2.8f), (4.5f, 2.8f), (9f, 5.8f), (15f, 5.8f) })
                Glass(g, "FrontGlass", new Vector3(x, H / 2f, 9f), new Vector3(w, H, 0.06f), m.darkGlass);
            Glass(g, "DoorTransom", new Vector3(0f, (5.2f + H) / 2f, 9f), new Vector3(6f, H - 5.2f, 0.06f), m.darkGlass);
            // White "blade" frame wrapping the box, LED edge, rooftop letters.
            Solid(g, "BladeTop", new Vector3(0f, H + 0.5f, 18f), new Vector3(38f, 1f, 20f), m.flagWhite);
            Solid(g, "BladeW", new Vector3(-19f, H / 2f, 18f), new Vector3(1.2f, H + 1f, 20f), m.flagWhite);
            Solid(g, "BladeE", new Vector3(19f, H / 2f, 18f), new Vector3(1.2f, H + 1f, 20f), m.flagWhite);
            Deco(g, "LedEdge", PrimitiveType.Cube, new Vector3(0f, H, 7.98f), new Vector3(38f, 0.1f, 0.06f), m.ledStrip);
            Deco(g, "RoofSignBack", PrimitiveType.Cube, new Vector3(0f, H + 2.2f, 10f), new Vector3(16f, 2.4f, 0.3f), premium ? m.goldTrim : m.signBoard);
            SignText(g, new Vector3(0f, H + 2.25f, 9.82f), name.ToUpper(), 20f, premium ? new Color(1f, 0.9f, 0.55f) : Color.white, new Vector2(15f, 2f));
            SignText(g, new Vector3(0f, H - 0.6f, 8.9f), premium ? "PREMIUM  ·  FLAGSHIP" : "FLAGSHIP SHOWROOM", 6f, new Color(0.6f, 0.9f, 1f), new Vector2(14f, 0.8f));
            // Inside: six platforms, mezzanine with offices, central feature, reception, lounge.
            foreach (var p in new[] { new Vector3(-6f, 0f, 17f), new Vector3(6f, 0f, 17f), new Vector3(-12f, 0f, 14f), new Vector3(12f, 0f, 14f), new Vector3(-12f, 0f, 22f), new Vector3(12f, 0f, 22f) })
            {
                Solid(g, "DisplayPlatform", p + new Vector3(0f, 0.08f, 0f), new Vector3(5.4f, 0.06f, 5.4f), m.platform);
                Deco(g, "PlatformGlow", PrimitiveType.Cylinder, p + new Vector3(0f, 0.05f, 0f), new Vector3(5.8f, 0.02f, 5.8f), premium ? m.lampGlow : m.ringGlow);
                PointLight(g, p + new Vector3(0f, 6f, 0f), 9f, 1.4f, new Color(1f, 0.97f, 0.92f));
            }
            Solid(g, "Mezzanine", new Vector3(0f, 5f, 25.2f), new Vector3(35.6f, 0.3f, 3.6f), m.flagWhite);
            Deco(g, "MezzRail", PrimitiveType.Cube, new Vector3(0f, 5.65f, 23.4f), new Vector3(35.6f, 1f, 0.05f), m.windowGlass);
            WindowRow(g, 26.8f, 7f, -16f, 16f, 3.4f, 2f, 0.8f, m.windowsGlow);
            Solid(g, "CoffeeBar", new Vector3(14.5f, 0.55f, 19f), new Vector3(0.9f, 1.1f, 3.4f), m.timber);
            for (int ix = -2; ix <= 2; ix++) Deco(g, "CeilingLine", PrimitiveType.Cube, new Vector3(ix * 7f, H - 0.03f, 18f), new Vector3(0.25f, 0.04f, 16f), m.ceilingLight);
            PointLight(g, new Vector3(0f, 8.5f, 18f), 24f, 1.8f, new Color(1f, 0.97f, 0.92f));
            AddReflectionProbe(g, "ShowroomReflections", new Vector3(0f, 5f, 18f), new Vector3(36f, H + 0.2f, 18.2f));
            FurnishShowroom(g, m, 3, premium);   // phase 13c
            // Forecourt: water feature planters, tall totem, flag row.
            foreach (float x in new[] { -10f, 10f })
            {
                Solid(g, "Pool", new Vector3(x, 0.2f, 7.4f), new Vector3(5f, 0.4f, 1.6f), m.concrete);
                Deco(g, "Water", PrimitiveType.Cube, new Vector3(x, 0.41f, 7.4f), new Vector3(4.7f, 0.02f, 1.3f), m.water);
            }
            Solid(g, "Totem", new Vector3(-17f, 4.5f, -20f), new Vector3(1.6f, 9f, 0.8f), m.flagWhite);
            Deco(g, "TotemPanel", PrimitiveType.Cube, new Vector3(-17f, 6.5f, -20.42f), new Vector3(1.4f, 4f, 0.04f), premium ? m.goldTrim : m.accentPanel);
            SignText(g, new Vector3(-17f, 6.5f, -20.46f), name.ToUpper(), 8f, Color.white, new Vector2(1.3f, 3.6f));
            Deco(g, "TotemLed", PrimitiveType.Cube, new Vector3(-17f, 2.5f, -20.42f), new Vector3(1.4f, 2f, 0.04f), m.ledStrip);
            for (int i = 0; i < 6; i++)
            {
                float x = -12.5f + i * 5f;
                Solid(g, "FlagPole", new Vector3(x, 3.5f, -20.6f), new Vector3(0.1f, 7f, 0.1f), m.metal);
                Deco(g, "Flag", PrimitiveType.Cube, new Vector3(x + 0.6f, 6.3f, -20.6f), new Vector3(1.2f, 0.8f, 0.03f), i % 2 == 0 ? (premium ? m.goldTrim : m.flagBlue) : m.flagWhite);
            }
        }

        // ------------------------------------------------------------------ SERVICE CENTRE
        /// <summary>Level 2: taller shed, wider walls, reception office on the side, bigger signs.</summary>
        static void BuildServiceCentreL2(Transform g, Mats m)
        {
            Solid(g, "FloorExt", new Vector3(0f, 0.021f, 9f), new Vector3(28f, 0.04f, 18f), m.concrete);
            Solid(g, "Wall_Back", new Vector3(0f, 3.25f, 18f), new Vector3(28.3f, 6.5f, 0.3f), m.brick);
            Solid(g, "Wall_W", new Vector3(-14f, 3.25f, 9f), new Vector3(0.3f, 6.5f, 18f), m.brick);
            Solid(g, "Wall_E", new Vector3(14f, 3.25f, 9f), new Vector3(0.3f, 6.5f, 18f), m.brick);
            foreach (float x in new[] { -13.7f, -4f, 4f, 13.7f }) Solid(g, "Pillar", new Vector3(x, 3.25f, 0f), new Vector3(0.7f, 6.5f, 0.4f), m.brick);
            Solid(g, "Header", new Vector3(0f, 6f, 0f), new Vector3(28.3f, 1.2f, 0.4f), m.cladding);
            PitchedRoof(g, "Roof", new Vector3(0f, 6.6f, 9f), 28.6f, 18.6f, 2.2f, m.roof);
            FrontSign(g, 26f, 0f, 7.6f, "AUTO WORLD SERVICE CENTRE", Color.white, m.accentPanel);
            SignText(g, new Vector3(0f, 6f, -0.25f), "SERVICING  ·  TYRES  ·  MOT  ·  REPAIRS", 5f, new Color(1f, 0.85f, 0.3f), new Vector2(24f, 0.8f));
            // Reception office on the east side.
            Solid(g, "Office", new Vector3(17.5f, 1.8f, 7f), new Vector3(6.6f, 3.6f, 10f), m.wall);
            Solid(g, "OfficeRoof", new Vector3(17.5f, 3.75f, 7f), new Vector3(7f, 0.3f, 10.4f), m.roof);
            Glass(g, "OfficeGlass", new Vector3(17.5f, 1.7f, 1.97f), new Vector3(5f, 2.2f, 0.06f), m.windowGlass);
            SignText(g, new Vector3(17.5f, 3.2f, 1.9f), "RECEPTION", 4f, new Color(0.15f, 0.25f, 0.45f), new Vector2(5f, 0.6f));
            for (int i = 0; i < 3; i++) Deco(g, "SkyLight", PrimitiveType.Cube, new Vector3(-8f + i * 8f, 7.9f, 9f), new Vector3(3f, 0.05f, 6f), m.windowsGlow);
            PointLight(g, new Vector3(0f, 5.8f, 9f), 22f, 1.6f, new Color(0.95f, 0.97f, 1f));
        }

        /// <summary>Level 3: modern clad workshop with a glazed band, canopy over the apron, two-storey glass office, solar roof, EV chargers.</summary>
        static void BuildServiceCentreL3(Transform g, Mats m)
        {
            Solid(g, "FloorExt", new Vector3(0f, 0.021f, 9.5f), new Vector3(30f, 0.04f, 19f), m.floor);
            Solid(g, "Wall_Back", new Vector3(0f, 4f, 19f), new Vector3(30.3f, 8f, 0.3f), m.cladding);
            Solid(g, "Wall_W", new Vector3(-15f, 4f, 9.5f), new Vector3(0.3f, 8f, 19f), m.cladding);
            Solid(g, "Wall_E", new Vector3(15f, 4f, 9.5f), new Vector3(0.3f, 8f, 19f), m.cladding);
            foreach (float x in new[] { -14.7f, -4f, 4f, 14.7f }) Solid(g, "Pillar", new Vector3(x, 3f, 0f), new Vector3(0.6f, 6f, 0.5f), m.flagWhite);
            Glass(g, "GlazedBand", new Vector3(0f, 7f, 0f), new Vector3(30.3f, 2f, 0.1f), m.darkGlass);
            Solid(g, "Roof", new Vector3(0f, 8.15f, 9.5f), new Vector3(30.8f, 0.3f, 19.8f), m.cladding);
            for (int i = 0; i < 4; i++)
            {
                var panel = Deco(g, "SolarPanel", PrimitiveType.Cube, new Vector3(-10.5f + i * 7f, 8.7f, 11f), new Vector3(6f, 0.08f, 12f), m.solar);
                panel.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            }
            Deco(g, "LedFascia", PrimitiveType.Cube, new Vector3(0f, 6.05f, -0.28f), new Vector3(30.3f, 0.1f, 0.05f), m.ledStrip);
            FrontSign(g, 22f, -0.1f, 7f, "AUTO WORLD SERVICE CENTRE", new Color(0.7f, 0.92f, 1f), m.signBoard);
            // Canopy over the apron, posts between the lanes.
            Solid(g, "Canopy", new Vector3(0f, 5.6f, -2.5f), new Vector3(30f, 0.3f, 5f), m.flagWhite);
            Deco(g, "CanopyLed", PrimitiveType.Cube, new Vector3(0f, 5.42f, -4.95f), new Vector3(30f, 0.06f, 0.05f), m.ledStrip);
            foreach (float x in new[] { -14.5f, -4.6f, 4.6f, 14.5f }) Solid(g, "CanopyPost", new Vector3(x, 2.8f, -4.6f), new Vector3(0.25f, 5.6f, 0.25f), m.metal);
            // Glass office, two floors.
            Solid(g, "OfficeFrame", new Vector3(19f, 3.5f, 8f), new Vector3(7.6f, 7f, 12f), m.cladding);
            Glass(g, "OfficeGlassLow", new Vector3(19f, 1.7f, 1.97f), new Vector3(6.8f, 2.6f, 0.06f), m.darkGlass);
            Glass(g, "OfficeGlassHigh", new Vector3(19f, 5.2f, 1.97f), new Vector3(6.8f, 2.6f, 0.06f), m.darkGlass);
            SignText(g, new Vector3(19f, 3.4f, 1.9f), "RECEPTION  ·  PARTS", 4f, Color.white, new Vector2(6.6f, 0.6f));
            foreach (float x in new[] { -13f, 13f })
            {
                Solid(g, "EvCharger", new Vector3(x, 0.8f, -6.5f), new Vector3(0.5f, 1.6f, 0.35f), m.flagWhite);
                Deco(g, "EvScreen", PrimitiveType.Cube, new Vector3(x, 1.2f, -6.69f), new Vector3(0.36f, 0.4f, 0.02f), m.ledStrip);
            }
            for (int i = 0; i < 4; i++) Deco(g, "CeilingLine", PrimitiveType.Cube, new Vector3(-10.5f + i * 7f, 7.95f, 9.5f), new Vector3(0.3f, 0.04f, 16f), m.ceilingLight);
            PointLight(g, new Vector3(0f, 7f, 9f), 24f, 1.8f, new Color(0.95f, 0.97f, 1f));
        }

        // ------------------------------------------------------------------ HOMES
        /// <summary>Levels 2-3 of each home (front faces the street at z = 3, the door stays where it is).</summary>
        static void BuildHomeLevel(Transform g, Mats m, int tier, int level, Vector3 door)
        {
            if (tier == 0 && level == 2)
            {
                // Timber cabin with a porch.
                Solid(g, "Cabin", new Vector3(0f, 1.6f, 6.6f), new Vector3(11f, 3.2f, 7f), m.timber);
                PitchedRoof(g, "CabinRoof", new Vector3(0f, 3.2f, 6.6f), 11f, 7.2f, 2f, m.roof);
                Solid(g, "Porch", new Vector3(door.x, 0.15f, 2.3f), new Vector3(3.4f, 0.3f, 1.6f), m.timber);
                Solid(g, "PorchRoof", new Vector3(door.x, 2.7f, 2.3f), new Vector3(3.6f, 0.15f, 1.8f), m.roof);
                foreach (float dx in new[] { -1.6f, 1.6f }) Solid(g, "PorchPost", new Vector3(door.x + dx, 1.35f, 1.6f), new Vector3(0.15f, 2.7f, 0.15f), m.timber);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.1f, 3.07f), new Vector3(1.1f, 2.1f, 0.06f), m.desk);
                foreach (float x in new[] { -3.5f, -0.8f }) Deco(g, "Window", PrimitiveType.Cube, new Vector3(x, 1.8f, 3.07f), new Vector3(1.3f, 1f, 0.05f), m.windowGlass);
                Solid(g, "Chimney", new Vector3(-4f, 4.6f, 7.5f), new Vector3(0.8f, 2.4f, 0.8f), m.brick);
            }
            else if (tier == 0)
            {
                // Two-storey family house with an attached garage on the driveway side.
                Solid(g, "House", new Vector3(1f, 3f, 7f), new Vector3(10f, 6f, 8f), m.brick);
                PitchedRoof(g, "HouseRoof", new Vector3(1f, 6f, 7f), 10f, 8.2f, 2.6f, m.roof);
                Solid(g, "Garage", new Vector3(-8.5f, 1.6f, 6.5f), new Vector3(5f, 3.2f, 7f), m.brick);
                Solid(g, "GarageRoof", new Vector3(-8.5f, 3.3f, 6.5f), new Vector3(5.3f, 0.2f, 7.3f), m.roof);
                Deco(g, "GarageDoor", PrimitiveType.Cube, new Vector3(-8.5f, 1.3f, 2.97f), new Vector3(3.6f, 2.5f, 0.06f), m.flagWhite);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.15f, 2.97f), new Vector3(1.1f, 2.2f, 0.06f), m.flagBlue);
                WindowRow(g, 2.97f, 4.4f, -3.5f, 5.5f, 1.5f, 1.2f, 1f, m.windowGlass);
                Deco(g, "Window", PrimitiveType.Cube, new Vector3(-1.5f, 1.7f, 2.97f), new Vector3(2.4f, 1.3f, 0.05f), m.windowGlass);
                Solid(g, "Porch", new Vector3(door.x, 2.5f, 2.4f), new Vector3(2.4f, 0.15f, 1.3f), m.roof);
                foreach (var p in new[] { new Vector3(-3f, 0f, 1.8f), new Vector3(5.5f, 0f, 1.8f) }) Plant(g, p, m, 0.8f);
            }
            else if (tier == 1 && level == 2)
            {
                // The house extended: wider, a double garage, conservatory and dormers.
                Solid(g, "House", new Vector3(-1f, 3f, 7f), new Vector3(13f, 6f, 8f), m.wall);
                PitchedRoof(g, "HouseRoof", new Vector3(-1f, 6f, 7f), 13f, 8.2f, 2.6f, m.flagRed);
                foreach (float x in new[] { -4f, 1f }) { Solid(g, "Dormer", new Vector3(x, 6.6f, 5.3f), new Vector3(1.8f, 1.4f, 1.4f), m.wall); Deco(g, "DormerWindow", PrimitiveType.Cube, new Vector3(x, 6.6f, 4.58f), new Vector3(1.2f, 0.8f, 0.05f), m.windowGlass); }
                Solid(g, "Garage", new Vector3(9.5f, 1.75f, 6f), new Vector3(7f, 3.5f, 6f), m.wall);
                Deco(g, "GarageDoor", PrimitiveType.Cube, new Vector3(9.5f, 1.4f, 2.97f), new Vector3(5.6f, 2.6f, 0.06f), m.metal);
                Glass(g, "Conservatory", new Vector3(-5f, 1.5f, 12.6f), new Vector3(5f, 3f, 3.2f), m.windowGlass);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.2f, 2.97f), new Vector3(1.2f, 2.3f, 0.06f), m.desk);
                foreach (var p in new[] { new Vector3(-5f, 1.8f, 2.97f), new Vector3(-5f, 4.4f, 2.97f), new Vector3(-1.5f, 4.4f, 2.97f), new Vector3(4.5f, 4.4f, 2.97f) })
                    Deco(g, "Window", PrimitiveType.Cube, p, new Vector3(1.6f, 1.2f, 0.05f), m.windowGlass);
            }
            else if (tier == 1)
            {
                // Modern flat-roofed home: two offset cubes, big glazing, garage, pool behind.
                Solid(g, "Lower", new Vector3(-1f, 1.7f, 7.5f), new Vector3(14f, 3.4f, 9f), m.flagWhite);
                Solid(g, "Upper", new Vector3(1.5f, 5.1f, 6.5f), new Vector3(10f, 3.4f, 7f), m.cladding);
                Glass(g, "LowerGlass", new Vector3(-4f, 1.6f, 2.97f), new Vector3(7f, 2.8f, 0.06f), m.darkGlass);
                Glass(g, "UpperGlass", new Vector3(1.5f, 5.1f, 2.97f), new Vector3(9f, 2.6f, 0.06f), m.darkGlass);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.2f, 2.97f), new Vector3(1.3f, 2.4f, 0.06f), m.timber);
                Solid(g, "Garage", new Vector3(9f, 1.6f, 6.5f), new Vector3(6f, 3.2f, 7f), m.cladding);
                Deco(g, "GarageDoor", PrimitiveType.Cube, new Vector3(9f, 1.35f, 2.97f), new Vector3(4.6f, 2.5f, 0.06f), m.timber);
                Solid(g, "PoolEdge", new Vector3(-2f, 0.15f, 16f), new Vector3(10f, 0.3f, 4.6f), m.concrete);
                Deco(g, "Pool", PrimitiveType.Cube, new Vector3(-2f, 0.31f, 16f), new Vector3(9.4f, 0.02f, 4f), m.water);
                Deco(g, "Led", PrimitiveType.Cube, new Vector3(1.5f, 3.42f, 2.94f), new Vector3(10f, 0.06f, 0.04f), m.ledStrip);
            }
            else if (level == 2)
            {
                // Villa with a new wing, a terrace and a bigger pool.
                Solid(g, "VillaBase", new Vector3(0f, 2f, 8f), new Vector3(18f, 4f, 10f), m.flagWhite);
                Solid(g, "VillaUpper", new Vector3(-3f, 5.8f, 9f), new Vector3(11f, 3.6f, 8f), m.flagWhite);
                Solid(g, "Wing", new Vector3(-13f, 2f, 9f), new Vector3(8f, 4f, 8f), m.flagWhite);
                Glass(g, "WingGlass", new Vector3(-13f, 2f, 4.97f), new Vector3(6.6f, 3f, 0.06f), m.windowGlass);
                Deco(g, "Glass1", PrimitiveType.Cube, new Vector3(-4f, 2f, 2.97f), new Vector3(7f, 3f, 0.06f), m.windowGlass);
                Deco(g, "Glass2", PrimitiveType.Cube, new Vector3(-3f, 5.8f, 4.97f), new Vector3(9f, 2.6f, 0.06f), m.windowGlass);
                Solid(g, "Terrace", new Vector3(-3f, 7.7f, 9f), new Vector3(11.4f, 0.2f, 8.4f), m.timber);
                Deco(g, "TerraceRail", PrimitiveType.Cube, new Vector3(-3f, 8.3f, 4.85f), new Vector3(11.4f, 1f, 0.05f), m.windowGlass);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.3f, 2.97f), new Vector3(1.6f, 2.6f, 0.06f), m.desk);
                Solid(g, "PoolEdge", new Vector3(-6f, 0.15f, 17f), new Vector3(14f, 0.3f, 5f), m.concrete);
                Deco(g, "Pool", PrimitiveType.Cube, new Vector3(-6f, 0.31f, 17f), new Vector3(13.4f, 0.02f, 4.4f), m.water);
            }
            else
            {
                // Mansion: three storeys, columned portico, two wings, fountain and a long pool.
                Solid(g, "Centre", new Vector3(0f, 4.5f, 9f), new Vector3(14f, 9f, 11f), m.flagWhite);
                foreach (float x in new[] { -12f, 12f }) Solid(g, "Wing", new Vector3(x, 3f, 10f), new Vector3(10f, 6f, 9f), m.flagWhite);
                Solid(g, "Portico", new Vector3(0f, 6.2f, 2.2f), new Vector3(8f, 0.5f, 3f), m.flagWhite);
                foreach (float x in new[] { -3.4f, -1.2f, 1.2f, 3.4f }) Solid(g, "Column", new Vector3(x + 2f, 3f, 1f), new Vector3(0.5f, 6f, 0.5f), m.flagWhite);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(door.x, 1.5f, 3.47f), new Vector3(2f, 3f, 0.06f), m.desk);
                for (int f = 0; f < 3; f++) WindowRow(g, 3.47f, 1.6f + f * 3f, -6.5f, 6.5f, 1.3f, 1.8f, 1.1f, m.windowGlass);
                foreach (float x in new[] { -12f, 12f }) for (int f = 0; f < 2; f++) WindowRow(g, 5.47f, 1.6f + f * 3f, x - 4.5f, x + 4.5f, 1.3f, 1.8f, 1f, m.windowGlass);
                Solid(g, "RoofTrim", new Vector3(0f, 9.1f, 9f), new Vector3(14.6f, 0.3f, 11.6f), m.goldTrim);
                Solid(g, "Fountain", new Vector3(-8f, 0.3f, -1f), new Vector3(3f, 0.6f, 3f), m.concrete);
                Deco(g, "FountainWater", PrimitiveType.Cylinder, new Vector3(-8f, 0.62f, -1f), new Vector3(2.6f, 0.02f, 2.6f), m.water);
                Solid(g, "PoolEdge", new Vector3(0f, 0.15f, 19f), new Vector3(20f, 0.3f, 5f), m.concrete);
                Deco(g, "Pool", PrimitiveType.Cube, new Vector3(0f, 0.31f, 19f), new Vector3(19.4f, 0.02f, 4.4f), m.water);
                foreach (float x in new[] { -17f, 17f }) Tree(g, new Vector3(x, 0f, 2f), m, 1.2f);
            }
        }

        // ------------------------------------------------------------------ INVESTMENTS
        static void BuildInvestmentLevel(Transform g, Mats m, string propertyId, Vector3 size, string sign, int level)
        {
            if (level == 2)
            {
                // Refurbished and extended: wider, half as tall again, new cladding, roof terrace.
                var s = new Vector3(size.x * 1.15f, size.y * 1.5f, size.z * 1.1f);
                Solid(g, "Building", new Vector3(0f, s.y / 2f, s.z / 2f + 2f), s, m.cladding);
                int floors = Mathf.FloorToInt(s.y / 3.3f);
                for (int f = 0; f < floors; f++) Deco(g, "Windows", PrimitiveType.Cube, new Vector3(0f, f * 3.3f + 1.8f, 1.95f), new Vector3(s.x * 0.9f, 1.5f, 0.06f), m.windowsGlow);
                Glass(g, "Entrance", new Vector3(0f, 1.5f, 1.92f), new Vector3(5f, 3f, 0.08f), m.windowGlass);
                Solid(g, "EntranceCanopy", new Vector3(0f, 3.3f, 0.8f), new Vector3(7f, 0.25f, 2.4f), m.flagWhite);
                Deco(g, "RoofTerraceRail", PrimitiveType.Cube, new Vector3(0f, s.y + 0.5f, 2.2f), new Vector3(s.x, 1f, 0.05f), m.windowGlass);
                Deco(g, "SignBoard", PrimitiveType.Cube, new Vector3(0f, s.y + 1.6f, 2.3f), new Vector3(s.x * 0.7f, 1.8f, 0.3f), m.signBoard);
                SignText(g, new Vector3(0f, s.y + 1.6f, 2.1f), sign, 12f, Color.white, new Vector2(s.x * 0.65f, 1.5f));
            }
            else
            {
                // Landmark rebuild: glass tower with a lit crown and a stepped podium.
                var s = new Vector3(size.x * 1.2f, size.y * 2.2f, size.z * 1.15f);
                Solid(g, "Podium", new Vector3(0f, 3f, s.z / 2f + 2f), new Vector3(s.x * 1.3f, 6f, s.z * 1.1f), m.flagWhite);
                Solid(g, "Tower", new Vector3(0f, s.y / 2f + 3f, s.z / 2f + 3f), new Vector3(s.x, s.y, s.z * 0.85f), m.darkGlass);
                for (float y = 6f; y < s.y + 3f; y += 3.3f) Deco(g, "FloorLine", PrimitiveType.Cube, new Vector3(0f, y, 2.2f + s.z * 0.075f), new Vector3(s.x + 0.1f, 0.15f, 0.08f), m.frame);
                for (float x = -s.x / 2f; x <= s.x / 2f + 0.01f; x += s.x / 6f) Deco(g, "Mullion", PrimitiveType.Cube, new Vector3(x, s.y / 2f + 3f, 2.2f + s.z * 0.075f), new Vector3(0.12f, s.y, 0.1f), m.frame);
                Solid(g, "Crown", new Vector3(0f, s.y + 4.5f, s.z / 2f + 3f), new Vector3(s.x * 0.8f, 3f, s.z * 0.6f), m.cladding);
                Deco(g, "CrownLed", PrimitiveType.Cube, new Vector3(0f, s.y + 6.05f, s.z / 2f + 3f), new Vector3(s.x * 0.8f, 0.12f, s.z * 0.6f), m.ledStrip);
                Glass(g, "Lobby", new Vector3(0f, 2f, 1.6f), new Vector3(8f, 4f, 0.08f), m.windowGlass);
                Deco(g, "SignBoard", PrimitiveType.Cube, new Vector3(0f, 5.2f, 1.5f), new Vector3(s.x * 0.8f, 1.2f, 0.2f), m.goldTrim);
                SignText(g, new Vector3(0f, 5.2f, 1.36f), sign, 10f, Color.white, new Vector2(s.x * 0.75f, 1f));
            }
        }

        static void BuildParkingGarageLevel(Transform g, Mats m, int level)
        {
            int decks = level == 2 ? 6 : 8;
            float width = level == 2 ? 20f : 22f;
            for (int d = 0; d < decks; d++)
            {
                Solid(g, "Deck", new Vector3(0f, d * 3.3f + 0.15f, 9f), new Vector3(width, 0.3f, 14f), m.concrete);
                Deco(g, "Barrier", PrimitiveType.Cube, new Vector3(0f, d * 3.3f + 0.75f, 2.1f), new Vector3(width, 0.9f, 0.15f), level == 3 ? m.cladding : m.concrete);
            }
            foreach (float x in new[] { -width / 2f + 0.5f, 0f, width / 2f - 0.5f })
                foreach (float z in new[] { 2.5f, 15.5f })
                    Solid(g, "Column", new Vector3(x, decks * 1.65f, z), new Vector3(0.6f, decks * 3.3f, 0.6f), m.concrete);
            Solid(g, "Ramp", new Vector3(width / 2f + 2.2f, 1.7f, 9f), new Vector3(4f, 0.3f, 14f), m.asphalt).transform.localRotation = Quaternion.Euler(-13f, 0f, 0f);
            if (level == 3)
            {
                // Perforated screen facade, solar canopy on the roof, EV signs.
                for (float x = -width / 2f + 1f; x < width / 2f; x += 2f)
                    Deco(g, "ScreenFin", PrimitiveType.Cube, new Vector3(x, decks * 1.65f, 1.6f), new Vector3(0.12f, decks * 3.3f, 0.5f), m.goldTrim);
                for (int i = 0; i < 3; i++) Deco(g, "SolarCanopy", PrimitiveType.Cube, new Vector3(-7f + i * 7f, decks * 3.3f + 2.2f, 9f), new Vector3(6f, 0.1f, 12f), m.solar).transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                Deco(g, "EvSign", PrimitiveType.Cube, new Vector3(width / 2f - 2f, 2.5f, 1.4f), new Vector3(2.4f, 1.2f, 0.1f), m.ledStrip);
            }
            Deco(g, "Sign", PrimitiveType.Cube, new Vector3(-width / 2f + 2.5f, decks * 3.3f + 1.5f, 2f), new Vector3(3.4f, 3.4f, 0.3f), m.flagBlue);
            SignText(g, new Vector3(-width / 2f + 2.5f, decks * 3.3f + 1.5f, 1.8f), "P", 22f, Color.white, new Vector2(3f, 3f));
        }
    }
}
