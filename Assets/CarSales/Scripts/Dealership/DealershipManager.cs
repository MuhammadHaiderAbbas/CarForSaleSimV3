using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// One dealership (there can be several - extra ones are bought as properties).
    /// Holds the parking spots, customer spawn/exit points and the open/closed state.
    /// </summary>
    public class DealershipManager : MonoBehaviour
    {
        public static readonly List<DealershipManager> All = new List<DealershipManager>();
        public static DealershipManager Main { get; private set; }

        public string dealershipId = "autoworld";
        public string displayName = "AUTO WORLD";
        [Tooltip("Property id that must be owned (empty = always owned).")]
        public string propertyId;
        [Tooltip("Customers' budgets are multiplied by this (luxury dealerships attract richer buyers).")]
        public float budgetMultiplier = 1f;
        public bool isMain;
        public List<ParkingSpot> spots = new List<ParkingSpot>();
        public Transform[] customerSpawnPoints;
        public Transform exitPoint;

        public bool ClosedByPlayer { get; set; }
        public bool IsOwned => string.IsNullOrEmpty(propertyId) || (CarSalesServices.Properties && CarSalesServices.Properties.Owns(propertyId));
        public bool IsOpenNow => IsOwned && !ClosedByPlayer && CarSalesServices.DayCycle && CarSalesServices.DayCycle.IsBusinessHours;

        void Awake()
        {
            if (!All.Contains(this)) All.Add(this);
            if (isMain || Main == null) { Main = this; CarSalesServices.Dealership = this; }
            spots.RemoveAll(s => s == null);
            if (spots.Count == 0) spots = GetComponentsInChildren<ParkingSpot>().ToList();
            spots.Sort((a, b) => a.index.CompareTo(b.index));
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (Main == this) Main = All.FirstOrDefault();
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged -= RefreshSpotVisuals;
            if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged -= RefreshSpotVisuals;
        }

        void Start()
        {
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged += RefreshSpotVisuals;
            if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged += RefreshSpotVisuals;
            RefreshSpotVisuals();
        }

        public static DealershipManager Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return Main;
            return All.FirstOrDefault(d => d.dealershipId == id);
        }

        public static IEnumerable<DealershipManager> Owned => All.Where(d => d.IsOwned);

        public static ParkingSpot FindFreeSpotAnywhere(out DealershipManager dealer)
        {
            foreach (var d in All.OrderByDescending(x => x.isMain))
            {
                if (!d.IsOwned) continue;
                var s = d.GetFreeSpot();
                if (s != null) { dealer = d; return s; }
            }
            dealer = null;
            return null;
        }

        public static int TotalFreeSpots => Owned.Sum(d => d.spots.Count(s => s.IsFree && s.IsUnlocked));
        public static int TotalCapacity => Owned.Sum(d => d.UnlockedSpotCount);

        /// <summary>Shows/hides parking lines of spots that are locked (upgrade or not owned).</summary>
        public void RefreshSpotVisuals()
        {
            foreach (var s in spots)
                foreach (Transform child in s.transform) child.gameObject.SetActive(s.IsUnlocked && IsOwned);
        }

        public int UnlockedSpotCount => IsOwned ? spots.Count(s => s.IsUnlocked) : 0;
        public ParkingSpot GetFreeSpot() => IsOwned ? spots.Find(s => s.IsFree && s.IsUnlocked) : null;
        public ParkingSpot GetSpot(int index) => spots.Find(s => s.index == index);

        public ParkingSpot NearestFreeSpot(Vector3 pos, float maxDistance)
        {
            if (!IsOwned) return null;
            ParkingSpot best = null;
            float bestD = maxDistance;
            var driven = CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving ? CarSalesServices.TestDrive.CurrentCar : null;
            foreach (var s in spots)
            {
                bool free = s.IsFree || (driven && s.Occupant == driven);   // the spot you just drove out of
                if (!free || !s.IsUnlocked) continue;
                float d = Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(s.transform.position.x, 0f, s.transform.position.z));
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public void ReleaseSpot(CarDisplay car)
        {
            foreach (var s in spots) if (s.Occupant == car) s.Occupant = null;
        }

        public void ReleaseAllSpots() { foreach (var s in spots) s.Occupant = null; }

        public List<CarDisplay> GetListedCars()
        {
            var list = new List<CarDisplay>();
            if (!CarSalesServices.Vehicles) return list;
            foreach (var d in CarSalesServices.Vehicles.All)
                if (d.gameObject.activeInHierarchy && d.Data.state == CarState.Listed &&
                    d.Data.location == CarLocation.Spot && Find(d.Data.dealershipId) == this)
                    list.Add(d);
            return list;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); Main = null; }
    }
}
