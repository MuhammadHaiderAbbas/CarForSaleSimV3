using CarSales.Studio;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11: puts a character body on an NPC. Order: Character Setup cast for the job, then any Asset Studio
    /// character, then nothing (the caller keeps its capsule). The body has no colliders and never moves itself.
    /// </summary>
    public static class CharacterSpawn
    {
        public static GameObject PrefabFor(CharacterUse role)
        {
            var lib = CharacterLibrary.Load();
            var p = lib ? lib.Pick(role) : null;
            return p ? p : AssetRegistry.RandomCustomerModel();
        }

        /// <summary>Adds a character under <paramref name="parent"/> standing at its feet. Null when no characters exist.</summary>
        public static CharacterAnimator Attach(Transform parent, CharacterUse role, Vector3 localFeet = default)
        {
            var prefab = PrefabFor(role);
            if (!prefab || !parent) return null;
            var body = Object.Instantiate(prefab, parent);
            body.name = "Body";
            body.transform.localPosition = localFeet;
            body.transform.localRotation = Quaternion.identity;
            foreach (var c in body.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var r in body.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(r);
            UIFactory.SetLayer(body, parent.gameObject.layer);
            var anim = CharacterAnimator.On(body);
            var lib = CharacterLibrary.Load();
            var animator = anim.Animator;
            if (animator && lib && lib.controller && !animator.runtimeAnimatorController) animator.runtimeAnimatorController = lib.controller;
            return anim;
        }

        /// <summary>Hides the capsule (or any) renderers on <paramref name="root"/> except inside the body.</summary>
        public static void HidePlaceholder(Transform root, Transform body)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (!body || !r.transform.IsChildOf(body)) { if (!r.GetComponent<TMPro.TMP_Text>()) r.enabled = false; }   // keep speech-bubble text
        }
    }
}
