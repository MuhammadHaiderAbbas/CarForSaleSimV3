using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 12: a head-and-shoulders portrait of a character for the UI (the customer in negotiation, sellers).
    /// A small camera follows the character's head and renders into a texture while the screen is open.
    /// Works with humanoid characters (head bone) and falls back to 1.6 m above the feet.
    /// </summary>
    public class PortraitCamera : MonoBehaviour
    {
        public RenderTexture Texture { get; private set; }
        Transform target, head;
        Camera cam;

        public static PortraitCamera Create()
        {
            var go = new GameObject("PortraitCamera");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<PortraitCamera>();
            p.cam = go.AddComponent<Camera>();
            p.cam.fieldOfView = 22f;
            p.cam.nearClipPlane = 0.05f;
            p.cam.farClipPlane = 30f;
            p.cam.clearFlags = CameraClearFlags.SolidColor;
            p.cam.backgroundColor = UITheme.Current.pictureBg;
            p.Texture = new RenderTexture(384, 384, 16) { name = "Portrait" };
            p.cam.targetTexture = p.Texture;
            p.cam.enabled = false;
            return p;
        }

        public void Follow(Transform character)
        {
            target = character;
            head = null;
            if (character)
            {
                var anim = character.GetComponentInChildren<Animator>();
                if (anim && anim.isHuman) head = anim.GetBoneTransform(HumanBodyBones.Head);
            }
            cam.enabled = target;
        }

        void LateUpdate()
        {
            if (!target) { cam.enabled = false; return; }
            Vector3 h = head ? head.position : target.position + Vector3.up * 1.6f;
            Vector3 fwd = target.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            cam.transform.position = h + fwd.normalized * 1.6f + Vector3.up * 0.05f;
            cam.transform.LookAt(h - Vector3.up * 0.08f);
        }

        void OnDestroy() { if (Texture) Texture.Release(); }
    }
}
