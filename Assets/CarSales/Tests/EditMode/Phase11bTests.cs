using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 11b: building levels 1-3, upgrades per level, the full rebuild and what each level gives.</summary>
    public class Phase11bTests
    {
        static IEnumerable<BuildingKind> Kinds => (BuildingKind[])Enum.GetValues(typeof(BuildingKind));

        [Test]
        public void EveryKind_HasUpgradesForLevels1And2_AndUniqueIds()
        {
            foreach (var k in Kinds)
            {
                var all = BuildingRules.All(k);
                Assert.GreaterOrEqual(BuildingRules.ForTier(k, 1).Count(), 3, k + " level 1");
                Assert.GreaterOrEqual(BuildingRules.ForTier(k, 2).Count(), 3, k + " level 2");
                Assert.IsFalse(all.Any(u => u.Tier < 1 || u.Tier > 2), k + ": upgrades only lead to levels 2 and 3");
                Assert.AreEqual(all.Count, all.Select(u => u.Id).Distinct().Count(), k + " ids");
                foreach (var u in all) Assert.IsFalse(string.IsNullOrEmpty(u.Name), k + " name");
            }
        }

        [Test]
        public void Rebuild_OnlyWhenEveryUpgradeOfTheLevelIsBought()
        {
            foreach (var k in Kinds)
            {
                var bought = new HashSet<string>();
                Func<BuildingUpgrade, bool> done = u => bought.Contains(u.Id);
                Assert.IsFalse(BuildingRules.CanRebuild(k, 1, done), k + ": nothing bought");
                var tier1 = BuildingRules.ForTier(k, 1).ToList();
                foreach (var u in tier1.Take(tier1.Count - 1)) bought.Add(u.Id);
                Assert.IsFalse(BuildingRules.CanRebuild(k, 1, done), k + ": one left");
                Assert.AreEqual(1, BuildingRules.Remaining(k, 1, done).Count);
                StringAssert.Contains("1 more upgrade", BuildingRules.NextStep(k, 1, done, 100000, false));
                bought.Add(tier1.Last().Id);
                Assert.IsTrue(BuildingRules.CanRebuild(k, 1, done), k + ": all bought");
                StringAssert.Contains("rebuild", BuildingRules.NextStep(k, 1, done, 100000, false));
                // Level 2 upgrades don't count until you're level 2.
                Assert.IsFalse(BuildingRules.CanRebuild(k, 2, done), k + ": level 2 needs its own upgrades");
                foreach (var u in BuildingRules.ForTier(k, 2)) bought.Add(u.Id);
                Assert.IsTrue(BuildingRules.CanRebuild(k, 2, done));
                Assert.IsFalse(BuildingRules.CanRebuild(k, 3, done), "level 3 is the top");
                Assert.AreEqual("Fully upgraded.", BuildingRules.NextStep(k, 3, done, 0, false));
                StringAssert.Contains("overnight", BuildingRules.NextStep(k, 1, done, 0, true));
            }
        }

        [Test]
        public void Upgrades_CanOnlyBeBoughtAtTheirLevel()
        {
            var u1 = BuildingRules.ForTier(BuildingKind.Home, 1).First();
            var u2 = BuildingRules.ForTier(BuildingKind.Home, 2).First();
            Assert.IsTrue(BuildingRules.CanBuy(u1, 1, false, false));
            Assert.IsFalse(BuildingRules.CanBuy(u2, 1, false, false), "next level's upgrade waits for the rebuild");
            Assert.IsFalse(BuildingRules.CanBuy(u1, 1, true, false), "already bought");
            Assert.IsFalse(BuildingRules.CanBuy(u1, 1, false, true), "builders on site");
            Assert.IsTrue(BuildingRules.CanBuy(u2, 2, false, false));
            Assert.IsFalse(BuildingRules.CanBuy(u2, 3, false, false));
        }

        [Test]
        public void RebuildCosts_RiseWithLevel_AndScaleWithPrice()
        {
            foreach (var k in Kinds)
            {
                Assert.AreEqual(0, BuildingRules.RebuildCost(k, 1, 100000));
                Assert.AreEqual(0, BuildingRules.RebuildCost(k, 4, 100000));
                Assert.Greater(BuildingRules.RebuildCost(k, 3, 100000), BuildingRules.RebuildCost(k, 2, 100000), k.ToString());
                Assert.Greater(BuildingRules.RebuildCost(k, 2, 0), 0, k + ": free buildings still cost something to rebuild");
            }
            Assert.Greater(BuildingRules.RebuildCost(BuildingKind.Home, 2, 350000), BuildingRules.RebuildCost(BuildingKind.Home, 2, 85000), "a villa costs more than a house");
            Assert.AreEqual(140000, BuildingRules.RebuildCost(BuildingKind.Investment, 2, 350000), "40% of the price");
        }

        [Test]
        public void Effects_GrowWithLevel_AndCountBoughtUpgrades()
        {
            var none = new HashSet<string>();
            Func<BuildingUpgrade, bool> nothing = u => false;
            Assert.AreEqual(0, BuildingRules.Effects(BuildingKind.Dealership, 1, nothing).Footfall);
            var lights = BuildingRules.Find(BuildingKind.Dealership, "lights");
            Assert.AreEqual(lights.Effect.Footfall, BuildingRules.Effects(BuildingKind.Dealership, 1, u => u.Id == "lights").Footfall, 1e-5);
            // After the rebuild the level 1 upgrades stay (they were all bought) plus the rebuild's own gain.
            var l2 = BuildingRules.Effects(BuildingKind.Dealership, 2, nothing);
            var l3 = BuildingRules.Effects(BuildingKind.Dealership, 3, nothing);
            Assert.Greater(l2.Footfall, BuildingRules.ForTier(BuildingKind.Dealership, 1).Sum(u => u.Effect.Footfall));
            Assert.Greater(l3.Footfall, l2.Footfall);
            Assert.AreEqual(4, l2.ExtraSpots, "level 2 unlocks the 4 lot spots");
            Assert.AreEqual(8, l3.ExtraSpots, "level 3 adds 4 showroom spots");
            Assert.AreEqual(8, BuildingRules.Effects(BuildingKind.MainDealership, 3, nothing).ExtraSpots, "same spots on AUTO WORLD");
            Assert.Greater(BuildingRules.Effects(BuildingKind.Investment, 3, nothing).Income, BuildingRules.Effects(BuildingKind.Investment, 2, nothing).Income);
            Assert.Greater(BuildingRules.Effects(BuildingKind.Home, 3, nothing).WakeEarlier, 0f);
            Assert.Greater(BuildingRules.Effects(BuildingKind.ServiceCentre, 2, nothing).JobSpeed, 0f);
            Assert.Greater(BuildingRules.RebuildGain(BuildingKind.Home, 3).WakeEarlier, 0f);
            StringAssert.Contains("parking", BuildingRules.RebuildGain(BuildingKind.Dealership, 2).Describe());
        }

        [Test]
        public void MainDealership_UsesTheUpgradesApp()
        {
            var main = BuildingRules.All(BuildingKind.MainDealership);
            Assert.IsTrue(main.All(u => u.IsLinked), "AUTO WORLD's upgrades are the existing Upgrades app ones");
            Assert.AreEqual(5, main.Select(u => u.LinkedUpgrade).Distinct().Count(), "all five upgrade types");
            foreach (var t in main.Select(u => u.LinkedUpgrade).Distinct())
                Assert.Greater(main.Where(u => u.LinkedUpgrade == t && u.Tier == 2).Max(u => u.LinkedLevel), main.Where(u => u.LinkedUpgrade == t && u.Tier == 1).Max(u => u.LinkedLevel), "level 2 asks for higher upgrade levels");
            // Linked effects aren't counted twice (the Upgrades app applies them).
            Assert.AreEqual(0f, BuildingRules.Effects(BuildingKind.MainDealership, 1, u => true).Footfall);
        }

        [Test]
        public void UpgradePrices_UseThePropertyPrice_WithAMinimum()
        {
            var kitchen = BuildingRules.Find(BuildingKind.Home, "kitchen");
            Assert.AreEqual(kitchen.MinCost, kitchen.PriceFor(0));
            Assert.AreEqual(17500, kitchen.PriceFor(350000), "5% of a $350,000 villa");
            var lift = BuildingRules.Find(BuildingKind.ServiceCentre, "lift2");
            Assert.AreEqual(12000, lift.PriceFor(999999), "fixed price");
        }

        [Test]
        public void LevelNames_AndOldDevelopments()
        {
            foreach (var k in Kinds) for (int l = 1; l <= 3; l++) Assert.IsFalse(string.IsNullOrEmpty(BuildingRules.LevelName(k, l)));
            Assert.AreEqual(1, BuildingRules.LevelFromDevelopment(0));
            Assert.AreEqual(2, BuildingRules.LevelFromDevelopment(1));
            Assert.AreEqual(3, BuildingRules.LevelFromDevelopment(3), "capped at 3");
        }
    }
}
