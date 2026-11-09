using System;

namespace CarSales
{
    /// <summary>One live or finished rental contract (saved to JSON). The car stays in your inventory.</summary>
    [Serializable]
    public class RentalContract
    {
        public string id;
        public string carInstanceId;    // CarInstance.instanceId
        public string carName;          // display name at the time of hire ("Imported Ridge")
        public string dealershipId;     // whose forecourt it went out from (empty = AUTO WORLD)
        public string renterName;       // the customer who took it
        public int startDay;
        public int dueDay;
        public int days;
        public int dailyRate;
        public int deposit;             // £ held and refunded (or kept) on return
        public bool active = true;
        public int lateNotifiedFor;     // the last "it's overdue" day a message was sent (0 = never)
    }
}
