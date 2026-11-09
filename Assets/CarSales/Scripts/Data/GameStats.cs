using System;

namespace CarSales
{
    /// <summary>Lifetime statistics (saved). Also drives the goals/objectives.</summary>
    [Serializable]
    public class GameStats
    {
        public int carsSold, carsBought, carsServiced, carsListed, shopOpened, upgradesBought, testDrives;
        public int totalRevenue, totalProfit, bestSale, daysPlayed;
        public int rentalsCompleted, rentalEarnings;
    }
}
