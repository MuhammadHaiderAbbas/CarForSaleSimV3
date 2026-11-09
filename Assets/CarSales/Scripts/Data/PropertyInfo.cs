using System;

namespace CarSales
{
    /// <summary>A purchasable property: a home, an extra dealership, or an investment that pays daily income.</summary>
    [Serializable]
    public class PropertyInfo
    {
        public string id;
        public string displayName;
        public PropertyType type;
        public int price;
        public int dailyIncome;       // investments
        public int dailyCost;         // dealership rent / upkeep
        public float wakeHour = 8f;   // homes: when you wake up
        public float dailyReputation; // homes: "well rested" bonus
        public string description;
        public bool ownedAtStart;
    }
}
