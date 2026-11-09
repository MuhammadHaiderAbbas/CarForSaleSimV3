using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Physical used-car lot: walk around, inspect for free, buy on the spot and drive away.
    /// Listings come from MarketplaceManager.LotListings (new cars every morning).
    /// </summary>
    public class UsedCarMarket : MonoBehaviour
    {
        public Transform[] lotSpots;
        [SerializeField] CarDisplay displayPrefab;
        readonly List<CarDisplay> shown = new List<CarDisplay>();

        void Start()
        {
            CarSalesServices.Marketplace.OnListingsChanged += Rebuild;
            Rebuild();
        }

        void OnDestroy() { if (CarSalesServices.Marketplace) CarSalesServices.Marketplace.OnListingsChanged -= Rebuild; }

        public void Rebuild()
        {
            foreach (var d in shown) if (d) Destroy(d.gameObject);
            shown.Clear();
            var listings = CarSalesServices.Marketplace.LotListings;
            int next = 0;
            foreach (var spot in lotSpots)
            {
                if (next >= listings.Count) break;
                if (Occupied(spot.position)) continue;   // one of YOUR cars is standing there
                var d = Instantiate(displayPrefab, spot.position, spot.rotation);
                d.InitializeForSale(listings[next++]);
                shown.Add(d);
            }
        }

        static bool Occupied(Vector3 pos)
        {
            if (!CarSalesServices.Vehicles) return false;
            foreach (var car in CarSalesServices.Vehicles.All)
                if (car && car.gameObject.activeInHierarchy && Vector3.Distance(car.transform.position, pos) < 2.5f) return true;
            return false;
        }
    }
}
