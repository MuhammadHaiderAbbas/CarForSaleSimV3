using System;
using System.Linq;
using CarSales.Foundation.Graphics;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 13a: generated surfaces and metre-based meshes.</summary>
    public class SurfaceGenTests
    {
        [Test]
        public void EverySurface_GeneratesFullTextureSets()
        {
            foreach (var info in SurfaceGen.All)
            {
                var m = SurfaceGen.Generate(info.Id, 64);
                int bytes = 64 * 64 * 4;
                Assert.AreEqual(bytes, m.Albedo.Length, info.Name);
                Assert.AreEqual(bytes, m.Normal.Length, info.Name);
                Assert.AreEqual(bytes, m.Mask.Length, info.Name);
                Assert.AreEqual(info.Emission ? bytes : 0, m.Emission.Length, info.Name + " emission");
                Assert.Greater(info.TileU, 0f); Assert.Greater(info.TileV, 0f);
            }
        }

        [Test]
        public void EverySurfaceId_HasInfo()
        {
            foreach (SurfaceId id in Enum.GetValues(typeof(SurfaceId)))
                Assert.DoesNotThrow(() => SurfaceGen.Info(id), id.ToString());
            Assert.AreEqual(Enum.GetValues(typeof(SurfaceId)).Length, SurfaceGen.All.Count);
        }

        [Test]
        public void Generation_IsDeterministic()
        {
            var a = SurfaceGen.Generate(SurfaceId.Brick, 64);
            var b = SurfaceGen.Generate(SurfaceId.Brick, 64);
            CollectionAssert.AreEqual(a.Albedo, b.Albedo);
            CollectionAssert.AreEqual(a.Normal, b.Normal);
            var c = SurfaceGen.Generate(SurfaceId.Brick, 64, seed: 2);
            CollectionAssert.AreNotEqual(a.Albedo, c.Albedo);
        }

        [Test]
        public void Textures_TileWithoutSeams()
        {
            // The jump across the wrap (last column → first column) must look like any other step between neighbours.
            foreach (var info in SurfaceGen.All)
            {
                int n = 128;
                var m = SurfaceGen.Generate(info.Id, n);
                double inside = 0, across = 0;
                for (int y = 0; y < n; y++)
                {
                    inside += Diff(m.Albedo, n, y, n / 2 - 1, n / 2);
                    across += Diff(m.Albedo, n, y, n - 1, 0);
                }
                double insideV = 0, acrossV = 0;
                for (int x = 0; x < n; x++)
                {
                    insideV += DiffV(m.Albedo, n, x, n / 2 - 1, n / 2);
                    acrossV += DiffV(m.Albedo, n, x, n - 1, 0);
                }
                // Grout lines and window frames sit on edges by design, so allow a generous margin.
                Assert.Less(across, inside * 3 + n * 12, $"{info.Name}: horizontal seam");
                Assert.Less(acrossV, insideV * 3 + n * 12, $"{info.Name}: vertical seam");
            }
        }

        static double Diff(byte[] px, int n, int y, int x0, int x1)
        {
            int a = (y * n + x0) * 4, b = (y * n + x1) * 4;
            return Math.Abs(px[a] - px[b]) + Math.Abs(px[a + 1] - px[b + 1]) + Math.Abs(px[a + 2] - px[b + 2]);
        }

        static double DiffV(byte[] px, int n, int x, int y0, int y1)
        {
            int a = (y0 * n + x) * 4, b = (y1 * n + x) * 4;
            return Math.Abs(px[a] - px[b]) + Math.Abs(px[a + 1] - px[b + 1]) + Math.Abs(px[a + 2] - px[b + 2]);
        }

        [Test]
        public void NormalMaps_AreUnitLengthAndFaceOut()
        {
            var m = SurfaceGen.Generate(SurfaceId.Paving, 64);
            for (int i = 0; i < m.Normal.Length; i += 4)
            {
                double x = m.Normal[i] / 127.5 - 1, y = m.Normal[i + 1] / 127.5 - 1, z = m.Normal[i + 2] / 127.5 - 1;
                Assert.AreEqual(1.0, Math.Sqrt(x * x + y * y + z * z), 0.03);
                Assert.Greater(z, 0.1);
            }
        }

        [Test]
        public void GroundSurfaces_AreNotFlatColours()
        {
            foreach (var id in new[] { SurfaceId.Asphalt, SurfaceId.Grass, SurfaceId.Paving, SurfaceId.Brick, SurfaceId.Concrete })
            {
                var m = SurfaceGen.Generate(id, 64);
                var lum = Enumerable.Range(0, 64 * 64).Select(i => (double)m.Albedo[i * 4] + m.Albedo[i * 4 + 1] + m.Albedo[i * 4 + 2]).ToArray();
                double mean = lum.Average(), sd = Math.Sqrt(lum.Select(v => (v - mean) * (v - mean)).Average());
                Assert.Greater(sd, 4.0, id + " has visible detail");
            }
        }

        [Test]
        public void Facades_LightSomeWindowsAtNight()
        {
            foreach (var id in new[] { SurfaceId.FacadeOffice, SurfaceId.FacadeApartment, SurfaceId.FacadeBrick })
            {
                var m = SurfaceGen.Generate(id, 256);
                int lit = 0, total = m.Emission.Length / 4;
                for (int i = 0; i < m.Emission.Length; i += 4) if (m.Emission[i] + m.Emission[i + 1] > 100) lit++;
                double share = lit / (double)total;
                Assert.Greater(share, 0.03, id + ": some windows lit");
                Assert.Less(share, 0.6, id + ": not the whole wall glowing");
            }
        }

        [Test]
        public void TintableSurfaces_AreBrightEnoughToTint()
        {
            foreach (var info in SurfaceGen.All.Where(i => i.Tintable))
            {
                var (r, g, b) = SurfaceGen.Generate(info.Id, 64).AverageAlbedo();
                Assert.Greater((r + g + b) / 3f, 0.6f, info.Name + " (a dark texture cannot be tinted lighter)");
            }
        }

        [Test]
        public void BadSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => SurfaceGen.Generate(SurfaceId.Asphalt, 100));
        }
    }

    public class MetricMeshTests
    {
        [Test]
        public void Box_UvsAreInMetres()
        {
            var m = MetricMesh.Box(4f, 3f, 2f, false);
            Assert.AreEqual(24, m.VertexCount);
            Assert.AreEqual(1, m.Submeshes.Count);
            Assert.AreEqual(36, m.Submeshes[0].Length);
            // The +Z face spans 4 m across and 3 m up.
            var faceZ = Enumerable.Range(0, m.VertexCount).Where(i => m.Normals[i * 3 + 2] > 0.5f).ToArray();
            Assert.AreEqual(4, faceZ.Length);
            Assert.AreEqual(4f, faceZ.Max(i => m.Uvs[i * 2]) - faceZ.Min(i => m.Uvs[i * 2]), 1e-4);
            Assert.AreEqual(3f, faceZ.Max(i => m.Uvs[i * 2 + 1]) - faceZ.Min(i => m.Uvs[i * 2 + 1]), 1e-4);
            // V = 0 is the bottom edge of every wall (brick courses line up with the ground).
            foreach (int i in faceZ) Assert.AreEqual(m.Positions[i * 3 + 1] < 0 ? 0f : 3f, m.Uvs[i * 2 + 1], 1e-4);
        }

        [Test]
        public void Box_SplitCaps_PutsTopAndBottomInSecondSubmesh()
        {
            var m = MetricMesh.Box(10f, 20f, 8f, true);
            Assert.AreEqual(2, m.Submeshes.Count);
            Assert.AreEqual(24, m.Submeshes[0].Length);
            Assert.AreEqual(12, m.Submeshes[1].Length);
            foreach (int v in m.Submeshes[1]) Assert.AreEqual(1f, Math.Abs(m.Normals[v * 3 + 1]), 1e-4);
        }

        static void AssertFrontFacing(MeshData m, string name)
        {
            // Unity's front faces wind clockwise as seen from outside, so cross(b - a, c - a) points out of the surface.
            foreach (var tris in m.Submeshes)
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                    float[] P(int i) => new[] { m.Positions[i * 3], m.Positions[i * 3 + 1], m.Positions[i * 3 + 2] };
                    var pa = P(a); var pb = P(b); var pc = P(c);
                    float ux = pb[0] - pa[0], uy = pb[1] - pa[1], uz = pb[2] - pa[2];
                    float vx = pc[0] - pa[0], vy = pc[1] - pa[1], vz = pc[2] - pa[2];
                    float nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                    float dot = nx * m.Normals[a * 3] + ny * m.Normals[a * 3 + 1] + nz * m.Normals[a * 3 + 2];
                    Assert.Greater(dot, 0f, $"{name}: triangle {t / 3} faces inwards");
                }
        }

        static void AssertTangentsFollowV(MeshData m, string name)
        {
            // Unity: bitangent = cross(normal, tangent) * w must point where V increases.
            foreach (var tris in m.Submeshes)
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                    // Find the edge with the largest change in V and compare directions.
                    int[] e = { a, b, c };
                    for (int k = 0; k < 3; k++)
                    {
                        int i = e[k], j = e[(k + 1) % 3];
                        float dv = m.Uvs[j * 2 + 1] - m.Uvs[i * 2 + 1], du = m.Uvs[j * 2] - m.Uvs[i * 2];
                        if (Math.Abs(dv) < 1e-4f || Math.Abs(du) > 1e-4f) continue;   // only edges where only V changes
                        float[] n = { m.Normals[i * 3], m.Normals[i * 3 + 1], m.Normals[i * 3 + 2] };
                        float[] tg = { m.Tangents[i * 4], m.Tangents[i * 4 + 1], m.Tangents[i * 4 + 2] };
                        float w = m.Tangents[i * 4 + 3];
                        float bx = (n[1] * tg[2] - n[2] * tg[1]) * w, by = (n[2] * tg[0] - n[0] * tg[2]) * w, bz = (n[0] * tg[1] - n[1] * tg[0]) * w;
                        float ex = m.Positions[j * 3] - m.Positions[i * 3], ey = m.Positions[j * 3 + 1] - m.Positions[i * 3 + 1], ez = m.Positions[j * 3 + 2] - m.Positions[i * 3 + 2];
                        Assert.Greater((bx * ex + by * ey + bz * ez) * Math.Sign(dv), 0f, $"{name}: bitangent against V");
                    }
                }
        }

        [Test]
        public void AllShapes_FaceOutwards_AndHaveConsistentTangents()
        {
            var shapes = new (string, MeshData)[]
            {
                ("box", MetricMesh.Box(3f, 2f, 5f, false)),
                ("split box", MetricMesh.Box(3f, 2f, 5f, true)),
                ("plane", MetricMesh.Plane(150f, 90f)),
                ("cylinder", MetricMesh.Cylinder(0.4f, 1.5f, 0.4f)),
            };
            foreach (var (name, m) in shapes)
            {
                AssertFrontFacing(m, name);
                AssertTangentsFollowV(m, name);
                foreach (var tris in m.Submeshes) Assert.IsTrue(tris.All(i => i >= 0 && i < m.VertexCount), name);
            }
        }

        [Test]
        public void Plane_MatchesUnitysPlaneAndScalesUvs()
        {
            var m = MetricMesh.Plane(150f, 90f);
            Assert.AreEqual(121, m.VertexCount);
            Assert.AreEqual(-5f, Enumerable.Range(0, m.VertexCount).Min(i => m.Positions[i * 3]), 1e-4);
            Assert.AreEqual(5f, Enumerable.Range(0, m.VertexCount).Max(i => m.Positions[i * 3]), 1e-4);
            Assert.AreEqual(1500f, Enumerable.Range(0, m.VertexCount).Max(i => m.Uvs[i * 2]), 1e-2);    // 1.5 km of grass in metres
            Assert.AreEqual(900f, Enumerable.Range(0, m.VertexCount).Max(i => m.Uvs[i * 2 + 1]), 1e-2);
        }

        [Test]
        public void Cylinder_UvRunsAroundInMetres()
        {
            var m = MetricMesh.Cylinder(1f, 1.5f, 1f, 24);
            float maxU = Enumerable.Range(0, m.VertexCount).Where(i => Math.Abs(m.Normals[i * 3 + 1]) < 0.5f).Max(i => m.Uvs[i * 2]);
            Assert.AreEqual(Math.PI, maxU, 1e-3);   // radius 0.5 m → circumference π m
            float maxV = Enumerable.Range(0, m.VertexCount).Where(i => Math.Abs(m.Normals[i * 3 + 1]) < 0.5f).Max(i => m.Uvs[i * 2 + 1]);
            Assert.AreEqual(3f, maxV, 1e-4);        // 2 units tall at scale 1.5
        }

        [Test]
        public void Keys_ShareMeshesBySizeToTheCentimetre()
        {
            Assert.AreEqual(MetricMesh.Key("box", 1.001f, 2f, 3f), MetricMesh.Key("box", 1.003f, 2f, 3f));
            Assert.AreNotEqual(MetricMesh.Key("box", 1f, 2f, 3f), MetricMesh.Key("box", 1.02f, 2f, 3f));
            Assert.AreNotEqual(MetricMesh.Key("box", 1f, 2f, 3f, true), MetricMesh.Key("box", 1f, 2f, 3f));
            Assert.AreEqual(MetricMesh.Key("box", -1f, 2f, 3f), MetricMesh.Key("box", 1f, 2f, 3f));
        }
    }
}

