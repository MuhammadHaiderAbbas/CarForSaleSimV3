using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using CarSales.Studio;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 11a: MOT lines, bidder names, character animation rules and AI look-alikes.</summary>
    public class Phase11Tests
    {
        // ------------------------------------------------------------------ FIXES
        [Test]
        public void Mot_ListsEachPartOnce()
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                var rng = new Random(seed);
                var comps = new List<ComponentState>();
                ComponentSet.Ensure(comps, PowertrainType.Petrol, new[] { 35f, 40f, 30f, 45f, 50f, 40f }, () => (float)(rng.NextDouble() * 30 - 15));
                var faults = FaultModel.Generate(comps, 160000, rng);
                var mot = MotTest.Run(comps, faults, 160000, 10, 2026);
                var lines = mot.fails.Concat(mot.advisories).ToList();
                Assert.AreEqual(lines.Count, lines.Distinct().Count(), $"seed {seed}: a line appears twice");
                Assert.IsFalse(mot.fails.Any(f => mot.advisories.Contains(f)), $"seed {seed}: same line as fail and advisory");
                // No part reported twice (component check + fault on the same part).
                var shocks = lines.Count(l => l.ToLowerInvariant().Contains("shock"));
                Assert.LessOrEqual(shocks, 1, $"seed {seed}: shock absorbers listed {shocks} times: {string.Join(" | ", lines)}");
                var bushes = lines.Count(l => l.ToLowerInvariant().Contains("bush"));
                Assert.LessOrEqual(bushes, 1, $"seed {seed}: bushes listed {bushes} times: {string.Join(" | ", lines)}");
                Assert.AreEqual(mot.fails.Count == 0, mot.passed);
            }
        }

        [Test]
        public void Mot_FaultThatFails_UpgradesAnAdvisoryOnTheSamePart()
        {
            var comps = new List<ComponentState>();
            ComponentSet.Ensure(comps, PowertrainType.Petrol, new[] { 90f, 90f, 90f, 90f, 90f, 90f }, () => 0f);
            ComponentSet.Set(comps, ComponentId.ShockAbsorbers, 35f);   // advisory from the part check
            var leak = FaultCatalog.All.FirstOrDefault(f => f.Component == ComponentId.ShockAbsorbers && f.FailsMot(1f));
            if (leak == null) Assert.Ignore("No shock absorber fault that fails the MOT in the catalogue.");
            var mot = MotTest.Run(comps, new List<FaultInstance> { new FaultInstance(leak.Id, 1f) }, 50000, 1, 2026);
            Assert.Contains(leak.Name, mot.fails);
            Assert.IsFalse(mot.advisories.Any(a => a.ToLowerInvariant().Contains("shock")), "the advisory was replaced by the fail");
            Assert.IsFalse(mot.passed);
        }

        [Test]
        public void Bidders_HaveDifferentNames_ThatFitTheirTrade()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                var bidders = AuctionRules.GenerateBidders(10, new Random(seed));
                var names = bidders.Select(b => b.name).ToList();
                Assert.AreEqual(names.Count, names.Distinct().Count(), $"seed {seed}: {string.Join(", ", names)}");
                foreach (var b in bidders)
                {
                    if (b.Archetype == BidderArchetype.Breaker) StringAssert.IsMatch("Breakers|Salvage", b.name);
                    if (b.Archetype == BidderArchetype.Exporter) StringAssert.IsMatch("Export", b.name);
                    if (b.Archetype == BidderArchetype.DealerGroup) StringAssert.Contains("Group", b.name);
                    if (b.Archetype == BidderArchetype.PrivateBuyer) Assert.AreEqual(2, b.name.Split(' ').Length, "a private buyer has a first name and a surname");
                }
            }
        }

        // ------------------------------------------------------------------ CHARACTERS
        [TestCase("Breathing Idle.fbx", CharacterAnim.Idle)]
        [TestCase("Assets/CarSales/Art/Characters/Animations/Walking.fbx", CharacterAnim.Walk)]
        [TestCase("Running (1).fbx", CharacterAnim.Run)]
        [TestCase("Sitting Idle", CharacterAnim.SitIdle)]
        [TestCase("Talking On Phone", CharacterAnim.PhoneCall)]
        [TestCase("Kneeling Pointing", CharacterAnim.Kneel)]
        [TestCase("Standing Greeting", CharacterAnim.Wave)]
        [TestCase("Looking Around", CharacterAnim.LookAround)]
        [TestCase("Raising Hand", CharacterAnim.RaiseHand)]
        [TestCase("Clapping", CharacterAnim.Clap)]
        [TestCase("Happy", CharacterAnim.Cheer)]
        [TestCase("Angry", CharacterAnim.Angry)]
        [TestCase("Thinking", CharacterAnim.Thinking)]
        [TestCase("Typing", CharacterAnim.Typing)]
        [TestCase("Talking", CharacterAnim.Talking)]
        [TestCase("Pointing", CharacterAnim.Point)]
        public void Match_MixamoNames(string name, CharacterAnim expected) => Assert.AreEqual(expected, CharacterSetup.Match(name));

        [Test]
        public void Match_IgnoresNoise()
        {
            Assert.IsNull(CharacterSetup.Match("mixamo.com"));
            Assert.IsNull(CharacterSetup.Match("__preview__Take 001"));
            Assert.IsNull(CharacterSetup.Match(""));
        }

        [Test]
        public void EverySearchTerm_MatchesItsOwnSlot()
        {
            // Downloading exactly what the list says must land in the right slot (Mixamo keeps the name as the file name).
            foreach (var r in CharacterSetup.Animations)
                Assert.AreEqual(r.Anim, CharacterSetup.Match(r.MixamoSearch + ".fbx"), r.MixamoSearch);
            Assert.AreEqual(Enum.GetValues(typeof(CharacterAnim)).Length, CharacterSetup.Animations.Length, "every animation has a requirement");
            Assert.AreEqual(CharacterSetup.Animations.Length, CharacterSetup.Animations.Select(a => a.Anim).Distinct().Count());
        }

        [Test]
        public void Fallbacks_FindTheClosestAvailable_AndNeverLoop()
        {
            var have = new HashSet<CharacterAnim> { CharacterAnim.Idle, CharacterAnim.Walk, CharacterAnim.Talking };
            Func<CharacterAnim, bool> has = have.Contains;
            Assert.AreEqual(CharacterAnim.Walk, CharacterSetup.Resolve(CharacterAnim.Run, has));
            Assert.AreEqual(CharacterAnim.Talking, CharacterSetup.Resolve(CharacterAnim.Wave, has));
            Assert.AreEqual(CharacterAnim.Talking, CharacterSetup.Resolve(CharacterAnim.PhoneCall, has));
            Assert.AreEqual(CharacterAnim.Idle, CharacterSetup.Resolve(CharacterAnim.Typing, has));
            Assert.AreEqual(CharacterAnim.Idle, CharacterSetup.Resolve(CharacterAnim.Kneel, has));
            // Clap -> Cheer -> Clap... must stop; with nothing similar it gives up (or reaches Talking via Wave).
            foreach (CharacterAnim a in Enum.GetValues(typeof(CharacterAnim)))
                Assert.DoesNotThrow(() => CharacterSetup.Resolve(a, x => false));
            Assert.IsNull(CharacterSetup.Resolve(CharacterAnim.Clap, x => false));
            Assert.AreEqual(CharacterAnim.Clap, CharacterSetup.Resolve(CharacterAnim.Clap, x => true));
        }

        [Test]
        public void RequiredSet_IsIdleWalkRunTalking()
        {
            var req = CharacterSetup.Animations.Where(a => a.Required).Select(a => a.Anim).OrderBy(a => a).ToList();
            CollectionAssert.AreEqual(new[] { CharacterAnim.Idle, CharacterAnim.Walk, CharacterAnim.Run, CharacterAnim.Talking }, req);
            Assert.AreEqual(4, CharacterSetup.MissingRequired(a => false).Count);
            Assert.AreEqual(0, CharacterSetup.MissingRequired(a => true).Count);
            Assert.IsTrue(CharacterSetup.Requirement(CharacterAnim.Walk).InPlace);
            Assert.IsTrue(CharacterSetup.Requirement(CharacterAnim.Run).InPlace);
        }

        [Test]
        public void Prompt_ShowsUntilSetUp_OrDismissed_AndAgainAfterAnUpdate()
        {
            int v = CharacterSetup.SetupVersion;
            Assert.IsTrue(CharacterSetup.ShouldPrompt(0, false, 0), "fresh project: ask");
            Assert.IsTrue(CharacterSetup.ShouldPrompt(0, true, 2), "set up but missing animations: ask");
            Assert.IsFalse(CharacterSetup.ShouldPrompt(0, true, 0), "all set up: don't ask");
            Assert.IsFalse(CharacterSetup.ShouldPrompt(v, false, 0), "said don't ask again for this version");
            Assert.IsTrue(CharacterSetup.ShouldPrompt(v - 1, false, 0), "dismissed an older version: ask again");
            var text = CharacterSetup.PromptText();
            foreach (var r in CharacterSetup.Animations) StringAssert.Contains(r.MixamoSearch, text);
            StringAssert.Contains(CharacterSetup.AnimationsFolder, text);
        }

        [TestCase("Ch08_Body", MaterialRole.Skin)]
        [TestCase("Hair_Mat", MaterialRole.Hair)]
        [TestCase("Eyelashes", MaterialRole.Eyes)]
        [TestCase("Shirt", MaterialRole.Top)]
        [TestCase("Jeans", MaterialRole.Bottom)]
        [TestCase("Sneakers", MaterialRole.Shoes)]
        [TestCase("Baseball_Cap", MaterialRole.Accessory)]
        [TestCase("Material_001", MaterialRole.Other)]
        public void Classify_Materials(string name, MaterialRole role) => Assert.AreEqual(role, VariantGenerator.Classify(name));

        [Test]
        public void Variants_AreVaried_Deterministic_AndSensible()
        {
            var parts = new[] { "Hat", "Glasses" };
            var blends = new[] { "Fat", "Thin" };
            var a = VariantGenerator.Generate(16, 42, parts, blends);
            var b = VariantGenerator.Generate(16, 42, parts, blends);
            Assert.AreEqual(16, a.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Height, b[i].Height, "same seed, same cast");
                Assert.AreEqual(a[i].Tints[MaterialRole.Top], b[i].Tints[MaterialRole.Top]);
                Assert.That(a[i].Height, Is.InRange(0.9f, 1.1f));
                Assert.That(a[i].Build, Is.InRange(0.9f, 1.12f));
                Assert.That(a[i].SkinTone, Is.InRange(0.5f, 1f));
                foreach (var w in a[i].BlendShapes.Values) Assert.That(w, Is.InRange(0f, 60f));
                Assert.IsNull(VariantGenerator.TintFor(a[i], MaterialRole.Eyes), "eyes keep their colour");
            }
            Assert.Greater(a.Select(v => v.Tints[MaterialRole.Top]).Distinct().Count(), 8, "clothes differ");
            Assert.Greater(a.Select(v => Math.Round(v.Height, 2)).Distinct().Count(), 8, "heights differ");
            Assert.AreEqual(4, a.Count(v => v.Role == CharacterUse.Staff), "a quarter wear workwear");
            Assert.IsTrue(a.Any(v => v.HiddenParts.Count > 0) && a.Any(v => v.HiddenParts.Count < parts.Length), "optional parts vary");
            Assert.AreNotEqual(a[0].Height, VariantGenerator.Generate(16, 43, parts, blends)[0].Height, "a new seed reshuffles");
        }

        [Test]
        public void OptionalParts_AndShapeBlends()
        {
            Assert.IsTrue(VariantGenerator.IsOptionalPart("Sunglasses_Mesh"));
            Assert.IsTrue(VariantGenerator.IsOptionalPart("Beard"));
            Assert.IsFalse(VariantGenerator.IsOptionalPart("Body"));
            Assert.IsTrue(VariantGenerator.IsShapeBlend("Body_Fat"));
            Assert.IsFalse(VariantGenerator.IsShapeBlend("viseme_aa"));
            Assert.IsFalse(VariantGenerator.IsShapeBlend("EyeBlink_L"));
        }
    }
}
