using System;
using TMPro;
using UnityEngine;

namespace CarSales
{
    public class UpgradesUI : ComputerPage
    {
        RectTransform list;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 12f);
            var t = UIFactory.Text(Area, "Invest in your dealership. Upgrades are permanent.", 18, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(t, h: 28);
            list = UIFactory.ScrollList(Area, 12f);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(list);
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType))) UpgradeRowUI.Create(list, type, this);
        }

        public void Buy(UpgradeType type)
        {
            if (CarSalesServices.Upgrades.TryBuy(type, out string msg)) CarSalesServices.UI.Success("Upgrade purchased", msg);
            else CarSalesServices.UI.Error(msg);
            Refresh();
        }
    }
}
