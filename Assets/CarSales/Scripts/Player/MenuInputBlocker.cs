using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Disables the player's movement/look scripts while a menu is open or while something else
    /// (e.g. a test drive) has taken control.
    /// </summary>
    public class MenuInputBlocker : MonoBehaviour
    {
        [SerializeField] Behaviour[] disableWhileMenuOpen;
        bool menuOpen, externallyBlocked;

        void Start() { if (CarSalesServices.UI) CarSalesServices.UI.OnMenuStateChanged += HandleMenu; }
        void OnDestroy() { if (CarSalesServices.UI) CarSalesServices.UI.OnMenuStateChanged -= HandleMenu; }

        void HandleMenu(bool open) { menuOpen = open; Apply(); }

        public void SetBlocked(bool blocked) { externallyBlocked = blocked; Apply(); }

        void Apply()
        {
            bool enable = !menuOpen && !externallyBlocked;
            foreach (var b in disableWhileMenuOpen) if (b) b.enabled = enable;
        }
    }
}
