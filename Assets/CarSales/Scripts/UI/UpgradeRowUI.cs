using TMPro;
using UnityEngine;

namespace CarSales
{
    public class UpgradeRowUI : MonoBehaviour
    {
        public static UpgradeRowUI Create(Transform parent, UpgradeType type, UpgradesUI owner)
        {
            var up = CarSalesServices.Upgrades;
            var info = up.Info(type);
            int level = up.Level(type), max = up.MaxLevel(type);

            var card = UIFactory.Panel(parent, "Upgrade_" + type, UIFactory.CardBg, 1.4f);
            UIFactory.LE(card, h: 120);
            var h = UIFactory.Horizontal(card, 16, 18f);
            h.childForceExpandHeight = false;

            string glyph = type == UpgradeType.LotExpansion ? "P" : type == UpgradeType.Marketing ? "M"
                         : type == UpgradeType.Mechanic ? "W" : type == UpgradeType.Detailing ? "D" : "S";
            UIFactory.Icon(card.transform, UIFactory.Accent, glyph, 56f);

            var col = UIFactory.VCol(card.transform, 4f);
            UIFactory.Text(col, info.displayName, 23, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.Text(col, info.description, 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            var pips = UIFactory.HRow(col, 12, 6, "Levels", false);
            for (int i = 0; i < max; i++)
            {
                var pip = UIFactory.Panel(pips, "Pip", i < level ? UIFactory.Gold : UIFactory.Ghost, 6f);
                UIFactory.LE(pip, h: 10, w: 46, flexW: 0);
            }
            UIFactory.Spacer(pips, 0f, 1f);

            var lvl = UIFactory.Text(card.transform, $"Level\n<size=140%><b>{level}/{max}</b></size>", 16, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.LE(lvl, w: 90, flexW: 0);

            if (up.IsMaxed(type))
            {
                var b = UIFactory.Btn(card.transform, "MAXED", UIFactory.Neutral, null, 52f, 18f);
                b.interactable = false;
                UIFactory.LE(b, h: 52, w: 190, flexW: 0);
            }
            else
            {
                int cost = up.NextCost(type);
                var b = UIFactory.Btn(card.transform, $"Upgrade\n{UIFactory.Money(cost)}", UIFactory.Accent, () => owner.Buy(type), 60f, 18f);
                b.interactable = CarSalesServices.Economy.CanAfford(cost);
                UIFactory.LE(b, h: 60, w: 190, flexW: 0);
            }
            return card.gameObject.AddComponent<UpgradeRowUI>();
        }
    }
}
