using UnityEngine;

namespace CarSales
{
    /// <summary>Front door / bed of a home. Sleep here to end the day (only in the home you live in).</summary>
    public class HomeBed : MonoBehaviour, IInteractable
    {
        public string propertyId;
        [Tooltip("Where the player stands after waking up.")]
        public Transform wakePoint;

        public static HomeBed Find(string id)
        {
            foreach (var b in UnityCompat.FindAll<HomeBed>()) if (b.propertyId == id) return b;
            return null;
        }

        bool IsMyHome => CarSalesServices.Properties && CarSalesServices.Properties.ActiveHome == propertyId;

        public bool CanInteract() => true;

        public string GetInteractionText()
        {
            var props = CarSalesServices.Properties;
            var info = props ? props.Get(propertyId) : null;
            string name = info != null ? info.displayName : "House";
            if (!props || !props.Owns(propertyId)) return $"{name} - for sale (see the sign / Real Estate)";
            if (!IsMyHome) return $"[E] Move into {name}";
            if (HomeInterior.Find(propertyId)) return Loc.T("home.goInside", "[E] Go inside");   // phase 13c: sleep in your bed
            var day = CarSalesServices.DayCycle;
            return day.CanSleep ? $"[E] Sleep - end Day {day.CurrentDay}" : $"{name} (you can sleep after {DayCycleManager.EarliestSleepHour:00}:00)";
        }

        public void Interact(GameObject interactor)
        {
            var props = CarSalesServices.Properties;
            if (!props || !props.Owns(propertyId)) { CarSalesServices.UI.realEstate.Show(); return; }
            if (!IsMyHome)
            {
                props.MoveInto(propertyId);
                CarSalesServices.UI.Success("Moved in!", $"{props.Get(propertyId).displayName} is now your home.");
                return;
            }
            var interior = HomeInterior.Find(propertyId);
            if (interior) { interior.Enter(); return; }   // phase 13c: walk-in home
            if (!CarSalesServices.DayCycle.CanSleep)
            {
                CarSalesServices.UI.Error($"You're not tired yet. You can sleep after {DayCycleManager.EarliestSleepHour:00}:00.");
                return;
            }
            CarSalesServices.DayCycle.Sleep(false);
        }
    }
}
