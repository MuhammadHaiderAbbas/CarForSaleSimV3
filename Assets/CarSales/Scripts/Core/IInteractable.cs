using UnityEngine;

namespace CarSales
{
    /// <summary>Anything the player can look at and press Interact on (cars, computer, customers, signs).</summary>
    public interface IInteractable
    {
        bool CanInteract();
        string GetInteractionText();
        void Interact(GameObject interactor);
    }
}
