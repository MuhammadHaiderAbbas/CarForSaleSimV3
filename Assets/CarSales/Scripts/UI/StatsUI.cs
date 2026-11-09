using TMPro;
using UnityEngine;

namespace CarSales
{
    public class StatsUI : ComputerPage
    {
        RectTransform grid;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 12f);
            grid = UIFactory.ScrollGrid(Area, new Vector2(285f, 120f), 4, 14f);
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(grid);
            var p = CarSalesServices.Progress;
            var s = p.Stats;
            var eco = CarSalesServices.Economy;
            Stat("Net worth", UIFactory.Money(p.NetWorth), UIFactory.Gold);
            Stat("Win goal", UIFactory.Money(p.WinNetWorth), UIFactory.TextDim);
            Stat("Cash", UIFactory.Money(eco.Money), eco.Money < 0 ? UIFactory.Bad : UIFactory.Good);
            Stat("Reputation", $"{eco.Reputation:0}/100", UIFactory.TextMain);
            Stat("Days played", s.daysPlayed.ToString(), UIFactory.TextMain);
            Stat("Cars sold", s.carsSold.ToString(), UIFactory.TextMain);
            Stat("Cars bought", s.carsBought.ToString(), UIFactory.TextMain);
            Stat("Repairs & washes", s.carsServiced.ToString(), UIFactory.TextMain);
            Stat("Total revenue", UIFactory.Money(s.totalRevenue), UIFactory.TextMain);
            Stat("Total profit", UIFactory.Money(s.totalProfit), s.totalProfit < 0 ? UIFactory.Bad : UIFactory.Good);
            Stat("Best single profit", UIFactory.Money(s.bestSale), UIFactory.TextMain);
            Stat("Avg. profit / car", UIFactory.Money(s.carsSold > 0 ? s.totalProfit / s.carsSold : 0), UIFactory.TextMain);
            Stat("Test drives", s.testDrives.ToString(), UIFactory.TextMain);
            Stat("Upgrades bought", s.upgradesBought.ToString(), UIFactory.TextMain);
            Stat("Goals completed", $"{Mathf.Min(p.ObjectiveIndex, p.ObjectiveCount)}/{p.ObjectiveCount}", UIFactory.TextMain);
            Stat("Loan", UIFactory.Money(CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0), UIFactory.Warn);
        }

        void Stat(string label, string value, Color color)
        {
            var card = UIFactory.Panel(grid, label, UIFactory.CardBg, 1.4f);
            var v = UIFactory.Vertical(card, 18, 6f);
            v.padding = new RectOffset(20, 16, 18, 14);
            UIFactory.Text(card.transform, label.ToUpper(), 14, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            UIFactory.Text(card.transform, value, 30, color, TextAlignmentOptions.Left, true);
        }
    }
}
