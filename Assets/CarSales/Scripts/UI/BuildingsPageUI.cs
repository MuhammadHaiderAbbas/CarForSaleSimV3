using System.Linq;
using CarSales.Sim;
using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11b Buildings app: every building you can upgrade, its level (1-3) and look, the upgrades of the current
    /// level, and the full rebuild once they're all bought. Dealership upgrades for AUTO WORLD are the Upgrades app ones.
    /// </summary>
    public class BuildingsPageUI : ComputerPage
    {
        RectTransform list;
        string selected;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 10f);
            list = UIFactory.ScrollList(Area, 8f);
        }

        void OnEnable() { if (CarSalesServices.Buildings) CarSalesServices.Buildings.OnBuildingChanged += Changed; }
        void OnDisable() { if (CarSalesServices.Buildings) CarSalesServices.Buildings.OnBuildingChanged -= Changed; }
        void Changed(string _) { if (list && isActiveAndEnabled) Refresh(); }

        static BuildingManager BM => CarSalesServices.Buildings;

        public void Show(string buildingId) { selected = buildingId; Refresh(); }

        RectTransform Row(string text, float h = 64f, Color? bg = null)
        {
            var row = UIFactory.Panel(list, "Row", bg ?? UIFactory.CardBg, 1.8f);
            UIFactory.LE(row, h: h);
            var hl = UIFactory.Horizontal(row, 10, 12f);
            hl.padding = new RectOffset(16, 12, 8, 8);
            var t = UIFactory.Text(row.transform, text, 16, UIFactory.TextMain, TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(t, flexW: 1);
            return row.rectTransform;
        }

        void Button(RectTransform row, string label, Color c, UnityEngine.Events.UnityAction a, bool on = true, float w = 190f)
        {
            var b = UIFactory.Btn(row, label, c, () => { a(); Refresh(); }, 42f, 14f);
            UIFactory.LE(b, h: 42, w: w, flexW: 0);
            b.interactable = on;
        }

        static string Stars(int level) => $"[{new string('#', level)}{new string('-', BuildingRules.MaxLevel - level)}]";

        public override void Refresh()
        {
            UIFactory.ClearChildren(list);
            if (!BM) { UIFactory.EmptyState(list, Loc.T("buildings.none", "No buildings"), ""); return; }
            if (string.IsNullOrEmpty(selected)) Overview(); else Detail(selected);
        }

        void Overview()
        {
            UIFactory.SectionTitle(list, Loc.T("buildings.title", "Your buildings"));
            var hint = UIFactory.Text(list, Loc.T("buildings.hint", "Buy every upgrade of a building's level, then rebuild it: builders work overnight and the next morning it's a completely new, bigger building (3 levels)."), 15, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
            hint.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(hint, h: 44);
            foreach (var id in BM.Buildings().Where(BuildingManager.Owns).Concat(BM.Buildings().Where(i => !BuildingManager.Owns(i))))
            {
                var k = BuildingManager.KindOf(id).Value;
                bool owned = BuildingManager.Owns(id);
                int lvl = BM.Level(id);
                string state = !owned ? $"<color={UIFactory.Hex(UIFactory.TextFaint)}>not owned</color>" :
                               BM.Constructing(id) ? $"<color={UIFactory.Hex(UIFactory.Warn)}>under construction</color>" :
                               BM.CanRebuild(id) ? $"<color={UIFactory.Hex(UIFactory.Good)}>ready to rebuild</color>" : BM.NextStep(id);
                var row = Row($"<b>{BuildingManager.NameOf(id)}</b>   <color={UIFactory.Hex(UIFactory.Gold)}>{Stars(lvl)}</color>  level {lvl}: {BuildingRules.LevelName(k, lvl)}\n<size=85%>{state}</size>", 70f);
                string sid = id;
                Button(row, owned ? Loc.T("buildings.open", "Upgrades") : Loc.T("buildings.view", "View"), owned ? UIFactory.Accent : UIFactory.Neutral, () => selected = sid, true, 150f);
            }
        }

        void Detail(string id)
        {
            var k = BuildingManager.KindOf(id).Value;
            int lvl = BM.Level(id);
            bool owned = BuildingManager.Owns(id), building = BM.Constructing(id);
            var top = Row($"<b>{BuildingManager.NameOf(id)}</b>   <color={UIFactory.Hex(UIFactory.Gold)}>{Stars(lvl)}</color>\nLevel {lvl}: {BuildingRules.LevelName(k, lvl)}", 70f);
            Button(top, Loc.T("common.back", "Back"), UIFactory.Neutral, () => selected = null, true, 120f);
            Row($"Now: {BM.Effects(id).Describe()}", 46f, UIFactory.CardLight);

            if (lvl < BuildingRules.MaxLevel)
            {
                UIFactory.SectionTitle(list, $"Level {lvl} upgrades");
                foreach (var u in BuildingRules.ForTier(k, lvl))
                {
                    bool done = BM.IsDone(id, u);
                    string effect = u.IsLinked ? u.Description : u.Effect.Describe();
                    var row = Row($"{(done ? "DONE  " : "")}<b>{u.Name}</b>{(u.IsLinked ? $" (level {u.LinkedLevel})" : "")}\n<size=85%>{u.Description}{(u.IsLinked ? "" : "  " + effect)}</size>", 62f);
                    var uu = u;
                    if (!done) Button(row, $"{Loc.T("buildings.buy", "Buy")} {UIFactory.Money(BM.UpgradePrice(id, u))}", UIFactory.Good, () => BM.BuyUpgrade(id, uu), owned && !building, 190f);
                }
                int to = lvl + 1;
                var rb = Row($"<b>Rebuild to level {to}: {BuildingRules.LevelName(k, to)}</b>\n<size=85%>{BuildingRules.RebuildGain(k, to).Describe()}. {BM.NextStep(id)}</size>", 76f, UIFactory.CardLight);
                Button(rb, $"{Loc.T("buildings.rebuild", "Rebuild")} {UIFactory.Money(BM.RebuildCost(id))}", UIFactory.Accent, () => BM.OfferRebuild(id), owned && BM.CanRebuild(id), 220f);
            }
            else Row("<b>Fully upgraded.</b> This is the best this building gets.", 50f, UIFactory.CardLight);

            // Preview of what's next.
            if (lvl + 1 < BuildingRules.MaxLevel + 1 && lvl < 2)
            {
                UIFactory.SectionTitle(list, "After the rebuild (level 2 upgrades)");
                foreach (var u in BuildingRules.ForTier(k, 2)) Row($"<color={UIFactory.Hex(UIFactory.TextDim)}>{u.Name}  -  {(u.IsLinked ? u.Description : u.Effect.Describe())}</color>", 40f);
            }
        }
    }
}
