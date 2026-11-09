using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>Add to ANY player (yours or the city's). Raycasts for IInteractable objects.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera cam;                       // empty = Camera.main
        [SerializeField] float distance = 3.5f;
        [SerializeField] LayerMask interactMask;           // set to "Interactable"
        [SerializeField] InputActionReference interactAction;

        public string CurrentPrompt { get; private set; } = "";
        /// <summary>Phase 11: extra reach while the camera is behind you (third-person view).</summary>
        public float ReachBonus { get; set; }
        /// <summary>The interactable the player is looking at (null if none).</summary>
        public IInteractable CurrentTarget => current;
        IInteractable current;
        InputAction interact;

        void OnEnable()
        {
            interact = CarSalesInput.Resolve(interactAction, "Interact");
            if (interact != null) interact.performed += OnInteract;
            if (interactMask.value == 0) interactMask = 1 << 20;   // default: layer 20 "Interactable"
        }

        void OnDisable() { if (interact != null) interact.performed -= OnInteract; }

        void Update()
        {
            if (!cam) cam = Camera.main;
            current = null;

            bool menuOpen = (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) ||
                            (CarSalesServices.TestDrive && CarSalesServices.TestDrive.BlocksInteraction);
            if (!menuOpen && cam &&
                Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit,
                                distance + ReachBonus, interactMask, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null && target.CanInteract()) current = target;
            }
            CurrentPrompt = current != null ? current.GetInteractionText() : "";
        }

        void OnInteract(InputAction.CallbackContext _)
        {
            if (current == null || (current is MonoBehaviour mb && mb == null)) return;
            if (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) return;
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.BlocksInteraction) return;
            current.Interact(gameObject);
        }
    }
}
