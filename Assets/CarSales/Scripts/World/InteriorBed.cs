using UnityEngine;

namespace CarSales
{
    /// <summary>Phase 13c: the bed in a walk-in home. Sleeping here ends the day, exactly like the front door used to.</summary>
    public class InteriorBed : MonoBehaviour, IInteractable
    {
        public HomeInterior home;

        public bool CanInteract() => home;

        public string GetInteractionText()
        {
            var day = CarSalesServices.DayCycle;
            if (!day) return "";
            return day.CanSleep
                ? string.Format(Loc.T("interior.sleep", "[E] Sleep - end Day {0}"), day.CurrentDay)
                : string.Format(Loc.T("interior.notTired", "Your bed (you can sleep after {0:00}:00)"), DayCycleManager.EarliestSleepHour);
        }

        public void Interact(GameObject interactor)
        {
            var day = CarSalesServices.DayCycle;
            if (!day) return;
            if (!day.CanSleep)
            {
                CarSalesServices.UI.Error(string.Format(Loc.T("interior.notTiredError", "You're not tired yet. You can sleep after {0:00}:00."), DayCycleManager.EarliestSleepHour));
                return;
            }
            day.Sleep(false);
        }
    }
}