namespace CarSales.Tests
{
    using CarSales.Sim;

    /// <summary>Phase 13b: day / night, haze and wet-ground rules.</summary>
    public class RealismSimTests
    {
        [Test]
        public void Darkness_FollowsTheSun()
        {
            Assert.AreEqual(0f, RealismSim.Darkness(12f), 1e-4);
            Assert.AreEqual(1f, RealismSim.Darkness(0f), 1e-4);
            Assert.AreEqual(1f, RealismSim.Darkness(23f), 1e-4);
            Assert.AreEqual(0.5f, RealismSim.Darkness(RealismSim.Sunrise), 0.01);
            Assert.AreEqual(0.5f, RealismSim.Darkness(RealismSim.Sunset), 0.01);
            Assert.AreEqual(RealismSim.Darkness(1f), RealismSim.Darkness(25f), 1e-4);   // wraps
            float last = -1f;
            for (float h = 19f; h <= 22f; h += 0.25f) { float d = RealismSim.Darkness(h); Assert.GreaterOrEqual(d, last); last = d; }
        }

        [Test]
        public void Haze_IsThickestAtDawn_AndAlwaysPresent()
        {
            Assert.Greater(RealismSim.Haze(6.5f), RealismSim.Haze(15f));
            for (float h = 0f; h < 24f; h += 0.5f)
            {
                Assert.Greater(RealismSim.Haze(h), 0f);
                Assert.Less(RealismSim.Haze(h), 0.004f, "town must stay readable");
            }
            Assert.AreEqual(0.025f, RealismSim.FogDensity(0.025f, 12f), 1e-6);   // weather fog wins when thicker
        }

