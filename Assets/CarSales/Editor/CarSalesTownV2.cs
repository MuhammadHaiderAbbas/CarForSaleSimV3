using System.Linq;
using CarSales.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Builds the Harbour City town from HarbourPlan - the test-drive loop, avenues and
    /// roundabouts, pavements and street lamps, every site in its zone, the dressing of each zone (park and lake,
    /// town-centre terraces, industrial estate, car parks, signs) and the countryside round the loop.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static Vector3 W(V2 p, float y = 0f) => new Vector3(p.x, y, p.z);

        /// <summary>A site group at its plan position and rotation.</summary>
        static Transform PlanSiteRoot(Transform env, string id)
        {
            var s = Town.Site(id);
            return Site(env, id, W(s.Position), s.Yaw);
        }

        // ------------------------------------------------------------------ GROUND AND COUNTRYSIDE
        static void BuildGroundV2(Transform env, Mats m)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(env, false);
            ground.transform.localScale = new Vector3(150f, 1f, 90f);   // 1,500 x 900 m
            ground.GetComponent<Renderer>().sharedMaterial = m.ground;
            ground.isStatic = true;

            // The sea (Harbour City faces south): a flat steel-blue plane lapping the southern arc of the ring.
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            sea.transform.SetParent(env, false);
            sea.transform.position = new Vector3(0f, 0.005f, -350f);
            sea.transform.localScale = new Vector3(220f, 1f, 22f);   // 2,200 x 220 m, z -460..-240
            sea.GetComponent<Renderer>().sharedMaterial = m.water;
            sea.isStatic = true;

            // Farm fields and hedgerows outside the loop (seeded so Build Everything always makes the same countryside).
            var g = Group(env, "Countryside");
            var rng = new System.Random(12);
            Material[] crops = { m.fieldWheat, m.fieldSoil, m.ground, m.fieldWheat };
            for (float x = -720f; x < 720f; x += 130f)
                for (float z = -430f; z < 430f; z += 105f)
                {
                    var c = new V2(x + 65f, z + 52f);
                    if (InsideLoop(c, 40f)) continue;
                    if (c.z < -252f) continue;   // the sea lies south of the ring
                    var field = Deco(g, "Field", PrimitiveType.Cube, new Vector3(c.x, 0.004f, c.z), new Vector3(118f, 0.008f, 94f), crops[rng.Next(crops.Length)]);
                    field.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Deco(g, "Hedge", PrimitiveType.Cube, new Vector3(c.x, 0.8f, c.z - 49f), new Vector3(120f, 1.6f, 1.4f), m.leaves);
                    Deco(g, "Hedge", PrimitiveType.Cube, new Vector3(c.x - 61f, 0.8f, c.z), new Vector3(1.4f, 1.6f, 96f), m.leaves);
                    if (rng.NextDouble() < 0.35) Tree(g, new Vector3(c.x - 61f, 0f, c.z + (float)(rng.NextDouble() * 80 - 40)), m, 1.3f);
                }
        }

        /// <summary>Is the point inside the test-drive loop (with a margin)?</summary>
        static bool InsideLoop(V2 p, float margin = 0f)
        {
            bool inside = false;
            var ring = Town.Ring;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                if ((ring[i].z > p.z) != (ring[j].z > p.z) && p.x < (ring[j].x - ring[i].x) * (p.z - ring[i].z) / (ring[j].z - ring[i].z) + ring[i].x)
                    inside = !inside;
            if (inside) return true;
            for (int i = 0; i < ring.Length - 1; i++) if (Town.SegmentDistance(p, ring[i], ring[i + 1]) < margin) return true;
            return false;
        }

        // ------------------------------------------------------------------ ROADS
        static void BuildPlanRoads(Transform root, Mats m)
        {
            int k = 0;
            foreach (var r in Town.Roads)
            {
                var g = Group(root, r.Name);
                bool loop = r.Pavements == false;
                for (int i = 0; i < r.Points.Length - 1; i++, k++)
                {
                    V2 a = r.Points[i], b = r.Points[i + 1];
                    float len = V2.Distance(a, b);
                    if (len < 1f) continue;
                    var dir = (b - a).Normalized;
                    float ta = Town.IsRoundabout(a) ? Town.RoundaboutRadius : 0f, tb = Town.IsRoundabout(b) ? Town.RoundaboutRadius : 0f;
                    // Extend plain ends by half the width so bends and junctions have no gaps.
                    float ea = ta > 0f ? 0f : r.Width / 2f, eb = tb > 0f ? 0f : r.Width / 2f;
                    var start = a + dir * (ta - ea); var end = b - dir * (tb - eb);
                    float pieceLen = V2.Distance(start, end);
                    var piece = Group(g, "Segment");
                    piece.position = W((start + end) * 0.5f);
                    piece.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                    Solid(piece, "Asphalt", new Vector3(0f, 0.008f + (k % 7) * 0.0006f, 0f), new Vector3(r.Width, 0.016f, pieceLen), m.asphalt);
                    // Centre line (dashed in town, solid edge lines on the loop), kept clear of junctions.
                    float clear = 14f;
                    for (float d = -len / 2f + ta + clear; d < len / 2f - tb - clear; d += 12f)
                        Deco(piece, "CentreDash", PrimitiveType.Cube, new Vector3(0f, 0.022f, d - (ea - eb) / 2f + (ta - tb) / 2f), new Vector3(0.15f, 0.01f, 4f), m.line);
                    if (loop)
                        foreach (float side in new[] { -1f, 1f })
                            Deco(piece, "EdgeLine", PrimitiveType.Cube, new Vector3(side * (r.Width / 2f - 0.35f), 0.022f, 0f), new Vector3(0.12f, 0.01f, Mathf.Max(1f, pieceLen - 2f * r.Width)), m.line);
                }
            }

            // Roundabouts: asphalt circle, raised grass island with a kerb, a tree in the middle.
            foreach (var rb in Town.Roundabouts)
            {
                var g = Group(root, "Roundabout");
                g.position = W(rb.Centre);
                float outer = rb.Radius + 5.5f, island = rb.Radius - 5f;
                var disc = Deco(g, "Asphalt", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, 0f), new Vector3(outer * 2f, 0.012f, outer * 2f), m.asphalt);
                disc.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var kerb = Prim(PrimitiveType.Cylinder, "Kerb", g, new Vector3(0f, 0.1f, 0f), new Vector3(island * 2f + 0.6f, 0.1f, island * 2f + 0.6f), m.sidewalk);
                kerb.AddComponent<MeshCollider>();
                kerb.isStatic = true;
                Deco(g, "Island", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(island * 2f, 0.02f, island * 2f), m.ground);
                Tree(g, Vector3.zero, m, 1.2f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    Deco(g, "Bush", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * island * 0.6f, 0.5f, Mathf.Sin(a) * island * 0.6f), new Vector3(1.6f, 0.9f, 1.6f), m.leaves);
                }
            }

            // Pavements (kerbs never cross another road) and street lamps along them.
            var pav = Group(root, "Pavements");
            foreach (var (a, b) in Town.Pavements())
            {
                float len = V2.Distance(a, b);
                var dir = (b - a).Normalized;
                var piece = Group(pav, "Pavement");
                piece.position = W((a + b) * 0.5f);
                piece.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
                Solid(piece, "Kerb", new Vector3(0f, 0.06f, 0f), new Vector3(2.4f, 0.12f, len), m.sidewalk);
                for (float d = -len / 2f + 12f; d < len / 2f - 6f; d += 42f)
                {
                    var lamp = Group(piece, "LampSpot");
                    lamp.localPosition = new Vector3(0.9f, 0f, d);
                    var toRoad = Vector3.Cross(Vector3.up, piece.forward);   // left of the pavement direction
                    var p = lamp.position;
                    float side = Town.RoadClearance(new V2(p.x + toRoad.x * 3f, p.z + toRoad.z * 3f)) < Town.RoadClearance(new V2(p.x - toRoad.x * 3f, p.z - toRoad.z * 3f)) ? 1f : -1f;
                    lamp.rotation = Quaternion.LookRotation(toRoad * side);
                    StreetLamp(lamp, Vector3.zero, 0.8f, 1.4f, m);
                }
            }

            // Trees along the loop's verges.
            var trees = Group(root, "LoopTrees");
            var ring = Town.Ring;
            for (int i = 0; i < ring.Length - 1; i++)
                foreach (var (pt, _) in RoadEditing.Scatter(new RoadSegment(ring[i], ring[i + 1]), 30f, 11f, 18f))
                    if (Town.RoadClearance(pt) > 4f && !Town.Sites.Any(s => InBox(s.WorldBox(), pt, 4f)))
                        Tree(trees, W(pt), m, 1.1f);
        }

        static bool InBox((float x0, float z0, float x1, float z1) b, V2 p, float pad) => p.x > b.x0 - pad && p.x < b.x1 + pad && p.z > b.z0 - pad && p.z < b.z1 + pad;

        /// <summary>A road sign on a post (white or blue board).</summary>
        static void RoadSign(Transform parent, Vector3 pos, float yaw, string text, Material board, Color textColor, float width = 3.2f)
        {
            var g = Group(parent, "Sign");
            g.position = pos;
            g.rotation = Quaternion.Euler(0f, yaw, 0f);
            Solid(g, "Post", new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 2.5f, 0.12f), m_signPost);
            Deco(g, "Board", PrimitiveType.Cube, new Vector3(0f, 2.6f, 0f), new Vector3(width, 1.1f, 0.08f), board);
            SignText(g, new Vector3(0f, 2.6f, -0.06f), text, 4f, textColor, new Vector2(width - 0.2f, 1f));
        }
        static Material m_signPost;

        // ------------------------------------------------------------------ ZONE DRESSING
        static void BuildZoneDecor(Transform env, Mats m, bool finalMap = false)
        {
            m_signPost = m.metal;
            var root = Group(env, "Zones");

            // 13 Public park: lawns, cross paths, lake with a stone edge, bandstand, benches and trees.
            {
                var z = Town.Zone(13);
                var g = Group(root, "Zone13_Park");
                var c = z.Centre;
                Solid(g, "ParkLawn", new Vector3(c.x, 0.006f, c.z), new Vector3(z.X1 - z.X0, 0.012f, z.Z1 - z.Z0), m.ground);
                Solid(g, "PathEW", new Vector3(c.x, 0.03f, c.z), new Vector3(z.X1 - z.X0 - 4f, 0.03f, 3f), m.sidewalk);
                Solid(g, "PathNS", new Vector3(c.x + 60f, 0.03f, c.z), new Vector3(3f, 0.03f, z.Z1 - z.Z0 - 4f), m.sidewalk);
                var lake = new Vector3(c.x - 70f, 0f, c.z + 32f);
                Deco(g, "Lake", PrimitiveType.Cube, lake + new Vector3(0f, 0.05f, 0f), new Vector3(90f, 0.05f, 40f), m.water);
                foreach (var (o, s) in new[] { (new Vector3(0f, 0.25f, 20.5f), new Vector3(91f, 0.5f, 1f)), (new Vector3(0f, 0.25f, -20.5f), new Vector3(91f, 0.5f, 1f)),
                                               (new Vector3(45.5f, 0.25f, 0f), new Vector3(1f, 0.5f, 42f)), (new Vector3(-45.5f, 0.25f, 0f), new Vector3(1f, 0.5f, 42f)) })
                    Solid(g, "LakeEdge", lake + o, s, m.concrete);
                var band = new Vector3(c.x + 60f, 0f, c.z + 30f);
                Solid(g, "BandstandBase", band + new Vector3(0f, 0.3f, 0f), new Vector3(8f, 0.6f, 8f), m.concrete);
                foreach (var o in new[] { new Vector3(-3.5f, 0f, -3.5f), new Vector3(3.5f, 0f, -3.5f), new Vector3(-3.5f, 0f, 3.5f), new Vector3(3.5f, 0f, 3.5f) })
                    Solid(g, "BandstandPost", band + o + new Vector3(0f, 2f, 0f), new Vector3(0.2f, 3f, 0.2f), m.flagWhite);
                Deco(g, "BandstandRoof", PrimitiveType.Cylinder, band + new Vector3(0f, 3.7f, 0f), new Vector3(9.5f, 0.4f, 9.5f), m.flagRed);
                var rng = new System.Random(13);
                for (int i = 0; i < 70; i++)
                {
                    var p = new Vector3(Mathf.Lerp(z.X0 + 6f, z.X1 - 6f, (float)rng.NextDouble()), 0f, Mathf.Lerp(z.Z0 + 6f, z.Z1 - 6f, (float)rng.NextDouble()));
                    if (Mathf.Abs(p.x - lake.x) < 52f && Mathf.Abs(p.z - lake.z) < 26f) continue;
                    if (Mathf.Abs(p.z - c.z) < 4f || Mathf.Abs(p.x - (c.x + 60f)) < 4f || (p - band).magnitude < 9f) continue;
                    Tree(g, p, m, 0.9f + (float)rng.NextDouble() * 0.5f);
                }
                for (int i = 0; i < 8; i++)
                {
                    var bench = Slot(g, "Bench", new Vector3(z.X0 + 30f + i * 38f, 0f, c.z - 3.2f), PlaceholderKind.Bench, new Vector3(1.8f, 1f, 0.6f), 180f);
                    Solid(bench, "BenchSeat", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.08f, 0.5f), m.desk);
                    Deco(bench, "BenchBack", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.25f), new Vector3(1.8f, 0.45f, 0.06f), m.desk);
                }
                AddMapPlace(g.gameObject, "Public Park", "G", new Color(0.3f, 0.75f, 0.35f), new Vector3(c.x, 0.1f, z.Z0 - 3f));
            }

            // 10 Town centre: a short terrace of shops in the gap between the Office Tower and the Parking Garage, facing Harbour Boulevard.
            {
                var g = Group(root, "Zone10_TownCentre");
                string[] shops = { "CAFE", "BAKERY", "BANK", "PHARMACY" };
                var rng = new System.Random(10);
                Solid(g, "Forecourt", new Vector3(-40f, 0.012f, -40f), new Vector3(52f, 0.024f, 12f), m.asphalt);
                int n = 0;
                for (float x = -62f; x <= -18f; x += 13f)
                {
                    float h = 7f + (float)rng.NextDouble() * 3.5f;
                    Building(g, new Vector3(x, 0f, -52f), new Vector3(12f, h, 10f), m.buildings[n % m.buildings.Length], m, false);
                    Deco(g, "Shopfront", PrimitiveType.Cube, new Vector3(x, 1.5f, -46.94f), new Vector3(10f, 2.6f, 0.06f), m.windowGlass);
                    SignText(g, new Vector3(x, 3.4f, -46.9f), shops[n % shops.Length], 4f, Color.white, new Vector2(9f, 0.7f), 180f);
                    n++;
                }
                AddMapPlace(g.gameObject, "Town Centre", "T", UIFactory.Info, new Vector3(-40f, 0.1f, -52f));
            }

            // 11 Industrial estate: warehouses, a fenced vehicle store and parked vans.
            {
                var g = Group(root, "Zone11_Industrial");
                foreach (var (x, z, w, d, h) in new[] { (262f, 124f, 38f, 22f, 9f), (330f, 124f, 40f, 22f, 10f), (398f, 124f, 36f, 22f, 8f), (280f, 180f, 60f, 30f, 11f), (372f, 180f, 52f, 30f, 10f) })
                {
                    Building(g, new Vector3(x, 0f, z), new Vector3(w, h, d), m.cladding, m, true);
                    Deco(g, "RollerDoor", PrimitiveType.Cube, new Vector3(x, 2.5f, z - d / 2f - 0.07f), new Vector3(6f, 5f, 0.06f), m.metal);
                }
                Solid(g, "Yard", new Vector3(325f, 0.012f, 147f), new Vector3(190f, 0.024f, 16f), m.concrete);
                for (int i = 0; i < 6; i++)
                {
                    float x = 245f + i * 30f;
                    Solid(g, "Van", new Vector3(x, 1.2f, 147f), new Vector3(2.2f, 2.4f, 5.6f), i % 2 == 0 ? m.flagWhite : m.flagBlue);
                }
                RoadSign(g, new Vector3(238f, 0f, 100f), 0f, "INDUSTRIAL ESTATE", m.accentPanel, Color.white, 4.4f);
                AddMapPlace(g.gameObject, "Industrial Estate", "I", UIFactory.Neutral, new Vector3(322f, 0.1f, 140f));
            }

            // 3 Auction (Dockside): customer car park west of the market, outdoor vehicle storage further west, fence along the yard.
            {
                var g = Group(root, "Zone3_AuctionLots");
                Solid(g, "AuctionParking", new Vector3(-385f, 0.012f, -112f), new Vector3(46f, 0.024f, 24f), m.asphalt);
                for (int i = 0; i < 9; i++) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(-405f + i * 5f, 0.026f, -112f), new Vector3(0.12f, 0.01f, 19f), m.line);
                Solid(g, "VehicleStorage", new Vector3(-375f, 0.012f, -60f), new Vector3(70f, 0.024f, 20f), m.asphalt);
                for (float x = -405f; x <= -345f; x += 5f) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(x, 0.026f, -60f), new Vector3(0.12f, 0.01f, 17f), m.line);
                for (float x = -408f; x <= -342f; x += 3f) Deco(g, "Fence", PrimitiveType.Cube, new Vector3(x, 0.9f, -49.5f), new Vector3(0.08f, 1.8f, 0.08f), m.metal);
                RoadSign(g, new Vector3(-289f, 0f, -85f), 180f, "AUCTION PARKING", m.flagBlue, Color.white);
            }

            // 2 Dealership: customer car park between AUTO WORLD and Harbour Boulevard.
            {
                var g = Group(root, "Zone2_CustomerParking");
                Solid(g, "Parking", new Vector3(20f, 0.012f, -13.5f), new Vector3(40f, 0.024f, 10f), m.asphalt);
                for (int i = 0; i < 8; i++) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(2f + i * 4.6f, 0.026f, -13.5f), new Vector3(0.12f, 0.01f, 8.5f), m.line);
                RoadSign(g, new Vector3(44f, 0f, -14.5f), 90f, "AUTO WORLD CUSTOMER PARKING", m.accentPanel, Color.white, 5f);
            }

            // 4/5 Inspection and body repair signage; 5 body shop building.
            {
                var g = Group(root, "Zone45_Services");
                RoadSign(g, new Vector3(-44f, 0f, 68f), 0f, "MOT · INSPECTION", m.flagBlue, Color.white);
                Building(g, new Vector3(170f, 0f, 118f), new Vector3(42f, 8f, 24f), m.buildings[2], m, true);
                SignText(g, new Vector3(170f, 6.5f, 105.9f), "BODY REPAIR & PAINT", 6f, Color.white, new Vector2(30f, 1.2f));
                Deco(g, "SprayDoor", PrimitiveType.Cube, new Vector3(160f, 2.5f, 105.93f), new Vector3(6f, 5f, 0.06f), m.metal);
            }

            // 12 Test-drive loop signs.
            {
                var g = Group(root, "Zone12_LoopSigns");
                var ring = Town.Ring;
                foreach (int i in new[] { 1, 5, 12, 16 })
                {
                    var a = ring[i]; var b = ring[i + 1];
                    var d = (b - a).Normalized;
                    var p = a + d * 20f + new V2(-d.z, d.x) * 8f;
                    float yaw = Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;
                    RoadSign(g, W(p), yaw, "TEST-DRIVE LOOP  ·  50", m.flagWhite, new Color(0.1f, 0.1f, 0.12f), 4.4f);
                }
            }

            // 1 Residential: hedges behind the homes.
            {
                var z = Town.Zone(1);
                var g = Group(root, "Zone1_Hedges");
                Deco(g, "Hedge", PrimitiveType.Cube, new Vector3((z.X0 + z.X1) / 2f, 0.7f, z.Z0 - 4f), new Vector3(z.X1 - z.X0 + 20f, 1.4f, 1.2f), m.leaves);
            }
        }
    }
}
