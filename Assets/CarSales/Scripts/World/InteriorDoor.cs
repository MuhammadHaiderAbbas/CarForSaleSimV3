using UnityEngine;

namespace CarSales
{
    /// <summary>Phase 13c: the inside of a home's front door - takes you back outside.</summary>
    public class InteriorDoor : MonoBehaviour, IInteractable
    {
        public HomeInterior home;

        public bool CanInteract() => home;
        public string GetInteractionText() => Loc.T("interior.leave", "[E] Go outside");
        public void Interact(GameObject interactor) { if (home) home.Leave(); }
    }
}
