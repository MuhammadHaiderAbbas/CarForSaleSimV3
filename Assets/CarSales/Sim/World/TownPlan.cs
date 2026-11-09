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

    /// <summary>One of the 14 zones of the master map.</summary>
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
    /// Phase 12 master map (CarForSaleSimV2): one connected town about 1,040 × 470 m inside a test-drive loop, with
    /// the 14 zones of the master map. Coordinates are game metres (x east, z north), origin near the dealership.
    /// Build Everything builds the roads, roundabouts and sites from this; traffic, pedestrians and the map read it.
    /// </summary>
    public static class TownPlan
    {
        /// <summary>Saved with each game; older saves move cars parked on the old streets to the dealership.</summary>
        public const int Version = 2;
        public const float RoundaboutRadius = 15f;

        static V2 P(float x, float z) => new V2(x, z);

        // ------------------------------------------------------------------ ROADS
        /// <summary>The test-drive loop (zone 12): a rural ring road round the whole town, with every junction on it.</summary>
        public static readonly V2[] Ring =
        {
            P(-449, 176), P(-351, 231), P(0, 234), P(215, 232), P(299, 231), P(468, 202), P(520, 98), P(521, 86), P(523, -20),
            P(510, -60), P(494, -104), P(416, -182), P(330, -205), P(234, -224), P(-26, -228), P(-150, -225), P(-254, -221),
            P(-416, -189), P(-494, -104), P(-511, -45), P(-517, -20), P(-507, 78), P(-478, 127), P(-449, 176),
        };

        public static readonly PlanRoad[] Roads =
        {
            new PlanRoad { Name = "Test-Drive Loop", Width = 10f, SpeedLimit = 22.4f, Pavements = false, Points = Ring },
            new PlanRoad { Name = "Northern Avenue", Width = 11f, SpeedLimit = 13.4f, Points = new[] { P(-478, 127), P(-410, 86), P(-120, 86), P(0, 86), P(117, 86), P(215, 86), P(293, 86), P(521, 86) } },
            new PlanRoad { Name = "Dealer Road", Width = 11f, SpeedLimit = 13.4f, Points = new[] { P(-511, -45), P(-300, -45), P(-120, -45), P(117, -45), P(293, -45), P(510, -60) } },
            new PlanRoad { Name = "Garage Lane", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(0, 234), P(0, 86) } },
            new PlanRoad { Name = "Works Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(215, 232), P(215, 86) } },
            new PlanRoad { Name = "Park Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(-120, 86), P(-120, -45) } },
            new PlanRoad { Name = "Showroom Way", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(117, 86), P(117, -45) } },
            new PlanRoad { Name = "Forecourt Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(293, 86), P(293, -45) } },
            new PlanRoad { Name = "Station Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(-120, -45), P(-130, -150), P(-150, -225) } },
            new PlanRoad { Name = "High Street", Width = 9f, SpeedLimit = 8.9f, Points = new[] { P(-494, -104), P(-300, -110), P(-130, -150) } },
            new PlanRoad { Name = "Market Lane", Width = 8f, SpeedLimit = 8.9f, Points = new[] { P(-300, -45), P(-300, -110) } },
            new PlanRoad { Name = "Depot Road", Width = 9f, SpeedLimit = 13.4f, Points = new[] { P(293, -45), P(330, -205) } },
        };

        /// <summary>UK roundabouts (clockwise). The two on the loop join it to Dealer Road.</summary>
        public static readonly PlanRoundabout[] Roundabouts =
        {
            new PlanRoundabout(-120, 86), new PlanRoundabout(215, 86), new PlanRoundabout(-120, -45), new PlanRoundabout(293, -45),
            new PlanRoundabout(-130, -150), new PlanRoundabout(-511, -45), new PlanRoundabout(510, -60),
        };

        /// <summary>The only junctions with traffic lights (the busy crossroads by the dealership); all others are give-way.</summary>
        public static readonly V2[] LightNodes = { P(117, -45), P(0, 86) };

        // ------------------------------------------------------------------ ZONES (master map numbers)
        public static readonly PlanZone[] Zones =
        {
            new PlanZone { Number = 1, Name = "Player Start - Residential Home", X0 = -440, Z0 = -138, X1 = -314, Z1 = -112 },
            new PlanZone { Number = 2, Name = "Main Dealership HQ", X0 = -40, Z0 = -38, X1 = 105, Z1 = 60 },
            new PlanZone { Number = 3, Name = "Used-Car Auction", X0 = -400, Z0 = 93, X1 = -150, Z1 = 195 },
            new PlanZone { Number = 4, Name = "Vehicle Inspection / MOT", X0 = -100, Z0 = 93, X1 = -8, Z1 = 190 },
            new PlanZone { Number = 5, Name = "Mechanic & Body Repair", X0 = 10, Z0 = 93, X1 = 205, Z1 = 190 },
            new PlanZone { Number = 6, Name = "Car Wash & Detailing", X0 = 125, Z0 = -38, X1 = 280, Z1 = 75 },
            new PlanZone { Number = 7, Name = "Fuel Station & Convenience", X0 = 305, Z0 = -38, X1 = 470, Z1 = 75 },
            new PlanZone { Number = 8, Name = "Parts & Tyre Shop", X0 = 140, Z0 = -150, X1 = 285, Z1 = -52 },
            new PlanZone { Number = 9, Name = "Private-Seller Market", X0 = -112, Z0 = -150, X1 = 108, Z1 = -52 },
            new PlanZone { Number = 10, Name = "Town Centre", X0 = -470, Z0 = -100, X1 = -140, Z1 = -52 },
            new PlanZone { Number = 11, Name = "Industrial Estate", X0 = 225, Z0 = 93, X1 = 420, Z1 = 210 },
            new PlanZone { Number = 12, Name = "Test-Drive Loop Road", X0 = -517, Z0 = -228, X1 = 523, Z1 = 234 },
            new PlanZone { Number = 13, Name = "Public Park", X0 = -480, Z0 = -36, X1 = -135, Z1 = 78 },
            new PlanZone { Number = 14, Name = "Future Expansion Area", X0 = 425, Z0 = 92, X1 = 505, Z1 = 205 },
            new PlanZone { Number = 14, Name = "Future Expansion Area", X0 = 340, Z0 = -185, X1 = 470, Z1 = -110 },
        };

        // ------------------------------------------------------------------ SITES (ids match the game's sites and properties)
        static PlanSite S(string id, string name, int zone, float x, float z, float yaw, float fx0, float fz0, float fx1, float fz1) =>
            new PlanSite { Id = id, Name = name, Zone = zone, Position = P(x, z), Yaw = yaw, FX0 = fx0, FZ0 = fz0, FX1 = fx1, FZ1 = fz1 };

        public static readonly PlanSite[] Sites =
        {
            S("Home_Trailer", "Starter Trailer", 1, -425, -118, 180, -13, -4, 13, 21),
            S("Home_House", "Suburban House", 1, -385, -118, 180, -13, -4, 13, 21),
            S("Home_Villa", "Hillside Villa", 1, -335, -121, 180, -19, -4, 19, 22),
            S("autoworld", "AUTO WORLD", 2, -10, -16, 0, -19, -21, 19, 28),
            S("AuctionCenter", "Auction Center", 3, -220, 103, 0, -15, -9, 35, 19),
            S("UsedCarMarket", "Used Car Market", 3, -330, 97, 0, -16, -1, 16, 24),
            S("ServiceCentre", "AUTO WORLD Service Centre", 4, -55, 111, 0, -16, -19, 24, 21),
            S("MechanicWorkshop", "Mechanic Workshop", 5, 100, 97, 0, -14, -4, 14, 22),
            S("CarWashCenter", "Car Wash", 6, 215, -34, 0, -11, -4, 11, 17),
            S("FuelStation", "Fuel Station", 7, 380, -34, 0, -15, -4, 15, 18),
            S("PartsDirect", "Parts Direct", 8, 245, -56, 180, -9, -3, 9, 13),
            S("MapleClose", "Maple Close", 9, -40, -82, 180, -23, -31, 23, 56),
            S("BirchClose", "Birch Close", 9, 75, -82, 180, -23, -31, 23, 56),
            S("RentalApartments", "Rental Apartments", 10, -220, -51.5f, 180, -13, 0, 13, 22),
            S("OfficeTower", "Office Tower", 10, -270, -51.5f, 180, -13, 0, 13, 22),
            S("ParkingGarage", "Parking Garage", 10, -400, -51.5f, 180, -12, 0, 16, 17),
            S("dealer_downtown", "Downtown Motors", 14, 455, 115, 0, -19, -21, 19, 28),
            S("dealer_luxury", "Luxury Lane", 14, 380, -155, 0, -19, -21, 19, 28),
        };

        public static PlanSite Site(string id) => Array.Find(Sites, s => s.Id == id);
        public static PlanZone Zone(int number) => Array.Find(Zones, z => z.Number == number);
        /// <summary>The zone with this number that contains the point (zone 14 has two plots).</summary>
        public static PlanZone ZoneAt(int number, V2 p) => Zones.Where(z => z.Number == number).OrderBy(z => z.Contains(p) ? 0 : 1).ThenBy(z => V2.Distance(z.Centre, p)).FirstOrDefault();

        /// <summary>Square the whole map fits in (for the world map and the ground).</summary>
        public static (float cx, float cz, float halfSize) MapFrame => (3f, 3f, 545f);

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

        /// <summary>Pavement lines (both sides of every town road), trimmed back from junctions so they don't cross other roads.</summary>
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
                    float ow = TownPlan.WidthOf(o.a, o.b) / 2f + 2.6f;
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
