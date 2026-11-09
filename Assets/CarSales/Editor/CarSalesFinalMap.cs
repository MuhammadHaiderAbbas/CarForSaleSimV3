using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14 "Final Map": builds the finished game town (the master-map town plus the Phase 14 POIs)
    ///
    ///   Tools &gt; Car Sales &gt; Final Map &gt; Build Final Map
    ///
    /// The Final Map reuses everything '2. Build Everything' made (materials, car database, prefabs) and only
    /// (re)creates the scene. New POIs (patrol station, tyre shop, RentGo rent-a-car lot, the south-side business
    /// park and two plots under construction) are dressed with the Sketchfab models listed in
    /// Assets/CarSales/FinalMap/final-map-assets.json. Download them first:
    ///
    ///   SKETCHFAB_TOKEN=&lt;your token&gt; python3 tools/download_sketchfab_assets.py
    ///
    /// Everything is optional: a model that is missing falls back to a primitive stand-in (a yellow box with a
    /// PlaceholderSlot, so Asset Studio &gt; Town can swap it later) and the scene still builds. The car GLBs are
    /// placed as static display cars - they are NOT for sale and never enter the stock system.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        public const string FinalScenePath = Gen + "/Scenes/FinalMap.unity";
        const string ManifestPath = Root + "/FinalMap/final-map-assets.json";

        static readonly List<string> buildReport = new List<string>();

        // ------------------------------------------------------------------ MENU
        [MenuItem("Tools/Car Sales/Final Map/Build Final Map", priority = 11)]
        static void BuildFinalMapMenu()
        {
            if (!File.Exists(GameScenePath))
            {
                EditorUtility.DisplayDialog("Car Sales",
                    "Run '2. Build Everything' first - the Final Map reuses its materials, car database and prefabs.", "OK");
                return;
            }
            var missing = FinalMapManifest().Where(a => !AssetDatabase.LoadAssetAtPath<GameObject>(a.importPath)).ToList();
            string assetNote = missing.Count == 0
                ? $"All {FinalMapManifest().Count} Sketchfab models were found.\n\n"
                : $"{missing.Count} of {FinalMapManifest().Count} Sketchfab models are not downloaded yet - those spots will use primitive stand-ins.\n" +
                  "Fetch them later with:\n    SKETCHFAB_TOKEN=<token> python3 tools/download_sketchfab_assets.py\n\n";
            if (!EditorUtility.DisplayDialog("Build Final Map",
                    assetNote + "Rebuild the Final Map scene now?\n\n" + FinalScenePath,
                    "Build", "Cancel")) return;
            try
            {
                buildReport.Clear();
                BuildFinalMap();
                string summary = string.Join("\n", buildReport);
                Debug.Log("[CarSales] Final Map build report:\n" + summary);
                EditorUtility.DisplayDialog("Car Sales Simulator",
                    "Final Map built:\n" + FinalScenePath + "\n\n" +
                    summary + "\n\nThe main menu now loads the Final Map. Open the scene and press PLAY to try it.",
                    "Let's go");
            }
            catch (OperationCanceledException) { /* user chose not to save the open scene */ }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Build Final Map", "Build failed:\n" + e.Message + "\n\nSee the Console for details.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/Car Sales/Final Map/Download Assets (instructions)", priority = 12)]
        static void DownloadInstructionsMenu()
        {
            const string cmd = "SKETCHFAB_TOKEN=<your token> python3 tools/download_sketchfab_assets.py";
            EditorGUIUtility.systemCopyBuffer = cmd;
            EditorUtility.DisplayDialog("Download the Final Map models",
                "The Final Map's real 3D models are not in the repository (several GB). Download them once with:\n\n" +
                cmd + "\n\n(Sketchfab API token: sketchfab.com → Settings → Password & API. The command has been copied to the clipboard.)\n\n" +
                "Files land in Assets/CarSales/Sketchfab/<category>/<uid>.glb (~3.5 GB, skipped if over 95 MB).\n" +
                "Then run 'Tools > Car Sales > Final Map > Build Final Map' again to place them.\n\n" +
                "Everything in the manifest is CC0 or CC-BY; see docs/FINAL_MAP_ASSETS.md and THIRD_PARTY_NOTICES.txt.",
                "OK");
        }

        /// <summary>Rewrites the MainMenu scene's MainMenuUI.gameScene (the wizard bakes "DealershipTest"; a Final Map build flips it).</summary>
        static void PointMainMenuAt(string sceneName)
        {
            if (!File.Exists(MenuScenePath))
            {
                Debug.LogWarning("[CarSales] Final Map: MainMenu.unity not found - the main menu still points at DealershipTest. Run '2. Build Everything' once.");
                return;
            }
            var menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
            int flipped = 0;
            foreach (var root in menu.GetRootGameObjects())
                foreach (var ui in root.GetComponentsInChildren<MainMenuUI>(true))
                {
                    SetString(ui, "gameScene", sceneName);
                    flipped++;
                }
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            EditorSceneManager.CloseScene(menu, true);
            Debug.Log($"[CarSales] Final Map: main menu now loads '{sceneName}' ({flipped} MainMenuUI component(s)).");
        }

        static void BuildFinalMap()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException();
            Progress("Loading materials", 0.05f);
            var mats = LoadMats();
            var db = Resources.Load<CarDatabase>(CarDatabase.ResourceName);
            var carDisplay = AssetDatabase.LoadAssetAtPath<CarDisplay>($"{Gen}/Prefabs/CarDisplay.prefab");
            var customer = AssetDatabase.LoadAssetAtPath<CustomerAI>($"{Gen}/Prefabs/Customer.prefab");
            if (!db || !carDisplay || !customer)
            {
                throw new InvalidOperationException("The Phase 1-13 generated assets are missing. Run 'Tools > Car Sales > 2. Build Everything' first.");
            }
            var actions = LoadActions();
            if (actions == null) throw new InvalidOperationException("Input actions are missing - run 'Tools > Car Sales > 2. Build Everything' first.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Progress("Building the Final Map", 0.1f);
            BuildSceneCore(mats, db, carDisplay, customer, actions, CarSales.Sim.FinalMapPlan.Version, FinalScenePath, finalMap: true);
            Progress("Main menu", 0.95f);
            PointMainMenuAt(Path.GetFileNameWithoutExtension(FinalScenePath));   // the menu scene is re-created by 'Build Everything' and always points at DealershipTest

            Progress("Saving", 0.97f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(FinalScenePath);
        }

        // ------------------------------------------------------------------ MANIFEST
        sealed class FinalMapAsset
        {
            public string key, category, uid, name, author, licence, importPath;
            public bool needsReview;
        }

        static List<FinalMapAsset> manifestCache;

        static List<FinalMapAsset> FinalMapManifest()
        {
            if (manifestCache != null) return manifestCache;
            manifestCache = new List<FinalMapAsset>();
            if (!File.Exists(ManifestPath))
            {
                Debug.LogWarning("[CarSales] Final Map manifest not found at " + ManifestPath + " - the Final Map will use primitive stand-ins only.");
                return manifestCache;
            }
            try
            {
                var root = CarSales.Foundation.MiniJson.Parse(File.ReadAllText(ManifestPath));
                foreach (var item in root["assets"].Items)
                {
                    var a = new FinalMapAsset
                    {
                        key = item["key"]?.AsString("") ?? "",
                        category = item["category"]?.AsString("") ?? "",
                        uid = item["uid"]?.AsString("") ?? "",
                        name = item["name"]?.AsString("") ?? "(unnamed)",
                        author = item["author"]?.AsString("") ?? "",
                        licence = item["licence"]?.AsString("") ?? "",
                        importPath = item["importPath"]?.AsString("") ?? "",
                        needsReview = item["needsReview"]?.AsBool(false) ?? false,
                    };
                    if (!string.IsNullOrEmpty(a.key)) manifestCache.Add(a);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[CarSales] Could not parse " + ManifestPath + ": " + e.Message);
            }
            return manifestCache;
        }

        /// <summary>Deterministic tiny hash (string.GetHashCode is randomised per process).</summary>
        static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s ?? "") h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        static FinalMapAsset FindAsset(string keyOrCategory, bool firstMatch)
        {
            var all = FinalMapManifest();
            if (firstMatch) return all.FirstOrDefault(a => a.category == keyOrCategory);
            return all.FirstOrDefault(a => a.key == keyOrCategory)
                ?? all.FirstOrDefault(a => a.name == keyOrCategory)
                ?? all.FirstOrDefault(a => a.category == keyOrCategory);
        }

        /// <summary>Picks up to <paramref name="count"/> assets from a category, deterministically (key order).</summary>
        static List<FinalMapAsset> PickFromCategory(string category, int count)
            => FinalMapManifest().Where(a => a.category == category).OrderBy(a => a.key).Take(count).ToList();

        // ------------------------------------------------------------------ GLB PLACEMENT
        static bool IsGlbReady(string importPath) => !string.IsNullOrEmpty(importPath) && AssetDatabase.LoadAssetAtPath<GameObject>(importPath);

        /// <summary>
        /// Scales the model to the footprint and stands it on the ground at <paramref name="localCentre"/> (parent-local).
        /// Buildings (allowSidewaysFlip: false) keep their front; cars and machines may turn 90° to fit.
        /// Returns false when the prefab is missing or has no renderers (caller decides on the fallback).
        /// </summary>
        static bool TryPlaceGlb(Transform parent, FinalMapAsset asset, Vector3 localCentre, Vector3 footprint, float yawDeg, bool allowSidewaysFlip, string what)
        {
            var ov = FindOverride(asset);
            if (ov != null && ov.skip)
            {
                buildReport.Add($"· {what}: skipped by override (Assets/CarSales/FinalMap/final-map-overrides.json); using a stand-in");
                Debug.LogWarning($"[CarSales] Final Map: '{what}' is skipped by final-map-overrides.json.");
                return false;
            }
            if (ov != null) { yawDeg += ov.yawOffset; footprint *= ov.footprintScale; }
            var prefab = string.IsNullOrEmpty(asset?.importPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(asset.importPath);
            if (asset == null)
            {
                buildReport.Add($"· {what}: not in the manifest (docs/FINAL_MAP_ASSETS.md); using a stand-in");
                Debug.LogWarning($"[CarSales] Final Map: no manifest entry for '{what}'. The build used a stand-in.");
                return false;
            }
            if (!prefab)
            {
                buildReport.Add($"· {what}: {asset.name} - NOT DOWNLOADED (run tools/download_sketchfab_assets.py); using a stand-in");
                Debug.LogWarning($"[CarSales] Final Map: '{asset.name}' not found at {asset.importPath}. Run tools/download_sketchfab_assets.py with SKETCHFAB_TOKEN, then rebuild.");
                return false;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            go.transform.localScale = Vector3.one;

            var rs = go.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0) { Debug.LogWarning($"[CarSales] Final Map: '{asset.name}' has no renderers - skipped."); UnityEngine.Object.DestroyImmediate(go); return false; }
            Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            float k = Mathf.Min(footprint.x / Mathf.Max(0.01f, b.size.x), footprint.z / Mathf.Max(0.01f, b.size.z));
            if (allowSidewaysFlip)
            {
                float kAlt = Mathf.Min(footprint.x / Mathf.Max(0.01f, b.size.z), footprint.z / Mathf.Max(0.01f, b.size.x));
                if (kAlt > k * 1.001f) { k = kAlt; go.transform.Rotate(0f, 90f, 0f); }
            }
            go.transform.localScale = Vector3.one * k;

            b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            Vector3 worldCentre = parent.TransformPoint(localCentre);
            go.transform.position += new Vector3(worldCentre.x - b.center.x, worldCentre.y - b.min.y, worldCentre.z - b.center.z);

            if (go.GetComponentsInChildren<Collider>(true).Length == 0)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            buildReport.Add($"· {what}: {asset.name} ({asset.licence?.ToUpperInvariant()}, {asset.author}{(asset.needsReview ? ", NEEDS REVIEW" : "")})");
            return true;
        }

        /// <summary>A labelled placeholder box with a PlaceholderSlot (Asset Studio &gt; Town can replace it with an approved model).</summary>
        static void StandIn(Transform parent, string name, Vector3 localPos, Vector3 size, PlaceholderKind kind, Mats m)
        {
            var slot = Slot(parent, name + "_StandIn", localPos, kind, size, 0f);
            Solid(slot, "Box", new Vector3(0f, size.y / 2f, 0f), size, m.buildings[2]);
        }

        /// <summary>Places a manifest asset, falling back to a stand-in primitive. Returns true if the real model is in place.</summary>
        static bool PlaceGlbOrStandIn(Transform parent, string key, Vector3 localCentre, Vector3 footprint, float yawDeg, bool allowSidewaysFlip, PlaceholderKind kind, Mats m, string what)
        {
            var asset = FindAsset(key, false);
            if (TryPlaceGlb(parent, asset, localCentre, footprint, yawDeg, allowSidewaysFlip, what)) return true;
            StandIn(parent, what.Replace(' ', '_'), new Vector3(localCentre.x, 0f, localCentre.z), new Vector3(footprint.x, Mathf.Max(4f, footprint.y), footprint.z), kind, m);
            return false;
        }

        // ------------------------------------------------------------------ THE POIs
        static void BuildPatrolStation(Transform env, Mats m)
        {
            var s = FinalMapPlan.Site("PatrolStation");
            var g = Site(env, s.Name, W(s.Position), s.Yaw);
            // Apron and parking in front (facing Dealer Road, north).
            Solid(g, "Apron", new Vector3(0f, 0.008f, -2f), new Vector3(24f, 0.016f, 10f), m.asphalt);
            foreach (float x in new[] { -6f, 0f, 6f }) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(x, 0.02f, -2f), new Vector3(0.12f, 0.01f, 8f), m.line);
            bool glb = PlaceGlbOrStandIn(g, "building_patrol", new Vector3(0f, 0f, 7.5f), new Vector3(19f, 9f, 15f), 0f, false, PlaceholderKind.Building, m, "Patrol station");
            if (!glb)
            {
                Solid(g, "Station", new Vector3(0f, 2.1f, 7.5f), new Vector3(18f, 4.2f, 12f), m.wall);
                Solid(g, "StationRoof", new Vector3(0f, 4.35f, 7.5f), new Vector3(19f, 0.3f, 13f), m.roof);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(0f, 1.2f, 1.4f), new Vector3(1.4f, 2.4f, 0.1f), m.desk);
                Glass(g, "WindowL", new Vector3(-6f, 1.6f, 1.36f), new Vector3(5f, 1.6f, 0.1f), m.windowGlass);
                Glass(g, "WindowR", new Vector3(6f, 1.6f, 1.36f), new Vector3(5f, 1.6f, 0.1f), m.windowGlass);
            }
            // Sign on the roof + a blue light bar (emissive only) + flag pole.
            Solid(g, "SignBoard", new Vector3(0f, glb ? 8.6f : 5.4f, 1.35f), new Vector3(13f, 1.5f, 0.35f), m.signBoard);
            SignText(g, new Vector3(0f, glb ? 8.6f : 5.4f, 1.14f), "PATROL STATION", 5f, Color.white, new Vector2(12f, 1.2f), 0f);
            var bar = Solid(g, "LightBar", new Vector3(0f, glb ? 9.6f : 6.6f, 6f), new Vector3(3.2f, 0.34f, 0.6f), m.signBoard);
            bar.GetComponent<Renderer>().sharedMaterial = m.lampGlow;
            Deco(g, "FlagPole", PrimitiveType.Cylinder, new Vector3(10.5f, 4f, 3f), new Vector3(0.14f, 8f, 0.14f), m.metal);
            Deco(g, "Flag", PrimitiveType.Cube, new Vector3(9.3f, 7.4f, 3f), new Vector3(2.2f, 1.3f, 0.06f), m.flagBlue);
            AddMapPlace(g.gameObject, "Patrol Station", "X", new Color(0.2f, 0.45f, 0.95f), new Vector3(0f, 0.1f, -6f));
        }

        static void BuildTyreShop(Transform env, Mats m)
        {
            var s = FinalMapPlan.Site("TyreShop");
            var g = Site(env, s.Name, W(s.Position), s.Yaw);
            Solid(g, "Workshop", new Vector3(0f, 2.1f, 6f), new Vector3(17f, 4.2f, 11f), m.buildings[0]);
            Solid(g, "WorkshopRoof", new Vector3(0f, 4.3f, 6f), new Vector3(17.5f, 0.25f, 11.5f), m.roof);
            Deco(g, "SignBoard", PrimitiveType.Cube, new Vector3(0f, 5.3f, 0.35f), new Vector3(15f, 1.4f, 0.3f), m.flagRed);
            SignText(g, new Vector3(0f, 5.3f, 0.14f), "TYRES & WHEELS", 4.6f, Color.white, new Vector2(14f, 1.1f), 0f);
            Deco(g, "Door", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.36f), new Vector3(4.2f, 3f, 0.12f), m.metal);
            foreach (float x in new[] { -6f, 6f }) Glass(g, "Window", new Vector3(x, 1.7f, 0.36f), new Vector3(3.4f, 1.7f, 0.1f), m.windowGlass);
            // Tyre stacks out front, hubcaps against the wall.
            foreach (float x in new[] { -7f, -4.6f, 4.6f })
                for (int i = 0; i < 3; i++)
                    Deco(g, "TyreStack", PrimitiveType.Cylinder, new Vector3(x, 0.42f + i * 0.42f, -1.6f), new Vector3(0.85f, 0.42f, 0.85f), m.tire);
            Solid(g, "Apron", new Vector3(0f, 0.008f, -2f), new Vector3(20f, 0.016f, 8f), m.asphalt);
            AddMapPlace(g.gameObject, "Tyres & Wheels", "P", new Color(0.85f, 0.35f, 0.15f), new Vector3(0f, 0.1f, -5f));
        }

        static void BuildRentGo(Transform env, Mats m)
        {
            var s = FinalMapPlan.Site("RentGo");
            var g = Site(env, s.Name, W(s.Position), s.Yaw);
            // Lot: asphalt with bay lines, a sales office at the back (a GLB showroom fits the part).
            Solid(g, "Lot", new Vector3(0f, 0.006f, 0f), new Vector3(30f, 0.012f, 19f), m.asphalt);
            foreach (float x in new[] { -10f, -5f, 0f, 5f, 10f }) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(x, 0.014f, 2f), new Vector3(0.12f, 0.01f, 13f), m.line);
            bool office = PlaceGlbOrStandIn(g, "Car showroom", new Vector3(0f, 0f, 6.5f), new Vector3(13f, 6f, 9f), 0f, false, PlaceholderKind.Building, m, "RentGo sales office");
            if (!office)
            {
                Solid(g, "Office", new Vector3(0f, 1.75f, 6.5f), new Vector3(13f, 3.5f, 8f), m.wall);
                Solid(g, "OfficeRoof", new Vector3(0f, 3.6f, 6.5f), new Vector3(13.5f, 0.25f, 8.5f), m.roof);
                Deco(g, "Door", PrimitiveType.Cube, new Vector3(0f, 1.1f, 2.4f), new Vector3(1.5f, 2.2f, 0.12f), m.desk);
                Glass(g, "ShowGlass", new Vector3(0f, 1.5f, 2.38f), new Vector3(10f, 2f, 0.1f), m.windowGlass);
            }
            Solid(g, "SignBoard", new Vector3(0f, office ? 6.4f : 4.6f, 2.32f), new Vector3(15f, 1.5f, 0.32f), m.flagRed);
            SignText(g, new Vector3(0f, office ? 6.4f : 4.6f, 2.1f), "RENTGO", 5f, Color.white, new Vector2(13f, 1.2f), 0f);
            SignText(g, new Vector3(0f, office ? 5.35f : 3.6f, 2.1f), "RENTALS & SALES", 2.6f, new Color(1f, 0.85f, 0.3f), new Vector2(12f, 0.7f), 0f);
            // Display cars out front - static, for show only (not in stock, not for sale, no CarVisual).
            var carAssets = new List<FinalMapAsset>();
            foreach (var cat in new[] { "car_hatchback", "car_sedan", "car_suv", "car_sports" })
                carAssets.AddRange(PickFromCategory(cat, 1));
            carAssets.AddRange(PickFromCategory("car_luxury", 1));
            float[] bayX = { -10f, -5f, 0f, 5f, 10f };
            int bay = 0;
            foreach (var asset in carAssets)
            {
                if (bay >= bayX.Length) break;
                if (TryPlaceGlb(g, asset, new Vector3(bayX[bay], 0.02f, 0.5f), new Vector3(4.6f, 1.6f, 4.6f), 0f, true, "RentGo display car")) bay++;   // missing GLB: leave the bay empty
            }
            // A couple of cars at the far end of the lot.
            foreach (var asset in PickFromCategory("car_pickup", 1))
                TryPlaceGlb(g, asset, new Vector3(-12.5f, 0.02f, -6f), new Vector3(4.8f, 1.7f, 4.8f), 90f, true, "RentGo display car");
            AddMapPlace(g.gameObject, "RentGo", "R", new Color(0.9f, 0.25f, 0.2f), new Vector3(0f, 0.1f, -9f));
        }

        static void BuildOffices(Transform env, Mats m)
        {
            // (site id, sign, manifest asset key, meter height, footprint)
            var specs = new (string id, string sign, string key, float height, Vector3 footprint)[]
            {
                ("NorthgateOffices", "NORTHGATE OFFICES", "Headquarters building, office building", 15f, new Vector3(23f, 15f, 16f)),
                ("OfficesWest", "OFFICES WEST", "Office building 2", 13f, new Vector3(19f, 13f, 14f)),
            };
            int fleetIndex = 0;
            foreach (var (id, sign, key, height, footprint) in specs)
            {
                fleetIndex++;
                var s = FinalMapPlan.Site(id);
                var g = Site(env, s.Name, W(s.Position), s.Yaw);
                bool glb = PlaceGlbOrStandIn(g, key, new Vector3(0f, 0f, footprint.z / 2f + 2f), footprint, 0f, false, PlaceholderKind.Building, m, sign);
                if (!glb)
                {
                    Solid(g, "Block", new Vector3(0f, height / 2f, footprint.z / 2f + 2f), new Vector3(footprint.x, height, footprint.z), m.darkGlass);
                    int floors = Mathf.FloorToInt(height / 3.3f);
                    for (int f = 0; f < floors; f++)
                        Deco(g, "Windows", PrimitiveType.Cube, new Vector3(0f, f * 3.3f + 1.8f, 2f - footprint.z / 2f + 0.02f), new Vector3(footprint.x * 0.86f, 1.4f, 0.08f), m.windowsGlow);
                }
                Solid(g, "Entrance", new Vector3(0f, 2.1f, 1.85f), new Vector3(5f, 4.2f, 0.3f), m.flagWhite);
                Deco(g, "SignBoard", PrimitiveType.Cube, new Vector3(0f, height + 1.1f, 2f), new Vector3(footprint.x * 0.8f, 1.6f, 0.3f), m.signBoard);
                SignText(g, new Vector3(0f, height + 1.1f, 1.78f), sign, 4.4f, Color.white, new Vector2(footprint.x * 0.75f, 1.3f), 0f);
                Solid(g, "Parking", new Vector3(0f, 0.006f, -3.5f), new Vector3(footprint.x + 10f, 0.012f, 6f), m.asphalt);
                StreetLamp(g, new Vector3(-footprint.x / 2f - 1f, 0f, 0f), 0.8f, 1.4f, m);
                StreetLamp(g, new Vector3(footprint.x / 2f + 1f, 0f, 0f), 0.8f, 1.4f, m);
                var car = PickFromCategory(fleetIndex == 1 ? "car_sedan" : "car_suv", 1).FirstOrDefault();
                if (car != null) TryPlaceGlb(g, car, new Vector3(footprint.x / 2f - 3f, 0.02f, -3.5f), new Vector3(4.6f, 1.6f, 4.6f), 90f, true, "company car");
                AddMapPlace(g.gameObject, s.Name, "O", UIFactory.Info, new Vector3(0f, 0.1f, -6f));
            }
        }

        static void BuildConstructionPlot(Transform env, Mats m, string siteId, string displayName, string finalKey, int startDay, int daysPerStage)
        {
            var s = FinalMapPlan.Site(siteId);
            var root = Site(env, s.Name, W(s.Position), s.Yaw);
            var stages = new Transform[ConstructionStages.StageCount];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = Group(root, $"Stage_{i}_{ConstructionStages.StageName(i).Replace(' ', '_')}");

            // ---- Stage 0: cleared soil, fence, stakes, a DANGER board
            {
                var g = stages[ConstructionStages.Ground];
                Solid(g, "Soil", new Vector3(0f, 0.012f, 5f), new Vector3(26f, 0.024f, 17f), m.fieldSoil);
                foreach (float x in new[] { -13f, -7f, -1f, 5f, 11f })
                {
                    Solid(g, "Fence", new Vector3(x, 1.1f, -2f), new Vector3(0.14f, 2.2f, 0.14f), m.metal);
                    Solid(g, "Fence", new Vector3(x, 1.1f, 12f), new Vector3(0.14f, 2.2f, 0.14f), m.metal);
                }
                Solid(g, "FenceRail", new Vector3(0f, 1.6f, -2f), new Vector3(26f, 0.16f, 0.1f), m.metal);
                Solid(g, "FenceRail", new Vector3(0f, 1.6f, 12f), new Vector3(26f, 0.16f, 0.1f), m.metal);
                foreach (float x in new[] { -13f, -6.5f, 0f, 6.5f, 13f }) Solid(g, "Stake", new Vector3(x, 0.4f, 5f), new Vector3(0.12f, 0.8f, 0.12f), m.desk);
                Deco(g, "DangerBoard", PrimitiveType.Cube, new Vector3(0f, 1.3f, -2.4f), new Vector3(3.4f, 1.5f, 0.15f), m.flagRed);
                SignText(g, new Vector3(0f, 1.45f, -2.5f), "SITE", 3.6f, Color.white, new Vector2(3.2f, 0.8f), 0f);
                SignText(g, new Vector3(0f, 0.95f, -2.5f), "KEEP OUT", 1.6f, Color.white, new Vector2(3.2f, 0.45f), 0f);
            }
            // ---- Stage 1: machines on site (crane, excavator, dozer, forklift, hut, scaffold, material piles)
            {
                var g = stages[ConstructionStages.Site];
                Solid(g, "Soil", new Vector3(0f, 0.012f, 5f), new Vector3(26f, 0.024f, 17f), m.fieldSoil);
                // Tower crane (primitive on purpose - two mast sections and a jib)
                Solid(g, "CraneMast", new Vector3(-6f, 6.5f, 6f), new Vector3(1.6f, 13f, 1.6f), m.flagRed);
                Solid(g, "CraneJib", new Vector3(-1f, 12.6f, 6f), new Vector3(11f, 0.9f, 1.1f), m.flagRed);
                Solid(g, "CraneCounterJib", new Vector3(-9.5f, 12.6f, 6f), new Vector3(6f, 0.9f, 1.1f), m.metal);
                Solid(g, "CraneCounterweight", new Vector3(-12f, 11.4f, 6f), new Vector3(1.6f, 2.2f, 1.6f), m.concrete);
                Deco(g, "CraneCable", PrimitiveType.Cylinder, new Vector3(2.5f, 8f, 6f), new Vector3(0.05f, 4.2f, 0.05f), m.blackGlass);
                Deco(g, "CraneHook", PrimitiveType.Cube, new Vector3(2.5f, 5.6f, 6f), new Vector3(0.4f, 0.6f, 0.4f), m.metal);
                // The real machines (with yellow-box stand-ins when not downloaded)
                int machine = 0;
                foreach (var asset in PickFromCategory("construction_machine", 6).Where(a => a.name != "Construction site, building site, tower crane").Take(4))
                {
                    Vector3 pos = new Vector3[] { new Vector3(6f, 0.02f, 1.5f), new Vector3(-1f, 0.02f, 9.5f), new Vector3(8f, 0.02f, 9f), new Vector3(1f, 0.02f, 0.5f) }[machine % 4];
                    if (!TryPlaceGlb(g, asset, pos, new Vector3(5.2f, 3f, 5.2f), 35f * machine, true, "construction machine"))
                        StandIn(g, "Machine_" + machine, pos, new Vector3(4.2f, 2.6f, 6.5f), PlaceholderKind.WorkshopKit, m);
                    machine++;
                }
                // Site hut + scaffold + material piles
                Solid(g, "SiteHut", new Vector3(-10.5f, 1.25f, 9.5f), new Vector3(3.6f, 2.5f, 2.6f), m.timber);
                Solid(g, "HutRoof", new Vector3(-10.5f, 2.6f, 9.5f), new Vector3(3.9f, 0.2f, 2.9f), m.roof);
                var scaffoldAsset = FindAsset("Pergula 3D scaffolding", false);
                TryPlaceGlb(g, scaffoldAsset, new Vector3(9f, 0.02f, 5f), new Vector3(7f, 5.5f, 3.2f), 90f, true, "scaffolding");
                Solid(g, "PileConcrete", new Vector3(-4f, 0.6f, 2f), new Vector3(2.4f, 1.2f, 2.4f), m.concrete);
                Solid(g, "PileBricks", new Vector3(-9f, 0.5f, 4f), new Vector3(1.8f, 1f, 1.8f), m.brick);
                Deco(g, "SandPile", PrimitiveType.Sphere, new Vector3(4f, 0.5f, 4.2f), new Vector3(2.6f, 1.1f, 2.6f), m.fieldWheat);
            }
            // ---- Stage 2: the concrete frame is up (a half-built shell when downloaded)
            {
                var g = stages[ConstructionStages.Draft];
                Solid(g, "Soil", new Vector3(0f, 0.012f, 5f), new Vector3(26f, 0.024f, 17f), m.fieldSoil);
                var asset = FindAsset("Building under construction", false);
                if (!TryPlaceGlb(g, asset, new Vector3(0f, 0f, 6f), new Vector3(21f, 10f, 14f), 0f, false, "draft shell"))
                {
                    foreach (float x in new[] { -8f, 0f, 8f })
                        foreach (float z in new[] { 1f, 11f })
                            Solid(g, "Column", new Vector3(x, 4.5f, z), new Vector3(0.9f, 9f, 0.9f), m.concrete);
                    Solid(g, "Slab1", new Vector3(0f, 3.2f, 6f), new Vector3(20f, 0.45f, 13f), m.concrete);
                    Solid(g, "Slab2", new Vector3(0f, 6.4f, 6f), new Vector3(20f, 0.45f, 13f), m.concrete);
                    Solid(g, "Slab3", new Vector3(0f, 9.2f, 6f), new Vector3(20f, 0.45f, 13f), m.concrete);
                }
            }
            // ---- Stage 3: the finished, signed building
            {
                var g = stages[ConstructionStages.Final];
                Solid(g, "Soil", new Vector3(0f, 0.012f, 5f), new Vector3(26f, 0.024f, 17f), m.ground);
                var asset = FindAsset(finalKey, false);
                bool glb = TryPlaceGlb(g, asset, new Vector3(0f, 0f, 6f), new Vector3(20f, 12f, 15f), 0f, false, "finished building");
                if (!glb)
                {
                    Solid(g, "Building", new Vector3(0f, 4f, 6f), new Vector3(17f, 8f, 13f), m.darkGlass);
                    Solid(g, "EntranceCanopy", new Vector3(0f, 3.4f, -0.6f), new Vector3(7f, 0.3f, 2.2f), m.flagWhite);
                    for (float y = 1.8f; y < 8f; y += 3.2f)
                        Deco(g, "FloorLine", PrimitiveType.Cube, new Vector3(0f, y, -0.55f), new Vector3(17.2f, 0.15f, 0.08f), m.frame);
                }
                Solid(g, "TotemPole", new Vector3(-9f, 1.5f, -2.6f), new Vector3(0.35f, 3f, 0.35f), m.metal);
                Solid(g, "TotemSign", new Vector3(-9f, 3.6f, -2.6f), new Vector3(9f, 1.6f, 0.3f), m.signBoard);
                SignText(g, new Vector3(-9f, 3.6f, -2.82f), displayName.ToUpperInvariant(), 3.4f, Color.white, new Vector2(8.6f, 1.2f), 0f);
                for (int i = 0; i < 6; i++) Deco(g, "Bay", PrimitiveType.Cube, new Vector3(-12.5f + i * 5f, 0.014f, -1.5f), new Vector3(0.12f, 0.01f, 9f), m.line);
            }

            var site = root.gameObject.AddComponent<ConstructionSite>();
            site.siteId = siteId;
            site.displayName = displayName;
            site.startDay = startDay;
            site.daysPerStage = daysPerStage;
            site.stageRoots = stages;
            AddMapPlace(root.gameObject, displayName, "Y", new Color(0.95f, 0.6f, 0.1f), new Vector3(0f, 0.1f, -4f));
        }

        static void BuildParkStatuesAndSigns(Transform env, Mats m)
        {
            // A statue in the park (models are CC0 - Rijksmuseum-style scans).
            var zone = Town.Zone(13);
            var g = Group(env, "Zone13_ParkExtras");
            foreach (var asset in PickFromCategory("prop_park", 2))
                TryPlaceGlb(g, asset, new Vector3(zone.Centre.x - 25f, 0f, zone.Centre.z + 12f), new Vector3(2.4f, 3.4f, 2.4f), 20f * (StableHash(asset.key) % 18), true, "park statue");
            // Road signs for the two new roads.
            var j0 = Group(env, "BusinessParkSigns");
            RoadSign(j0, new Vector3(112f, 0f, -133f), 0f, "SOUTHSIDE BUSINESS PARK", m.flagBlue, Color.white, 5.2f);
            RoadSign(j0, new Vector3(140f, 0f, -147f), 270f, "HORIZON DRIVE · RING ROAD", m.flagWhite, new Color(0.1f, 0.1f, 0.12f), 4.6f);
        }

        /// <summary>
        /// The curated 7,742-face hero villa (CC BY, by zigurat architecture studio) becomes the Hillside Villa's exterior shell.
        /// Final map only; the master map keeps the primitive villa untouched. Door, pool and plants stay as they are.
        /// When the model is not downloaded the primitive shell is kept and the build says so in the report.
        /// </summary>
        static void BuildHeroVilla(Transform env)
        {
            var asset = FindAsset("building_house_01", false);
            var site = env.Find("Home_Villa");
            var level1 = site ? site.Find("Level1") : null;
            if (level1 == null)
            {
                buildReport.Add("· Hero villa: Home_Villa/Level1 not found in the scene - the primitive villa was kept.");
                Debug.LogWarning("[CarSales] Final Map: Home_Villa/Level1 not found; the hero villa was not placed.");
                return;
            }
            if (!TryPlaceGlb(level1, asset, new Vector3(0f, 0f, 8f), new Vector3(17f, 7f, 11f), 0f, false, "hero villa"))
            {
                buildReport.Add("· Hero villa: building_house_01 not downloaded - kept the primitive villa (run tools/download_sketchfab_assets.py)");
                return;
            }
            foreach (var shellPart in new[] { "VillaBase", "VillaUpper", "Glass1", "Glass2", "RoofTrim" })
            {
                var t = level1.Find(shellPart);
                if (t) t.gameObject.SetActive(false);
            }
            buildReport.Add("· Hero villa: primitive shell replaced with the curated GLB (CC BY, zigurat architecture studio).");
        }

        static void BuildFinalMapExtras(Transform env, Mats m)
        {
            Progress("Hero villa", 0.54f);
            BuildHeroVilla(env);
            Progress("Patrol station", 0.55f);
            BuildPatrolStation(env, m);
            Progress("Tyres & Wheels", 0.58f);
            BuildTyreShop(env, m);
            Progress("RentGo", 0.60f);
            BuildRentGo(env, m);
            Progress("Southside business park", 0.63f);
            BuildOffices(env, m);
            Progress("Northgate Site", 0.66f);
            BuildConstructionPlot(env, m, "NorthgateSite", "Northgate Business Park", "Office building 2", startDay: 1, daysPerStage: 2);
            Progress("Dockside Site", 0.69f);
            BuildConstructionPlot(env, m, "DocksideSite", "Dockside Works", "General Ship Repair", startDay: 2, daysPerStage: 3);
            Progress("Park statues and signs", 0.72f);
            BuildParkStatuesAndSigns(env, m);
            buildReport.Add("· Final Map POIs placed. Missing models are listed above - fetch and rebuild to swap them in.");
        }
    }
}
