using CarSales.Studio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CarSales
{
    /// <summary>
    /// Phase 11: your own character (Character Setup → main character) on the player. First person: you see its shadow;
    /// third person (V, or Settings): the camera moves behind your shoulder and you see yourself walk, run and stand.
    /// Added by CarSalesBootstrap to the player; does nothing until a player character exists.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        static readonly Vector3 ThirdPersonOffset = new Vector3(0.45f, 0.2f, -3.2f);
        CharacterAnimator body;
        CharacterController controller;
        PlayerInteractor interactor;
        Transform cam;
        Renderer[] renderers;
        InputAction toggle;
        Vector3 lastPos;
        float speed;

        public bool ThirdPerson { get; private set; }

        void Start()
        {
            controller = GetComponent<CharacterController>();
            interactor = GetComponent<PlayerInteractor>();
            var c = GetComponentInChildren<Camera>();
            cam = c ? c.transform : null;
            var lib = CharacterLibrary.Load();
            if (lib && lib.player)
            {
                var root = new GameObject("PlayerCharacter").transform;
                root.SetParent(transform, false);
                var go = Instantiate(lib.player, root);
                go.name = "Body";
                foreach (var col in go.GetComponentsInChildren<Collider>(true)) Destroy(col);
                foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
                body = CharacterAnimator.On(go);
                if (body.Animator && lib.controller && !body.Animator.runtimeAnimatorController) body.Animator.runtimeAnimatorController = lib.controller;
                renderers = go.GetComponentsInChildren<Renderer>(true);
            }
            toggle = CarSalesInput.Resolve(null, "ToggleView");
            lastPos = transform.position;
            SetView(GameSettings.ThirdPerson && body);
        }

        void Update()
        {
            if (toggle != null && toggle.WasPressedThisFrame() && body && !(CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen))
            {
                SetView(!ThirdPerson);
                GameSettings.ThirdPerson = ThirdPerson;
            }
            if (!body) return;
            var v = (transform.position - lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPos = transform.position;
            v.y = 0f;
            speed = Mathf.Lerp(speed, Mathf.Min(v.magnitude, 8f), 1f - Mathf.Exp(-10f * Time.deltaTime));
            body.SetSpeed(speed);
        }

        void LateUpdate()
        {
            if (!ThirdPerson || !cam || !cam.parent) return;
            // Keep the camera out of walls: pull it in when something is between your head and the camera.
            var pivot = cam.parent;
            var wanted = pivot.TransformPoint(ThirdPersonOffset);
            var dir = wanted - pivot.position;
            float dist = dir.magnitude;
            if (Physics.SphereCast(pivot.position, 0.2f, dir.normalized, out var hit, dist, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
                dist = Mathf.Max(0.3f, hit.distance - 0.1f);
            cam.position = pivot.position + dir.normalized * dist;
            if (interactor) interactor.ReachBonus = dist;
        }

        public void SetView(bool thirdPerson)
        {
            ThirdPerson = thirdPerson && body;
            if (cam && !ThirdPerson) cam.localPosition = Vector3.zero;
            if (interactor && !ThirdPerson) interactor.ReachBonus = 0f;
            if (renderers != null)
                foreach (var r in renderers) if (r) r.shadowCastingMode = ThirdPerson ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
        }

        /// <summary>Plays a gesture on your character (e.g. raising your paddle at an auction).</summary>
        public void Gesture(CharacterAnim a) { if (body) body.Play(a); }
    }
}
