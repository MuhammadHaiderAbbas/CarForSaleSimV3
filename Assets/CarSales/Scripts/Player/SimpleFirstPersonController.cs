using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>
    /// Minimal first-person controller so the project works with no extra assets.
    /// In the city project you can delete this and use the city's own player instead.
    /// Missing input references fall back to default keys (see CarSalesInput).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] InputActionReference moveAction, lookAction, jumpAction, sprintAction;
        [SerializeField] float walkSpeed = 4f, sprintSpeed = 7f, jumpHeight = 1.1f, gravity = -20f;

        CharacterController controller;
        InputAction move, look, jump, sprint;
        float pitch, verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraPivot)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam) cameraPivot = cam.transform.parent != transform ? cam.transform.parent : cam.transform;
            }
        }

        void OnEnable()
        {
            move = CarSalesInput.Resolve(moveAction, "Move");
            look = CarSalesInput.Resolve(lookAction, "Look");
            jump = CarSalesInput.Resolve(jumpAction, "Jump");
            sprint = CarSalesInput.Resolve(sprintAction, "Sprint");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (move == null || look == null || !controller) return;

            // Look
            Vector2 lookDelta = look.ReadValue<Vector2>() * GameSettings.MouseSensitivity;
            if (GameSettings.InvertY) lookDelta.y = -lookDelta.y;   // phase 12 setting
            transform.Rotate(0f, lookDelta.x, 0f);
            pitch = Mathf.Clamp(pitch - lookDelta.y, -85f, 85f);
            if (cameraPivot) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Move
            Vector2 input = move.ReadValue<Vector2>();
            float speed = sprint != null && sprint.IsPressed() ? sprintSpeed : walkSpeed;
            if (CarSalesServices.DayCycle && CarSalesServices.DayCycle.Energy < 15f) speed *= 0.6f;   // exhausted
            Vector3 motion = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controller.isGrounded && jump != null && jump.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;
            motion.y = verticalVelocity;

            controller.Move(motion * Time.deltaTime);
        }
    }
}