        [Test]
        public void FogColour_IsDarkAtNight_AndWarmAtGoldenHour()
        {
            var noon = RealismSim.FogColour(13f, 1f);
            var night = RealismSim.FogColour(1f, 1f);
            var dusk = RealismSim.FogColour(RealismSim.Sunset, 1f);
            Assert.Less(night.r + night.g + night.b, (noon.r + noon.g + noon.b) * 0.2f);
            Assert.Greater(noon.b, noon.r, "day haze is blue-grey");
            Assert.Greater(dusk.r - dusk.b, noon.r - noon.b, "golden hour is warmer");
            var cloudy = RealismSim.FogColour(13f, 0.4f);
            Assert.Less(cloudy.g, noon.g, "cloud dims the haze");
        }

        [Test]
        public void StreetLights_AndWindows_ComeOnAtDusk()
        {
            Assert.IsFalse(RealismSim.StreetLightsOn(RealismSim.Darkness(13f), false));
            Assert.IsTrue(RealismSim.StreetLightsOn(RealismSim.Darkness(22f), false));
            Assert.IsTrue(RealismSim.StreetLightsOn(0.2f, true), "fog and heavy rain switch them on earlier");
            Assert.IsFalse(RealismSim.StreetLightsOn(0.2f, false));
            Assert.AreEqual(0f, RealismSim.WindowGlow(0f), 1e-4);
            Assert.AreEqual(1f, RealismSim.WindowGlow(1f), 1e-4);
        }

