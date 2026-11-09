using UnityEngine;

namespace CarSales
{
    /// <summary>Registers the car database. Lives on the CarSalesSystems prefab.</summary>
    public class CarSalesBootstrap : MonoBehaviour
    {
        [SerializeField] CarDatabase database;

        void Awake()
        {
            if (!database) database = CarDatabase.LoadDefault();
            CarSalesServices.Database = database;
            if (!database) Debug.LogError("[CarSales] No CarDatabase assigned on CarSalesBootstrap.");
            // Phase 5 service: added here so older generated scenes work without re-running Build Everything.
            if (!GetComponent<WorkshopManager>()) gameObject.AddComponent<WorkshopManager>();
            if (!GetComponent<MarketManager>()) gameObject.AddComponent<MarketManager>();
            if (!GetComponent<StaffManager>()) gameObject.AddComponent<StaffManager>();
            if (!GetComponent<CrmManager>()) gameObject.AddComponent<CrmManager>();
            if (!GetComponent<SourcingManager>()) gameObject.AddComponent<SourcingManager>();
            if (!GetComponent<WorldManager>()) gameObject.AddComponent<WorldManager>();
            if (!GetComponent<BusinessManager>()) gameObject.AddComponent<BusinessManager>();
            if (!GetComponent<AmbienceManager>()) gameObject.AddComponent<AmbienceManager>();
            if (!GetComponent<BuildingManager>()) gameObject.AddComponent<BuildingManager>();   // phase 11b
            if (!GetComponent<RentalManager>()) gameObject.AddComponent<RentalManager>();   // phase 14: hire fleet
            if (!GetComponent<StaffAvatars>()) gameObject.AddComponent<StaffAvatars>();   // phase 11: visible staff
            var fpc = FindAnyObjectByType<SimpleFirstPersonController>();
            if (fpc && !fpc.GetComponent<PlayerAvatar>()) fpc.gameObject.AddComponent<PlayerAvatar>();   // phase 11: your character
            if (!FindAnyObjectByType<TrafficSystem>()) new GameObject("Traffic").AddComponent<TrafficSystem>();
            if (!FindAnyObjectByType<PedestrianSystem>()) new GameObject("Pedestrians").AddComponent<PedestrianSystem>();
        }
    }
}
