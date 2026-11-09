using UnityEngine;

namespace CarSales
{
    public class DealerComputer : MonoBehaviour, IInteractable
    {
        void Start() => MinimapMarker.Attach(gameObject, UIFactory.Accent, 3.5f);

        public bool CanInteract() => true;
        public string GetInteractionText() => "[E] Use Computer (Marketplace, Upgrades, Bank, Stats)";
        public void Interact(GameObject interactor) => CarSalesServices.UI.computerHub.Show();
    }
}