        [Test]
        public void Wetness_SoaksFast_DriesSlowly()
        {
            float w = 0f;
            w = RealismSim.StepWetness(w, 1f, 0.2f);
            Assert.AreEqual(0.6f, w, 1e-4);
            w = RealismSim.StepWetness(w, 1f, 1f);
            Assert.AreEqual(1f, w, 1e-4, "clamped at the target");
            w = RealismSim.StepWetness(w, 0f, 1f);
            Assert.AreEqual(0.5f, w, 1e-4, "half dry after an hour");
            Assert.AreEqual(0f, RealismSim.StepWetness(w, 0f, 5f), 1e-4);
            Assert.AreEqual(0.3f, RealismSim.StepWetness(0.3f, 1f, 0f), 1e-6, "no time, no change");
            Assert.AreEqual(1f, RealismSim.WetTarget(true, true));
            Assert.AreEqual(0f, RealismSim.WetTarget(false, false));
        }

        [Test]
        public void WetGround_IsDarkerAndShinier()
        {
            Assert.AreEqual(1f, RealismSim.WetDarken(0f), 1e-6);
            Assert.Less(RealismSim.WetDarken(1f), 0.75f);
            Assert.AreEqual(0.42f, RealismSim.WetSmoothness(0.42f, 0f), 1e-6);
            Assert.AreEqual(0.9f, RealismSim.WetSmoothness(0.42f, 1f), 1e-6);
        }
    }
}

