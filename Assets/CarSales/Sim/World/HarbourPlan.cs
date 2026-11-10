using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSales.Sim
{
    /// <summary>A named road of the town plan (a polyline through its junctions).</summary>
    public sealed class PlanRoad
    {
        public string Name;
        public float Width;
        public float SpeedLimit;      // m/s
        public bool Pavements = true;
        public V2[] Points;
    }

    /// <summary>One of the zones of the town plan.</summary>
    public sealed class PlanZone
    {
        public int Number;
        public string Name;
        public float X0, Z0, X1, Z1;
        public bool Contains(V2 p, float pad = 0f) => p.x >= X0 - pad && p.x <= X1 + pad && p.z >= Z0 - pad && p.z <= Z1 + pad;
        public V2 Centre => new V2((X0 + X1) / 2f, (Z0 + Z1) / 2f);
    }

    /// <summary>
    /// Where a site goes. Footprint is in the site's local space (x right, z forward/away from the road it faces);
    /// yaw 0 = the site faces south (its front is local -Z), 180 = it faces north.
    /// </summary>
    public sealed class PlanSite
    {
        public string Id;
        public string Name;
        public int Zone;
        public V2 Position;
        public float Yaw;
        public float FX0, FZ0, FX1, FZ1;

        public V2 ToWorld(float lx, float lz)
        {
            double r = Yaw * Math.PI / 180.0;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            // Unity yaw: local +Z turns towards +X as yaw grows.
            return new V2(Position.x + lx * c + lz * s, Position.z - lx * s + lz * c);
        }

        /// <summary>World-space bounding box of the footprint.</summary>
        public (float x0, float z0, float x1, float z1) WorldBox()
        {
            var pts = new[] { ToWorld(FX0, FZ0), ToWorld(FX1, FZ0), ToWorld(FX0, FZ1), ToWorld(FX1, FZ1) };
            return (pts.Min(p => p.x), pts.Min(p => p.z), pts.Max(p => p.x), pts.Max(p => p.z));
        }
    }

    public struct PlanRoundabout { public V2 Centre; public float Radius; public PlanRoundabout(float x, float z, float r = 15f) { Centre = new V2(x, z); Radius = r; } }

    /// <summary>
    /// "Harbour City" (CarForSaleSimV4): the one and only town of Car For Sale Simulator.
    /// A fictional port town — the ring road is called the Harbour Loop in-game, with a south-facing
    /// seafront, a docks quarter in the south-west, the dealership beachhead along Harbour Boulevard,
    /// the business park on Business Park Road and the two plots that are always under construction.
    /// Every coordinate was built and checked with tools/plan_check.py (the same maths as the EditMode
    /// tests): no site's world box touches a road, every entrance is within 14 m of one, no two sites
    /// overlap, and the whole road network is one connected town.
    /// This replaces TownPlan (V2) and FinalMapPlan (V3) — they were deleted; the POI programme
    /// is built into the one map now.
    /// </summary>
    public static class HarbourPlan
    {
        /// <summary>Saved with each game (4 = Harbour City).</summary>
        public const int Version = 4;
        public const float RoundaboutRadius = 15f;

        static V2 P(float x, float z) => new V2(x, z);

        // ------------------------------------------------------------------ ROADS
        /// <summary>The test-drive loop (zone 12): the outer ring of the town, with every junction on it.</summary>
        public static readonly V2[] Ring =
        {
            P(-452, 178), P(-336, 230), P(-124, 250), P(80, 238), P(256, 230), P(352, 210), P(462, 128), P(508, 22), P(472, -75),
            P(416, -162), P(308, -215), P(200, -221), P(166, -238), P(133, -242), P(0, -252), P(-152, -238), P(-276, -218), P(-376, -170), P(-462, -113), P(-500, -22),
            P(-505, 65), P(-496, 130), P(-454, 170), P(-452, 178),
        };

        public static readonly PlanRoad[] Roads =
        {
            new PlanRoad { Name = "Test-Drive Loop", Width = 10f, SpeedLimit = 22.4f, Pavements = false, Points = Ring },
            // Northern Avenue sweeps across the top of the town from the west arc to the north-east arc.
            new PlanRoad { Name = "Northern Avenue", Width = 11f, SpeedLimit = 13.4f, Points = new[] { P(-505, 65), P(-350, 74), P(-150, 60), P(80, 58), P(280, 64), P(330, 70), P(350, 79), P(462, 128) } },
            // Harbour Boulevard — the high street: dealers on the north side, everything else on the south.
            new PlanRoad { Name = "Harbour Boulevard", Width = 11f, SpeedLimit = 13.4f, Points = new[] { P(-495, -20), P(-350, -30), P(-300, -30), P(-150, -30), P(-20, -24), P(80, -25), P(140, -28), P(290, -40), P(330, -45), P(472, -75) } },
            // Guildhall Street — the civic axis from the ring road to Harbour Boulevard.
            new PlanRoad { Name = "Guildhall Street", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(80, 238), P(80, 120), P(80, 58), P(80, -25) } },
            new PlanRoad { Name = "Station Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(-150, 60), P(-150, -30), P(-150, -150), P(-150, -232) } },
            new PlanRoad { Name = "Park Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(-350, 74), P(-350, -30) } },
            // Dockside Road — down to the docks in the south-west.
            new PlanRoad { Name = "Dockside Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(-372, -165), P(-300, -110), P(-300, -30) } },
            new PlanRoad { Name = "Forecourt Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(330, 70), P(330, -45), P(330, -140) } },
            // The two new roads serving the south-side business park (Horizon Drive drops to the ring road).
            new PlanRoad { Name = "Horizon Drive", Width = 8f, SpeedLimit = 11.2f, Points = new[] { P(200, -140), P(200, -221) } },
            new PlanRoad { Name = "Business Park Road", Width = 8f, SpeedLimit = 11.2f, Points = new[] { P(200, -140), P(330, -140) } },
        };

        /// <summary>UK roundabouts (clockwise). The busiest junctions on Harbour Boulevard and Northern Avenue.</summary>
        public static readonly PlanRoundabout[] Roundabouts =
        {
            new PlanRoundabout(-350, 74), new PlanRoundabout(-150, 60), new PlanRoundabout(330, 70),
            new PlanRoundabout(-350, -30), new PlanRoundabout(-300, -30), new PlanRoundabout(-150, -30),
            new PlanRoundabout(200, -140), new PlanRoundabout(330, -140),
        };

        /// <summary>Junctions with traffic lights (the two crossroads on Guildhall Street, plus Forecourt Road × Harbour Boulevard).</summary>
        public static readonly V2[] LightNodes = { P(80, 58), P(80, -25), P(330, -45) };

        // ------------------------------------------------------------------ ZONES (14 zones, two 14s + zone 15 = the business park)
        public static readonly PlanZone[] Zones =
        {
            new PlanZone { Number = 1, Name = "Player Start - Residential Home", X0 = -455, Z0 = 74, X1 = -290, Z1 = 118 },
            new PlanZone { Number = 2, Name = "Main Dealership HQ", X0 = -20, Z0 = -15, X1 = 135, Z1 = 55 },
            new PlanZone { Number = 3, Name = "Used-Car Auction & Dockside Plot", X0 = -470, Z0 = -125, X1 = -160, Z1 = -40 },
            new PlanZone { Number = 4, Name = "Vehicle Inspection / MOT", X0 = -110, Z0 = 65, X1 = -5, Z1 = 185 },
            new PlanZone { Number = 5, Name = "Mechanic & Body Repair", X0 = 30, Z0 = 65, X1 = 215, Z1 = 208 },
            new PlanZone { Number = 6, Name = "Car Wash & Detailing", X0 = 140, Z0 = -12, X1 = 290, Z1 = 55 },
            new PlanZone { Number = 7, Name = "Fuel Station & Convenience", X0 = 300, Z0 = -62, X1 = 465, Z1 = 50 },
            new PlanZone { Number = 8, Name = "Parts & Tyre Shop", X0 = 140, Z0 = -130, X1 = 285, Z1 = -50 },
            new PlanZone { Number = 9, Name = "Private-Seller Market", X0 = -115, Z0 = -130, X1 = 135, Z1 = -52 },
            new PlanZone { Number = 10, Name = "Town Centre", X0 = -140, Z0 = -130, X1 = 15, Z1 = -30 },
            new PlanZone { Number = 11, Name = "Industrial Estate", X0 = 235, Z0 = 125, X1 = 415, Z1 = 208 },
            new PlanZone { Number = 12, Name = "Test-Drive Loop Road", X0 = -520, Z0 = -260, X1 = 520, Z1 = 265 },
            new PlanZone { Number = 13, Name = "Public Park", X0 = -488, Z0 = -18, X1 = -160, Z1 = 52 },
            new PlanZone { Number = 14, Name = "Future Expansion Area - Northgate (Downtown Motors)", X0 = 330, Z0 = 55, X1 = 470, Z1 = 120 },
            new PlanZone { Number = 14, Name = "Future Expansion Area - Southgate (Luxury Lane)", X0 = 345, Z0 = -135, X1 = 470, Z1 = -63 },
            new PlanZone { Number = 15, Name = "Southside Business Park", X0 = 130, Z0 = -205, X1 = 340, Z1 = -148 },
        };

        // ------------------------------------------------------------------ SITES (same ids as the game's sites and properties)
        static PlanSite S(string id, string name, int zone, float x, float z, float yaw, float fx0, float fz0, float fx1, float fz1) =>
            new PlanSite { Id = id, Name = name, Zone = zone, Position = P(x, z), Yaw = yaw, FX0 = fx0, FZ0 = fz0, FX1 = fx1, FZ1 = fz1 };

        public static readonly PlanSite[] Sites =
        {
            S("Home_Trailer", "Starter Trailer", 1, -425, 88, 0, -13, -4, 13, 21),
            S("Home_House", "Suburban House", 1, -385, 88, 0, -13, -4, 13, 21),
            S("Home_Villa", "Hillside Villa", 1, -300, 88, 0, -19, -4, 19, 22),
            S("autoworld", "AUTO WORLD", 2, 20, 14, 0, -19, -21, 19, 28),
            S("AuctionCenter", "Auction Center", 3, -272, -95, 90, -15, -9, 35, 19),
            S("UsedCarMarket", "Used Car Market", 3, -318, -95, 270, -16, -1, 16, 24),
            S("ServiceCentre", "AUTO WORLD Service Centre", 4, -55, 86, 0, -16, -19, 24, 21),
            S("MechanicWorkshop", "Mechanic Workshop", 5, 150, 80, 0, -14, -4, 14, 22),
            S("CarWashCenter", "Car Wash", 6, 215, -10, 0, -11, -4, 11, 17),
            S("FuelStation", "Fuel Station", 7, 400, -36, 0, -15, -4, 15, 18),
            S("TyreShop", "Tyres & Wheels", 8, 168, -48, 180, -9, -3, 9, 13),
            S("PartsDirect", "Parts Direct", 8, 245, -48, 180, -9, -3, 9, 13),
            S("MapleClose", "Maple Close", 9, -40, -66, 180, -23, -31, 23, 56),
            S("BirchClose", "Birch Close", 9, 110, -66, 180, -23, -31, 23, 56),
            S("RentalApartments", "Rental Apartments", 10, -115, -49, 180, -13, 0, 13, 22),
            S("OfficeTower", "Office Tower", 10, -78, -48, 180, -13, 0, 13, 22),
            S("ParkingGarage", "Parking Garage", 10, 0, -45, 180, -12, 0, 16, 17),
            S("PatrolStation", "Patrol Station", 10, -126, -95, 90, -12, -4, 12, 18),
            S("dealer_downtown", "Downtown Motors", 14, 415, 64, 180, -19, -21, 19, 28),
            S("dealer_luxury", "Luxury Lane", 14, 390, -95, 180, -19, -21, 19, 28),
            S("RentGo", "RentGo Rentals & Sales", 15, 293, -155, 180, -15, -3, 15, 17),
            S("NorthgateOffices", "Northgate Offices", 15, 235, -152, 180, -13, -3, 13, 15),
            S("OfficesWest", "Offices West", 15, 263, -152, 180, -11, -3, 11, 13),
            // The two plots that are mid-construction (see ConstructionSite):
            S("NorthgateSite", "Northgate Site (under construction)", 14, 360, 105, 270, -11, -3, 11, 14),
            S("DocksideSite", "Dockside Site (under construction)", 3, -240, -52, 180, -15, -3, 15, 15),
        };

        public static PlanSite Site(string id) => Array.Find(Sites, s => s.Id == id);
        public static PlanZone Zone(int number) => Array.Find(Zones, z => z.Number == number);
        /// <summary>The zone with this number that contains the point (zone 14 has two plots).</summary>
        public static PlanZone ZoneAt(int number, V2 p) => Zones.Where(z => z.Number == number).OrderBy(z => z.Contains(p) ? 0 : 1).ThenBy(z => V2.Distance(z.Centre, p)).FirstOrDefault();

        /// <summary>Square the whole map fits in (for the world map and the ground). — the sea lies to the south.</summary>
        public static (float cx, float cz, float halfSize) MapFrame => (-4f, 1f, 530f);

        // ------------------------------------------------------------------ TRAFFIC NETWORK
        /// <summary>Straight segments of every road, before roundabouts are added.</summary>
        public static List<RoadSegment> RawSegments()
        {
            var list = new List<RoadSegment>();
            foreach (var r in Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                    list.Add(new RoadSegment(r.Points[i], r.Points[i + 1], r.SpeedLimit));
            return list;
        }

        /// <summary>The traffic network: every road segment, with each roundabout turned into a one-way clockwise ring.</summary>
        public static List<RoadSegment> Segments()
        {
            var segs = RawSegments();
            foreach (var rb in Roundabouts) segs = Roundabout.Expand(segs, rb.Centre, rb.Radius);
            return segs;
        }

        public static float WidthOf(V2 a, V2 b)
        {
            foreach (var r in Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                    if ((V2.Distance(r.Points[i], a) < 0.5f && V2.Distance(r.Points[i + 1], b) < 0.5f) || (V2.Distance(r.Points[i], b) < 0.5f && V2.Distance(r.Points[i + 1], a) < 0.5f))
                        return r.Width;
            return 9f;
        }

        // ------------------------------------------------------------------ GEOMETRY HELPERS
        public static float SegmentDistance(V2 p, V2 a, V2 b)
        {
            var ab = b - a; float l2 = V2.Dot(ab, ab);
            float t = l2 < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, V2.Dot(p - a, ab) / l2));
            return V2.Distance(p, a + ab * t);
        }

        /// <summary>Distance from a point to the nearest road edge (negative = on the road).</summary>
        public static float RoadClearance(V2 p)
        {
            float best = float.MaxValue;
            foreach (var r in Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                    best = Math.Min(best, SegmentDistance(p, r.Points[i], r.Points[i + 1]) - r.Width / 2f);
            foreach (var rb in Roundabouts) best = Math.Min(best, V2.Distance(p, rb.Centre) - (rb.Radius + 6f));
            return best;
        }

        /// <summary>Does the world box overlap any road surface (with a margin)?</summary>
        public static bool BoxTouchesRoad((float x0, float z0, float x1, float z1) b, float margin = 0.5f)
        {
            for (float x = b.x0; x <= b.x1 + 0.01f; x += Math.Max(1f, (b.x1 - b.x0) / 24f))
                for (float z = b.z0; z <= b.z1 + 0.01f; z += Math.Max(1f, (b.z1 - b.z0) / 24f))
                    if (RoadClearance(new V2(x, z)) < margin) return true;
            return false;
        }

        /// <summary>Pavement lines (both sides of every paved road), trimmed back from junctions so they don't cross other roads.</summary>
        public static List<(V2 a, V2 b)> Pavements(float trim = 9f)
        {
            var raw = RawSegments();
            var nodes = raw.SelectMany(s => new[] { s.a, s.b }).ToList();
            int Count(V2 p) => nodes.Count(n => V2.Distance(n, p) < 1f);
            // How far back from a junction the pavement has to stop: further when another road meets at a shallow angle.
            float Trim(V2 node, V2 dir, float width)
            {
                float t = 0f;
                foreach (var o in raw)
                {
                    V2 far; if (V2.Distance(o.a, node) < 1f) far = o.b; else if (V2.Distance(o.b, node) < 1f) far = o.a; else continue;
                    var od = (far - node).Normalized;
                    if (V2.Dot(od, dir) > 0.999f) continue;   // this segment itself
                    float sin = Math.Abs(dir.x * od.z - dir.z * od.x);
                    float ow = HarbourPlan.WidthOf(o.a, o.b) / 2f + 2.6f;
                    t = Math.Max(t, Math.Min(60f, (ow + width / 2f) / Math.Max(0.2f, sin) + 2f));
                }
                return t;
            }
            var list = new List<(V2, V2)>();
            foreach (var r in Roads.Where(x => x.Pavements))
                for (int i = 0; i < r.Points.Length - 1; i++)
                {
                    V2 a = r.Points[i], b = r.Points[i + 1];
                    var d = (b - a).Normalized; float len = V2.Distance(a, b);
                    float ta = IsRoundabout(a) ? trim + RoundaboutRadius : Count(a) > 1 ? Math.Max(trim, Trim(a, d, r.Width)) : 0f;
                    float tb = IsRoundabout(b) ? trim + RoundaboutRadius : Count(b) > 1 ? Math.Max(trim, Trim(b, d * -1f, r.Width)) : 0f;
                    if (len - ta - tb < 6f) continue;
                    var left = new V2(-d.z, d.x);
                    float off = r.Width / 2f + 1.2f;
                    foreach (float side in new[] { -1f, 1f })
                        list.Add((a + d * ta + left * (off * side), b - d * tb + left * (off * side)));
                }
            return list;
        }

        public static bool IsRoundabout(V2 p) => Roundabouts.Any(r => V2.Distance(r.Centre, p) < 1f);
    }

    /// <summary>Turns a junction node into a small one-way ring (clockwise seen from above: the UK way round).</summary>
    public static class Roundabout
    {
        public const int MinPoints = 8;

        public static List<RoadSegment> Expand(List<RoadSegment> segments, V2 centre, float radius)
        {
            var result = new List<RoadSegment>();
            var attach = new List<V2>();
            float limit = 8.9f;
            foreach (var s in segments)
            {
                bool aHere = V2.Distance(s.a, centre) < 1f, bHere = V2.Distance(s.b, centre) < 1f;
                if (!aHere && !bHere) { result.Add(s); continue; }
                var seg = s;
                if (aHere) { var p = centre + (s.b - centre).Normalized * radius; seg.a = p; attach.Add(p); }
                if (bHere) { var p = centre + (s.a - centre).Normalized * radius; seg.b = p; attach.Add(p); }
                result.Add(seg);
            }
            if (attach.Count == 0) return result;
            // Ring points: the attachments plus extra points so the ring is round enough.
            var angles = attach.Select(p => Angle(p - centre)).ToList();
            for (int k = 0; k < MinPoints; k++)
            {
                double a = k * 2 * Math.PI / MinPoints + Math.PI / MinPoints;
                if (angles.All(x => AngleGap(x, a) > Math.PI / MinPoints * 0.6)) angles.Add(a);
            }
            angles.Sort();
            angles.Reverse();   // decreasing angle = clockwise seen from above (x east, z north)
            var pts = angles.Select(a => centre + new V2((float)Math.Cos(a), (float)Math.Sin(a)) * radius).ToList();
            for (int i = 0; i < pts.Count; i++)
                result.Add(new RoadSegment(pts[i], pts[(i + 1) % pts.Count], limit) { oneWay = true });
            return result;
        }

        static double Angle(V2 v) { double a = Math.Atan2(v.z, v.x); return a < 0 ? a + 2 * Math.PI : a; }
        static double AngleGap(double a, double b) { double d = Math.Abs(a - b) % (2 * Math.PI); return d > Math.PI ? 2 * Math.PI - d : d; }
    }
}
