using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Sim;
using NUnit.Framework;
using UnityEngine;

namespace CarSales.Tests
{
    /// <summary>Phase 14: the Final Map plan - zones, sites, roads, traffic and the construction plots.</summary>
    public class Phase14Tests
    {
        [Test]
        public void Zones_MasterMapPlusBusinessPark()
        {
            var numbers = FinalMapPlan.Zones.Select(z => z.Number).Distinct().OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(1, 15), numbers, "the master map's 14 zones plus zone 15 (Southside Business Park)");
            Assert.AreEqual(2, FinalMapPlan.Zones.Count(z => z.Number == 14), "two expansion plots, as on the master map");
            Assert.AreEqual(1, FinalMapPlan.Zones.Count(z => z.Number == 15));
            foreach (var z in FinalMapPlan.Zones) { Assert.Less(z.X0, z.X1, z.Name); Assert.Less(z.Z0, z.Z1, z.Name); }
            StringAssert.Contains("Business Park", FinalMapPlan.Zone(15).Name);
        }

        [Test]
        public void EverySite_IsInItsZone_OffTheRoads_AndReachable()
        {
            foreach (var s in FinalMapPlan.Sites)
            {
                var zone = FinalMapPlan.ZoneAt(s.Zone, s.Position);
                Assert.IsTrue(zone.Contains(s.Position, 40f), $"{s.Name} ({s.Position.x}, {s.Position.z}) is in zone {s.Zone} {zone.Name}");
                var box = s.WorldBox();
                Assert.IsFalse(FinalMapPlan.BoxTouchesRoad(box), $"{s.Name} footprint {box} sits on a road");
                var front = s.ToWorld((s.FX0 + s.FX1) / 2f, s.FZ0 - 2f);
                Assert.Less(FinalMapPlan.RoadClearance(front), 14f, $"{s.Name}: entrance {front.x:0},{front.z:0} is far from any road");
            }
        }