namespace CarSales.Tests
{
    using System.Collections.Generic;
    using CarSales.Sim;

    /// <summary>Phase 13c: the furniture plans for walk-in homes and showrooms.</summary>
    public class InteriorPlanTests
    {
        static bool Overlap((float minX, float minZ, float maxX, float maxZ) a, (float minX, float minZ, float maxX, float maxZ) b, float margin = 0.02f) =>
            a.minX < b.maxX - margin && a.maxX > b.minX + margin && a.minZ < b.maxZ - margin && a.maxZ > b.minZ + margin;

        static string Describe(FurnitureItem i) => $"{i.Type} at ({i.X}, {i.Z})";

        /// <summary>A chair pushed under its table or desk (by no more than 15 cm) is how real rooms look.</summary>
        static bool Tucked(FurnitureItem a, FurnitureItem b)
        {
            if (!((a.IsSeat && b.IsTable) || (b.IsSeat && a.IsTable))) return false;
            var fa = a.Footprint(); var fb = b.Footprint();
            float ox = System.Math.Min(fa.maxX, fb.maxX) - System.Math.Max(fa.minX, fb.minX);
            float oz = System.Math.Min(fa.maxZ, fb.maxZ) - System.Math.Max(fa.minZ, fb.minZ);
            return System.Math.Min(ox, oz) <= 0.15f;
        }

        [Test]
        public void Homes_FurnitureFitsInsideTheWalls_AndNothingOverlaps()
        {
            for (int tier = 0; tier <= 2; tier++)
            {
                var L = InteriorPlan.Home(tier);
                float hx = L.SizeX / 2f, hz = L.SizeZ / 2f;
                var blocking = L.Items.Where(i => i.Blocks).ToList();
                foreach (var i in blocking)
                {
                    var f = i.Footprint();
                    Assert.IsTrue(f.minX >= -hx - 0.01f && f.maxX <= hx + 0.01f && f.minZ >= -hz - 0.01f && f.maxZ <= hz + 0.01f, $"tier {tier}: {Describe(i)} pokes through a wall");
                }
                for (int a = 0; a < blocking.Count; a++)
                    for (int b = a + 1; b < blocking.Count; b++)
                    {
                        if (Tucked(blocking[a], blocking[b])) continue;
                        Assert.IsFalse(Overlap(blocking[a].Footprint(), blocking[b].Footprint()), $"tier {tier}: {Describe(blocking[a])} overlaps {Describe(blocking[b])}");
                    }
                foreach (var (x, g0, g1) in L.Partitions)
                    foreach (var i in blocking)
                    {
                        var f = i.Footprint();
                        bool crosses = f.minX < x + 0.07f && f.maxX > x - 0.07f;
                        Assert.IsFalse(crosses, $"tier {tier}: {Describe(i)} sits on the partition at x={x}");
                    }
            }
        }

