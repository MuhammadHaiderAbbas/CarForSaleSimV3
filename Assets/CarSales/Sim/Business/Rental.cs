using System;

namespace CarSales.Sim
{
    /// <summary>
    /// Car-rental rules shared by the game and the tests (pure C#, no UnityEngine).
    /// A rental earns a daily rate; the renter pays a refundable deposit; bringing the car back
    /// late costs a fixed fee per day (deducted from the deposit first).
    /// </summary>
    public static class RentalRules
    {
        public const int MinDays = 1;
        public const int MaxDays = 30;
        public const int LateFeePerDay = 250;         // £ per day past the due date
        public const float DepositFactor = 0.10f;     // 10% of market value
        public const int MinDeposit = 200;
        public const float RateFactor = 0.0035f;      // ~0.35% of value per day
        public const int MinDailyRate = 25;

        /// <summary>Daily rate in whole pounds for a car worth <paramref name="value"/>.</summary>
        public static int DailyRate(int value) => Math.Max(MinDailyRate, (int)Math.Round(value * RateFactor));

        /// <summary>Refundable deposit in whole pounds.</summary>
        public static int Deposit(int value) => Math.Max(MinDeposit, (int)Math.Round(value * DepositFactor));

        /// <summary>Total late fee for <paramref name="daysLate"/> days overdue (0 before the due day).</summary>
        public static int LateFee(int daysLate) => Math.Max(0, daysLate) * LateFeePerDay;

        /// <summary>Days between <paramref name="dueDay"/> and <paramref name="today"/> (negative while the contract is still running).</summary>
        public static int DaysLate(int dueDay, int today) => today - dueDay;

        /// <summary>Rental income for a contract of <paramref name="days"/> days at <paramref name="dailyRate"/> per day.</summary>
        public static int IncomeForDays(int dailyRate, int days) => dailyRate * Math.Max(0, days);

        /// <summary>Damage a renter does per day of hire (condition points).</summary>
        public const float WearPerDay = 0.35f;

        /// <summary>Extra miles a rental car picks up per day.</summary>
        public const int MilesPerDay = 118;

        /// <summary>Everything quoted for a car before it goes out.</summary>
        public static RentalQuote Quote(int value, int days)
        {
            days = ClampDays(days);
            return new RentalQuote { DailyRate = DailyRate(value), Deposit = Deposit(value), Days = days, Total = DailyRate(value) * days };
        }

        public static int ClampDays(int days) => days < MinDays ? MinDays : (days > MaxDays ? MaxDays : days);

        /// <summary>What the renter gets back when it comes back on time (the deposit minus nothing).</summary>
        public static int RefundOnReturn(int deposit, int daysLate) => Math.Max(0, deposit - LateFee(daysLate));

        /// <summary>Company keeps the late fees instead of refunding them.</summary>
        public static int LateFeeKept(int deposit, int daysLate) => Math.Min(deposit, LateFee(daysLate));
    }

    /// <summary>One quote for one car and term.</summary>
    public struct RentalQuote
    {
        public int DailyRate;
        public int Deposit;
        public int Days;
        public int Total;    // DailyRate * Days
    }
}
