using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>A named place shown on the map, the minimap, the GPS and the taxi/tow lists.</summary>
    public class MapPlace : MonoBehaviour
    {
        public static readonly List<MapPlace> All = new List<MapPlace>();

        public string placeName = "Place";
        public string glyph = "?";
        public Color color = new Color(0.13f, 0.47f, 0.95f);
        [Tooltip("Where the player arrives by taxi (empty = this transform).")]
        public Transform arrivalPoint;
        [Tooltip("Only list on the map when this property is owned (empty = always).")]
        public string requiresProperty;

        public Vector3 ArrivalPosition => arrivalPoint ? arrivalPoint.position : transform.position;
        public bool IsVisible => string.IsNullOrEmpty(requiresProperty) || (CarSalesServices.Properties && CarSalesServices.Properties.Owns(requiresProperty));

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        void Start() => MinimapMarker.Attach(gameObject, color, 7f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();
    }
}
