using System;
using System.Collections.Generic;

namespace CarSales
{
    /// <summary>Root of the JSON save file. New fields simply get defaults when loading an older save.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 4;
        public int money;
        public float reputation;
        public int day;
        public List<CarInstance> ownedCars = new List<CarInstance>();
        public List<CarInstance> marketListings = new List<CarInstance>();

        // v2
        public int[] upgradeLevels = new int[0];
        public int loan;
        public int daysInDebt;
        public int objectiveIndex;
        public bool hasWon;
        public GameStats stats = new GameStats();

        // v3
        public List<GameMessage> messages = new List<GameMessage>();

        // v4
        public float hour = -1f;
        public float energy = 100f;
        public List<string> ownedProperties = new List<string>();
        public string activeHome;
        public List<CarInstance> lotListings = new List<CarInstance>();
        public bool shopClosedByPlayer;

        // Phase 12: town plan the save was made on (0 = the old two-road town).
        public int townLayout;

        // Cars out on hire (the Rentals app / RentGo competitor). Older saves have no list - they simply have none.
        public List<RentalContract> rentals = new List<RentalContract>();
    }
}
