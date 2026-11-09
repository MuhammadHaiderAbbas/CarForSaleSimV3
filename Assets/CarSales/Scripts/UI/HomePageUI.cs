using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>Computer dashboard: key numbers, current goal, today's results, shortcuts.</summary>
    public class HomePageUI : ComputerPage
    {
        TextMeshProUGUI welcome, cash, worth, rep, cars, loan, goal, today, tip;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 16f);
            welcome = UIFactory.Text(Area, "Welcome back!", 32, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(welcome, h: 76);

            var row = UIFactory.HRow(Area, 112, 14);
            cash = StatCard(row, "Cash");
            worth = StatCard(row, "Net worth");
            rep = StatCard(row, "Reputation");
            cars = StatCard(row, "Cars on lot");
            loan = StatCard(row, "Loan");

            var goalCard = UIFactory.Panel(Area, "Goal", UIFactory.CardBg, 1.4f);
            UIFactory.Vertical(goalCard, 18, 6f);
            UIFactory.Text(goalCard.transform, "CURRENT GOAL", 15, UIFactory.Accent, TextAlignmentOptions.Left, true);
            goal = UIFactory.Text(goalCard.transform, "", 22, UIFactory.TextMain, TextAlignmentOptions.Left, true);

            var todayCard = UIFactory.Panel(Area, "Today", UIFactory.CardBg, 1.4f);
            UIFactory.Vertical(todayCard, 18, 6f);
            UIFactory.Text(todayCard.transform, "TODAY", 15, UIFactory.Accent, TextAlignmentOptions.Left, true);
            today = UIFactory.Text(todayCard.transform, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left);

            var buttons = UIFactory.HRow(Area, 58, 14);
            UIFactory.Btn(buttons, "Browse Marketplace", UIFactory.Accent, () => CarSalesServices.UI.marketplace.Show(), 56f);
            UIFactory.Btn(buttons, "My Inventory", UIFactory.AccentDark, () => CarSalesServices.UI.inventory.Show(), 56f);
            UIFactory.Btn(buttons, "Messages", UIFactory.AccentDark, () => CarSalesServices.UI.messages.Show(), 56f);
            UIFactory.Btn(buttons, "Upgrades", UIFactory.Neutral, () => CarSalesServices.UI.upgrades.Show(), 56f);

            UIFactory.Spacer(Area);
            tip = UIFactory.Text(Area, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
        }

        static TextMeshProUGUI StatCard(Transform parent, string label)
        {
            var card = UIFactory.Panel(parent, label, UIFactory.CardBg, 1.4f);
            var v = UIFactory.Vertical(card, 16, 4f);
            v.padding = new RectOffset(18, 16, 16, 14);
            UIFactory.Text(card.transform, label.ToUpper(), 14, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            return UIFactory.Text(card.transform, "-", 30, UIFactory.TextMain, TextAlignmentOptions.Left, true);
        }

        public override void Refresh()
        {
            var eco = CarSalesServices.Economy;
            var day = CarSalesServices.DayCycle;
            var p = CarSalesServices.Progress;
            int h = Mathf.FloorToInt(day.Hour), m = Mathf.FloorToInt((day.Hour - h) * 60f);
            welcome.text = $"Welcome back, boss!\n<size=60%><color={UIFactory.Hex(UIFactory.TextDim)}>Day {day.CurrentDay}  -  {h:00}:{m:00}  -  " +
                           (day.IsOpen ? "Shop OPEN" : "Shop CLOSED") + "</color></size>";
            cash.text = UIFactory.Money(eco.Money);
            cash.color = eco.Money < 0 ? UIFactory.Bad : UIFactory.Good;
            worth.text = p ? UIFactory.Money(p.NetWorth) : "-";
            worth.color = UIFactory.Gold;
            rep.text = $"{eco.Reputation:0}<size=60%>/100</size>";
            cars.text = $"{CarSalesServices.Inventory.OwnedCars.Count}<size=60%>/{DealershipManager.TotalCapacity} spots</size>";
            int l = CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0;
            loan.text = l > 0 ? UIFactory.Money(l) : "None";
            loan.color = l > 0 ? UIFactory.Warn : UIFactory.TextMain;
            goal.text = p ? p.CurrentObjectiveText : "";
            var t = day.Today;
            today.text = $"Sold <b>{t.carsSold}</b>   |   Bought <b>{t.carsBought}</b>   |   Revenue <b>{UIFactory.Money(t.revenue)}</b>   |   " +
                         $"Expenses <b>{UIFactory.Money(t.expenses)}</b>   |   Profit on sales <b>{UIFactory.Money(t.profit)}</b>";
            int unread = CarSalesServices.Messages ? CarSalesServices.Messages.UnreadCount : 0;
            int income = CarSalesServices.Properties ? CarSalesServices.Properties.DailyIncome : 0;
            tip.text = (unread > 0 ? $"You have {unread} unread message(s) - private sellers may be offering great deals!  " : "Tip: TAB = inventory, M = map, P = phone (tow / taxi).  ") +
                       (income > 0 ? $"Your properties earn {UIFactory.Money(income)} every night." : "");
        }
    }
}
