using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Put in a scene root: tells the rest of the game which town it is (4 = Harbour City).
    /// Everything that needs the town (traffic, pedestrians, the world map, save migration) reads CarSales.Sim.Town.
    /// </summary>
    public class TownMapMarker : MonoBehaviour
    {
        [Tooltip("4 = Harbour City. Any older value loads Harbour City anyway - it is the only town.")]
        public int townVersion = CarSales.Sim.HarbourPlan.Version;

        void Awake() => CarSales.Sim.Town.Set(townVersion);
    }
}
