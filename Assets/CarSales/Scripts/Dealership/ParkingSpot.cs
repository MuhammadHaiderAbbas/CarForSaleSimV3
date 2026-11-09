using UnityEngine;

namespace CarSales
{
    /// <summary>A place where a car can be parked. The blue (Z) axis = car front.</summary>
    public class ParkingSpot : MonoBehaviour
    {
        public int index;
        [Tooltip("Lot Expansion upgrade level needed before this spot can be used (0 = always available).")]
        public int requiredLotLevel;
        public CarDisplay Occupant { get; set; }
        public bool IsFree => Occupant == null;
        [Tooltip("Phase 11b: building level needed (2 or 3 = the bigger rebuilt dealership's extra spots; 0 = always).")]
        public int requiredBuildingLevel;
        DealershipManager dealer;
        public bool IsUnlocked
        {
            get
            {
                if (CarSalesServices.Upgrades && CarSalesServices.Upgrades.LotLevel < requiredLotLevel) return false;
                if (requiredBuildingLevel <= 1) return true;
                if (!dealer) dealer = GetComponentInParent<DealershipManager>();
                return dealer && CarSalesServices.Buildings && CarSalesServices.Buildings.Level(dealer.dealershipId) >= requiredBuildingLevel;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = requiredLotLevel > 0 && !Application.isPlaying ? new Color(1f, 0.6f, 0f) : IsFree ? Color.green : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * 0.75f, new Vector3(2.4f, 1.5f, 5f));
            Gizmos.DrawLine(Vector3.up * 0.1f, Vector3.forward * 3f + Vector3.up * 0.1f);
        }
    }
}
