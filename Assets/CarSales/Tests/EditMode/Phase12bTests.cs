using System;
using System.Linq;
using CarSales.Foundation;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 12b: every interface icon parses and draws something sensible.</summary>
    public class Phase12bTests
    {
        static int Filled(byte[] a) => a.Count(v => v > 127);

        [Test]
        public void EveryIcon_Rasterises_ToAVisibleShape()
        {
            foreach (UIIcon icon in Enum.GetValues(typeof(UIIcon)))
            {
                if (icon == UIIcon.None) continue;
                Assert.IsTrue(IconLibrary.Svg.ContainsKey(icon), icon + " has SVG data");
                var a = VectorIcon.Rasterize(IconLibrary.Svg[icon], 48);
                int filled = Filled(a);
                Assert.Greater(filled, 48 * 48 / 40, $"{icon}: almost nothing drawn ({filled} px)");
                Assert.Less(filled, 48 * 48 * 9 / 10, $"{icon}: nearly the whole square is filled ({filled} px) - the background box was drawn");
            }
        }

        [Test]
        public void Rectangle_FillsExactlyItsArea()
        {
            var a = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M6 6h12v12H6z\"/></svg>", 24, 4, 0f);
            Assert.AreEqual(144, Filled(a), "12x12 square");
            Assert.AreEqual(255, a[12 * 24 + 12]);
            Assert.AreEqual(0, a[2 * 24 + 2]);
        }

        [Test]
        public void Holes_UseTheNonZeroRule_AndArcsMakeCircles()
        {
            // A ring: outer square clockwise, inner square counter-clockwise = a hole.
            var ring = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M2 2h20v20H2z M8 8v8h8V8z\"/></svg>", 24, 4, 0f);
            Assert.AreEqual(0, ring[12 * 24 + 12], "hole in the middle");
            Assert.AreEqual(255, ring[4 * 24 + 4]);
            var circle = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M12 2a10 10 0 1 0 0.001 0z\"/></svg>", 96, 4, 0f);
            double area = Filled(circle) / (96.0 * 96.0) * 576.0;   // in SVG units
            Assert.AreEqual(Math.PI * 100, area, 12, "a circle of radius 10");
        }

        [Test]
        public void Relative_Commands_AndCurves()
        {
            var polys = VectorIcon.PathPolys("m4 4 l4 0 l0 4 l-4 0 z M10 10 c0 2 2 4 4 4 s4 -2 4 -4 z");
            Assert.AreEqual(2, polys.Count);
            Assert.AreEqual(8f, polys[0].Max(p => p.X), 1e-4);
            Assert.AreEqual(14f, polys[1].Max(p => p.Y), 0.2f);
        }
    }
}

