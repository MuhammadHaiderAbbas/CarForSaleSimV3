using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 12a: the master-map town plan - roads, roundabouts, zones and sites fit together.</summary>
    public class Phase12Tests
    {
        [Test]
        public void Zones_AreTheFourteenOfTheMasterMap()
        {
            CollectionAssert.AreEqual(Enumerable.Range(1, 14), TownPlan.Zones.Select(z => z.Number).Distinct().OrderBy(n => n));
            Assert.AreEqual(2, TownPlan.Zones.Count(z => z.Number == 14), "two expansion plots, as on the master map");
            foreach (var z in TownPlan.Zones) { Assert.Less(z.X0, z.X1, z.Name); Assert.Less(z.Z0, z.Z1, z.Name); }
            StringAssert.Contains("Test-Drive", TownPlan.Zone(12).Name);
            StringAssert.Contains("Dealership", TownPlan.Zone(2).Name);
        }

        [Test]
        public void EverySite_IsInItsZone_OffTheRoads_AndReachable()
        {
            foreach (var s in TownPlan.Sites)
            {
                var zone = TownPlan.ZoneAt(s.Zone, s.Position);
                Assert.IsTrue(zone.Contains(s.Position, 40f), $"{s.Name} ({s.Position.x}, {s.Position.z}) is in zone {s.Zone} {zone.Name}");
                var box = s.WorldBox();
                Assert.IsFalse(TownPlan.BoxTouchesRoad(box), $"{s.Name} footprint {box} sits on a road");
                // The front of the site (its entrance side) is close to a road.
                var front = s.ToWorld((s.FX0 + s.FX1) / 2f, s.FZ0 - 2f);
                Assert.Less(TownPlan.RoadClearance(front), 14f, $"{s.Name}: entrance {front.x:0},{front.z:0} is far from any road");
            }
        }

        [Test]
        public void Sites_DontOverlapEachOther()
        {
            var sites = TownPlan.Sites;
            for (int i = 0; i < sites.Length; i++)
                for (int j = i + 1; j < sites.Length; j++)
                {
                    var a = sites[i].WorldBox(); var b = sites[j].WorldBox();
                    bool overlap = a.x0 < b.x1 && b.x0 < a.x1 && a.z0 < b.z1 && b.z0 < a.z1;
                    Assert.IsFalse(overlap, $"{sites[i].Name} overlaps {sites[j].Name}");
                }
        }

        [Test]
        public void GameSites_AllHaveAPlace()
        {
            foreach (var id in new[] { "autoworld", "dealer_downtown", "dealer_luxury", "ServiceCentre", "FuelStation", "MechanicWorkshop", "PartsDirect",
                                       "CarWashCenter", "UsedCarMarket", "AuctionCenter", "MapleClose", "Home_Trailer", "Home_House", "Home_Villa",
                                       "RentalApartments", "OfficeTower", "ParkingGarage" })
                Assert.IsNotNull(TownPlan.Site(id), id);
            Assert.AreEqual(TownPlan.Sites.Length, TownPlan.Sites.Select(s => s.Id).Distinct().Count());
        }

        [Test]
        public void Yaw_TurnsTheFootprint()
        {
            var north = new PlanSite { Position = new V2(10, 20), Yaw = 0, FX0 = -1, FZ0 = 0, FX1 = 1, FZ1 = 5 };
            Assert.AreEqual(25f, north.ToWorld(0, 5).z, 1e-4, "yaw 0: local +z is world +z");
            var south = new PlanSite { Position = new V2(10, 20), Yaw = 180, FX0 = -1, FZ0 = 0, FX1 = 1, FZ1 = 5 };
            Assert.AreEqual(15f, south.ToWorld(0, 5).z, 1e-4, "yaw 180: local +z is world -z");
            Assert.AreEqual(8f, south.ToWorld(2, 0).x, 1e-4, "and local +x is world -x");
        }

        [Test]
        public void RoadNetwork_IsOneConnectedTown()
        {
            var segs = TownPlan.Segments();
            var nodes = new List<V2>();
            int Node(V2 p) { int i = nodes.FindIndex(n => V2.Distance(n, p) < 1f); if (i < 0) { nodes.Add(p); i = nodes.Count - 1; } return i; }
            var edges = segs.Select(s => (Node(s.a), Node(s.b))).ToList();
            var seen = new HashSet<int> { 0 };
            var queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0) { int n = queue.Dequeue(); foreach (var (a, b) in edges) { if (a == n && seen.Add(b)) queue.Enqueue(b); if (b == n && seen.Add(a)) queue.Enqueue(a); } }
            Assert.AreEqual(nodes.Count, seen.Count, "every road can be reached from every other");
        }

        [Test]
        public void Traffic_CanDriveEverywhere_NoDeadEnds()
        {
            var lanes = RoadGraph.Build(TownPlan.Segments(), 9f, 6f, TownPlan.LightNodes);
            Assert.Greater(lanes.Count, 60);
            foreach (var l in lanes) Assert.IsNotEmpty(l.next, $"lane {l.id} ({l.start.x:0},{l.start.z:0})->({l.end.x:0},{l.end.z:0}) is a dead end");
            // Every lane can reach every other lane (strongly connected): start anywhere, end anywhere.
            var reach = new HashSet<int> { 0 }; var q = new Queue<int>(); q.Enqueue(0);
            while (q.Count > 0) foreach (var n in lanes[q.Dequeue()].next) if (reach.Add(n)) q.Enqueue(n);
            Assert.AreEqual(lanes.Count, reach.Count, "traffic can get from anywhere to anywhere");
        }

        [Test]
        public void TrafficLights_OnlyAtTheTwoBusyCrossroads()
        {
            var lanes = RoadGraph.Build(TownPlan.Segments(), 9f, 6f, TownPlan.LightNodes);
            var lit = lanes.Where(l => l.endsAtJunction).Select(l => l.end).ToList();
            Assert.IsNotEmpty(lit);
            foreach (var p in lit) Assert.IsTrue(TownPlan.LightNodes.Any(n => V2.Distance(n, p) < 12f), $"light at {p.x:0},{p.z:0}");
        }

        [Test]
        public void Roundabouts_AreOneWayClockwiseRings()
        {
            var segs = Roundabout.Expand(new List<RoadSegment>
            {
                new RoadSegment(new V2(0, 100), new V2(0, 0)), new RoadSegment(new V2(0, 0), new V2(100, 0)),
                new RoadSegment(new V2(0, 0), new V2(0, -100)), new RoadSegment(new V2(-100, 0), new V2(0, 0)),
            }, new V2(0, 0), 15f);
            var ring = segs.Where(s => s.oneWay).ToList();
            Assert.GreaterOrEqual(ring.Count, Roundabout.MinPoints);
            foreach (var s in ring)
            {
                Assert.AreEqual(15f, V2.Distance(s.a, new V2(0, 0)), 0.01f);
                // Clockwise seen from above: the cross product (a x b) is negative.
                Assert.Less(s.a.x * s.b.z - s.a.z * s.b.x, 0f, "clockwise");
            }
            foreach (var s in segs.Where(x => !x.oneWay))
                Assert.IsTrue(V2.Distance(s.a, new V2(0, 0)) >= 14.9f && V2.Distance(s.b, new V2(0, 0)) >= 14.9f, "roads stop at the ring");
            var lanes = RoadGraph.Build(segs);
            Assert.AreEqual(ring.Count + 8, lanes.Count, "one lane per ring piece, two per road");
            foreach (var l in lanes.Where(x => V2.Distance(x.end, new V2(0, 0)) < 20f)) Assert.IsNotEmpty(l.next, "at the roundabout you can always drive on");
        }

        [Test]
        public void Pavements_DontCrossOtherRoads()
        {
            var pav = TownPlan.Pavements();
            Assert.Greater(pav.Count, 20);
            foreach (var (a, b) in pav)
                for (float t = 0f; t <= 1f; t += 0.1f)
                {
                    var p = a + (b - a) * t;
                    Assert.Greater(TownPlan.RoadClearance(p), -0.2f, $"pavement point {p.x:0},{p.z:0} is on a road");
                }
        }

        [Test]
        public void Town_FitsTheMapFrame()
        {
            var (cx, cz, half) = TownPlan.MapFrame;
            foreach (var p in TownPlan.Ring)
            {
                Assert.LessOrEqual(Math.Abs(p.x - cx), half);
                Assert.LessOrEqual(Math.Abs(p.z - cz), half);
            }
            float w = TownPlan.Ring.Max(p => p.x) - TownPlan.Ring.Min(p => p.x);
            float h = TownPlan.Ring.Max(p => p.z) - TownPlan.Ring.Min(p => p.z);
            Assert.That(w, Is.InRange(950f, 1100f), "about 1 km across");
            Assert.That(h, Is.InRange(420f, 520f));
            float loop = 0f; for (int i = 0; i < TownPlan.Ring.Length - 1; i++) loop += V2.Distance(TownPlan.Ring[i], TownPlan.Ring[i + 1]);
            Assert.That(loop, Is.InRange(2500f, 3200f), "test-drive loop is 2.5-3 km");
        }
    }
}
