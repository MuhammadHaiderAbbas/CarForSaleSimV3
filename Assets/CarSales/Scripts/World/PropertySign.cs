using UnityEngine;

namespace CarSales
{
    /// <summary>"FOR SALE" sign at a property. Hides itself once you own the property.</summary>
    public class PropertySign : MonoBehaviour, IInteractable
    {
        public string propertyId;
        [SerializeField] GameObject visual;

        void Start()
        {
            if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged += Refresh;
            Refresh();
        }

        void OnDestroy() { if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged -= Refresh; }

        void Refresh()
        {
            bool owned = CarSalesServices.Properties && CarSalesServices.Properties.Owns(propertyId);
            if (visual) visual.SetActive(!owned);
            foreach (var c in GetComponents<Collider>()) c.enabled = !owned;
        }

        public bool CanInteract() => CarSalesServices.Properties && !CarSalesServices.Properties.Owns(propertyId);

        public string GetInteractionText()
        {
            var info = CarSalesServices.Properties.Get(propertyId);
            return info == null ? "For sale" : $"[E] FOR SALE: {info.displayName} - {UIFactory.Money(info.price)}";
        }

        public void Interact(GameObject interactor) => CarSalesServices.UI.realEstate.Show();
    }
}
