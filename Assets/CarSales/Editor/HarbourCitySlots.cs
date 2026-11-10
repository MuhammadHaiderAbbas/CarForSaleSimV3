using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Foundation;
using CarSales.Sim;
using UnityEditor;
using UnityEngine;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Per-site model upload slots for Harbour City. Every site in the plan has one slot in
    /// Assets/CarSales/HarbourCity/harbour-map-slots.json ({ siteId, path, yawOffset, scale }).
    /// The Harbour City builder applies the slots last: the uploaded model is fitted to the site's
    /// footprint, stands on the ground at the footprint centre, and everything on the site that is not
    /// ground/lot/fence/sign/light/tree/bay dressing is hidden (i.e. the generated building is replaced).
    ///
    ///   Tools &gt; Car Sales &gt; Harbour City &gt; Map Slots…
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        const string SlotsPath = Root + "/HarbourCity/harbour-map-slots.json";

        internal sealed class MapSlot
        {
            public string siteId, path;
            public float yawOffset, scale = 1f;
        }

        /// <summary>Reads the slots file. Missing or broken file = no slots (never throws during a build).</summary>
        internal static List<MapSlot> LoadHarbourCitySlots()
        {
            var list = new List<MapSlot>();
            if (!File.Exists(SlotsPath)) return list;
            try
            {
                var root = CarSales.Foundation.MiniJson.Parse(File.ReadAllText(SlotsPath));
                var slots = root?["slots"]?.Items;
                if (slots == null) return list;
                foreach (var item in slots)
                {
                    string id = item?["siteId"]?.AsString("") ?? "";
                    if (string.IsNullOrEmpty(id)) continue;
                    list.Add(new MapSlot
                    {
                        siteId = id,
                        path = item["path"]?.AsString("") ?? "",
                        yawOffset = (float)(item["yawOffset"]?.Num ?? 0.0),
                        scale = Mathf.Clamp((float)(item["scale"]?.Num ?? 1.0), 0.1f, 10f),
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CarSales] Could not parse {SlotsPath}: {e.Message} - no uploaded models were applied.");
            }
            return list;
        }

        internal static void SaveHarbourCitySlots(IEnumerable<MapSlot> slots)
        {
            var lines = new List<string> { "{", "  \"version\": 1,", "  \"slots\": [" };
            var kept = slots.Where(s => s != null && !string.IsNullOrEmpty(s.path)).ToList();
            for (int i = 0; i < kept.Count; i++)
            {
                var s = kept[i];
                lines.Add("    { \"siteId\": " + MiniJson.Quote(s.siteId) +
                          ", \"path\": " + MiniJson.Quote(s.path) +
                          ", \"yawOffset\": " + s.yawOffset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                          ", \"scale\": " + s.scale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                          " }" + (i < kept.Count - 1 ? "," : ""));
            }
            lines.Add("  ]");
            lines.Add("}");
            File.WriteAllText(SlotsPath, string.Join("\n", lines) + "\n");
        }

        /// <summary>Names a site root can have in the scene (plan roots are named by site id; the POI builders use the display name).</summary>
        static Transform FindSiteRoot(Transform env, PlanSite site)
        {
            var direct = env.Find(site.Id) ?? env.Find(site.Name);
            if (direct) return direct;
            // Deeper search in case the root is grouped.
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
                if (t != env && (t.name == site.Id || t.name == site.Name)) return t;
            return null;
        }

        // What stays visible when an uploaded model takes over a site (case-insensitive substring match).
        static readonly string[] slotKeepTokens =
        {
            "lawn", "apron", "lot", "bay", "drive", "ground", "park", "fence", "sign",
            "light", "tree", "line", "curb", "path", "road", "hedge", "soil", "stake", "deck",
        };

        /// <summary>Applies the per-site uploaded models after everything else is built. Public so the Map Slots window can call it.</summary>
        internal static void ApplyHarbourCitySlots(Transform env)
        {
            var slots = LoadHarbourCitySlots();
            if (slots.Count == 0) return;
            foreach (var slot in slots)
            {
                var site = HarbourPlan.Site(slot.siteId);
                if (site == null) { Debug.LogWarning($"[CarSales] Map slot '{slot.siteId}' is not a Harbour City site - skipped."); continue; }
                if (string.IsNullOrEmpty(slot.path)) continue;
                var root = FindSiteRoot(env, site);
                if (!root) { Debug.LogWarning($"[CarSales] Map slot '{slot.siteId}': site root not found in the scene - skipped."); continue; }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(slot.path);
                if (!prefab)
                {
                    Debug.LogWarning($"[CarSales] Map slot '{slot.siteId}': model not found at {slot.path} - the generated building was kept.");
                    continue;
                }

                // 1) Hand the site's generated buildings over: keep only ground/lot/dressing children.
                int hidden = 0;
                foreach (Transform child in root)
                {
                    string n = child.name.ToLowerInvariant();
                    if (slotKeepTokens.Any(t => n.Contains(t))) continue;
                    child.gameObject.SetActive(false);
                    hidden++;
                }

                // 2) Fit the uploaded model into the plot and stand it on the ground.
                var holder = new GameObject("Slot_Model_" + site.Id).transform;
                holder.SetParent(root, false);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0f, slot.yawOffset, 0f);
                go.transform.localScale = Vector3.one;

                var rs = go.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
                if (rs.Length == 0)
                {
                    Debug.LogWarning($"[CarSales] Map slot '{slot.siteId}': '{prefab.name}' has no renderers - skipped.");
                    UnityEngine.Object.DestroyImmediate(holder.gameObject);
                    continue;
                }
                Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float plotX = site.FX1 - site.FX0, plotZ = site.FZ1 - site.FZ0;
                float k = Mathf.Min(plotX * 0.82f / Mathf.Max(0.01f, b.size.x), plotZ * 0.82f / Mathf.Max(0.01f, b.size.z)) * slot.scale;
                go.transform.localScale = Vector3.one * k;
                b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                Vector3 plotCentre = root.TransformPoint(new Vector3((site.FX0 + site.FX1) / 2f, 0f, (site.FZ0 + site.FZ1) / 2f));
                go.transform.position += new Vector3(plotCentre.x - b.center.x, root.position.y - b.min.y, plotCentre.z - b.center.z);
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
                Debug.Log($"[CarSales] Map slot '{slot.siteId}': '{prefab.name}' fitted to the plot ({hidden} generated object(s) hidden).");
            }
        }
    }

    /// <summary>
    /// Tools &gt; Car Sales &gt; Harbour City &gt; Map Slots… — one upload picker per site of the plan,
    /// with a live preview, yaw/scale tweak and Apply. Sites are listed in plan order.
    /// </summary>
    public sealed class MapSlotsWindow : EditorWindow
    {
        [MenuItem("Tools/Car Sales/Harbour City/Map Slots…", priority = 15)]
        static void Show() => GetWindow<MapSlotsWindow>(true, "Harbour City — Map Slots", true).minSize = new Vector2(680, 480);

        sealed class Row
        {
            public CarSalesSetupWizard.MapSlot slot;
            public GameObject picked;
            public Vector2 scroll;
        }

        readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        Vector2 scrollPos;
        string status;

        static string Glyph(string id)
        {
            if (id.StartsWith("Home", StringComparison.OrdinalIgnoreCase)) return "H";
            if (id.IndexOf("dealer", StringComparison.OrdinalIgnoreCase) >= 0 || id == "autoworld") return "D";
            if (id.IndexOf("Fuel", StringComparison.OrdinalIgnoreCase) >= 0) return "F";
            if (id.IndexOf("Wash", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Mechanic", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Tyre", StringComparison.OrdinalIgnoreCase) >= 0) return "W";
            if (id.IndexOf("Auction", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Market", StringComparison.OrdinalIgnoreCase) >= 0) return "U";
            if (id.IndexOf("Patrol", StringComparison.OrdinalIgnoreCase) >= 0) return "P";
            if (id.IndexOf("Site", StringComparison.OrdinalIgnoreCase) >= 0) return "C";
            if (id.IndexOf("RentGo", StringComparison.OrdinalIgnoreCase) >= 0) return "R";
            if (id.IndexOf("Rental", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("Apartment", StringComparison.OrdinalIgnoreCase) >= 0) return "B";
            return "B";
        }

        static Color GlyphColor(string id)
        {
            int h = 23; foreach (char c in id) h = h * 31 + c;
            float hue = (h & 0x7fffffff) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.55f, 0.85f);
        }

        void OnEnable()
        {
            rows.Clear();
            var loaded = CarSalesSetupWizard.LoadHarbourCitySlots();
            foreach (var site in HarbourPlan.Sites)
            {
                var slot = loaded.FirstOrDefault(l => l.siteId == site.Id) ?? new CarSalesSetupWizard.MapSlot { siteId = site.Id, path = "", yawOffset = 0f, scale = 1f };
                var row = new Row { slot = slot };
                if (!string.IsNullOrEmpty(slot.path)) row.picked = AssetDatabase.LoadAssetAtPath<GameObject>(slot.path);
                rows[site.Id] = row;
            }
        }

        void Apply()
        {
            foreach (var row in rows.Values) row.slot.path = row.picked ? AssetDatabase.GetAssetPath(row.picked) : "";
            CarSalesSetupWizard.SaveHarbourCitySlots(rows.Values.Select(r => r.slot));
            AssetDatabase.Refresh();
            status = "Saved. Rebuild Harbour City to see the models in the scene.";
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Harbour City — per-site models", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Upload one model per site (any prefab or .glb/.fbx). At build time the uploaded model replaces the site's generated building " +
                "— ground, lot lines, fences, signs, lamps and trees are kept. Zones and coordinates come from the Harbour City plan.",
                UnityEditor.MessageType.None);
            EditorGUILayout.Space(4);

            int assigned = rows.Values.Count(r => r.picked);
            EditorGUILayout.LabelField($"{assigned} of {rows.Count} sites have an uploaded model.", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var site in HarbourPlan.Sites)
            {
                var row = rows[site.Id];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.BeginHorizontal();

                    // glyph
                    var glyphStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                    Rect glyphRect = GUILayoutUtility.GetRect(34, 34, GUILayout.Width(34), GUILayout.Height(34));
                    EditorGUI.DrawRect(glyphRect, GlyphColor(site.Id));
                    GUI.Label(glyphRect, Glyph(site.Id), glyphStyle);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(site.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{site.Id}  ·  zone {site.Zone}  ·  plot {(site.FX1 - site.FX0):0}×{(site.FZ1 - site.FZ0):0} m", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    // thumbnail
                    if (row.picked)
                    {
                        var preview = AssetPreview.GetAssetPreview(row.picked);
                        if (preview) GUILayout.Label(preview, GUILayout.Width(56), GUILayout.Height(56));
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    row.picked = (GameObject)EditorGUILayout.ObjectField(row.picked, typeof(GameObject), false, GUILayout.MinWidth(200));
                    if (EditorGUI.EndChangeCheck() && row.picked) row.slot.path = AssetDatabase.GetAssetPath(row.picked);
                    if (GUILayout.Button("Clear", GUILayout.Width(52))) { row.picked = null; row.slot.path = ""; }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Yaw", GUILayout.Width(28));
                    row.slot.yawOffset = EditorGUILayout.Slider(row.slot.yawOffset, -180f, 180f);
                    EditorGUILayout.LabelField("Scale", GUILayout.Width(38));
                    row.slot.scale = EditorGUILayout.Slider(row.slot.scale, 0.5f, 2f);
                    EditorGUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(row.slot.path))
                        EditorGUILayout.LabelField(row.slot.path, EditorStyles.miniLabel);
                }
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, UnityEditor.MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply (save slots file)", GUILayout.Height(28))) Apply();
            if (GUILayout.Button("Apply + Build Harbour City", GUILayout.Height(28)))
            {
                Apply();
                // Same entry point as Tools > Harbour City > Build Harbour City (without the confirm dialog).
                CarSalesSetupWizard.BuildHarbourCity();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }
    }
}