        [Test]
        public void Homes_YouCanWalkFromTheDoorToTheBed()
        {
            for (int tier = 0; tier <= 2; tier++)
            {
                var L = InteriorPlan.Home(tier);
                Assert.GreaterOrEqual(L.BedIndex, 0, $"tier {tier} has a bed");
                float hx = L.SizeX / 2f, hz = L.SizeZ / 2f;
                var obstacles = InteriorPlan.HomeObstacles(L);
                Assert.IsTrue(InteriorPlan.CanWalk(-hx, -hz, hx, hz, obstacles, L.EntryX, L.EntryZ, L.Wake.x, L.Wake.z, 0.32f),
                    $"tier {tier}: no clear path from the door ({L.EntryX}, {L.EntryZ}) to beside the bed ({L.Wake.x}, {L.Wake.z})");
                // The wake point is right next to the bed (so E on the bed works straight away).
                var bed = L.Items[L.BedIndex].Footprint();
                float dx = System.Math.Max(0f, System.Math.Max(bed.minX - L.Wake.x, L.Wake.x - bed.maxX));
                float dz = System.Math.Max(0f, System.Math.Max(bed.minZ - L.Wake.z, L.Wake.z - bed.maxZ));
                Assert.Less(System.Math.Sqrt(dx * dx + dz * dz), 1.2, $"tier {tier}: wake point is too far from the bed");
            }
        }

        [Test]
        public void Homes_EveryRoomHasWindowsAndTheKitchenIsReachable()
        {
            for (int tier = 0; tier <= 2; tier++)
            {
                var L = InteriorPlan.Home(tier);
                Assert.GreaterOrEqual(L.Items.Count(i => i.Type == FurnitureType.Window), 2, $"tier {tier} windows");
                var kitchen = L.Items.First(i => i.Type == FurnitureType.Kitchen);
                var f = kitchen.Footprint();
                double r = kitchen.Yaw * System.Math.PI / 180.0;
                float fx = kitchen.X + (float)System.Math.Sin(r) * 0.75f, fz = kitchen.Z + (float)System.Math.Cos(r) * 0.75f;   // standing at the worktop
                float hx = L.SizeX / 2f, hz = L.SizeZ / 2f;
                Assert.IsTrue(InteriorPlan.CanWalk(-hx, -hz, hx, hz, InteriorPlan.HomeObstacles(L), L.EntryX, L.EntryZ, fx, fz, 0.3f), $"tier {tier}: can't reach the kitchen");
            }
        }

        [Test]
        public void Showrooms_FurnitureKeepsClearOfPlatformsAndWalls()
        {
            for (int level = 1; level <= 3; level++)
            {
                var (side, front, back) = InteriorPlan.ShowroomRoom(level);
                var items = InteriorPlan.Showroom(level).Where(i => i.Blocks).ToList();
                var obstacles = InteriorPlan.ShowroomObstacles(level);
                foreach (var i in items)
                {
                    var f = i.Footprint();
                    Assert.IsTrue(f.minX >= -side - 0.01f && f.maxX <= side + 0.01f && f.minZ >= front - 0.01f && f.maxZ <= back + 0.01f, $"level {level}: {Describe(i)} outside the showroom");
                    foreach (var o in obstacles)
                    {
                        if (i.IsSeat && o.Name == "Computer" && o.Overlaps(f.minX, f.minZ, f.maxX, f.maxZ, -0.15f) == false) continue;   // the chair at the sales desk
                        Assert.IsFalse(o.Overlaps(f.minX, f.minZ, f.maxX, f.maxZ, -0.02f), $"level {level}: {Describe(i)} overlaps the {o.Name}");
                    }
                }
                for (int a = 0; a < items.Count; a++)
                    for (int b = a + 1; b < items.Count; b++)
                    {
                        if (Tucked(items[a], items[b])) continue;
                        Assert.IsFalse(Overlap(items[a].Footprint(), items[b].Footprint()), $"level {level}: {Describe(items[a])} overlaps {Describe(items[b])}");
                    }
            }
        }

        [Test]
        public void Showrooms_CustomersCanReachEveryCarAndTheComputer()
        {
            for (int level = 1; level <= 3; level++)
            {
                var (side, front, back) = InteriorPlan.ShowroomRoom(level);
                var obstacles = InteriorPlan.Showroom(level).Where(i => i.Blocks).Select(i => i.Footprint()).ToList();
                var rects = InteriorPlan.ShowroomObstacles(level);
                foreach (var o in rects) obstacles.Add((o.MinX, o.MinZ, o.MaxX, o.MaxZ));
                const float entranceX = 0f;
                float entranceZ = front + 0.6f;
                foreach (var o in rects)
                {
                    // A customer must be able to stand beside each car platform (any side) and in front of the computer.
                    float cx = (o.MinX + o.MaxX) / 2f, cz = (o.MinZ + o.MaxZ) / 2f;
                    var sides = new[] { (cx, o.MinZ - 0.45f), (cx, o.MaxZ + 0.45f), (o.MinX - 0.45f, cz), (o.MaxX + 0.45f, cz) };
                    bool reached = sides.Any(p => p.Item1 > -side && p.Item1 < side && p.Item2 > front && p.Item2 < back &&
                                                  InteriorPlan.CanWalk(-side, front, side, back, obstacles, entranceX, entranceZ, p.Item1, p.Item2, 0.35f));
                    Assert.IsTrue(reached, $"level {level}: customers can't get to the {o.Name} at ({cx}, {cz})");
                }
            }
        }

