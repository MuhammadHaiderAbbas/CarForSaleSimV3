using System;

namespace CarSales
{
    [Serializable]
    public class DailyReport
    {
        public int day;
        public int carsSold, carsBought;
        public int revenue, expenses, profit;
        public int rent, interest, propertyIncome, passOutFee;
        public bool passedOut;
    }
}
