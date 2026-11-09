using System.Collections.Generic;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>The town's road segments for the traffic lane graph (set by the wizard / World Builder). Without one, the default town is used.</summary>
    public class RoadNetwork : MonoBehaviour
    {
        public List<RoadSegment> segments = new List<RoadSegment>();
        public float roadWidth = 9f;
        [Tooltip("Phase 12: junctions with traffic lights (empty = every junction).")]
        public List<V2> lightNodes = new List<V2>();
        public static RoadNetwork Instance { get; private set; }
        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }
    }
}
