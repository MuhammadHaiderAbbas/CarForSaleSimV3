using System.Collections.Generic;

namespace CarSales.Sim
{
    /// <summary>
    /// Which town is loaded (2 = the master map, 3 = the final map). The Final Map scene carries a TownMapMarker
    /// (set by Tools &gt; Car Sales &gt; Final Map &gt; Build Final Map); everything else - roads, pavements, the map
    /// frame, traffic, the world map and GPS - reads this one place. New games default to the final map.
    /// </summary>
    public static class Town
    {
        /// <summary>2 = master map (V2), 3 = final map (V3). Change with Set() at scene start.</summary>
        public static int Current = 3;

        public static void Set(int version) => Current = version >= 3 ? 3 : 2;
        public static bool IsFinal => Current >= 3;
        public static int Version => IsFinal ? FinalMapPlan.Version : TownPlan.Version;

        // Delegates (same shapes as TownPlan / FinalMapPlan).
        public static PlanRoad[] Roads => IsFinal ? FinalMapPlan.Roads : TownPlan.Roads;
        public static V2[] Ring => IsFinal ? FinalMapPlan.Ring : TownPlan.Ring;
        public static PlanRoundabout[] Roundabouts => IsFinal ? FinalMapPlan.Roundabouts : TownPlan.Roundabouts;
        public static V2[] LightNodes => IsFinal ? FinalMapPlan.LightNodes : TownPlan.LightNodes;
        public static PlanZone[] Zones => IsFinal ? FinalMapPlan.Zones : TownPlan.Zones;
        public static PlanSite[] Sites => IsFinal ? FinalMapPlan.Sites : TownPlan.Sites;

        public static PlanSite Site(string id) => IsFinal ? FinalMapPlan.Site(id) : TownPlan.Site(id);
        public static PlanZone Zone(int number) => IsFinal ? FinalMapPlan.Zone(number) : TownPlan.Zone(number);
        public static List<RoadSegment> Segments() => IsFinal ? FinalMapPlan.Segments() : TownPlan.Segments();
        public static float WidthOf(V2 a, V2 b) => IsFinal ? FinalMapPlan.WidthOf(a, b) : TownPlan.WidthOf(a, b);
        public static float RoadClearance(V2 p) => IsFinal ? FinalMapPlan.RoadClearance(p) : TownPlan.RoadClearance(p);
        public static bool BoxTouchesRoad((float x0, float z0, float x1, float z1) b, float margin = 0.5f)
            => IsFinal ? FinalMapPlan.BoxTouchesRoad(b, margin) : TownPlan.BoxTouchesRoad(b, margin);
        public static List<(V2 a, V2 b)> Pavements(float trim = 9f) => IsFinal ? FinalMapPlan.Pavements(trim) : TownPlan.Pavements(trim);
        public static bool IsRoundabout(V2 p) => IsFinal ? FinalMapPlan.IsRoundabout(p) : TownPlan.IsRoundabout(p);
        public static (float cx, float cz, float halfSize) MapFrame => IsFinal ? FinalMapPlan.MapFrame : TownPlan.MapFrame;
        public static float RoundaboutRadius => IsFinal ? FinalMapPlan.RoundaboutRadius : TownPlan.RoundaboutRadius;
    }
}
