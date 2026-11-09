using System;
using UnityEngine;

namespace CarSales
{
    /// <summary>The current GPS destination (set from the map). Shown on the HUD and minimap.</summary>
    public static class GpsSystem
    {
        static MapPlace target;
        public static MapPlace Target => target ? target : null;
        public static event Action OnChanged;

        public static void SetTarget(MapPlace place)
        {
            target = place;
            OnChanged?.Invoke();
            if (place && CarSalesServices.UI) CarSalesServices.UI.Notify($"GPS set: {place.placeName}. Follow the arrow at the top of the screen.");
        }

        public static void Clear() { target = null; OnChanged?.Invoke(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { target = null; OnChanged = null; }
    }
}