namespace CarSales.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using CarSales.Studio;
    using NUnit.Framework;

    /// <summary>Phase 12c: Sketchfab links, real brands, release checklist.</summary>
    public class Phase12cTests
    {
        [Test]
        public void SketchfabLinks_GiveTheModelId()
        {
            Assert.AreEqual("8e87379f40fd40dcac0a751e22c1a188", SketchfabLinks.Uid("https://sketchfab.com/3d-models/bmw-m4-f82-8e87379f40fd40dcac0a751e22c1a188"));
            Assert.AreEqual("f42d9d13b0404b5b9a2302a4c590a94d", SketchfabLinks.Uid("https://sketchfab.com/3d-models/mersedes-benz-s65-w222-f42d9d13b0404b5b9a2302a4c590a94d?utm=x"));
            Assert.IsNull(SketchfabLinks.Uid("https://sketchfab.com/someone"));
            var pasted = "https://sketchfab.com/3d-models/a-138a753d04dd4d8f8982d8f5583d212d\nhttps://sketchfab.com/3d-models/b-c35a14d811b042d792a6da69381f7f80\n\nhttps://sketchfab.com/3d-models/a-138a753d04dd4d8f8982d8f5583d212d";
            CollectionAssert.AreEqual(new[] { "138a753d04dd4d8f8982d8f5583d212d", "c35a14d811b042d792a6da69381f7f80" }, SketchfabLinks.Uids(pasted));
        }

        [Test]
        public void Brands_AreFound_ByWholeWord()
        {
            CollectionAssert.AreEquivalent(new[] { "Mercedes-Benz" }, TrademarkCheck.Brands("Mersedes-Benz S65 W222"));
            CollectionAssert.Contains(TrademarkCheck.Brands("Mercedes-Benz S63 Coupe Brabus 800"), "Brabus");
            CollectionAssert.AreEqual(new[] { "BMW" }, TrademarkCheck.Brands("BMW M4 F82"));
            CollectionAssert.AreEqual(new[] { "Toyota" }, TrademarkCheck.Brands("Toyota Crown Athlete S210"));
            Assert.IsEmpty(TrademarkCheck.Brands("Kestrel Velo"), "the game's own fictional makes are fine");
            Assert.IsEmpty(TrademarkCheck.Brands("Fordham Motors"), "whole words only");
            Assert.IsEmpty(TrademarkCheck.Brands(""));
        }

        [Test]
        public void ReleaseChecklist_BlocksWhatCantShip()
        {
            var good = ReleaseRules.Evaluate(true, 16, 0, 0, new List<string>(), false, "Haider Games", "Car For Sale Simulator V2", "0.9.0", true, 12);
            Assert.IsTrue(ReleaseRules.CanRelease(good), string.Join("\n", good.Where(c => c.Level != CheckLevel.Pass).Select(c => c.Title + ": " + c.Detail)));

            var noPeople = ReleaseRules.Evaluate(false, 0, 4, 0, new List<string>(), false, "Haider Games", "Car For Sale Simulator V2", "0.9.0", true, 12);
            Assert.IsFalse(ReleaseRules.CanRelease(noPeople), "no characters: plain shapes would ship");

            var brands = ReleaseRules.Evaluate(true, 16, 0, 0, new List<string> { "BMW M4 F82" }, false, "Haider Games", "Car For Sale Simulator V2", "0.9.0", true, 12);
            Assert.IsFalse(ReleaseRules.CanRelease(brands), "real brands need a decision");
            var acknowledged = ReleaseRules.Evaluate(true, 16, 0, 0, new List<string> { "BMW M4 F82" }, true, "Haider Games", "Car For Sale Simulator V2", "0.9.0", true, 12);
            Assert.IsTrue(ReleaseRules.CanRelease(acknowledged), "after confirming the rights it's a warning");
            Assert.AreEqual(CheckLevel.Warning, acknowledged.First(c => c.Title == "Car brands").Level);

            var defaults = ReleaseRules.Evaluate(true, 16, 0, 0, new List<string>(), false, "DefaultCompany", "CarSalesSim", "0.9.0", true, 12);
            Assert.IsFalse(ReleaseRules.CanRelease(defaults));
            var blockers = ReleaseRules.Evaluate(true, 16, 0, 2, new List<string>(), false, "A", "B", "1", false, 12);
            Assert.AreEqual(2, blockers.Count(c => c.Level == CheckLevel.Blocker), "licences and notices");
        }
    }
}

namespace CarSales.Tests
{
    using CarSales.Studio;
    using NUnit.Framework;

    public class ImportLimitTests
    {
        [Test]
        public void HighDetailCars_AreNeverBlocked()
        {
            var stats = new ModelStats { SizeX = 1.9f, SizeY = 1.4f, SizeZ = 4.8f, Triangles = 454913, Vertices = 300000, Renderers = 40, Materials = 48 };
            var issues = ImportValidation.Validate(AssetCategory.Vehicle, stats, 4.8f, new LicenceInfo { licence = LicenceType.CCBY, author = "someone" }, true, 1);
            Assert.IsFalse(issues.Exists(i => i.Level == IssueLevel.Error && i.Message.Contains("triangles")), "no triangle limit");
            Assert.IsTrue(issues.Exists(i => i.Level == IssueLevel.Warning && i.Message.Contains("triangles")), "still advised");
            stats.Triangles = 3000000;
            issues = ImportValidation.Validate(AssetCategory.Vehicle, stats, 4.8f, new LicenceInfo { licence = LicenceType.CCBY, author = "someone" }, true, 1);
            Assert.IsFalse(ImportValidation.HasErrors(issues), string.Join("; ", issues.ConvertAll(i => i.Message)));
        }
    }
}
