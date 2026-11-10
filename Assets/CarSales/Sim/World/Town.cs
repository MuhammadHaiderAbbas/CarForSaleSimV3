using System.Collections.Generic;

namespace CarSales.Sim
{
    /// <summary>
    /// The one town of Car For Sale Simulator: Harbour City (see HarbourPlan). Everything that needs the town —
    /// roads, pavements, the map frame, traffic, the world map, GPS, saves — reads this one place.
    /// Kept as a facade so older call sites (TownMapMarker.awake, saves, editor tooling) keep working; everything
    /// delegates to HarbourPlan.
    /// </summary>
    public static class Town
    {
        /// <summary>The town version stamped into scenes and saves (4 = Harbour City).</summary>
        public const int HarbourCity = HarbourPlan.Version;
        public static int Current = HarbourCity;

        /// <summary>Harbour City is the only town — accepting any older version and loading it anyway.</summary>
        public static void Set(int version) => Current = HarbourCity;
        public static bool IsFinal => true;
        public static int Version => HarbourPlan.Version;

        // Delegates.
        public static PlanRoad[] Roads => HarbourPlan.Roads;
        public static V2[] Ring => HarbourPlan.Ring;
        public static PlanRoundabout[] Roundabouts => HarbourPlan.Roundabouts;
        public static V2[] LightNodes => HarbourPlan.LightNodes;
        public static PlanZone[] Zones => HarbourPlan.Zones;
        public static PlanSite[] Sites => HarbourPlan.Sites;

        public static PlanSite Site(string id) => HarbourPlan.Site(id);
        public static PlanZone Zone(int number) => HarbourPlan.Zone(number);
        public static PlanZone ZoneAt(int number, V2 p) => HarbourPlan.ZoneAt(number, p);
        public static List<RoadSegment> Segments() => HarbourPlan.Segments();
        public static float WidthOf(V2 a, V2 b) => HarbourPlan.WidthOf(a, b);
        public static float SegmentDistance(V2 p, V2 a, V2 b) => HarbourPlan.SegmentDistance(p, a, b);
        public static float RoadClearance(V2 p) => HarbourPlan.RoadClearance(p);
        public static bool BoxTouchesRoad((float x0, float z0, float x1, float z1) b, float margin = 0.5f) => HarbourPlan.BoxTouchesRoad(b, margin);
        public static List<(V2 a, V2 b)> Pavements(float trim = 9f) => HarbourPlan.Pavements(trim);
        public static bool IsRoundabout(V2 p) => HarbourPlan.IsRoundabout(p);
        public static (float cx, float cz, float halfSize) MapFrame => HarbourPlan.MapFrame;
        public static float RoundaboutRadius => HarbourPlan.RoundaboutRadius;
    }
}
