using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using CarSales.Sim;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Builds the low-poly TOWN: roads, three dealerships, fuel station, mechanic workshop, car wash,
    /// used car market, auction center, three homes, investment properties and a skyline.
    /// Every "site" is built in its own local space (front = local -Z, facing the road) and then placed/rotated.
    /// World layout: main road along z = -27, north road along z = 95, avenues at x = -70 / +70.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        class TownRefs
        {
            public GameObject mainDealer;
            public readonly List<NavMeshSurface> surfaces = new List<NavMeshSurface>();
            public Transform playerStart;
        }

        /// <summary>
        /// The town from the plan (Harbour City). Every site is built at its plan position.
        /// finalMap also builds the Harbour City POIs (patrol station, tyres, rent-a-car lot, business park, the two construction plots).
        /// </summary>
        static TownRefs BuildTown(Transform env, Mats m, CarDatabase db, CarDisplay carDisplayPrefab, CustomerAI customerPrefab, bool finalMap = false)
        {
            var refs = new TownRefs();
            BuildGroundV2(env, m);
            Progress("Roads and roundabouts", 0.45f);
            BuildPlanRoads(Group(env, "Roads"), m);

            Progress("Dealerships", 0.5f);
            Vector3 Pos(string id) => W(Town.Site(id).Position);
            float Yaw(string id) => Town.Site(id).Yaw;
            refs.mainDealer = BuildDealershipSite(env, m, refs, carDisplayPrefab, customerPrefab, "autoworld", "AUTO WORLD", "autoworld", Pos("autoworld"), true, 1f, false, yaw: Yaw("autoworld"));
            BuildDealershipSite(env, m, refs, carDisplayPrefab, customerPrefab, "dealer_downtown", "Downtown Motors", "dealer_downtown", Pos("dealer_downtown"), false, 1f, false, yaw: Yaw("dealer_downtown"));
            BuildDealershipSite(env, m, refs, carDisplayPrefab, customerPrefab, "dealer_luxury", "Luxury Lane", "dealer_luxury", Pos("dealer_luxury"), false, 1.6f, true, yaw: Yaw("dealer_luxury"));

            Progress("Town services", 0.6f);
            BuildFuelStation(PlanSiteRoot(env, "FuelStation"), m);
            BuildWorkshop(PlanSiteRoot(env, "MechanicWorkshop"), m, db);
            BuildServiceCentre(PlanSiteRoot(env, "ServiceCentre"), m);
            BuildPartsShop(PlanSiteRoot(env, "PartsDirect"), m);
            BuildMapleClose(PlanSiteRoot(env, "MapleClose"), m, "Maple Close");
            BuildMapleClose(PlanSiteRoot(env, "BirchClose"), m, "Birch Close");
            BuildCarWashCenter(PlanSiteRoot(env, "CarWashCenter"), m);
            BuildUsedCarMarket(PlanSiteRoot(env, "UsedCarMarket"), m, carDisplayPrefab);
            BuildAuctionCenter(PlanSiteRoot(env, "AuctionCenter"), m, carDisplayPrefab);

            Progress("Homes & properties", 0.65f);
            refs.playerStart = BuildHome(PlanSiteRoot(env, "Home_Trailer"), m, PropertyManager.StarterHome, "Starter Trailer", 0);
            BuildHome(PlanSiteRoot(env, "Home_House"), m, "home_house", "Suburban House", 1);
            BuildHome(PlanSiteRoot(env, "Home_Villa"), m, "home_villa", "Hillside Villa", 2);
            BuildInvestment(PlanSiteRoot(env, "RentalApartments"), m, "prop_apartments", "Rental Apartments", new Vector3(16f, 13f, 10f), "APARTMENTS");
            BuildInvestment(PlanSiteRoot(env, "OfficeTower"), m, "prop_office", "Office Tower", new Vector3(16f, 42f, 14f), "OFFICES");
            BuildParkingGarage(PlanSiteRoot(env, "ParkingGarage"), m);

            Progress("Park, town centre, industrial estate", 0.68f);
            BuildZoneDecor(env, m, finalMap);
            if (finalMap) BuildHarbourCityExtras(env, m);
            return refs;
        }

        // ------------------------------------------------------------------ HELPERS
        static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        static Transform Site(Transform parent, string name, Vector3 pos, float yaw)
        {
            var g = Group(parent, name);
            g.localPosition = pos;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return g;
        }

        /// <summary>Group for placeholder primitives that the Asset Studio can replace with an imported model.</summary>
        static Transform Slot(Transform parent, string name, Vector3 pos, PlaceholderKind kind, Vector3 footprint, float yaw = 0f)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = pos;
            var slot = g.gameObject.AddComponent<PlaceholderSlot>();
            slot.kind = kind;
            slot.footprint = footprint;
            slot.yaw = yaw;
            return g;
        }

        static void StreetLamp(Transform g, Vector3 basePos, float headOffsetZ, float headLength, Mats m, float pole = 0.16f)
        {
            var t = Slot(g, "StreetLamp", basePos, PlaceholderKind.StreetLamp, new Vector3(0.6f, 6f, 0.6f), headOffsetZ < 0f ? 180f : 0f);
            Solid(t, "LampPole", new Vector3(0f, 3f, 0f), new Vector3(pole, 6f, pole), m.metal);
            Deco(t, "LampHead", PrimitiveType.Cube, new Vector3(0f, 5.9f, headOffsetZ), new Vector3(pole * 2.6f, 0.12f, headLength), m.lampGlow);
        }

        static GameObject Solid(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, pos, size, mat, keepCollider: true);
            go.isStatic = true;
            return go;
        }

        static GameObject Deco(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Prim(type, name, parent, pos, size, mat);
            go.isStatic = true;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void PointLight(Transform parent, Vector3 pos, float range, float intensity, Color color)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
        }

        static void SignText(Transform parent, Vector3 pos, string text, float size, Color color, Vector2 box, float yRotation = 0f)
        {
            var t = Text3D(text, parent, pos, text, size, color);
            t.rectTransform.sizeDelta = box;
            t.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 1f;
            t.fontSizeMax = size;
        }

        /// <summary>A sign band across the front of a building (local z = frontZ).</summary>
        static void FrontSign(Transform g, float width, float frontZ, float y, string text, Color textColor, Material band)
        {
            Deco(g, "SignBand", PrimitiveType.Cube, new Vector3(0f, y, frontZ - 0.05f), new Vector3(width, 1.5f, 0.5f), band);
            SignText(g, new Vector3(0f, y, frontZ - 0.34f), text, 12f, textColor, new Vector2(width - 1f, 1.3f));
        }

        static MapPlace AddMapPlace(GameObject go, string name, string glyph, Color color, Vector3 arrivalLocal)
        {
            var place = go.AddComponent<MapPlace>();
            place.placeName = name;
            place.glyph = glyph;
            place.color = color;
            place.arrivalPoint = Child("Arrival", go.transform, arrivalLocal);
            return place;
        }

        static GameObject InteractBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Solid(parent, name, pos, size, mat);
            go.layer = InteractLayer;
            return go;
        }

        static ServiceZone AddZone(Transform g, ServiceType type, string name, Vector3 center, Vector3 size, params Vector3[] bayPositions)
        {
            var zone = g.gameObject.AddComponent<ServiceZone>();
            zone.type = type;
            zone.displayName = name;
            zone.center = center;
            zone.size = size;
            var bays = new List<Transform>();
            for (int i = 0; i < bayPositions.Length; i++)
            {
                var b = Child("Bay_" + i, g, bayPositions[i]);
                b.localRotation = Quaternion.Euler(0f, 180f, 0f);   // nose towards the road
                bays.Add(b);
            }
            zone.bays = bays.ToArray();
            return zone;
        }

        static void PropertyForSaleSign(Transform g, Vector3 pos, string propertyId, Mats m)
        {
            var root = new GameObject("ForSaleSign_" + propertyId) { layer = InteractLayer };
            root.transform.SetParent(g, false);
            root.transform.localPosition = pos;
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.3f, 0f);
            box.size = new Vector3(2f, 2.6f, 0.4f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Prim(PrimitiveType.Cube, "Post", visual, new Vector3(-0.8f, 1f, 0f), new Vector3(0.1f, 2f, 0.1f), m.metal);
            Prim(PrimitiveType.Cube, "Post", visual, new Vector3(0.8f, 1f, 0f), new Vector3(0.1f, 2f, 0.1f), m.metal);
            Prim(PrimitiveType.Cube, "Board", visual, new Vector3(0f, 1.8f, 0f), new Vector3(1.9f, 0.9f, 0.06f), m.flagRed);
            SignText(visual, new Vector3(0f, 1.8f, -0.05f), "FOR SALE", 4f, Color.white, new Vector2(1.7f, 0.7f));
            var sign = root.AddComponent<PropertySign>();
            sign.propertyId = propertyId;
            Set(sign, "visual", visual.gameObject);
        }

        // ------------------------------------------------------------------ ROADS
        static void Road(Transform g, Vector3 center, float length, bool alongX, Mats m)
        {
            Vector3 size = alongX ? new Vector3(length, 0.016f, 9f) : new Vector3(9f, 0.016f, length);
            Solid(g, "Road", center + new Vector3(0f, 0.008f, 0f), size, m.asphalt);
            for (float t = -length / 2f + 4f; t < length / 2f - 4f; t += 9f)
            {
                Vector3 p = alongX ? new Vector3(center.x + t, 0.02f, center.z) : new Vector3(center.x, 0.02f, center.z + t);
                Vector3 s = alongX ? new Vector3(3f, 0.01f, 0.18f) : new Vector3(0.18f, 0.01f, 3f);
                Deco(g, "LaneDash", PrimitiveType.Cube, p, s, m.line);
            }
        }

        static void Tree(Transform g, Vector3 pos, Mats m, float s)
        {
            var t = Slot(g, "Tree", pos, PlaceholderKind.Tree, new Vector3(3.2f, 6f, 3.2f) * s, Mathf.Abs(pos.x * 37f + pos.z * 11f) % 360f);
            Solid(t, "TreeTrunk", new Vector3(0f, 1.5f * s, 0f), new Vector3(0.35f, 3f, 0.35f) * s, m.trunk);
            Deco(t, "TreeCanopy", PrimitiveType.Sphere, new Vector3(0f, 3.9f * s, 0f), new Vector3(3.2f, 2.8f, 3.2f) * s, m.leaves);
            Deco(t, "TreeCanopy2", PrimitiveType.Sphere, new Vector3(0.5f, 5f * s, 0.3f), new Vector3(2.2f, 2f, 2.2f) * s, m.leaves);
        }

        static void Plant(Transform g, Vector3 pos, Mats m, float scale) => PottedPlant(g, pos, scale, m);   // phase 13c: a real-looking plant

        // ------------------------------------------------------------------ DEALERSHIPS
        /// <summary>Lot + glass showroom + spots + customers. Uses the original AUTO WORLD layout (world-aligned, road to the south).</summary>
        static GameObject BuildDealershipSite(Transform env, Mats m, TownRefs refs, CarDisplay carDisplayPrefab, CustomerAI customerPrefab,
            string id, string name, string propertyId, Vector3 origin, bool isMain, float budgetMultiplier, bool premium,
            GameObject building = null, bool computer = false, float yaw = 0f, GameObject buildingL2 = null, GameObject buildingL3 = null)
        {
            var site = new GameObject("Dealership_" + name);
            site.transform.SetParent(env, false);
            site.transform.localPosition = origin;
            site.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var g = site.transform;

            // Lot
            Solid(g, "Lot_Asphalt", new Vector3(0f, 0.01f, -9f), new Vector3(36f, 0.02f, 24f), m.asphalt);
            Deco(g, "Curb_W", PrimitiveType.Cube, new Vector3(-18.1f, 0.06f, -9f), new Vector3(0.25f, 0.12f, 24f), m.sidewalk);
            Deco(g, "Curb_E", PrimitiveType.Cube, new Vector3(18.1f, 0.06f, -9f), new Vector3(0.25f, 0.12f, 24f), m.sidewalk);
            for (int i = -2; i <= 2; i++)
                Deco(g, "AisleMark", PrimitiveType.Cube, new Vector3(i * 6f, 0.025f, -9f), new Vector3(1.6f, 0.01f, 0.25f), m.yellowLine);
            Solid(g, "Path_ToShowroom", new Vector3(0f, 0.015f, 6.5f), new Vector3(6f, 0.03f, 7f), m.sidewalk);

            // Phase 11b: three looks (level 1 = the original or your imported building; 2-3 rebuilt, bigger showrooms).
            var levels = LevelRoots(g, id, new Vector3(0f, 0f, 18f), new Vector3(38f, 11f, 20f));
            if (building) FitBuilding(levels[0], building, new Vector3(0f, 0f, 17f), new Vector3(24f, 8f, 14f));   // an imported building instead of the glass showroom
            else BuildShowroom(levels[0], m, name, premium);
            if (buildingL2) FitBuilding(levels[1], buildingL2, new Vector3(0f, 0f, 18f), new Vector3(32f, 9f, 16f)); else BuildShowroomL2(levels[1], m, name, premium);
            if (buildingL3) FitBuilding(levels[2], buildingL3, new Vector3(0f, 0f, 18f), new Vector3(36f, 11f, 18f)); else BuildShowroomL3(levels[2], m, name, premium);
            BuildDealerProps(g, m, premium);

            var dealer = site.AddComponent<DealershipManager>();
            dealer.dealershipId = id;
            dealer.displayName = name;
            dealer.propertyId = isMain ? "" : propertyId;
            dealer.budgetMultiplier = budgetMultiplier;
            dealer.isMain = isMain;
            var spawner = site.AddComponent<CustomerSpawner>();

            var spotsParent = Child("ParkingSpots", g, Vector3.zero);
            var spots = new List<Object>();
            int index = 0;
            for (int i = 0; i < 6; i++) spots.Add(CreateSpot(spotsParent, index++, new Vector3(-10f + i * 4f, 0.02f, -4f), 180f, m));
            for (int i = 0; i < 6; i++)
            {
                var spot = CreateSpot(spotsParent, index++, new Vector3(-10f + i * 4f, 0.02f, -14f), 0f, m);
                if (isMain) spot.requiredLotLevel = i < 3 ? 1 : 2;     // Lot Expansion upgrade (main dealership)
                spots.Add(spot);
            }
            spots.Add(CreateSpot(spotsParent, index++, new Vector3(-6f, 0.12f, 17f), 180f, m, lines: false));
            spots.Add(CreateSpot(spotsParent, index++, new Vector3(6f, 0.12f, 17f), 180f, m, lines: false));
            // Phase 11b: the rebuilt dealership's extra spots (4 on the lot at level 2, 4 more inside at level 3).
            foreach (var (pos, spotYaw) in new[] { (new Vector3(-14.5f, 0.02f, -4f), 180f), (new Vector3(14.5f, 0.02f, -4f), 180f), (new Vector3(-14.5f, 0.02f, -14f), 0f), (new Vector3(14.5f, 0.02f, -14f), 0f) })
            {
                var spot = CreateSpot(spotsParent, index++, pos, spotYaw, m);
                spot.requiredBuildingLevel = 2;
                spots.Add(spot);
            }
            foreach (var pos in new[] { new Vector3(-12f, 0.14f, 14f), new Vector3(12f, 0.14f, 14f), new Vector3(-12f, 0.14f, 22f), new Vector3(12f, 0.14f, 22f) })
            {
                var spot = CreateSpot(spotsParent, index++, pos, 180f, m, lines: false);
                spot.requiredBuildingLevel = 3;
                spots.Add(spot);
            }

            if (isMain || computer) BuildOfficeComputer(g, m);
            BuildShopSign(g, m);
            if (!isMain && !string.IsNullOrEmpty(propertyId)) PropertyForSaleSign(g, new Vector3(14f, 0f, -19.5f), propertyId, m);

            var spawnA = Child("CustomerSpawn_West", g, new Vector3(-26f, 0f, -9f));
            var spawnB = Child("CustomerSpawn_East", g, new Vector3(26f, 0f, -9f));
            var exit = Child("CustomerExit", g, new Vector3(0f, 0f, -36f));
            SetArray(dealer, "spots", spots.ToArray());
            SetArray(dealer, "customerSpawnPoints", new Object[] { spawnA, spawnB });
            Set(dealer, "exitPoint", exit);
            Set(spawner, "customerPrefab", customerPrefab);

            // Local NavMesh for this dealership's customers
            var navGo = new GameObject("NavMesh");
            navGo.transform.SetParent(g, false);
            var surface = navGo.AddComponent<NavMeshSurface>();
            // Bake from colliders only: sign text (TextMeshPro) and decoration meshes have no walkable geometry and made
            // "Source mesh ... has invalid vertex data" warnings; imported meshes may also be unreadable at runtime.
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0f, 3f, -1f);
            surface.size = new Vector3(66f, 14f, 78f);
            surface.layerMask = ~((1 << StudioLayer) | (1 << MinimapLayer) | (1 << 5));
            navGo.AddComponent<RuntimeNavMeshBaker>();
            refs.surfaces.Add(surface);

            AddMapPlace(site, name, "D", premium ? UIFactory.Gold : UIFactory.Accent, new Vector3(0f, 0.1f, -24f));
            var siteInfo = site.AddComponent<Site>();
            siteInfo.siteId = id;
            siteInfo.displayName = name;
            siteInfo.function = SiteFunction.Dealership;
            siteInfo.entrance = Child("Entrance", g, new Vector3(0f, 0f, -22f));
            HideUpperLevels(levels);
            return site;
        }

        /// <summary>Puts an imported building where the showroom would be: scaled to the footprint, on the ground, with colliders.</summary>
        static void FitBuilding(Transform g, GameObject prefab, Vector3 centre, Vector3 footprint, bool turn = true, bool colliders = true)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.name = turn ? "Building" : "Interior";
            go.transform.localPosition = centre;
            go.transform.localRotation = Quaternion.Euler(0f, turn ? 180f : 0f, 0f);   // a building's front (+Z) faces the lot
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds Bounds() { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
            var bb = Bounds();
            float k = Mathf.Min(footprint.x / Mathf.Max(0.01f, bb.size.x), footprint.z / Mathf.Max(0.01f, bb.size.z));
            go.transform.localScale = Vector3.one * k;
            bb = Bounds();
            var target = g.TransformPoint(centre);
            go.transform.position += new Vector3(target.x - bb.center.x, target.y - bb.min.y, target.z - bb.center.z);
            if (colliders && go.GetComponentsInChildren<Collider>().Length == 0)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        }

        static void BuildShowroom(Transform g, Mats m, string name, bool premium)
        {
            var facade = premium ? m.signBoard : m.accentPanel;
            Solid(g, "Floor", new Vector3(0f, 0.02f, 17f), new Vector3(24f, 0.04f, 14f), premium ? (m.marble ?? m.platform) : m.floor);
            Solid(g, "Wall_North", new Vector3(0f, 2.5f, 24f), new Vector3(24.3f, 5f, 0.3f), m.wall);
            Solid(g, "Wall_West", new Vector3(-12f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Wall_East", new Vector3(12f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Roof", new Vector3(0f, 5.15f, 17f), new Vector3(24.8f, 0.3f, 14.8f), m.roof);

            foreach (float x in new[] { -12f, -7.5f, -3f, 3f, 7.5f, 12f })
                Solid(g, "FramePost", new Vector3(x, 2.5f, 10f), new Vector3(0.22f, 5f, 0.32f), m.frame);
            foreach (float x in new[] { -9.75f, -5.25f, 5.25f, 9.75f })
                Solid(g, "GlassPane", new Vector3(x, 2.5f, 10f), new Vector3(4.3f, 5f, 0.06f), m.windowGlass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Solid(g, "FrameBottom", new Vector3(0f, 0.08f, 10f), new Vector3(24.2f, 0.16f, 0.32f), m.frame);
            Solid(g, "DoorHeader", new Vector3(0f, 4.4f, 10f), new Vector3(6f, 1.2f, 0.32f), m.frame);

            Deco(g, "SignBand", PrimitiveType.Cube, new Vector3(0f, 5.95f, 9.95f), new Vector3(24.8f, 1.5f, 0.5f), facade);
            Deco(g, "SignStripe", PrimitiveType.Cube, new Vector3(0f, 5.22f, 9.68f), new Vector3(24.8f, 0.06f, 0.05f), premium ? m.lampGlow : m.ringGlow);
            SignText(g, new Vector3(0f, 6.15f, 9.66f), name.ToUpper(), 16f, premium ? new Color(1f, 0.85f, 0.4f) : Color.white, new Vector2(20f, 1.4f));
            SignText(g, new Vector3(0f, 5.5f, 9.66f), premium ? "PREMIUM CARS" : "USED CARS", 5.5f, new Color(0.45f, 0.7f, 1f), new Vector2(10f, 0.6f));

            for (int ix = -1; ix <= 1; ix++)
                for (int iz = 0; iz < 3; iz++)
                    Deco(g, "CeilingLight", PrimitiveType.Cube, new Vector3(ix * 7.5f, 4.97f, 12.5f + iz * 4.5f), new Vector3(3f, 0.04f, 0.8f), m.ceilingLight);
            PointLight(g, new Vector3(-6f, 4.4f, 15f), 14f, 1.6f, new Color(1f, 0.96f, 0.9f));
            PointLight(g, new Vector3(6f, 4.4f, 19f), 14f, 1.6f, new Color(1f, 0.96f, 0.9f));
            AddReflectionProbe(g, "ShowroomReflections", new Vector3(0f, 2.5f, 17f), new Vector3(24f, 5.2f, 14.2f));

            foreach (float x in new[] { -6f, 6f })
            {
                Solid(g, "DisplayPlatform", new Vector3(x, 0.07f, 17f), new Vector3(6f, 0.05f, 6f), m.platform);
                Deco(g, "PlatformGlow", PrimitiveType.Cylinder, new Vector3(x, 0.045f, 17f), new Vector3(6.3f, 0.02f, 6.3f), premium ? m.lampGlow : m.ringGlow);
            }

            SignText(g, new Vector3(-8.2f, 3.2f, 23.8f), name.ToUpper(), 5f, new Color(0.15f, 0.25f, 0.45f), new Vector2(8f, 1f), 180f);
            foreach (var p in new[] { new Vector3(-11f, 0f, 23f), new Vector3(11f, 0f, 11f), new Vector3(-11f, 0f, 11f) }) Plant(g, p, m, 1f);
            FurnishShowroom(g, m, 1, premium);   // phase 13c
        }

        static void BuildDealerProps(Transform g, Mats m, bool premium)
        {
            foreach (var p in new[] { new Vector3(-32f, 0f, -14f), new Vector3(32f, 0f, -14f), new Vector3(-32f, 0f, 2f), new Vector3(32f, 0f, 2f), new Vector3(-30f, 0f, 18f), new Vector3(30f, 0f, 18f) })
                Tree(g, p, m, 1f);
            foreach (var p in new[] { new Vector3(-19f, 0f, -20.5f), new Vector3(19f, 0f, -20.5f), new Vector3(-19f, 0f, 2.5f), new Vector3(19f, 0f, 2.5f) })
            {
                StreetLamp(g, p, p.z < -9f ? 1f : -1f, 0.6f, m, 0.18f);
            }
            PointLight(g, new Vector3(0f, 6f, -9f), 26f, 1.4f, new Color(1f, 0.9f, 0.7f));
            var bannerMats = premium ? new[] { m.signBoard, m.flagWhite, m.signBoard } : new[] { m.flagRed, m.flagBlue, m.flagWhite };
            int b = 0;
            foreach (float x in new[] { -16.5f, -13.5f, 13.5f, 16.5f })
            {
                var basePos = new Vector3(x, 0f, -20.2f);
                Solid(g, "BannerPole", basePos + new Vector3(0f, 2.5f, 0f), new Vector3(0.1f, 5f, 0.1f), m.metal);
                Deco(g, "Banner", PrimitiveType.Cube, basePos + new Vector3(0.38f, 3.3f, 0f), new Vector3(0.7f, 2.8f, 0.04f), bannerMats[b % 3]);
                SignText(g, basePos + new Vector3(0.38f, 3.3f, -0.03f), premium ? "VIP" : "SALE", 3f, b % 3 == 2 ? new Color(0.8f, 0.1f, 0.1f) : Color.white, new Vector2(0.7f, 0.6f));
                b++;
            }
            foreach (float x in new[] { -8f, 8f })
            {
                var bench = Slot(g, "Bench", new Vector3(x, 0f, 7.6f), PlaceholderKind.Bench, new Vector3(1.8f, 1.0f, 0.6f), 180f);
                Solid(bench, "BenchSeat", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.08f, 0.5f), m.desk);
                Deco(bench, "BenchBack", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.25f), new Vector3(1.8f, 0.45f, 0.06f), m.desk);
                Deco(bench, "BenchLeg", PrimitiveType.Cube, new Vector3(-0.75f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.45f), m.metal);
                Deco(bench, "BenchLeg", PrimitiveType.Cube, new Vector3(0.75f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.45f), m.metal);
            }
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                Solid(g, "Planter", new Vector3(x, 0.3f, 8.4f), new Vector3(1.3f, 0.6f, 1.3f), m.concrete);
                Deco(g, "Bush", PrimitiveType.Sphere, new Vector3(x, 0.85f, 8.4f), new Vector3(1.2f, 0.8f, 1.2f), m.leaves);
            }
        }

        static void BuildOfficeComputer(Transform g, Mats m)
        {
            var computer = new GameObject("Computer") { layer = InteractLayer };
            computer.transform.SetParent(g, false);
            computer.transform.localPosition = new Vector3(0f, 0.04f, 21.5f);
            computer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var cBox = computer.AddComponent<BoxCollider>();
            cBox.center = new Vector3(0f, 0.65f, 0f);
            cBox.size = new Vector3(1.8f, 1.3f, 0.9f);
            // Phase 13c: a real desk (top, side panels, drawers, monitor stand); the box collider stays the same and the
            // showroom furniture puts an office chair in front of it.
            var c = computer.transform;
            var dark = m.blackGlass ? m.blackGlass : m.signBoard;
            Part(c, "DeskTop", new Vector3(0f, 0.73f, 0f), new Vector3(1.6f, 0.04f, 0.8f), m.desk);
            foreach (var sx in new[] { -1f, 1f }) Part(c, "DeskSide", new Vector3(sx * 0.77f, 0.355f, 0f), new Vector3(0.04f, 0.71f, 0.76f), m.frame);
            Part(c, "Modesty", new Vector3(0f, 0.5f, -0.35f), new Vector3(1.5f, 0.36f, 0.02f), m.frame);
            Part(c, "Drawers", new Vector3(0.5f, 0.35f, 0.02f), new Vector3(0.42f, 0.6f, 0.68f), m.desk);
            Part(c, "StandBase", new Vector3(0f, 0.758f, -0.2f), new Vector3(0.24f, 0.015f, 0.2f), dark);
            Part(c, "StandNeck", new Vector3(0f, 0.86f, -0.24f), new Vector3(0.05f, 0.2f, 0.03f), dark);
            Prim(PrimitiveType.Cube, "Monitor", c, new Vector3(0f, 1.02f, -0.2f), new Vector3(0.75f, 0.45f, 0.04f), m.screen);
            Prim(PrimitiveType.Cube, "MonitorBack", c, new Vector3(0f, 1.02f, -0.23f), new Vector3(0.78f, 0.48f, 0.03f), m.signBoard);
            Prim(PrimitiveType.Cube, "Keyboard", c, new Vector3(0f, 0.765f, 0.15f), new Vector3(0.5f, 0.025f, 0.18f), m.signBoard);
            Part(c, "Mouse", new Vector3(0.36f, 0.77f, 0.16f), new Vector3(0.06f, 0.025f, 0.1f), m.signBoard);
            Part(c, "Papers", new Vector3(-0.5f, 0.755f, 0.05f), new Vector3(0.21f, 0.006f, 0.3f), m.flagWhite, euler: new Vector3(0f, 10f, 0f));
            Part(c, "Mug", new Vector3(-0.55f, 0.8f, -0.2f), new Vector3(0.08f, 0.05f, 0.08f), m.pot, type: PrimitiveType.Cylinder);
            computer.AddComponent<DealerComputer>();
        }

        static void BuildShopSign(Transform g, Mats m)
        {
            var sign = new GameObject("ShopSign") { layer = InteractLayer };
            sign.transform.SetParent(g, false);
            sign.transform.localPosition = new Vector3(3f, 0.04f, 20.5f);
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var sBox = sign.AddComponent<BoxCollider>();
            sBox.center = new Vector3(0f, 1.1f, 0f);
            sBox.size = new Vector3(1.3f, 1.6f, 0.3f);
            Prim(PrimitiveType.Cube, "Post", sign.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.1f, 1.4f, 0.1f), m.metal);
            Prim(PrimitiveType.Cube, "Board", sign.transform, new Vector3(0f, 1.5f, 0f), new Vector3(1.2f, 0.5f, 0.06f), m.signBoard);
            var label = Text3D("Label", sign.transform, new Vector3(0f, 1.5f, 0.05f), "CLOSED", 3f, Color.red);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.rectTransform.sizeDelta = new Vector2(1.1f, 0.45f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.5f;
            label.fontSizeMax = 3f;
            Set(sign.AddComponent<ShopToggleSign>(), "label", label);
        }

        // ------------------------------------------------------------------ FUEL STATION
        static void BuildFuelStation(Transform g, Mats m)
        {
            Solid(g, "Forecourt", new Vector3(0f, 0.012f, 8f), new Vector3(22f, 0.024f, 18f), m.concrete);
            Solid(g, "Canopy", new Vector3(0f, 5f, 6f), new Vector3(19f, 0.5f, 11f), m.wall);
            Deco(g, "CanopyBand", PrimitiveType.Cube, new Vector3(0f, 5f, 0.45f), new Vector3(19f, 0.55f, 0.1f), m.flagRed);
            Deco(g, "CanopyLight", PrimitiveType.Cube, new Vector3(0f, 4.73f, 6f), new Vector3(16f, 0.04f, 8f), m.ceilingLight);
            foreach (var p in new[] { new Vector3(-8.5f, 2.5f, 2f), new Vector3(8.5f, 2.5f, 2f), new Vector3(-8.5f, 2.5f, 10f), new Vector3(8.5f, 2.5f, 10f) })
                Solid(g, "Pillar", p, new Vector3(0.5f, 5f, 0.5f), m.frame);
            foreach (float x in new[] { -3.3f, 3.3f })
            {
                Solid(g, "PumpIsland", new Vector3(x, 0.15f, 6f), new Vector3(1.2f, 0.3f, 5f), m.concrete);
                foreach (float z in new[] { 4.5f, 7.5f })
                {
                    Solid(g, "Pump", new Vector3(x, 1.1f, z), new Vector3(0.7f, 1.6f, 0.6f), m.wall);
                    Deco(g, "PumpScreen", PrimitiveType.Cube, new Vector3(x, 1.5f, z - 0.31f), new Vector3(0.45f, 0.3f, 0.02f), m.screen);
                    Deco(g, "PumpStripe", PrimitiveType.Cube, new Vector3(x, 0.6f, z - 0.31f), new Vector3(0.72f, 0.15f, 0.02f), m.flagRed);
                }
            }
            PointLight(g, new Vector3(0f, 4.4f, 6f), 18f, 1.8f, Color.white);

            Solid(g, "Shop", new Vector3(0f, 2f, 17f), new Vector3(12f, 4f, 6f), m.wall);
            Solid(g, "ShopWindow", new Vector3(-3f, 1.8f, 13.97f), new Vector3(4f, 2.2f, 0.06f), m.windowGlass);
            FrontSign(g, 12f, 13.95f, 4.6f, "FUEL & SHOP", Color.white, m.flagRed);
            var kiosk = InteractBox(g, "FuelKiosk", new Vector3(3f, 0.6f, 13.4f), new Vector3(2.2f, 1.2f, 0.9f), m.desk);
            Deco(g, "KioskScreen", PrimitiveType.Cube, new Vector3(3f, 1.4f, 13.4f), new Vector3(0.6f, 0.4f, 0.05f), m.screen);

            // Price pylon
            Solid(g, "Pylon", new Vector3(-11f, 3f, -1f), new Vector3(1.6f, 6f, 0.5f), m.signBoard);
            SignText(g, new Vector3(-11f, 4.6f, -1.27f), "FUEL", 6f, new Color(1f, 0.85f, 0.2f), new Vector2(1.5f, 0.8f));
            SignText(g, new Vector3(-11f, 3.4f, -1.27f), "£0.90 / %", 4f, Color.white, new Vector2(1.5f, 0.6f));

            var zone = AddZone(g, ServiceType.FuelStation, "Fuel Station", new Vector3(0f, 2f, 6f), new Vector3(19f, 4f, 11f),
                               new Vector3(0f, 0.05f, 6f), new Vector3(-6.2f, 0.05f, 6f), new Vector3(6.2f, 0.05f, 6f));
            var desk = kiosk.AddComponent<ServiceDesk>();
            desk.zone = zone;
            desk.type = ServiceType.FuelStation;
            AddMapPlace(g.gameObject, "Fuel Station", "F", UIFactory.Gold, new Vector3(0f, 0.1f, -4f));
        }

        // ------------------------------------------------------------------ MECHANIC WORKSHOP
        static void BuildWorkshop(Transform g, Mats m, CarDatabase db)
        {
            Solid(g, "Apron", new Vector3(0f, 0.012f, -2f), new Vector3(16f, 0.024f, 5f), m.concrete);
            Solid(g, "Floor", new Vector3(0f, 0.02f, 7f), new Vector3(16f, 0.04f, 14f), m.epoxy ?? m.concrete);
            Solid(g, "Wall_Back", new Vector3(0f, 2.5f, 14f), new Vector3(16.3f, 5f, 0.3f), m.wall);
            Solid(g, "Wall_W", new Vector3(-8f, 2.5f, 7f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Wall_E", new Vector3(8f, 2.5f, 7f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Pillar_W", new Vector3(-7.6f, 2.5f, 0f), new Vector3(0.8f, 5f, 0.3f), m.wall);
            Solid(g, "Pillar_E", new Vector3(7.6f, 2.5f, 0f), new Vector3(0.8f, 5f, 0.3f), m.wall);
            Solid(g, "Pillar_Mid", new Vector3(0f, 2.5f, 0f), new Vector3(0.6f, 5f, 0.3f), m.wall);
            Solid(g, "Header", new Vector3(0f, 4.5f, 0f), new Vector3(16.3f, 1f, 0.3f), m.wall);
            Solid(g, "Roof", new Vector3(0f, 5.15f, 7f), new Vector3(16.8f, 0.3f, 14.8f), m.roof);
            FrontSign(g, 16.8f, 0f, 5.95f, "MECHANIC WORKSHOP", Color.white, m.accentPanel);
            foreach (float x in new[] { -3.8f, 3.8f })
                Deco(g, "BayStripe", PrimitiveType.Cube, new Vector3(x, 0.045f, 7f), new Vector3(3f, 0.01f, 6f), m.yellowLine);

            foreach (float x in new[] { -6.4f, -1.2f })
            {
                Solid(g, "LiftPost", new Vector3(x, 1.6f, 7f), new Vector3(0.35f, 3.2f, 0.35f), m.liftYellow);
                Deco(g, "LiftArm", PrimitiveType.Cube, new Vector3(x + (x < -3.8f ? 0.6f : -0.6f), 0.3f, 7f), new Vector3(1.2f, 0.1f, 0.25f), m.liftYellow);
            }
            FurnishWorkshop(g, m);   // phase 13c: tool chest, workbench with pegboard, tyre rack, tube lights
            for (int i = 0; i < 4; i++)
                Deco(g, "Tire", PrimitiveType.Cylinder, new Vector3(-7.2f, 0.13f + i * 0.26f, 12.8f), new Vector3(0.75f, 0.12f, 0.75f), m.tire).AddComponent<BoxCollider>();
            foreach (float z in new[] { 2.2f, 3.3f })
                Deco(g, "OilBarrel", PrimitiveType.Cylinder, new Vector3(7.2f, 0.5f, z), new Vector3(0.6f, 0.5f, 0.6f), m.barrel).AddComponent<CapsuleCollider>();
            PointLight(g, new Vector3(0f, 4.4f, 7f), 16f, 1.8f, new Color(0.95f, 0.97f, 1f));
            for (int i = 0; i < 3; i++)
                Deco(g, "TrafficCone", PrimitiveType.Cylinder, new Vector3(-6f + i * 1.1f, 0.3f, -3.5f), new Vector3(0.35f, 0.3f, 0.35f), m.cone);

            var desk = InteractBox(g, "MechanicDesk", new Vector3(5.5f, 0.55f, 12.6f), new Vector3(2.4f, 1.1f, 0.9f), m.desk);
            Deco(g, "DeskScreen", PrimitiveType.Cube, new Vector3(5.5f, 1.35f, 12.6f), new Vector3(0.6f, 0.4f, 0.05f), m.screen);
            var zone = AddZone(g, ServiceType.Workshop, "Mechanic Workshop", new Vector3(0f, 2f, 7f), new Vector3(16f, 4f, 14f),
                               new Vector3(-3.8f, 0.05f, 7f), new Vector3(3.8f, 0.05f, 7f));
            var sd = desk.AddComponent<ServiceDesk>();
            sd.zone = zone;
            sd.type = ServiceType.Workshop;
            AddMapPlace(g.gameObject, "Mechanic Workshop", "W", UIFactory.Warn, new Vector3(0f, 0.1f, -4f));
        }

        // ------------------------------------------------------------------ CAR WASH
        static void BuildCarWashCenter(Transform g, Mats m)
        {
            Solid(g, "Apron", new Vector3(0f, 0.012f, 6f), new Vector3(20f, 0.024f, 22f), m.concrete);
            Solid(g, "Wall_W", new Vector3(-5f, 2.5f, 7f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Wall_E", new Vector3(5f, 2.5f, 7f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Roof", new Vector3(0f, 5.15f, 7f), new Vector3(10.6f, 0.3f, 14.6f), m.roof);
            FrontSign(g, 10.6f, 0f, 5.95f, "CAR WASH", new Color(0.5f, 0.85f, 1f), m.accentPanel);
            foreach (var p in new[] { new Vector3(-3.3f, 0f, 4.5f), new Vector3(3.3f, 0f, 4.5f), new Vector3(-3.3f, 0f, 9.5f), new Vector3(3.3f, 0f, 9.5f) })
            {
                Deco(g, "Brush", PrimitiveType.Cylinder, p + new Vector3(0f, 1.9f, 0f), new Vector3(1.2f, 1.8f, 1.2f), m.brushBlue).AddComponent<CapsuleCollider>();
                Deco(g, "BrushAxle", PrimitiveType.Cylinder, p + new Vector3(0f, 4.2f, 0f), new Vector3(0.15f, 0.6f, 0.15f), m.metal);
            }
            var top = Deco(g, "TopBrush", PrimitiveType.Cylinder, new Vector3(0f, 3.9f, 7f), new Vector3(0.9f, 4.3f, 0.9f), m.brushBlue);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Deco(g, "Gantry", PrimitiveType.Cube, new Vector3(0f, 4.7f, 7f), new Vector3(9.5f, 0.3f, 0.4f), m.metal);
            PointLight(g, new Vector3(0f, 4.3f, 7f), 12f, 1.6f, new Color(0.7f, 0.85f, 1f));
            for (int i = 0; i < 5; i++)
                Deco(g, "Puddle", PrimitiveType.Cylinder, new Vector3(-1.5f + i * 0.8f, 0.045f, 2f + (i % 2) * 0.7f), new Vector3(1.2f, 0.005f, 0.8f), m.windowGlass);

            var booth = InteractBox(g, "WashAttendant", new Vector3(7.5f, 1.2f, 1.5f), new Vector3(2f, 2.4f, 2f), m.wall);
            Deco(g, "BoothWindow", PrimitiveType.Cube, new Vector3(7.5f, 1.5f, 0.48f), new Vector3(1.4f, 0.8f, 0.05f), m.windowGlass);
            var zone = AddZone(g, ServiceType.CarWash, "Car Wash", new Vector3(0f, 2f, 7f), new Vector3(9.5f, 4f, 14f), new Vector3(0f, 0.05f, 7f));
            var sd = booth.AddComponent<ServiceDesk>();
            sd.zone = zone;
            sd.type = ServiceType.CarWash;
            AddMapPlace(g.gameObject, "Car Wash", "C", new Color(0.3f, 0.8f, 1f), new Vector3(0f, 0.1f, -4f));
        }

        // ------------------------------------------------------------------ USED CAR MARKET
        static void BuildUsedCarMarket(Transform g, Mats m, CarDisplay carDisplayPrefab)
        {
            Solid(g, "Lot", new Vector3(0f, 0.012f, 11f), new Vector3(32f, 0.024f, 24f), m.asphalt);
            foreach (float x in new[] { -16f, 16f })
                Deco(g, "Fence", PrimitiveType.Cube, new Vector3(x, 0.6f, 11f), new Vector3(0.1f, 1.2f, 24f), m.metal);
            Deco(g, "FenceBack", PrimitiveType.Cube, new Vector3(0f, 0.6f, 23f), new Vector3(32f, 1.2f, 0.1f), m.metal);
            foreach (float x in new[] { -13f, 13f })
                Solid(g, "ArchPost", new Vector3(x, 3f, -0.5f), new Vector3(0.4f, 6f, 0.4f), m.metal);
            Deco(g, "ArchBoard", PrimitiveType.Cube, new Vector3(0f, 5.8f, -0.5f), new Vector3(26.4f, 1.4f, 0.3f), m.flagBlue);
            SignText(g, new Vector3(0f, 5.8f, -0.7f), "USED CAR MARKET", 10f, Color.white, new Vector2(24f, 1.2f));
            Solid(g, "OfficeCabin", new Vector3(11f, 1.5f, 20.5f), new Vector3(6f, 3f, 4f), m.wall);
            Deco(g, "CabinWindow", PrimitiveType.Cube, new Vector3(11f, 1.7f, 18.47f), new Vector3(3f, 1.2f, 0.05f), m.windowGlass);
            for (int i = 0; i < 8; i++)
                Deco(g, "Pennant", PrimitiveType.Cube, new Vector3(-14f + i * 4f, 4.6f, -0.5f), new Vector3(0.5f, 0.6f, 0.03f), i % 2 == 0 ? m.flagRed : m.flagWhite);

            var spots = new List<Transform>();
            foreach (float z in new[] { 6f, 14.5f })
                for (int i = 0; i < 4; i++)
                {
                    var s = Child($"LotSpot_{spots.Count}", g, new Vector3(-10.5f + i * 7f, 0.03f, z));
                    s.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    Deco(g, "SpotLine", PrimitiveType.Cube, new Vector3(-10.5f + i * 7f - 3.5f, 0.03f, z), new Vector3(0.1f, 0.01f, 5.2f), m.line);
                    spots.Add(s);
                }
            var market = g.gameObject.AddComponent<UsedCarMarket>();
            market.lotSpots = spots.ToArray();
            Set(market, "displayPrefab", carDisplayPrefab);
            AddMapPlace(g.gameObject, "Used Car Market", "U", new Color(0.75f, 0.5f, 1f), new Vector3(0f, 0.1f, -4f));
        }

        // ------------------------------------------------------------------ AUCTION CENTER
        static void BuildAuctionCenter(Transform g, Mats m, CarDisplay carDisplayPrefab)
        {
            // Sale hall you can walk into (phase 6): seats, stage, rostrum, big screen.
            Solid(g, "HallFloor", new Vector3(0f, 0.02f, 9f), new Vector3(26f, 0.04f, 18f), m.floor);
            Solid(g, "Wall_Back", new Vector3(0f, 4f, 18f), new Vector3(26.3f, 8f, 0.3f), m.buildings[1]);
            Solid(g, "Wall_W", new Vector3(-13f, 4f, 9f), new Vector3(0.3f, 8f, 18f), m.buildings[1]);
            Solid(g, "Wall_E", new Vector3(13f, 4f, 9f), new Vector3(0.3f, 8f, 18f), m.buildings[1]);
            Solid(g, "Front_W", new Vector3(-8f, 4f, 0f), new Vector3(10f, 8f, 0.3f), m.buildings[1]);
            Solid(g, "Front_E", new Vector3(8f, 4f, 0f), new Vector3(10f, 8f, 0.3f), m.buildings[1]);
            Solid(g, "Front_Header", new Vector3(0f, 6.25f, 0f), new Vector3(6f, 3.5f, 0.3f), m.buildings[1]);
            Solid(g, "Roof", new Vector3(0f, 8.15f, 9f), new Vector3(26.8f, 0.3f, 18.8f), m.roof);
            for (int i = 0; i < 4; i++)
                Deco(g, "Window", PrimitiveType.Cube, new Vector3(i < 2 ? -11f + i * 4f : 7f + (i - 2) * 4f, 4.5f, -0.18f), new Vector3(2.4f, 3f, 0.06f), m.windowsGlow);
            FrontSign(g, 26f, 0f, 7.2f, "AUCTION CENTER", new Color(1f, 0.85f, 0.4f), m.signBoard);
            Solid(g, "EntranceCanopy", new Vector3(0f, 3.4f, -1.8f), new Vector3(8f, 0.3f, 3.6f), m.signBoard);
            foreach (float x in new[] { -3.7f, 3.7f })
                Solid(g, "CanopyPost", new Vector3(x, 1.7f, -3.4f), new Vector3(0.25f, 3.4f, 0.25f), m.metal);
            Solid(g, "Apron", new Vector3(0f, 0.012f, -5f), new Vector3(30f, 0.024f, 8f), m.asphalt);

            for (int row = 0; row < 4; row++)
                foreach (float x in new[] { -6f, 6f })
                {
                    Solid(g, "Bench", new Vector3(x, 0.45f, 3.5f + row * 1.7f), new Vector3(8f, 0.1f, 0.5f), m.desk);
                    Deco(g, "BenchBack", PrimitiveType.Cube, new Vector3(x, 0.8f, 3.25f + row * 1.7f), new Vector3(8f, 0.5f, 0.06f), m.desk);
                }
            Solid(g, "Stage", new Vector3(0f, 0.12f, 12.6f), new Vector3(10f, 0.2f, 5f), m.platform);
            Deco(g, "StageRing", PrimitiveType.Cube, new Vector3(0f, 0.23f, 10.05f), new Vector3(10f, 0.02f, 0.1f), m.ringGlow);
            var stage = Child("Stage", g, new Vector3(0f, 0.24f, 12.6f));
            stage.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Solid(g, "Rostrum", new Vector3(6.8f, 0.7f, 15.6f), new Vector3(2.2f, 1.4f, 1f), m.signBoard);
            Deco(g, "Gavel", PrimitiveType.Cube, new Vector3(6.8f, 1.45f, 15.4f), new Vector3(0.3f, 0.1f, 0.1f), m.trunk);
            var auctioneer = Deco(g, "Auctioneer", PrimitiveType.Capsule, new Vector3(6.8f, 1.6f, 16.4f), new Vector3(0.55f, 0.9f, 0.55f), m.customer);
            auctioneer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Deco(g, "ScreenPanel", PrimitiveType.Cube, new Vector3(-2f, 5.2f, 17.8f), new Vector3(7f, 3.2f, 0.1f), m.screen);
            var screen = Text3D("Screen", g, new Vector3(-2f, 5.2f, 17.72f), "AUCTION CENTER", 9f, Color.white);
            screen.rectTransform.sizeDelta = new Vector2(6.6f, 3f);
            screen.enableAutoSizing = true;
            screen.fontSizeMin = 1f;
            screen.fontSizeMax = 9f;
            PointLight(g, new Vector3(0f, 7f, 6f), 18f, 1.6f, new Color(1f, 0.92f, 0.8f));
            PointLight(g, new Vector3(0f, 6f, 13f), 12f, 2.2f, new Color(1f, 0.95f, 0.9f));

            var hall = g.gameObject.AddComponent<AuctionHall>();
            hall.center = new Vector3(0f, 4f, 9f);
            hall.size = new Vector3(26f, 8f, 18f);
            hall.stage = stage;
            hall.screen = screen;
            Set(hall, "displayPrefab", carDisplayPrefab);

            // Registration desk under the canopy.
            var desk = InteractBox(g, "AuctionDesk", new Vector3(-2.2f, 0.6f, -1.2f), new Vector3(2.6f, 1.2f, 0.9f), m.desk);
            Deco(g, "DeskScreen", PrimitiveType.Cube, new Vector3(-2.2f, 1.35f, -1.2f), new Vector3(0.6f, 0.4f, 0.05f), m.screen);
            PointLight(g, new Vector3(0f, 3f, -2f), 10f, 1.4f, new Color(1f, 0.9f, 0.75f));
            var zone = AddZone(g, ServiceType.Auction, "Auction Car Park", new Vector3(0f, 2f, -5f), new Vector3(30f, 4f, 8f),
                               new Vector3(-12f, 0.05f, -5f), new Vector3(-8f, 0.05f, -5f), new Vector3(8f, 0.05f, -5f), new Vector3(12f, 0.05f, -5f));
            foreach (var bay in zone.bays) bay.localRotation = Quaternion.identity;
            var sd = desk.AddComponent<ServiceDesk>();
            sd.zone = zone;
            sd.type = ServiceType.Auction;

            // Viewing yard with 12 numbered bays (east side).
            Solid(g, "YardPad", new Vector3(25f, 0.01f, 7f), new Vector3(20f, 0.02f, 21f), m.asphalt);
            SignText(g, new Vector3(25f, 3.2f, -3.6f), "VIEWING YARD", 6f, new Color(1f, 0.85f, 0.4f), new Vector2(8f, 1f));
            Solid(g, "YardSignPost", new Vector3(25f, 1.4f, -3.4f), new Vector3(0.2f, 2.8f, 0.2f), m.metal);
            var bays = new List<Transform>();
            for (int r = 0; r < 2; r++)
                for (int i = 0; i < 6; i++)
                {
                    float x = 17.2f + i * 3.15f, z = r == 0 ? 2.5f : 12f;
                    var b = Child($"YardBay_{bays.Count + 1}", g, new Vector3(x, 0.03f, z));
                    b.localRotation = Quaternion.Euler(0f, r == 0 ? 0f : 180f, 0f);
                    bays.Add(b);
                    Deco(g, "BayLine", PrimitiveType.Cube, new Vector3(x - 1.575f, 0.025f, z), new Vector3(0.1f, 0.01f, 5f), m.line);
                    SignText(g, new Vector3(x, 0.6f, r == 0 ? -0.4f : 14.9f), (bays.Count).ToString(), 4f, Color.white, new Vector2(1f, 0.6f));
                }
            var yard = g.gameObject.AddComponent<AuctionYard>();
            yard.bays = bays.ToArray();
            Set(yard, "displayPrefab", carDisplayPrefab);
            AddMapPlace(g.gameObject, "Auction Center", "A", UIFactory.Bad, new Vector3(0f, 0.1f, -9f));
        }

        // ------------------------------------------------------------------ HOMES
        /// <returns>The wake point (where the player starts).</returns>
        static Transform BuildHome(Transform g, Mats m, string propertyId, string name, int tier)
        {
            Solid(g, "Lawn", new Vector3(0f, 0.01f, 8f), new Vector3(tier == 2 ? 38f : 26f, 0.02f, 24f), m.ground);
            Vector3 doorPos;
            float drivewayX;
            var home = g;   // the street side stays; the house itself has three levels (phase 11b)
            var levels = LevelRoots(g, propertyId, new Vector3(0f, 0f, 9f), new Vector3(tier == 2 ? 36f : 24f, tier == 2 ? 10f : 7f, 14f));
            g = levels[0];
            if (tier == 0)
            {
                Solid(g, "Trailer", new Vector3(0f, 1.8f, 6f), new Vector3(10f, 3f, 3.6f), m.pot);
                Deco(g, "TrailerStripe", PrimitiveType.Cube, new Vector3(0f, 1.3f, 4.18f), new Vector3(10f, 0.3f, 0.05f), m.flagBlue);
                Deco(g, "TrailerRoof", PrimitiveType.Cube, new Vector3(0f, 3.35f, 6f), new Vector3(10.2f, 0.12f, 3.8f), m.metal);
                foreach (float x in new[] { -3f, 0.5f })
                    Deco(g, "Window", PrimitiveType.Cube, new Vector3(x, 2.1f, 4.18f), new Vector3(1.2f, 0.8f, 0.05f), m.windowGlass);
                Solid(g, "Step", new Vector3(3f, 0.15f, 3.7f), new Vector3(1.4f, 0.3f, 0.8f), m.concrete);
                Deco(g, "Wheel", PrimitiveType.Cylinder, new Vector3(-2f, 0.35f, 4.1f), new Vector3(0.7f, 0.1f, 0.7f), m.tire).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                doorPos = new Vector3(3f, 1.3f, 4.15f);
                drivewayX = -8f;
            }
            else if (tier == 1)
            {
                Solid(g, "House", new Vector3(0f, 3f, 7f), new Vector3(10f, 6f, 8f), m.wall);
                var roof = Deco(g, "Roof", PrimitiveType.Cube, new Vector3(0f, 6f, 7f), new Vector3(10.8f, 4.6f, 4.6f), m.roofTiles ?? m.flagRed);
                roof.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                foreach (var p in new[] { new Vector3(-3f, 1.8f, 2.97f), new Vector3(-3f, 4.4f, 2.97f), new Vector3(3f, 4.4f, 2.97f) })
                    Deco(g, "Window", PrimitiveType.Cube, p, new Vector3(1.6f, 1.2f, 0.05f), m.windowGlass);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(2f, 1.2f, 2.97f), new Vector3(1.2f, 2.3f, 0.06f), m.desk);
                Solid(g, "Garage", new Vector3(8.5f, 1.75f, 6f), new Vector3(5f, 3.5f, 6f), m.wall);
                Deco(g, "GarageDoor", PrimitiveType.Cube, new Vector3(8.5f, 1.4f, 2.97f), new Vector3(3.6f, 2.6f, 0.06f), m.metal);
                doorPos = new Vector3(2f, 1.2f, 2.9f);
                drivewayX = 8.5f;
            }
            else
            {
                Solid(g, "VillaBase", new Vector3(0f, 2f, 8f), new Vector3(18f, 4f, 10f), m.flagWhite);
                Solid(g, "VillaUpper", new Vector3(-3f, 5.8f, 9f), new Vector3(11f, 3.6f, 8f), m.flagWhite);
                Deco(g, "Glass1", PrimitiveType.Cube, new Vector3(-4f, 2f, 2.97f), new Vector3(7f, 3f, 0.06f), m.windowGlass);
                Deco(g, "Glass2", PrimitiveType.Cube, new Vector3(-3f, 5.8f, 4.97f), new Vector3(9f, 2.6f, 0.06f), m.windowGlass);
                Deco(g, "RoofTrim", PrimitiveType.Cube, new Vector3(0f, 4.05f, 8f), new Vector3(18.4f, 0.15f, 10.4f), m.frame);
                Deco(g, "Pool", PrimitiveType.Cube, new Vector3(-6f, 0.03f, 16f), new Vector3(8f, 0.04f, 4f), m.brushBlue);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(4f, 1.3f, 2.97f), new Vector3(1.6f, 2.6f, 0.06f), m.desk);
                foreach (float x in new[] { -9f, 9f }) Plant(g, new Vector3(x, 0f, 1.5f), m, 1.3f);
                doorPos = new Vector3(4f, 1.3f, 2.9f);
                drivewayX = 13.5f;
            }

            g = home;
            BuildHomeLevel(levels[1], m, tier, 2, doorPos);
            BuildHomeLevel(levels[2], m, tier, 3, doorPos);
            HideUpperLevels(levels);
            Solid(g, "Driveway", new Vector3(drivewayX, 0.015f, 0f), new Vector3(4.5f, 0.03f, 8f), m.concrete);
            Deco(g, "Mailbox", PrimitiveType.Cube, new Vector3(drivewayX - 3.2f, 0.9f, -3.5f), new Vector3(0.4f, 0.35f, 0.5f), m.flagRed);

            // Front door = the bed (sleeping / moving in)
            var door = new GameObject("FrontDoor") { layer = InteractLayer };
            door.transform.SetParent(g, false);
            door.transform.localPosition = doorPos;
            var col = door.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 2.4f, 0.4f);
            var wake = Child("WakePoint", g, new Vector3(doorPos.x, 0f, doorPos.z - 2f));
            wake.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var bed = door.AddComponent<HomeBed>();
            bed.propertyId = propertyId;
            bed.wakePoint = wake;
            BuildHomeInterior(g, m, propertyId, tier, wake);   // phase 13c: walk in, sleep in a real bed
            PointLight(g, doorPos + new Vector3(0f, 1.5f, -0.8f), 6f, 1f, new Color(1f, 0.85f, 0.6f));

            var zone = AddZone(g, ServiceType.Home, name, new Vector3(drivewayX, 2f, 0f), new Vector3(4.5f, 4f, 8f), new Vector3(drivewayX, 0.05f, 0f));
            zone.propertyId = propertyId;
            if (tier > 0) PropertyForSaleSign(g, new Vector3(drivewayX > 0 ? -6f : 6f, 0f, -3f), propertyId, m);
            AddMapPlace(g.gameObject, name, "H", new Color(0.2f, 0.8f, 0.65f), new Vector3(doorPos.x, 0.1f, -2f));
            return wake;
        }

        // ------------------------------------------------------------------ INVESTMENTS
        static void BuildInvestment(Transform g, Mats m, string propertyId, string name, Vector3 size, string sign)
        {
            var site = g;
            var levels = LevelRoots(g, propertyId, new Vector3(0f, 0f, size.z / 2f + 2f), new Vector3(size.x * 1.5f, size.y * 2.2f, size.z * 1.3f));
            g = levels[0];
            Solid(g, "Building", new Vector3(0f, size.y / 2f, size.z / 2f + 2f), size, m.buildings[propertyId.Length % m.buildings.Length]);
            int floors = Mathf.FloorToInt(size.y / 3.3f);
            for (int f = 0; f < floors; f++)
                Deco(g, "Windows", PrimitiveType.Cube, new Vector3(0f, f * 3.3f + 1.8f, 1.95f), new Vector3(size.x * 0.86f, 1.3f, 0.06f), m.windowsGlow);
            Deco(g, "Entrance", PrimitiveType.Cube, new Vector3(0f, 1.3f, 1.92f), new Vector3(3f, 2.6f, 0.08f), m.windowGlass);
            Deco(g, "SignBoard", PrimitiveType.Cube, new Vector3(0f, size.y + 1f, 2f), new Vector3(size.x * 0.8f, 1.6f, 0.3f), m.signBoard);
            SignText(g, new Vector3(0f, size.y + 1f, 1.8f), sign, 10f, Color.white, new Vector2(size.x * 0.75f, 1.3f));
            g = site;
            BuildInvestmentLevel(levels[1], m, propertyId, size, sign, 2);
            BuildInvestmentLevel(levels[2], m, propertyId, size, sign, 3);
            HideUpperLevels(levels);
            PropertyForSaleSign(g, new Vector3(size.x * 0.75f + 2f, 0f, 0.5f), propertyId, m);
            AddMapPlace(g.gameObject, name, "$", UIFactory.Gold, new Vector3(0f, 0.1f, -2f));
        }

        static void BuildParkingGarage(Transform g, Mats m)
        {
            var site = g;
            var levels = LevelRoots(g, "prop_parking", new Vector3(0f, 0f, 9f), new Vector3(26f, 27f, 16f));
            g = levels[0];
            for (int level = 0; level < 4; level++)
                Solid(g, "Deck", new Vector3(0f, level * 3.3f + 0.15f, 9f), new Vector3(18f, 0.3f, 14f), m.concrete);
            foreach (float x in new[] { -8.5f, 0f, 8.5f })
                foreach (float z in new[] { 2.5f, 15.5f })
                    Solid(g, "Column", new Vector3(x, 5f, z), new Vector3(0.6f, 10f, 0.6f), m.concrete);
            Deco(g, "Sign", PrimitiveType.Cube, new Vector3(-7f, 11.5f, 2f), new Vector3(3f, 3f, 0.3f), m.flagBlue);
            SignText(g, new Vector3(-7f, 11.5f, 1.8f), "P", 20f, Color.white, new Vector2(2.6f, 2.6f));
            g = site;
            BuildParkingGarageLevel(levels[1], m, 2);
            BuildParkingGarageLevel(levels[2], m, 3);
            HideUpperLevels(levels);
            PropertyForSaleSign(g, new Vector3(15f, 0f, 0.5f), "prop_parking", m);
            AddMapPlace(g.gameObject, "Parking Garage", "$", UIFactory.Gold, new Vector3(0f, 0.1f, -2f));
        }

        // ------------------------------------------------------------------ SKYLINE
        static void BuildBackdrop(Transform g, Mats m)
        {
            float[] heights = { 22f, 34f, 18f, 28f, 40f, 24f, 30f, 26f, 36f, 20f };
            for (int i = 0; i < 10; i++)
            {
                float x = -225f + i * 50f;
                Building(g, new Vector3(x, 0f, 225f), new Vector3(30f, heights[i], 14f), m.buildings[i % m.buildings.Length], m, true);
                Building(g, new Vector3(x + 20f, 0f, -110f), new Vector3(28f, heights[(i + 4) % heights.Length] * 0.8f, 14f), m.buildings[(i + 1) % m.buildings.Length], m, false);
            }
        }

        static void Building(Transform g, Vector3 pos, Vector3 size, Material mat, Mats m, bool facesSouth)
        {
            // Front of a replacement model (+Z) faces the street: south-facing buildings turn 180°.
            var t = Slot(g, "Building", pos, PlaceholderKind.Building, size, facesSouth ? 180f : 0f);
            var body = Solid(t, "Building", new Vector3(0f, size.y / 2f, 0f), size, mat);
            var facade = FacadeFor(mat, size, m);
            if (facade)
            {
                // Phase 13: a real façade (windows, frames, lit rooms at night) on the walls and a gravel roof on top.
                body.GetComponent<Renderer>().sharedMaterials = new[] { facade, m.roof };
                RoofClutter(t, size, m, Mathf.Abs(pos.x * 7f + pos.z * 3f));
                return;
            }
            float faceZ = facesSouth ? -size.z / 2f - 0.06f : size.z / 2f + 0.06f;
            int floors = Mathf.FloorToInt(size.y / 3.2f);
            for (int f = 1; f < floors; f++)
                Deco(t, "Windows", PrimitiveType.Cube, new Vector3(0f, f * 3.2f + 0.6f, faceZ), new Vector3(size.x * 0.86f, 1.3f, 0.06f), m.windowsGlow);
        }

        /// <summary>Glass offices for tall or clad blocks; rendered or brick flats for the rest.</summary>
        static Material FacadeFor(Material mat, Vector3 size, Mats m)
        {
            if (m.facadeOffice == null) return null;
            if (mat == m.cladding || size.y >= 18f) return m.facadeOffice;
            int i = System.Array.IndexOf(m.buildings, mat);
            return i == 1 ? m.facadeBrick : m.facadeApartment;
        }

        /// <summary>Parapet, air-conditioning units, a lift housing and a water tank: what a real flat roof has on it.</summary>
        static void RoofClutter(Transform t, Vector3 size, Mats m, float seed)
        {
            float top = size.y, hx = size.x / 2f, hz = size.z / 2f;
            Deco(t, "Parapet_N", PrimitiveType.Cube, new Vector3(0f, top + 0.45f, hz - 0.15f), new Vector3(size.x, 0.9f, 0.3f), m.concrete);
            Deco(t, "Parapet_S", PrimitiveType.Cube, new Vector3(0f, top + 0.45f, -hz + 0.15f), new Vector3(size.x, 0.9f, 0.3f), m.concrete);
            Deco(t, "Parapet_E", PrimitiveType.Cube, new Vector3(hx - 0.15f, top + 0.45f, 0f), new Vector3(0.3f, 0.9f, size.z - 0.6f), m.concrete);
            Deco(t, "Parapet_W", PrimitiveType.Cube, new Vector3(-hx + 0.15f, top + 0.45f, 0f), new Vector3(0.3f, 0.9f, size.z - 0.6f), m.concrete);
            int units = 2 + (int)(seed % 3f);
            for (int i = 0; i < units; i++)
            {
                float x = -hx + 2.5f + ((seed * (i + 3)) % Mathf.Max(1f, size.x - 5f));
                float z = -hz + 2f + ((seed * (i + 7)) % Mathf.Max(1f, size.z - 4f));
                var unit = Deco(t, "AirCon", PrimitiveType.Cube, new Vector3(x, top + 0.6f, z), new Vector3(1.6f, 1.2f, 1.1f), m.metal);
                unit.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
            if (size.y > 12f)
            {
                var housing = Deco(t, "LiftHousing", PrimitiveType.Cube, new Vector3(hx * 0.4f, top + 1.6f, 0f), new Vector3(3.2f, 3.2f, 3.6f), m.concrete);
                housing.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                var tank = Deco(t, "WaterTank", PrimitiveType.Cylinder, new Vector3(-hx * 0.5f, top + 1.4f, hz * 0.3f), new Vector3(2.2f, 1.4f, 2.2f), m.metal);
                tank.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }
        }
    }
}
