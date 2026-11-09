using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// An area where something happens to cars inside it (repair, wash, refuel, park at home...).
    /// Bays are the spots where tow trucks drop cars.
    /// </summary>
    public class ServiceZone : MonoBehaviour
    {
        public static readonly List<ServiceZone> All = new List<ServiceZone>();

        public ServiceType type;
        public string displayName = "Service";
        public Vector3 center = new Vector3(0f, 1f, 6f);
        public Vector3 size = new Vector3(16f, 4f, 14f);
        public Transform[] bays;
        [Tooltip("Home zones: the property id of that home.")]
        public string propertyId;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public bool Contains(Vector3 worldPos)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos) - center;
            return Mathf.Abs(local.x) <= size.x / 2f && Mathf.Abs(local.y) <= size.y / 2f + 2f && Mathf.Abs(local.z) <= size.z / 2f;
        }

        public static ServiceZone At(Vector3 pos)
        {
            foreach (var z in All) if (z && z.Contains(pos)) return z;
            return null;
        }

        public static ServiceZone Find(ServiceType type, string propertyId = null)
        {
            foreach (var z in All)
                if (z && z.type == type && (propertyId == null || z.propertyId == propertyId)) return z;
            return null;
        }

        /// <summary>A bay with no car standing on it (falls back to the first bay).</summary>
        public Transform FreeBay()
        {
            if (bays == null || bays.Length == 0) return transform;
            foreach (var b in bays)
            {
                bool taken = false;
                if (CarSalesServices.Vehicles)
                    foreach (var d in CarSalesServices.Vehicles.All)
                        if (d && d.gameObject.activeInHierarchy && Vector3.Distance(d.transform.position, b.position) < 2.5f) { taken = true; break; }
                if (!taken) return b;
            }
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();
    }
}
