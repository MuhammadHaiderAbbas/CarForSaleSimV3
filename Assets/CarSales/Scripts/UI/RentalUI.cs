using System.Linq;
using CarSales.Sim;
using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Rentals app (phase 14, Final Map): put any forecourt car out on hire for 1-30 days,
    /// see what is out, who has it, when it is due back and what the late fees are — then return it.
    /// The competitor lot next to Luxury Lane (RentGo) runs the same business; this is how you compete with it.
    /// </summary>
    public class RentalUI : ComputerPage
    {
        RectTransform list;
        int days = 7;
        string renterSeed = "walk-in";
        static readonly int[] DayChoices = { 1, 3, 7, 14, 30 };

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 10f);
            list = UIFactory.ScrollList(Area, 8f);
        }

        void OnEnable() { if (CarSalesServices.Rentals) CarSalesServices.Rentals.OnRentalsChanged += Changed; }
        void OnDisable() { if (CarSalesServices.Rentals) CarSalesServices.Rentals.OnRentalsChanged -= Changed; }
        void Changed() { if (list && isActiveAndEnabled) Refresh(); }

        static RentalManager RM => CarSalesServices.Rentals;
        static string TodayText() => "Day " + (CarSalesServices.DayCycle ? CarSalesServices.DayCycle.CurrentDay : CarRecords.Today);

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

        static string FreshName()
        {
            string[] first = { "Alex", "Sam", "Jordan", "Priya", "Casey", "Riley", "Morgan", "Dev", "Amelia", "Tom", "Nina", "Liam" };
            string[] last = { "Walker", "Shaw", "Patel", "Nguyen", "Hughes", "Brooks", "Khan", "Mills", "Clarke", "Webb", "Ford", "Gray" };
            int s = Mathf.Abs((System.DateTime.Now.Millisecond * 31 + UnityEngine.Random.Range(0, 100000)));
            return first[s % first.Length] + " " + last[(s / first.Length + UnityEngine.Random.Range(0, 97)) % last.Length];
        }

        public override void Refresh()
        {
            UIFactory.ClearChildren(list);
            if (!RM) { UIFactory.EmptyState(list, "Rentals are not available", "The rental office system is not set up in this scene."); return; }
            var day = CarSalesServices.DayCycle;
            int today = day ? day.CurrentDay : CarRecords.Today;

            UIFactory.SectionTitle(list, "Hire fleet");
            int income = RM.DailyHireIncome;
            var active = RM.Active();
            Row($"<b>{active.Count}</b> car(s) out on hire  ·  <b>{UIFactory.Money(income)}</b> per day coming in\n<size=85%>Today is {TodayText()}. Deposits are refundable; late fees come out of the deposit.</size>", 64f, UIFactory.CardLight);

            // -------------------------------------------------- hire something out
            UIFactory.SectionTitle(list, "Rent a car out");
            var inv = CarSalesServices.Inventory;
            var candidates = inv ? inv.OwnedCars.Where(c => !c.personal && !RM.IsRentedOut(c.instanceId) && c.state != CarState.InTransit).OrderByDescending(c => CarValueCalculator.MarketValue(c)).ToList() : new System.Collections.Generic.List<CarInstance>();

            var term = Row($"Hire term: <b>{days} day(s)</b>\n<size=85%>Renter: {(string.IsNullOrEmpty(renterSeed) ? "(generated at signing)" : renterSeed)}</size>", 56f, UIFactory.CardLight);
            Button(term, "Change term", UIFactory.Neutral, () => days = DayChoices[(System.Array.IndexOf(DayChoices, days) + 1 + DayChoices.Length) % DayChoices.Length], true, 90f);
            Button(term, "Renter", UIFactory.Info, () => renterSeed = FreshName(), true, 100f);

            if (candidates.Count == 0) UIFactory.EmptyState(list, "No cars available", "Every car is out on hire or on the road (or it is your personal car).");
            foreach (var car in candidates.Take(12))
            {
                int value = CarValueCalculator.MarketValue(car);
                var quote = RentalRules.Quote(Mathf.Max(1, value), days);
                var row = Row($"<b>{car.Name}</b>  ·  {UIFactory.Money(value)} value\n<size=85%>{UIFactory.Money(quote.DailyRate)} / day  ·  deposit {UIFactory.Money(quote.Deposit)}  ·  total {UIFactory.Money(quote.Total)} for {days}d</size>", 66f);
                var id = car;
                Button(row, "Rent out", UIFactory.Good, () =>
                {
                    string who = string.IsNullOrEmpty(renterSeed) || renterSeed == "walk-in" ? FreshName() : renterSeed;
                    if (!RM.RentOut(id, who, days, out var err)) CarSalesServices.UI.Notify(err ?? "Can't rent that car.");
                    renterSeed = "walk-in";
                }, true, 130f);
            }

            // -------------------------------------------------- what is out right now
            UIFactory.SectionTitle(list, "Out on hire");
            if (active.Count == 0) UIFactory.EmptyState(list, "Nothing out", "Use 'Rent a car out' above — the hire fee is paid up front, the deposit is refunded (or kept) when it comes back.");
            foreach (var c in active)
            {
                int late = RentalRules.DaysLate(c.dueDay, today);
                string lateText = late > 0 ? $"<color={UIFactory.Hex(UIFactory.Bad)}>{late} day(s) LATE</color>" : $"due Day {c.dueDay}";
                int fee = RentalRules.LateFeeKept(c.deposit, late);
                int refund = RentalRules.RefundOnReturn(c.deposit, late);
                var row = Row($"<b>{c.carName}</b> — {c.renterName}\n<size=85%>Started Day {c.startDay} · {lateText} · {UIFactory.Money(c.dailyRate)}/day · return ≈ {UIFactory.Money(fee)} kept / {UIFactory.Money(refund)} refunded</size>", 70f,
                              late > 0 ? UIFactory.WithAlpha(UIFactory.Bad, 0.18f) : (Color?)null);
                var cid = c.id;
                Button(row, late > 0 ? "Return (late)" : "Return", late > 0 ? UIFactory.Warn : UIFactory.Accent, () =>
                {
                    if (!RM.Return(cid, out var msg)) CarSalesServices.UI.Notify("Could not return the car.");
                    else CarSalesServices.UI.Notify(msg);
                }, true, 150f);
            }

            // -------------------------------------------------- all-time numbers
            var s = CarSalesServices.Progress?.Stats;
            if (s != null)
                Row($"All time: <b>{s.rentalsCompleted}</b> completed hires · <b>{UIFactory.Money(s.rentalEarnings)}</b> earned in hire income and late fees", 46f, UIFactory.CardLight);
        }
    }
}
