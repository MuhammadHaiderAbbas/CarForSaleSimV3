using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Put in a scene root: tells the rest of the game which town it is (2 = master map, 3 = final map).
    /// Everything that needs the town (traffic, pedestrians, the world map, save migration) reads CarSales.Sim.Town.
    /// </summary>
    public class TownMapMarker : MonoBehaviour
    {
        [Tooltip("2 = master map (V2), 3 = final map (V3).")]
        public int townVersion = 3;

        void Awake() => CarSales.Sim.Town.Set(townVersion);
    }
}
