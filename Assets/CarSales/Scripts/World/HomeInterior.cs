using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 13c: the furnished inside of a home. It is built below the house (out of sight, under the ground) and the
    /// front door takes you there; the inside door brings you back. You sleep in the bed and wake up beside it.
    /// Whether the player is inside is worked out from where the camera is, so any teleport keeps it right.
    /// </summary>
    public class HomeInterior : MonoBehaviour
    {
        public string propertyId;
        [Tooltip("Where you stand after coming in (just inside the door, facing the room).")]
        public Transform entryPoint;
        [Tooltip("Where you wake up (beside the bed).")]
        public Transform wakePoint;
        [Tooltip("Where you stand after going out (the front path).")]
        public Transform exitPoint;
        [Tooltip("The rooms' size in metres (x, height, z); the floor is at this object's position.")]
        public Vector3 size = new Vector3(12f, 3f, 9f);

        static readonly List<HomeInterior> all = new List<HomeInterior>();

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        public static HomeInterior Find(string propertyId)
        {
            if (string.IsNullOrEmpty(propertyId)) return null;
            foreach (var h in all) if (h && h.propertyId == propertyId) return h;
            return null;
        }

        public bool Contains(Vector3 worldPoint)
        {
            var p = transform.InverseTransformPoint(worldPoint);
            return Mathf.Abs(p.x) <= size.x * 0.5f + 0.5f && p.y >= -1f && p.y <= size.y + 1f && Mathf.Abs(p.z) <= size.z * 0.5f + 0.5f;
        }

        /// <summary>The interior containing <paramref name="worldPoint"/>, or null when outdoors.</summary>
        public static HomeInterior Containing(Vector3 worldPoint)
        {
            foreach (var h in all) if (h && h.Contains(worldPoint)) return h;
            return null;
        }

        /// <summary>Keeps <see cref="InteriorState"/> in step with the camera (called by the Realism Director each frame).</summary>
        public static void Track()
        {
            var cam = Camera.main;
            var inside = cam ? Containing(cam.transform.position) : null;
            InteriorState.Inside = inside != null;
            InteriorState.PropertyId = inside ? inside.propertyId : "";
        }

        public void Enter()
        {
            var p = entryPoint ? entryPoint : transform;
            TowService.TeleportPlayer(p.position, p.eulerAngles.y);
            Track();
        }

        public void Leave()
        {
            var p = exitPoint;
            if (!p) { var bed = HomeBed.Find(propertyId); p = bed ? (bed.wakePoint ? bed.wakePoint : bed.transform) : null; }
            if (!p) return;
            TowService.TeleportPlayer(p.position, p.eulerAngles.y);
            Track();
        }

        public void WakeUp()
        {
            var p = wakePoint ? wakePoint : entryPoint;
            if (!p) return;
            TowService.TeleportPlayer(p.position, p.eulerAngles.y);
            Track();
        }
    }
}
