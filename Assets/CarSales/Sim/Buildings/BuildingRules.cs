using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSales.Sim
{
    /// <summary>What kind of building it is (saved as numbers: only add at the end).</summary>
    public enum BuildingKind { MainDealership = 0, Dealership = 1, ServiceCentre = 2, Home = 3, Investment = 4 }

    /// <summary>What a building gives you. Added up from its level and the upgrades bought.</summary>
    public struct BuildingEffects
    {
        public int ExtraSpots;          // parking spots unlocked (dealerships)
        public float Footfall;          // +share of customers
        public float Budget;            // +share of customer budgets
        public float ReputationPerDay;
        public float Income;            // +share of nightly income (investments)
        public float RepairDiscount;    // -share of parts and labour (service centre)
        public float JobSpeed;          // +share of workshop speed
        public float WakeEarlier;       // hours (homes)
        public float Energy;            // +energy when you wake (homes)
        public int ExtraStaff;          // staff slots (dealerships)

        public static BuildingEffects operator +(BuildingEffects a, BuildingEffects b) => new BuildingEffects
        {
            ExtraSpots = a.ExtraSpots + b.ExtraSpots, Footfall = a.Footfall + b.Footfall, Budget = a.Budget + b.Budget,
            ReputationPerDay = a.ReputationPerDay + b.ReputationPerDay, Income = a.Income + b.Income, RepairDiscount = a.RepairDiscount + b.RepairDiscount,
            JobSpeed = a.JobSpeed + b.JobSpeed, WakeEarlier = a.WakeEarlier + b.WakeEarlier, Energy = a.Energy + b.Energy, ExtraStaff = a.ExtraStaff + b.ExtraStaff,
        };

        public string Describe()
        {
            var parts = new List<string>();
            if (ExtraSpots > 0) parts.Add($"+{ExtraSpots} parking spots");
            if (Footfall > 0) parts.Add($"+{Footfall * 100:0}% customers");
            if (Budget > 0) parts.Add($"+{Budget * 100:0}% customer budgets");
            if (ReputationPerDay > 0) parts.Add($"+{ReputationPerDay:0.#} reputation a day");
            if (Income > 0) parts.Add($"+{Income * 100:0}% income");
            if (RepairDiscount > 0) parts.Add($"{RepairDiscount * 100:0}% cheaper repairs");
            if (JobSpeed > 0) parts.Add($"+{JobSpeed * 100:0}% workshop speed");
            if (WakeEarlier > 0) parts.Add($"wake {WakeEarlier * 60:0} min earlier");
            if (Energy > 0) parts.Add($"+{Energy:0} energy in the morning");
            if (ExtraStaff > 0) parts.Add($"+{ExtraStaff} staff slots");
            return parts.Count == 0 ? "No effect" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// One upgrade you buy for a building at a level. Tier 1 upgrades are bought at level 1 and lead to the level 2
    /// rebuild; tier 2 at level 2 lead to level 3. Main-dealership upgrades are the Upgrades app ones (linked).
    /// </summary>
    public sealed class BuildingUpgrade
    {
        public string Id, Name, Description;
        public int Tier;
        public int Cost;                 // fixed cost; or
        public float CostShare;          // share of the property price (homes, investments, other dealerships)
        public int MinCost;
        public int LinkedUpgrade = -1;   // UpgradeType index (main dealership), with LinkedLevel
        public int LinkedLevel;
        public BuildingEffects Effect;
        public bool IsLinked => LinkedUpgrade >= 0;
        public int PriceFor(int propertyPrice) => Cost > 0 ? Cost : Math.Max(MinCost, (int)Math.Round(propertyPrice * CostShare / 100.0 / 100.0) * 100);   // share %, rounded to £100
    }

    /// <summary>
    /// Phase 11b building levels: every building you own goes from level 1 to 3. Buy each upgrade of the current
    /// level, then pay for the full rebuild: builders work overnight and the building is replaced with the next,
    /// completely different one (bigger, new look, more of what it does).
    /// </summary>
    public static class BuildingRules
    {
        public const int MaxLevel = 3;
        public const int ConstructionNights = 1;

        // UpgradeType order in the runtime: LotExpansion 0, Marketing 1, Mechanic 2, Detailing 3, Showroom 4.
        static BuildingUpgrade Link(string id, string name, int type, int level, int tier) =>
            new BuildingUpgrade { Id = id, Name = name, Tier = tier, LinkedUpgrade = type, LinkedLevel = level, Description = $"Upgrades app: {name} level {level}." };

        static readonly Dictionary<BuildingKind, BuildingUpgrade[]> Upgrades = new Dictionary<BuildingKind, BuildingUpgrade[]>
        {
            { BuildingKind.MainDealership, new[]
            {
                Link("lot1", "Lot Expansion", 0, 1, 1), Link("mkt1", "Marketing", 1, 1, 1), Link("mech1", "Mechanic Workshop", 2, 1, 1),
                Link("det1", "Detailing Bay", 3, 1, 1), Link("show1", "Showroom Upgrade", 4, 1, 1),
                Link("lot2", "Lot Expansion", 0, 2, 2), Link("mkt3", "Marketing", 1, 3, 2), Link("mech3", "Mechanic Workshop", 2, 3, 2),
                Link("det2", "Detailing Bay", 3, 2, 2), Link("show3", "Showroom Upgrade", 4, 3, 2),
            } },
            { BuildingKind.Dealership, new[]
            {
                new BuildingUpgrade { Id = "lights", Name = "Forecourt floodlights", Tier = 1, CostShare = 4f, MinCost = 4000, Description = "The cars shine at night.", Effect = new BuildingEffects { Footfall = 0.04f } },
                new BuildingUpgrade { Id = "signs", Name = "Pennants & signage", Tier = 1, CostShare = 2.5f, MinCost = 3000, Description = "Passers-by notice you.", Effect = new BuildingEffects { Footfall = 0.03f } },
                new BuildingUpgrade { Id = "lounge", Name = "Customer lounge", Tier = 1, CostShare = 5f, MinCost = 6000, Description = "Coffee and comfy chairs.", Effect = new BuildingEffects { ReputationPerDay = 0.2f } },
                new BuildingUpgrade { Id = "totem", Name = "Digital totem sign", Tier = 2, CostShare = 8f, MinCost = 12000, Description = "A lit sign by the road.", Effect = new BuildingEffects { Footfall = 0.05f } },
                new BuildingUpgrade { Id = "refit", Name = "Showroom refit", Tier = 2, CostShare = 12f, MinCost = 20000, Description = "New floors, lights and displays.", Effect = new BuildingEffects { Budget = 0.05f } },
                new BuildingUpgrade { Id = "coffee", Name = "Coffee bar", Tier = 2, CostShare = 6f, MinCost = 8000, Description = "Customers stay longer.", Effect = new BuildingEffects { ReputationPerDay = 0.3f } },
            } },
            { BuildingKind.ServiceCentre, new[]
            {
                new BuildingUpgrade { Id = "lift2", Name = "Second lift", Tier = 1, Cost = 12000, Description = "Two cars up at once.", Effect = new BuildingEffects { JobSpeed = 0.1f } },
                new BuildingUpgrade { Id = "diag", Name = "Diagnostic computer", Tier = 1, Cost = 8000, Description = "Finds the fault first time.", Effect = new BuildingEffects { RepairDiscount = 0.05f } },
                new BuildingUpgrade { Id = "balancer", Name = "Tyre balancer", Tier = 1, Cost = 6000, Description = "Tyres done in-house.", Effect = new BuildingEffects { JobSpeed = 0.05f } },
                new BuildingUpgrade { Id = "booth", Name = "Paint booth", Tier = 2, Cost = 30000, Description = "Body work without the town garage.", Effect = new BuildingEffects { RepairDiscount = 0.05f } },
                new BuildingUpgrade { Id = "store", Name = "Parts store", Tier = 2, Cost = 18000, Description = "Common parts on the shelf.", Effect = new BuildingEffects { RepairDiscount = 0.05f } },
                new BuildingUpgrade { Id = "align", Name = "Wheel alignment rig", Tier = 2, Cost = 20000, Description = "Laser alignment.", Effect = new BuildingEffects { JobSpeed = 0.1f } },
            } },
            { BuildingKind.Home, new[]
            {
                new BuildingUpgrade { Id = "bed", Name = "Proper bed", Tier = 1, CostShare = 2f, MinCost = 1500, Description = "You sleep better.", Effect = new BuildingEffects { Energy = 5f } },
                new BuildingUpgrade { Id = "kitchen", Name = "New kitchen", Tier = 1, CostShare = 5f, MinCost = 4000, Description = "Breakfast at home.", Effect = new BuildingEffects { Energy = 5f } },
                new BuildingUpgrade { Id = "garden", Name = "Garden", Tier = 1, CostShare = 3f, MinCost = 2500, Description = "Neighbours are impressed.", Effect = new BuildingEffects { ReputationPerDay = 0.1f } },
                new BuildingUpgrade { Id = "gym", Name = "Home gym", Tier = 2, CostShare = 8f, MinCost = 8000, Description = "More energy all day.", Effect = new BuildingEffects { Energy = 10f } },
                new BuildingUpgrade { Id = "office", Name = "Home office", Tier = 2, CostShare = 6f, MinCost = 6000, Description = "Work calls from home.", Effect = new BuildingEffects { ReputationPerDay = 0.2f } },
                new BuildingUpgrade { Id = "garage", Name = "Double garage", Tier = 2, CostShare = 10f, MinCost = 10000, Description = "Room for your own cars.", Effect = new BuildingEffects { WakeEarlier = 0.25f } },
            } },
            { BuildingKind.Investment, new[]
            {
                new BuildingUpgrade { Id = "lobby", Name = "Refurbished lobby", Tier = 1, CostShare = 3f, MinCost = 2000, Description = "Better tenants.", Effect = new BuildingEffects { Income = 0.05f } },
                new BuildingUpgrade { Id = "security", Name = "Security & CCTV", Tier = 1, CostShare = 3f, MinCost = 2000, Description = "Fewer empty units.", Effect = new BuildingEffects { Income = 0.05f } },
                new BuildingUpgrade { Id = "facade", Name = "New facade", Tier = 1, CostShare = 4f, MinCost = 3000, Description = "Higher rents.", Effect = new BuildingEffects { Income = 0.05f } },
                new BuildingUpgrade { Id = "solar", Name = "Solar roof", Tier = 2, CostShare = 5f, MinCost = 4000, Description = "Lower running costs.", Effect = new BuildingEffects { Income = 0.06f } },
                new BuildingUpgrade { Id = "lifts", Name = "New lifts", Tier = 2, CostShare = 5f, MinCost = 4000, Description = "Premium units.", Effect = new BuildingEffects { Income = 0.06f } },
                new BuildingUpgrade { Id = "concierge", Name = "Concierge", Tier = 2, CostShare = 6f, MinCost = 5000, Description = "Top-end tenants.", Effect = new BuildingEffects { Income = 0.08f } },
            } },
        };

        /// <summary>What the rebuild itself gives at each level (level 1 = nothing extra).</summary>
        static BuildingEffects LevelEffect(BuildingKind k, int level)
        {
            if (level <= 1) return default;
            bool top = level >= 3;
            switch (k)
            {
                case BuildingKind.MainDealership:
                    return top ? new BuildingEffects { ExtraSpots = 8, Footfall = 0.3f, Budget = 0.1f, ReputationPerDay = 1.5f, ExtraStaff = 6 }
                               : new BuildingEffects { ExtraSpots = 4, Footfall = 0.1f, ReputationPerDay = 0.5f, ExtraStaff = 2 };
                case BuildingKind.Dealership:
                    return top ? new BuildingEffects { ExtraSpots = 8, Footfall = 0.25f, Budget = 0.08f, ReputationPerDay = 1f, ExtraStaff = 4 }
                               : new BuildingEffects { ExtraSpots = 4, Footfall = 0.1f, ReputationPerDay = 0.3f, ExtraStaff = 2 };
                case BuildingKind.ServiceCentre:
                    return top ? new BuildingEffects { JobSpeed = 0.4f, RepairDiscount = 0.1f } : new BuildingEffects { JobSpeed = 0.15f };
                case BuildingKind.Home:
                    return top ? new BuildingEffects { WakeEarlier = 1f, Energy = 15f, ReputationPerDay = 0.5f } : new BuildingEffects { WakeEarlier = 0.5f, Energy = 5f, ReputationPerDay = 0.2f };
                default:
                    return top ? new BuildingEffects { Income = 0.6f } : new BuildingEffects { Income = 0.3f };
            }
        }

        public static IReadOnlyList<BuildingUpgrade> All(BuildingKind k) => Upgrades[k];
        public static IEnumerable<BuildingUpgrade> ForTier(BuildingKind k, int tier) => Upgrades[k].Where(u => u.Tier == tier);
        public static BuildingUpgrade Find(BuildingKind k, string id) => Array.Find(Upgrades[k], u => u.Id == id);

        /// <summary>The full rebuild to <paramref name="toLevel"/>.</summary>
        public static int RebuildCost(BuildingKind k, int toLevel, int propertyPrice)
        {
            if (toLevel < 2 || toLevel > MaxLevel) return 0;
            bool top = toLevel == 3;
            switch (k)
            {
                case BuildingKind.MainDealership: return top ? 180000 : 60000;
                case BuildingKind.ServiceCentre: return top ? 140000 : 50000;
                case BuildingKind.Dealership: return RoundTo(Math.Max(top ? 100000 : 40000, propertyPrice * (top ? 0.7 : 0.35)));
                case BuildingKind.Home: return RoundTo(Math.Max(top ? 60000 : 25000, propertyPrice * (top ? 0.8 : 0.4)));
                default: return RoundTo(Math.Max(top ? 50000 : 20000, propertyPrice * (top ? 0.8 : 0.4)));
            }
        }

        static int RoundTo(double v) => (int)Math.Round(v / 1000.0) * 1000;

        /// <summary>Upgrades of the current level still to buy before the rebuild.</summary>
        public static List<BuildingUpgrade> Remaining(BuildingKind k, int level, Func<BuildingUpgrade, bool> done) =>
            level >= MaxLevel ? new List<BuildingUpgrade>() : ForTier(k, level).Where(u => !done(u)).ToList();

        /// <summary>Every upgrade of this level is bought: the full rebuild can be offered.</summary>
        public static bool CanRebuild(BuildingKind k, int level, Func<BuildingUpgrade, bool> done) =>
            level >= 1 && level < MaxLevel && Remaining(k, level, done).Count == 0;

        /// <summary>Can this upgrade be bought now? Only the current level's ones (the next level's wait for the rebuild).</summary>
        public static bool CanBuy(BuildingUpgrade u, int level, bool alreadyDone, bool constructing) =>
            u != null && !alreadyDone && !constructing && u.Tier == level && level < MaxLevel;

        public static BuildingEffects Effects(BuildingKind k, int level, Func<BuildingUpgrade, bool> done)
        {
            var e = LevelEffect(k, Math.Max(1, Math.Min(MaxLevel, level)));
            // Upgrades of earlier levels were all bought before their rebuild; this level's count once bought.
            // Linked ones (main dealership) have their effect in the Upgrades app already.
            foreach (var u in Upgrades[k])
                if (!u.IsLinked && (u.Tier < level || (u.Tier == level && done(u)))) e += u.Effect;
            return e;
        }

        public static BuildingEffects RebuildGain(BuildingKind k, int toLevel)
        {
            var a = LevelEffect(k, toLevel); var b = LevelEffect(k, toLevel - 1);
            return new BuildingEffects
            {
                ExtraSpots = a.ExtraSpots - b.ExtraSpots, Footfall = a.Footfall - b.Footfall, Budget = a.Budget - b.Budget, ReputationPerDay = a.ReputationPerDay - b.ReputationPerDay,
                Income = a.Income - b.Income, RepairDiscount = a.RepairDiscount - b.RepairDiscount, JobSpeed = a.JobSpeed - b.JobSpeed, WakeEarlier = a.WakeEarlier - b.WakeEarlier,
                Energy = a.Energy - b.Energy, ExtraStaff = a.ExtraStaff - b.ExtraStaff,
            };
        }

        static readonly Dictionary<BuildingKind, string[]> Names = new Dictionary<BuildingKind, string[]>
        {
            { BuildingKind.MainDealership, new[] { "Forecourt & showroom", "Two-storey showroom", "Flagship glass showroom" } },
            { BuildingKind.Dealership, new[] { "Showroom", "Two-storey showroom", "Flagship glass showroom" } },
            { BuildingKind.ServiceCentre, new[] { "Three-bay garage", "Service centre with office", "Modern service centre" } },
            { BuildingKind.Home, new[] { "As bought", "Extended", "Rebuilt" } },
            { BuildingKind.Investment, new[] { "As bought", "Refurbished and extended", "Landmark rebuild" } },
        };

        public static string LevelName(BuildingKind k, int level) => Names[k][Math.Max(1, Math.Min(MaxLevel, level)) - 1];

        /// <summary>What to do next, in one sentence.</summary>
        public static string NextStep(BuildingKind k, int level, Func<BuildingUpgrade, bool> done, int propertyPrice, bool constructing)
        {
            if (constructing) return "Builders are working overnight - the new building is ready in the morning.";
            if (level >= MaxLevel) return "Fully upgraded.";
            var left = Remaining(k, level, done);
            if (left.Count > 0) return $"Buy {left.Count} more upgrade{(left.Count == 1 ? "" : "s")}, then rebuild to level {level + 1}.";
            return $"All upgrades done: rebuild into a {LevelName(k, level + 1).ToLowerInvariant()} for £{RebuildCost(k, level + 1, propertyPrice):N0}.";
        }

        /// <summary>Old saves: a developed investment property (Phase 9, 0-3) starts at the matching building level.</summary>
        public static int LevelFromDevelopment(int developmentLevel) => Math.Max(1, Math.Min(MaxLevel, 1 + developmentLevel));
    }
}