        [Test]
        public void Footprint_TurnsWithYaw()
        {
            var sofa = new FurnitureItem(FurnitureType.Sofa, 0f, 0f, 90f, 2f);
            var f = sofa.Footprint();
            Assert.AreEqual(0.95f, f.maxX - f.minX, 1e-3);   // depth now runs along X
            Assert.AreEqual(2f, f.maxZ - f.minZ, 1e-3);
            var rug = new FurnitureItem(FurnitureType.Rug, 0f, 0f, 0f, 3f, 2f);
            Assert.IsFalse(rug.Blocks);
        }

        [Test]
        public void CanWalk_FindsPathsAroundObstacles_AndRespectsWalls()
        {
            var wall = new List<(float, float, float, float)> { (-0.1f, -5f, 0.1f, 3f) };   // a wall with a gap at the top
            Assert.IsTrue(InteriorPlan.CanWalk(-5f, -5f, 5f, 5f, wall, -3f, -3f, 3f, -3f, 0.3f));
            wall.Add((-0.1f, 3f, 0.1f, 5f));   // close the gap
            Assert.IsFalse(InteriorPlan.CanWalk(-5f, -5f, 5f, 5f, wall, -3f, -3f, 3f, -3f, 0.3f));
            Assert.IsFalse(InteriorPlan.CanWalk(-5f, -5f, 5f, 5f, new List<(float, float, float, float)>(), -4.9f, 0f, 0f, 0f, 0.3f), "start inside the wall margin");
        }
    }
}

namespace CarSales.Tests
{
    using CarSales.Studio;

    /// <summary>Phase 13d: the realism checklist's scoring and next step.</summary>
    public class RealismChecklistTests
    {
        [Test]
        public void FreshProject_ScoresLow_AndStartsWithTheBiggestWin()
        {
            var items = RealismChecklist.Evaluate(new RealismInputs { MissingAnimations = 99 });
            Assert.Less(RealismChecklist.Score(items), 15);
            Assert.AreEqual("cars", RealismChecklist.NextStep(items).Id, "real cars matter most");
            Assert.IsTrue(items.All(i => i.Next.Length > 0 && i.Why.Length > 0 && i.Title.Length > 0));
        }

        [Test]
        public void EverythingDone_Scores100_WithNoNextStep()
        {
            var items = RealismChecklist.Evaluate(new RealismInputs
            {
                GraphicsApplied = true, Skies = 1, ImportedCars = 6, ScannedKeySurfaces = 8, MainCharacter = true, MissingAnimations = 0,
                TownKindsReplaced = 4, FurnitureKindsReplaced = 9, AmbienceClips = 5, Music = true, LicenceBlockers = 0,
            });
            Assert.AreEqual(100, RealismChecklist.Score(items));
            Assert.IsNull(RealismChecklist.NextStep(items));
        }

        [Test]
        public void Progress_IsPartialAndCapped()
        {
            var items = RealismChecklist.Evaluate(new RealismInputs { ImportedCars = 3, ScannedKeySurfaces = 20, MissingAnimations = 99 });
            Assert.AreEqual(0.5f, items.First(i => i.Id == "cars").Progress, 1e-4);
            Assert.AreEqual(1f, items.First(i => i.Id == "surfaces").Progress, 1e-4);
            Assert.AreEqual(0f, items.First(i => i.Id == "characters").Progress, 1e-4);
            var withChar = RealismChecklist.Evaluate(new RealismInputs { MainCharacter = true, MissingAnimations = 3 });
            Assert.AreEqual(0.75f, withChar.First(i => i.Id == "characters").Progress, 1e-4);
            var blocked = RealismChecklist.Evaluate(new RealismInputs { LicenceBlockers = 2 });
            StringAssert.Contains("2 asset", blocked.First(i => i.Id == "licences").Next);
        }
    }
}
