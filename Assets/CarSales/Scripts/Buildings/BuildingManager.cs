using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Foundation;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    [Serializable]
    public class BuildingRecord
    {
        public string id;
        public int level = 1;
        public List<string> upgrades = new List<string>();
        public int constructingTo;      // > 0: builders are on site
        public int readyDay;
        public int offeredFor;          // the level whose "rebuild?" question was already asked
    }

    [Serializable]
    public class BuildingsState
    {
        public int version = 1;
        public List<BuildingRecord> buildings = new List<BuildingRecord>();
    }

    /// <summary>
    /// Phase 11b: levels 1-3 for every building you own (dealerships, the service centre, homes, investment
    /// properties). Buy the current level's upgrades, then you're asked to pay for the full rebuild; builders work
    /// overnight and in the morning the building is replaced by the next level's (BuildingLevelVisual). Module "buildings".
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public const string ServiceCentreId = "service_centre";
        BuildingsState state = new BuildingsState();
        public BuildingsState State => state;
        public event Action<string> OnBuildingChanged;

        void Awake() => CarSalesServices.Buildings = this;

        void Start()
        {
            // AUTO WORLD's upgrades are bought in the Upgrades app: ask about the rebuild when the last one is bought there too.
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged += UpgradesChanged;
        }

        void OnDestroy() { if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged -= UpgradesChanged; }

        void UpgradesChanged()
        {
            var main = DealershipManager.Main;
            if (!main) return;
            Changed(main.dealershipId);
            TryOffer(main.dealershipId);
        }

        /// <summary>Asks once per level (you can always rebuild later from the Buildings app).</summary>
        void TryOffer(string id)
        {
            if (!CanRebuild(id)) return;
            var r = Record(id);
            if (r.offeredFor >= r.level) return;
            r.offeredFor = r.level;
            OfferRebuild(id);
        }
        void OnEnable() => EventBus.Subscribe<DayStarted>(OnDayStarted);
        void OnDisable() => EventBus.Unsubscribe<DayStarted>(OnDayStarted);

        public void SetState(BuildingsState s)
        {
            state = s ?? new BuildingsState();
            if (s == null) MigrateDevelopments();
            foreach (var v in BuildingLevelVisual.All) v.Apply(instant: true);
            OnBuildingChanged?.Invoke(null);
        }

        public void ResetState() { state = new BuildingsState(); foreach (var v in BuildingLevelVisual.All) v.Apply(instant: true); OnBuildingChanged?.Invoke(null); }

        /// <summary>Phase 9 saves: developed investment properties keep their level.</summary>
        void MigrateDevelopments()
        {
            var biz = CarSalesServices.Business; var props = CarSalesServices.Properties;
            if (!biz || !props) return;
            foreach (var p in props.Properties.Where(p => p.type == PropertyType.Investment))
            {
                int dev = biz.DevelopmentLevel(p.id);
                if (dev > 0) Record(p.id).level = BuildingRules.LevelFromDevelopment(dev);
            }
        }

        // ------------------------------------------------------------------ WHAT A BUILDING IS
        public static BuildingKind? KindOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == ServiceCentreId) return BuildingKind.ServiceCentre;
            var main = DealershipManager.Main;
            if (main && main.dealershipId == id) return BuildingKind.MainDealership;
            var p = CarSalesServices.Properties ? CarSalesServices.Properties.Get(id) : null;
            if (p == null) return DealershipManager.Find(id) && DealershipManager.Find(id).dealershipId == id ? BuildingKind.Dealership : (BuildingKind?)null;
            return p.type == PropertyType.Home ? BuildingKind.Home : p.type == PropertyType.Investment ? BuildingKind.Investment : BuildingKind.Dealership;
        }

        public static string NameOf(string id)
        {
            if (id == ServiceCentreId) return "AUTO WORLD Service Centre";
            var p = CarSalesServices.Properties ? CarSalesServices.Properties.Get(id) : null;
            if (p != null) return p.displayName;
            var d = DealershipManager.Find(id);
            return d ? d.displayName : id;
        }

        static int PriceOf(string id) { var p = CarSalesServices.Properties ? CarSalesServices.Properties.Get(id) : null; return p != null ? p.price : 0; }

        public static bool Owns(string id)
        {
            if (id == ServiceCentreId) return true;
            var main = DealershipManager.Main;
            if (main && main.dealershipId == id) return true;
            return CarSalesServices.Properties && CarSalesServices.Properties.Owns(id);
        }

        /// <summary>Every building that can be upgraded (owned or not), in a stable order.</summary>
        public IEnumerable<string> Buildings()
        {
            var ids = new List<string>();
            if (DealershipManager.Main) ids.Add(DealershipManager.Main.dealershipId);
            foreach (var d in DealershipManager.All) if (d && !ids.Contains(d.dealershipId)) ids.Add(d.dealershipId);
            ids.Add(ServiceCentreId);
            if (CarSalesServices.Properties)
                foreach (var p in CarSalesServices.Properties.Properties)
                    if ((p.type == PropertyType.Home || p.type == PropertyType.Investment) && !ids.Contains(p.id)) ids.Add(p.id);
            return ids.Where(i => KindOf(i).HasValue);
        }

        BuildingRecord Record(string id)
        {
            var r = state.buildings.Find(b => b.id == id);
            if (r == null) { r = new BuildingRecord { id = id }; state.buildings.Add(r); }
            return r;
        }

        BuildingRecord Peek(string id) => state.buildings.Find(b => b.id == id);

        public int Level(string id) { var r = Peek(id); return r != null ? Mathf.Clamp(r.level, 1, BuildingRules.MaxLevel) : 1; }
        public bool Constructing(string id) { var r = Peek(id); return r != null && r.constructingTo > 0; }

        public bool IsDone(string id, BuildingUpgrade u)
        {
            if (u == null) return false;
            if (u.IsLinked) return CarSalesServices.Upgrades && CarSalesServices.Upgrades.Level((UpgradeType)u.LinkedUpgrade) >= u.LinkedLevel;
            var r = Peek(id);
            return r != null && r.upgrades.Contains(u.Id);
        }

        public BuildingEffects Effects(string id)
        {
            var k = KindOf(id);
            if (!k.HasValue || !Owns(id)) return default;
            return BuildingRules.Effects(k.Value, Level(id), u => IsDone(id, u));
        }

        public string NextStep(string id)
        {
            var k = KindOf(id);
            return k.HasValue ? BuildingRules.NextStep(k.Value, Level(id), u => IsDone(id, u), PriceOf(id), Constructing(id)) : "";
        }

        public int UpgradePrice(string id, BuildingUpgrade u)
        {
            if (u.IsLinked) return CarSalesServices.Upgrades ? Mathf.Max(0, CarSalesServices.Upgrades.NextCost((UpgradeType)u.LinkedUpgrade)) : 0;
            return u.PriceFor(PriceOf(id));
        }

        public int RebuildCost(string id) { var k = KindOf(id); return k.HasValue ? BuildingRules.RebuildCost(k.Value, Level(id) + 1, PriceOf(id)) : 0; }
        public bool CanRebuild(string id) { var k = KindOf(id); return k.HasValue && Owns(id) && !Constructing(id) && BuildingRules.CanRebuild(k.Value, Level(id), u => IsDone(id, u)); }

        // ------------------------------------------------------------------ BUYING
        public bool BuyUpgrade(string id, BuildingUpgrade u)
        {
            var k = KindOf(id);
            if (!k.HasValue || u == null) return false;
            if (!Owns(id)) { CarSalesServices.UI.Error("Buy the building first."); return false; }
            if (!BuildingRules.CanBuy(u, Level(id), IsDone(id, u), Constructing(id)))
            { CarSalesServices.UI.Error(u.Tier > Level(id) ? $"That's a level {u.Tier} upgrade: rebuild first." : "Not available."); return false; }
            if (u.IsLinked)
            {
                // Main dealership: the Upgrades app levels (may need several purchases to reach the level).
                var up = CarSalesServices.Upgrades;
                if (!up || !up.TryBuy((UpgradeType)u.LinkedUpgrade, out string msg)) { CarSalesServices.UI.Error(up ? "Not enough money!" : "No upgrades."); return false; }
                CarSalesServices.UI.Success("Upgraded", msg);
            }
            else
            {
                int cost = u.PriceFor(PriceOf(id));
                if (!CarSalesServices.Economy.TrySpend(cost, TxCategory.Upgrades, $"{NameOf(id)}: {u.Name}", id)) { CarSalesServices.UI.Error($"You need {UIFactory.Money(cost)}."); return false; }
                Record(id).upgrades.Add(u.Id);
                CarSalesServices.PlaySfx(Sfx.Purchase);
                CarSalesServices.UI.Success(u.Name, $"{NameOf(id)}: {u.Effect.Describe()}.");
            }
            Changed(id);
            if (!u.IsLinked) TryOffer(id);   // linked ones come back through UpgradesChanged
            return true;
        }

        /// <summary>"Everything bought - rebuild it?" (the system asks you once the level is complete).</summary>
        public void OfferRebuild(string id)
        {
            var k = KindOf(id);
            if (!k.HasValue || !CanRebuild(id)) return;
            int to = Level(id) + 1, cost = RebuildCost(id);
            string body = $"Every level {to - 1} upgrade at {NameOf(id)} is done. Rebuild it into a {BuildingRules.LevelName(k.Value, to).ToLower()}?\n\n" +
                          $"The builders work overnight; tomorrow the building is completely new: {BuildingRules.RebuildGain(k.Value, to).Describe()}.\n\nCost: {UIFactory.Money(cost)}";
            UIFactory.Confirm($"Rebuild to level {to}", body, $"Rebuild for {UIFactory.Money(cost)}", UIFactory.Good, () => StartRebuild(id));
        }

        public bool StartRebuild(string id)
        {
            if (!CanRebuild(id)) { CarSalesServices.UI.Error(NextStep(id)); return false; }
            int cost = RebuildCost(id);
            var k = KindOf(id).Value;
            if (!CarSalesServices.Economy.TrySpend(cost, TxCategory.PropertyDevelopment, $"Rebuild {NameOf(id)} to level {Level(id) + 1}", id))
            { CarSalesServices.UI.Error($"You need {UIFactory.Money(cost)} for the rebuild."); return false; }
            var r = Record(id);
            r.constructingTo = r.level + 1;
            r.readyDay = CarRecords.Today + BuildingRules.ConstructionNights;
            CarSalesServices.PlaySfx(Sfx.Success);
            CarSalesServices.UI.Success("Builders booked", $"{NameOf(id)} will be a {BuildingRules.LevelName(k, r.constructingTo).ToLower()} tomorrow morning.");
            Changed(id);
            return true;
        }

        void OnDayStarted(DayStarted e)
        {
            var finished = state.buildings.Where(b => b.constructingTo > 0 && e.Day >= b.readyDay).ToList();
            foreach (var b in finished)
            {
                b.level = Mathf.Clamp(b.constructingTo, 1, BuildingRules.MaxLevel);
                b.constructingTo = 0;
                var k = KindOf(b.id);
                if (CarSalesServices.Messages) CarSalesServices.Messages.Add(MessageType.Info, $"Builders: {NameOf(b.id)} is finished", $"Your {(k.HasValue ? BuildingRules.LevelName(k.Value, b.level).ToLower() : "building")} is ready: {(k.HasValue ? BuildingRules.RebuildGain(k.Value, b.level).Describe() : "")}.");
                Changed(b.id);
            }
            // Reputation from fine buildings.
            float rep = Buildings().Where(Owns).Sum(id => Effects(id).ReputationPerDay);
            if (rep > 0f && CarSalesServices.Economy) CarSalesServices.Economy.ChangeReputation(rep);
        }

        void Changed(string id)
        {
            foreach (var v in BuildingLevelVisual.All) if (v.buildingId == id) v.Apply(instant: false);
            foreach (var d in DealershipManager.All) if (d) d.RefreshSpotVisuals();
            OnBuildingChanged?.Invoke(id);
        }

        // ------------------------------------------------------------------ EFFECTS READ BY OTHER SYSTEMS
        public float FootfallFactor(string dealershipId) => 1f + Effects(dealershipId).Footfall;
        public float BudgetFactor(string dealershipId) => 1f + Effects(dealershipId).Budget;
        public float IncomeFactor(string propertyId) => 1f + Effects(propertyId).Income;
        public float RepairCostFactor => Mathf.Clamp(1f - Effects(ServiceCentreId).RepairDiscount, 0.5f, 1f);
        public float JobSpeedFactor => 1f + Effects(ServiceCentreId).JobSpeed;
        public int ExtraStaff => Buildings().Where(Owns).Sum(id => Effects(id).ExtraStaff);
        public float WakeEarlier(string homeId) => Effects(homeId).WakeEarlier;
        public float MorningEnergy(string homeId) => Effects(homeId).Energy;
    }
}
