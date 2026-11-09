using System;

namespace CarSales
{
    public enum MessageType { Info, SellerOffer, Bank, Goal }

    /// <summary>An inbox message on the dealer computer (saved).</summary>
    [Serializable]
    public class GameMessage
    {
        public string id;
        public MessageType type;
        public string title;
        public string body;
        public int day;
        public bool read;
        public bool resolved;            // offers: accepted / declined / expired
        public CarInstance offerCar;     // SellerOffer only
        public int offerPrice;
        public int expiresDay;
    }
}
