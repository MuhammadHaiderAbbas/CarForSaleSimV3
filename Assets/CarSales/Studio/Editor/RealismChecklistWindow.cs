using System.Linq;
using CarSales.EditorTools;
using CarSales.Foundation.Graphics;
using UnityEditor;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 13d: Tools > Car Sales > Realism > Realism Checklist. Every step from the generated town to a realistic one,
    /// with live progress from your project and a button that opens the right tool. docs/REALISM_GUIDE.md has the detail.
    /// </summary>
    public class RealismChecklistWindow : EditorWindow
    {
        Vector2 scroll;

        static readonly SurfaceId[] Key =
        {
            SurfaceId.Asphalt, SurfaceId.Paving, SurfaceId.Concrete, SurfaceId.Brick, SurfaceId.Render, SurfaceId.Grass, SurfaceId.FloorTile, SurfaceId.Wood,
        };

        [MenuItem("Tools/Car Sales/Realism/Realism Checklist", priority = 34)]
        public static void Open() => GetWindow<RealismChecklistWindow>("Realism Checklist").minSize = new Vector2(560f, 520f);

        static RealismInputs Gather()
        {
            var reg = AssetRegistry.Load();
            var records = reg ? reg.records.Where(r => r != null).ToList() : new System.Collections.Generic.List<AssetRecord>();
            var lib = SurfaceLibrary.Load(false);
            var placeholders = PlaceholderReplacer.LoadSettings(false);
            int Kinds(int from, int to) => placeholders
                ? placeholders.entries.Count(e => e != null && (int)e.kind >= from && (int)e.kind <= to && e.assetIds.Count > 0) : 0;
            var chars = AssetDatabase.LoadAssetAtPath<CharacterSetupData>(CharacterSetupData.AssetPath);
            int missing = chars ? CharacterSetup.MissingRequired(a => chars.Clip(a) != null).Count : 99;
            var audio = AudioLibrary.Load();
            return new RealismInputs
            {
                GraphicsApplied = RealisticGraphics.IsApplied(),
                Skies = records.Count(r => r.category == AssetCategory.Sky),
                ImportedCars = records.Count(r => r.category == AssetCategory.Vehicle && r.prefab),
                ScannedKeySurfaces = lib ? Key.Count(k => lib.For(k)?.scanned) : 0,
                MainCharacter = chars && chars.mainCharacter,
                MissingAnimations = missing,
                TownKindsReplaced = Kinds(0, 3),
                FurnitureKindsReplaced = Kinds(4, 12),
                AmbienceClips = audio ? new[] { audio.street, audio.showroom, audio.workshop, audio.auctionHall, audio.rain }.Count(c => c) : 0,
                Music = audio && audio.music,
                LicenceBlockers = reg ? reg.ReleaseBlockers().Count() : 0,
            };
        }

        void OnGUI()
        {
            var items = RealismChecklist.Evaluate(Gather());
            int score = RealismChecklist.Score(items);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Realism: {score}%", new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 });
            var bar = GUILayoutUtility.GetRect(100f, 14f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, score / 100f, "");
            var next = RealismChecklist.NextStep(items);
            EditorGUILayout.HelpBox(next == null ? "Everything is done. Play-test at every graphics level, then run the Release Checklist."
                                                 : "Next: " + next.Title + "\n" + next.Next, UnityEditor.MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var it in items)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField((it.Done ? "Done:  " : "To do:  ") + it.Title, EditorStyles.boldLabel);
                        if (!it.Done && GUILayout.Button("Open", GUILayout.Width(60f))) OpenTool(it.Tool);
                    }
                    var r = GUILayoutUtility.GetRect(100f, 6f, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(r, it.Progress, "");
                    EditorGUILayout.LabelField(it.Why, EditorStyles.wordWrappedMiniLabel);
                    if (!it.Done) EditorGUILayout.LabelField(it.Next, EditorStyles.wordWrappedLabel);
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Open the full guide (docs/REALISM_GUIDE.md)"))
            {
                string local = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "docs", "REALISM_GUIDE.md"));
                if (System.IO.File.Exists(local)) EditorUtility.OpenWithDefaultApp(local);
                else Application.OpenURL("https://github.com/MuhammadHaiderAbbas/carforsalesim/blob/main/docs/REALISM_GUIDE.md");
            }
        }

        void OnInspectorUpdate() => Repaint();

        static void OpenTool(string tool)
        {
            switch (tool)
            {
                case "graphics": EditorApplication.ExecuteMenuItem("Tools/Car Sales/Realism/Apply Realistic Graphics"); break;
                case "surfaces": SurfaceLibraryWindow.Open(); break;
                case "characters": EditorApplication.ExecuteMenuItem("Tools/Car Sales/Character Setup"); break;
                case "release": EditorApplication.ExecuteMenuItem("Tools/Car Sales/Release Checklist"); break;
                case "audio":
                    var audio = AudioLibrary.Load();
                    if (audio) { Selection.activeObject = audio; EditorGUIUtility.PingObject(audio); }
                    else EditorUtility.DisplayDialog("Sound", "No Audio Library yet. Run Tools > Car Sales > 2. Build Everything first.", "OK");
                    break;
                default: EditorApplication.ExecuteMenuItem("Tools/Car Sales/Asset Studio"); break;
            }
        }
    }
}
