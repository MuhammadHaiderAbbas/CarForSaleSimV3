using System.Linq;
using CarSales.Foundation.Graphics;
using UnityEditor;
using UnityEngine;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Tools > Car Sales > Realism > Surface Library: every town surface with its generated texture, and a slot for a
    /// scanned material (Poly Haven / ambientCG via the Asset Studio) to use instead. Apply updates the town at once.
    /// </summary>
    public class SurfaceLibraryWindow : EditorWindow
    {
        Vector2 scroll;
        string status = "";

        [MenuItem("Tools/Car Sales/Realism/Surface Library", priority = 36)]
        public static void Open() => GetWindow<SurfaceLibraryWindow>("Surface Library").minSize = new Vector2(560f, 420f);

        void OnGUI()
        {
            var lib = SurfaceLibrary.Load(true);
            EditorGUILayout.HelpBox(
                "Every road, pavement, wall, roof and floor uses one of these surfaces. The game ships with generated textures; " +
                "for photo-real results download scanned ones (Asset Studio > Online > Poly Haven > Textures, all CC0) and drop the " +
                "material here. Set 'metres' to the real size of one copy of the texture (shown on its Poly Haven page).", UnityEditor.MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var info in SurfaceGen.All)
            {
                if (info.Id == SurfaceId.GrassMacro) continue;   // internal: large-scale variation
                var e = lib.For(info.Id);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var preview = CarSalesSetupWizard.SurfacePreview(info.Id);
                    var rect = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f));
                    if (preview) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(info.Name, EditorStyles.boldLabel);
                        EditorGUI.BeginChangeCheck();
                        var mat = (Material)EditorGUILayout.ObjectField("Scanned material", e?.scanned, typeof(Material), false);
                        float metres = EditorGUILayout.FloatField("Metres per copy", e != null ? e.metres : Mathf.Max(1f, info.TileU));
                        if (EditorGUI.EndChangeCheck())
                        {
                            var entry = lib.GetOrAdd(info.Id);
                            entry.scanned = mat;
                            entry.metres = Mathf.Max(0.1f, metres);
                            EditorUtility.SetDirty(lib);
                        }
                        EditorGUILayout.LabelField(e != null && e.scanned ? "Using your scanned material" : $"Generated ({info.TileU:0.#} x {info.TileV:0.#} m)", EditorStyles.miniLabel);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply to the town now", GUILayout.Height(28f)))
                {
                    status = "Applying...";
                    EditorApplication.delayCall += () =>   // outside OnGUI: it may import textures
                    {
                        AssetDatabase.SaveAssets();
                        int n = CarSalesSetupWizard.ReapplySurfaces();
                        status = n < 0 ? "Run Tools > Car Sales > 2. Build Everything first." : $"{n} materials updated. Press Play to see them.";
                        Repaint();
                    };
                }
                if (GUILayout.Button("Download scanned textures...", GUILayout.Height(28f)))
                    EditorApplication.ExecuteMenuItem("Tools/Car Sales/Asset Studio");
            }
            if (status.Length > 0) EditorGUILayout.HelpBox(status, UnityEditor.MessageType.None);
            int scanned = lib.ScannedCount(), total = SurfaceGen.All.Count(i => i.Id != SurfaceId.GrassMacro);
            EditorGUILayout.LabelField($"{scanned} of {total} surfaces use scanned textures.", EditorStyles.miniLabel);
        }
    }
}