        [Test]
        public void Sites_DontOverlapEachOther()
        {
            var sites = FinalMapPlan.Sites;
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
                                       "RentalApartments", "OfficeTower", "ParkingGarage",
                                       "PatrolStation", "TyreShop", "RentGo", "NorthgateOffices", "OfficesWest", "NorthgateSite", "DocksideSite" })
                Assert.IsNotNull(FinalMapPlan.Site(id), id);
            Assert.AreEqual(FinalMapPlan.Sites.Length, FinalMapPlan.Sites.Select(s => s.Id).Distinct().Count(), "site ids are unique");
        }

        [Test]
        public void NewRoads_ExistAndServeTheBusinessPark()
        {
            Assert.IsTrue(FinalMapPlan.Roads.Any(r => r.Name == "Horizon Drive"), "Horizon Drive is missing");
            Assert.IsTrue(FinalMapPlan.Roads.Any(r => r.Name == "Business Park Road"), "Business Park Road is missing");
            // Business Park Road runs past all three of the south-side sites it serves.
            var bpr = FinalMapPlan.Roads.First(r => r.Name == "Business Park Road");
            foreach (var id in new[] { "NorthgateOffices", "OfficesWest", "RentGo" })
            {
                var s = FinalMapPlan.Site(id);
                var front = s.ToWorld((s.FX0 + s.FX1) / 2f, s.FZ0 - 2f);
                float d = bpr.Points.Select((p, i) => i == bpr.Points.Length - 1 ? float.MaxValue : FinalMapPlan.SegmentDistance(front, p, bpr.Points[i + 1])).Min();
                Assert.Less(d, 12f, $"{s.Name} is not on Business Park Road");
            }
            // Horizon Drive reaches the southern ring road.
            var hd = FinalMapPlan.Roads.First(r => r.Name == "Horizon Drive");
            var south = hd.Points.Last();
            float best = float.MaxValue;
            for (int i = 0; i < FinalMapPlan.Ring.Length - 1; i++) best = Math.Min(best, FinalMapPlan.SegmentDistance(south, FinalMapPlan.Ring[i], FinalMapPlan.Ring[i + 1]));
            Assert.Less(best, 6f, "Horizon Drive does not meet the ring road");
        }

        [Test]
        public void Traffic_IsConnected_WithLightsAtTheSameJunctions()
        {
            var segs = FinalMapPlan.Segments();
            Assert.Greater(segs.Count, 40);
            var lanes = RoadGraph.Build(segs, 9f, 6f, FinalMapPlan.LightNodes);
            Assert.IsTrue(lanes.Any(l => l.endsAtJunction));
            foreach (var l in lanes.Where(x => x.endsAtJunction)) Assert.IsNotEmpty(l.next, $"lane {l.id} at a junction leads somewhere");
            foreach (var p in FinalMapPlan.LightNodes) Assert.IsTrue(lanes.Any(l => V2.Distance(l.start, p) < 12f || V2.Distance(l.end, p) < 12f), $"light at {p.x:0},{p.z:0}");
        }

        [Test]
        public void Pavements_DontCrossOtherRoads()
        {
            var pav = FinalMapPlan.Pavements();
            Assert.IsNotEmpty(pav);
            foreach (var (a, b) in pav)
            {
                float len = V2.Distance(a, b);
                if (len < 0.5f) continue;
                for (float t = 0f; t <= 1f; t += 1f / Mathf.Max(1f, len / 6f))
                {
                    var p = a + (b - a) * t;
                    Assert.GreaterOrEqual(FinalMapPlan.RoadClearance(p), -0.2f, $"pavement point {p.x:0},{p.z:0} is on a road");
                }
            }
        }

        [Test]
        public void ConstructionPlots_AdvanceGroundToFinished()
        {
            Assert.AreEqual(ConstructionStages.Ground, ConstructionStages.StageAt(1, 2, 1));
            Assert.AreEqual(ConstructionStages.Ground, ConstructionStages.StageAt(1, 2, 2));
            Assert.AreEqual(ConstructionStages.Site, ConstructionStages.StageAt(1, 2, 3));
            Assert.AreEqual(ConstructionStages.Draft, ConstructionStages.StageAt(1, 2, 5));
            Assert.AreEqual(ConstructionStages.Final, ConstructionStages.StageAt(1, 2, 7));
            Assert.AreEqual(ConstructionStages.Final, ConstructionStages.StageAt(1, 2, 400));
            Assert.AreEqual(0, ConstructionStages.DaysUntilNextStage(1, 2, 7), "already final");
            Assert.AreEqual(1, ConstructionStages.DaysUntilNextStage(1, 2, 2));
            StringAssert.Contains("Finished", ConstructionStages.StageName(ConstructionStages.Final));
            // The plan really does mark the two plots as the construction sites.
            Assert.AreEqual("Northgate Site (under construction)", FinalMapPlan.Site("NorthgateSite").Name);
            Assert.AreEqual("Dockside Site (under construction)", FinalMapPlan.Site("DocksideSite").Name);
        }

        [Test]
        public void MapFrame_FitsTheRingAndTheNewRoads()
        {
            var (cx, cz, half) = FinalMapPlan.MapFrame;
            Assert.GreaterOrEqual(half, 500f);
            foreach (var p in FinalMapPlan.Roads.SelectMany(r => r.Points))
            {
                Assert.LessOrEqual(Mathf.Abs(p.x - cx), half, $"road point {p.x} escapes the map frame");
                Assert.LessOrEqual(Mathf.Abs(p.z - cz), half, $"road point {p.z} escapes the map frame");
            }
        }
    }

    /// <summary>The Final Map asset manifest: downloads are repeatable, licences are valid, no duplicates.</summary>
    public class Phase14AssetManifestTests
    {
        static string ManifestPath()
        {
            string p = Path.Combine(Application.dataPath, "CarSales", "FinalMap", "final-map-assets.json");
            return File.Exists(p) ? p : null;
        }

        static List<Dictionary<string, object>> LoadAssets()
        {
            string path = ManifestPath();
            if (path == null) return null;
            var root = CarSales.Foundation.MiniJson.Parse(File.ReadAllText(path));
            var list = new List<Dictionary<string, object>>();
            foreach (var item in root["assets"].Items)
            {
                var d = new Dictionary<string, object>();
                foreach (var (k, v) in item.Fields)
                    d[k] = v.Kind == CarSales.Foundation.JsonKind.String ? (object)v.Str
                         : v.Kind == CarSales.Foundation.JsonKind.Bool ? v.Bool
                         : v.Kind == CarSales.Foundation.JsonKind.Number ? v.Num
                         : null;
                list.Add(d);
            }
            return list;
        }

        [Test]
        public void Manifest_ExistsAndIsWellFormed()
        {
            var assets = LoadAssets();
            if (assets == null) Assert.Ignore("final-map-assets.json not found (unexpected in this repo)");
            Assert.Greater(assets.Count, 50, "expected a real working set of models");
            foreach (var a in assets)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)a["uid"]), "every asset needs its Sketchfab uid (for re-download)");
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)a["name"]));
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)a["importPath"]), "every asset needs its import path");
                StringAssert.StartsWith("Assets/CarSales/Sketchfab/", (string)a["importPath"]);
                StringAssert.EndsWith(".glb", (string)a["importPath"]);
            }
        }

        [Test]
        public void Manifest_LicencesAreKnownAndFreeForCommercialUse()
        {
            var assets = LoadAssets();
            if (assets == null) Assert.Ignore("final-map-assets.json not found");
            var allowed = new HashSet<string> { "cc0", "by", "by-sa" };
            foreach (var a in assets)
                Assert.IsTrue(allowed.Contains((string)a["licence"]), $"{a["name"]}: licence '{a["licence"]}' is not an allowed commercial-use licence");
        }

        [Test]
        public void Manifest_HasNoDuplicates_AndNoOversizedFiles()
        {
            var assets = LoadAssets();
            if (assets == null) Assert.Ignore("final-map-assets.json not found");
            var uids = assets.Select(a => (string)a["uid"]).ToList();
            Assert.AreEqual(uids.Count, uids.Distinct().Count(), "duplicate Sketchfab uids");
            var paths = assets.Select(a => (string)a["importPath"]).ToList();
            Assert.AreEqual(paths.Count, paths.Distinct().Count(), "duplicate import paths");
            var faces = assets.Select(a => Convert.ToDouble(a["faceCount"])).ToList();
            Assert.LessOrEqual(faces.Max(), 1_500_000, "a model is beyond the face budget for a mid-range PC");
            foreach (var a in assets)
            {
                if (a["glbBytes"] == null) continue;
                Assert.LessOrEqual(Convert.ToDouble(a["glbBytes"]), 95_000_000d, $"{a["name"]} is over the 95 MB git/GitHub cap");
            }
        }

        [Test]
        public void Manifest_EveryCarCategoryIsStocked()
        {
            var assets = LoadAssets();
            if (assets == null) Assert.Ignore("final-map-assets.json not found");
            foreach (var cat in new[] { "car_hatchback", "car_sedan", "car_suv", "car_sports", "car_pickup", "car_luxury" })
                Assert.IsTrue(assets.Any(a => (string)a["category"] == cat), $"no models curated for {cat}");
        }
    }
}
