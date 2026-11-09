using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Studio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 12 Release Checklist (Tools > Car Sales > Release Checklist): everything a paid Steam build needs -
    /// animated characters, shippable licences, a decision on real car brands, the game's name and company, the
    /// version, third-party notices. Release builds refuse to start while there are blockers (development builds
    /// only warn).
    /// </summary>
    public class ReleaseChecklistWindow : EditorWindow
    {
        static string AckKey => "CarSales.BrandsAcknowledged." + Application.dataPath.GetHashCode();
        Vector2 scroll;

        [MenuItem("Tools/Car Sales/Release Checklist", priority = 39)]
        public static void Open() => GetWindow<ReleaseChecklistWindow>("Release Checklist").minSize = new Vector2(640f, 460f);

        public static List<string> BrandedCars()
        {
            var list = new List<string>();
            var db = CarDatabase.LoadDefault();
            if (db) foreach (var c in db.cars.Where(c => c))
                    if (TrademarkCheck.Brands(c.brand + " " + c.displayName).Count > 0) list.Add($"{c.brand} {c.displayName}".Trim());
            foreach (var guid in AssetDatabase.FindAssets("t:CarDefinition", new[] { "Assets/CarSales/Data/Cars" }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CarDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (c && TrademarkCheck.Brands(c.brand + " " + c.displayName).Count > 0) { string n = $"{c.brand} {c.displayName}".Trim(); if (!list.Contains(n)) list.Add(n); }
            }
            var reg = StudioPaths.Registry();
            foreach (var r in reg.records.Where(r => r != null && r.category == AssetCategory.Vehicle))
                if (TrademarkCheck.Brands(r.displayName).Count > 0 && !list.Contains(r.displayName)) list.Add(r.displayName);
            return list;
        }

        public static int CarModels()
        {
            var db = CarDatabase.LoadDefault();
            int n = db ? db.cars.Count(c => c) : 0;
            return n + AssetDatabase.FindAssets("t:CarDefinition", new[] { "Assets/CarSales/Data/Cars" }).Length;
        }

        public static List<ReleaseCheck> Gather()
        {
            var lib = CharacterBuilder.LoadLibrary();
            int missing = lib ? CharacterSetup.MissingRequired(a => lib.Has(a)).Count : CharacterSetup.MissingRequired(a => false).Count;
            int blockers = StudioPaths.Registry().ReleaseBlockers().Count();
            return ReleaseRules.Evaluate(lib && lib.player, lib ? lib.variants.Count(v => v.prefab) : 0, missing, blockers, BrandedCars(), EditorPrefs.GetBool(AckKey, false),
                                         PlayerSettings.companyName, PlayerSettings.productName, PlayerSettings.bundleVersion, File.Exists("THIRD_PARTY_NOTICES.txt"), CarModels());
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var checks = Gather();
            bool ok = ReleaseRules.CanRelease(checks);
            EditorGUILayout.HelpBox(ok ? "Ready for a release build (Tools > Car Sales > Build > Windows (Release))." : "Not ready: fix the red items. Development builds still work.",
                                    ok ? UnityEditor.MessageType.Info : UnityEditor.MessageType.Error);
            foreach (var c in checks)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var col = c.Level == CheckLevel.Pass ? new Color(0.35f, 0.8f, 0.4f) : c.Level == CheckLevel.Warning ? new Color(1f, 0.75f, 0.25f) : new Color(1f, 0.4f, 0.35f);
                    var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = col } };
                    GUILayout.Label(c.Level == CheckLevel.Pass ? "OK" : c.Level == CheckLevel.Warning ? "!" : "X", style, GUILayout.Width(24));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(c.Title, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(c.Detail, EditorStyles.wordWrappedLabel);
                    }
                    if (c.Level != CheckLevel.Pass)
                    {
                        if (c.Title == "Characters" && GUILayout.Button("Fix", GUILayout.Width(60))) CharacterSetupWindow.Open();
                        if (c.Title == "Game name & company" || c.Title == "Version") if (GUILayout.Button("Fix", GUILayout.Width(60))) SettingsService.OpenProjectSettings("Project/Player");
                        if (c.Title == "Asset licences" && GUILayout.Button("Fix", GUILayout.Width(60))) EditorApplication.ExecuteMenuItem("Tools/Car Sales/Asset Studio");
                    }
                }
            }
            EditorGUILayout.Space();
            bool ack = EditorPrefs.GetBool(AckKey, false);
            bool newAck = EditorGUILayout.ToggleLeft("I have renamed the real-brand cars and removed their badges, or I have the makers' licences.", ack);
            if (newAck != ack) EditorPrefs.SetBool(AckKey, newAck);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Release builds stop here while there are blockers.</summary>
        class Gate : IPreprocessBuildWithReport
        {
            public int callbackOrder => 1;
            public void OnPreprocessBuild(BuildReport report)
            {
                bool development = (report.summary.options & BuildOptions.Development) != 0;
                var checks = Gather();
                var problems = checks.Where(c => c.Level == CheckLevel.Blocker).ToList();
                foreach (var c in checks.Where(c => c.Level == CheckLevel.Warning)) Debug.LogWarning($"[Release Checklist] {c.Title}: {c.Detail}");
                if (problems.Count == 0) return;
                string text = string.Join("\n", problems.Select(c => $" - {c.Title}: {c.Detail}"));
                if (development) { Debug.LogWarning("[Release Checklist] Development build with release blockers:\n" + text); return; }
                throw new BuildFailedException("Not ready for a release build (Tools > Car Sales > Release Checklist):\n" + text);
            }
        }
    }
}
