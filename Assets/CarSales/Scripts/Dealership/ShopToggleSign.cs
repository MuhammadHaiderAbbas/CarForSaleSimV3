using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>OPEN / CLOSED sign at a dealership. Interact to close it early (or reopen) today.</summary>
    public class ShopToggleSign : MonoBehaviour, IInteractable
    {
        [SerializeField] TMP_Text label;
        DealershipManager dealer;
        bool lastOpen = true;

        void Start()
        {
            dealer = GetComponentInParent<DealershipManager>();
            UpdateLabel(true);
        }

        void Update() => UpdateLabel(false);

        void UpdateLabel(bool force)
        {
            bool open = dealer ? dealer.IsOpenNow : CarSalesServices.DayCycle && CarSalesServices.DayCycle.IsOpen;
            if (!force && open == lastOpen) return;
            lastOpen = open;
            if (!label) return;
            label.text = open ? "OPEN" : "CLOSED";
            label.color = open ? Color.green : Color.red;
        }

        public bool CanInteract() => !dealer || dealer.IsOwned;

        public string GetInteractionText()
        {
            var day = CarSalesServices.DayCycle;
            if (!day.IsBusinessHours) return $"Opening hours {day.OpenHour:00}:00 - {day.CloseHour:00}:00";
            return dealer && dealer.ClosedByPlayer ? "[E] Reopen this dealership" : "[E] Close this dealership for now";
        }

        public void Interact(GameObject interactor)
        {
            if (!dealer) { CarSalesServices.DayCycle.ToggleShop(); return; }
            if (!CarSalesServices.DayCycle.IsBusinessHours) { CarSalesServices.UI.Notify("Dealerships open automatically at 08:00."); return; }
            dealer.ClosedByPlayer = !dealer.ClosedByPlayer;
            CarSalesServices.UI.Notify(dealer.ClosedByPlayer ? $"{dealer.displayName} is closed for now." : $"{dealer.displayName} is open again!");
        }
    }
}
