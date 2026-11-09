using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 13b: four graphics tiers (Low / Medium / High / Ultra) for a PC release. Copies your project's URP asset
    /// and renderer once per tier into Assets/CarSales/Data/Rendering, tunes them (shadows, soft shadows, ambient
    /// occlusion, many lights with Forward+, reflection probe blending, HDR colour grading, texture streaming) and points
    /// the Quality levels at them. Your original assets are never edited; Restore puts the old settings back.
    /// Everything is set by field name, so a URP version that lacks a field just skips it.
    /// </summary>
    public static class RealisticGraphics
    {
        public const string Folder = "Assets/CarSales/Data/Rendering";
        const string BackupPath = Folder + "/original-graphics.json";
        public static readonly string[] Tiers = { "Low", "Medium", "High", "Ultra" };
        const int DefaultTier = 2;

        static string TierPath(int t) => $"{Folder}/CarSales_{Tiers[t]}.asset";
        static string RendererPath(int t) => $"{Folder}/CarSales_{Tiers[t]}_Renderer.asset";

        [Serializable] class Backup { public string pipeline = ""; public int current; public List<Level> levels = new List<Level>(); }
        [Serializable] class Level { public string name = ""; public string pipeline = ""; public float lodBias = 1f; public int aniso = 1; public int vSync = 1; }

        /// <summary>True until the first Apply (Build Everything applies once; after a Restore it leaves your settings alone).</summary>
        public static bool NeverApplied => !File.Exists(BackupPath);

        /// <summary>True when the four tiers exist and the Quality levels use them.</summary>
        public static bool IsApplied() =>
            File.Exists(TierPath(DefaultTier)) && QualitySettings.names.SequenceEqual(Tiers);

        [MenuItem("Tools/Car Sales/Realism/Apply Realistic Graphics", priority = 35)]
        static void ApplyMenu()
        {
            if (!EditorUtility.DisplayDialog("Realistic graphics",
                    "This makes four graphics levels (Low, Medium, High, Ultra) from copies of your URP settings:\n\n" +
                    "• sharper, softer and longer shadows\n• ambient occlusion (contact shadows in corners)\n" +
                    "• Forward+ so every street lamp and headlight can light the scene\n• HDR colour grading, texture streaming\n\n" +
                    "Your original URP asset is not changed, and Tools > Car Sales > Realism > Restore Original Graphics undoes it.",
                    "Apply", "Cancel")) return;
            EditorUtility.DisplayDialog("Realistic graphics", Apply(), "OK");
        }

        [MenuItem("Tools/Car Sales/Realism/Restore Original Graphics", priority = 37)]
        static void RestoreMenu() => EditorUtility.DisplayDialog("Realistic graphics", Restore(), "OK");

        // ------------------------------------------------------------------ APPLY
        public static string Apply()
        {
            var source = GraphicsSettings.defaultRenderPipeline;
            if (!source) source = QualitySettings.renderPipeline;
            if (!source || !source.GetType().Name.Contains("Universal"))
                return "No URP asset is set in Project Settings > Graphics. Install the Universal Render Pipeline (the Car Sales installer does this) and try again.";
            Directory.CreateDirectory(Folder);
            if (!File.Exists(BackupPath)) SaveBackup();

            string sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceRenderer = DefaultRenderer(source);
            string sourceRendererPath = sourceRenderer ? AssetDatabase.GetAssetPath(sourceRenderer) : null;
            var report = new List<string>();
            var assets = new RenderPipelineAsset[4];
            for (int t = 0; t < 4; t++)
            {
                if (!File.Exists(TierPath(t)) && !AssetDatabase.CopyAsset(sourcePath, TierPath(t)))
                    return "Could not copy " + sourcePath + ".";
                if (sourceRendererPath != null && !File.Exists(RendererPath(t)) && !AssetDatabase.CopyAsset(sourceRendererPath, RendererPath(t)))
                    return "Could not copy " + sourceRendererPath + ".";
                AssetDatabase.ImportAsset(TierPath(t));
                var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(TierPath(t));
                var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath(t));
                if (!asset) return "Copy of the URP asset could not be loaded.";
                if (renderer) LinkRenderer(asset, renderer);
                ConfigurePipeline(asset, t, report);
                if (renderer) ConfigureRenderer(renderer, t, report);
                assets[t] = asset;
            }
            SetQualityLevels(assets, report);
            GraphicsSettings.defaultRenderPipeline = assets[DefaultTier];
            QualitySettings.SetQualityLevel(DefaultTier, true);
            if (PlayerSettings.colorSpace != ColorSpace.Linear) { PlayerSettings.colorSpace = ColorSpace.Linear; report.Add("Colour space set to Linear."); }
            AssetDatabase.SaveAssets();
            var skipped = report.Where(r => r.StartsWith("skipped")).Distinct().ToList();
            return "Graphics levels Low / Medium / High / Ultra are ready (High is the default).\n\n" +
                   string.Join("\n", report.Where(r => !r.StartsWith("skipped")).Distinct()) +
                   (skipped.Count > 0 ? $"\n\n{skipped.Count} setting(s) do not exist in your URP version and were left alone." : "");
        }

        static ScriptableObject DefaultRenderer(RenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) return null;
            var idx = so.FindProperty("m_DefaultRendererIndex");
            int i = idx != null ? Mathf.Clamp(idx.intValue, 0, list.arraySize - 1) : 0;
            return list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableObject;
        }

        static void LinkRenderer(RenderPipelineAsset asset, ScriptableObject renderer)
        {
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null) return;
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            var idx = so.FindProperty("m_DefaultRendererIndex");
            if (idx != null) idx.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigurePipeline(RenderPipelineAsset asset, int t, List<string> report)
        {
            var so = new SerializedObject(asset);
            void S(string name, object v) => Set(so, name, v, report);
            S("m_SupportsHDR", true);
            S("m_MSAA", new[] { 1, 2, 4, 4 }[t]);
            S("m_RenderScale", new[] { 0.85f, 1f, 1f, 1f }[t]);
            S("m_MainLightRenderingMode", 1);                                  // per pixel
            S("m_MainLightShadowsSupported", true);
            S("m_MainLightShadowmapResolution", new[] { 1024, 2048, 4096, 4096 }[t]);
            S("m_AdditionalLightsRenderingMode", 1);                           // per pixel
            S("m_AdditionalLightsPerObjectLimit", new[] { 4, 8, 8, 8 }[t]);
            S("m_AdditionalLightShadowsSupported", t >= 2);
            S("m_AdditionalLightsShadowmapResolution", new[] { 512, 1024, 2048, 4096 }[t]);
            S("m_ShadowDistance", new[] { 60f, 110f, 170f, 260f }[t]);
            S("m_ShadowCascadeCount", new[] { 2, 3, 4, 4 }[t]);
            S("m_SoftShadowsSupported", t >= 1);
            S("m_SoftShadowQuality", new[] { 1, 1, 2, 3 }[t]);                 // low / low / medium / high
            S("m_ReflectionProbeBlending", t >= 1);
            S("m_ReflectionProbeBoxProjection", true);
            S("m_RequireDepthTexture", true);
            S("m_UseSRPBatcher", true);
            S("m_ColorGradingMode", t >= 2 ? 1 : 0);                           // HDR grading on High+
            S("m_ColorGradingLutSize", t >= 2 ? 32 : 16);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            report.Add($"{Tiers[t]}: shadows {new[] { 60, 110, 170, 260 }[t]} m, {new[] { 2, 3, 4, 4 }[t]} cascades, MSAA x{new[] { 1, 2, 4, 4 }[t]}.");
        }

        static void ConfigureRenderer(ScriptableObject renderer, int t, List<string> report)
        {
            var so = new SerializedObject(renderer);
            Set(so, "m_RenderingMode", 2, report);   // Forward+: no per-object light limit (street lamps, headlights)
            so.ApplyModifiedPropertiesWithoutUndo();
            var ssao = EnsureFeature(renderer, "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion", "ScreenSpaceAmbientOcclusion", report);
            if (ssao)
            {
                var fso = new SerializedObject(ssao);
                Set(fso, "m_Active", t >= 1, report);
                Set(fso, "m_Settings.Downsample", t <= 1, report);
                fso.ApplyModifiedPropertiesWithoutUndo();
                if (t >= 1) report.Add($"{Tiers[t]}: ambient occlusion on.");
            }
            EditorUtility.SetDirty(renderer);
        }

        /// <summary>Adds a renderer feature (by type name) the way URP's own inspector does, unless one is already there.</summary>
        static ScriptableObject EnsureFeature(ScriptableObject renderer, string typeName, string displayName, List<string> report)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName, false)).FirstOrDefault(x => x != null);
            if (type == null) { report.Add("skipped " + displayName); return null; }
            var so = new SerializedObject(renderer);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            if (list == null || map == null) { report.Add("skipped renderer features"); return null; }
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableObject existing && existing.GetType() == type) return existing;

            var feature = ScriptableObject.CreateInstance(type);
            feature.name = displayName;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            so.Update();
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            map.InsertArrayElementAtIndex(map.arraySize);
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
            return feature;
        }

        static void SetQualityLevels(RenderPipelineAsset[] assets, List<string> report)
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (!qs) { report.Add("skipped quality levels"); return; }
            var so = new SerializedObject(qs);
            var levels = so.FindProperty("m_QualitySettings");
            if (levels == null) { report.Add("skipped quality levels"); return; }
            if (levels.arraySize == 0) levels.InsertArrayElementAtIndex(0);
            while (levels.arraySize < 4) levels.InsertArrayElementAtIndex(levels.arraySize - 1);   // copies the highest level
            while (levels.arraySize > 4) levels.DeleteArrayElementAtIndex(levels.arraySize - 1);
            for (int t = 0; t < 4; t++)
            {
                var e = levels.GetArrayElementAtIndex(t);
                void S(string name, object v) => SetRel(e, name, v, report);
                S("name", Tiers[t]);
                var rp = e.FindPropertyRelative("customRenderPipeline");
                if (rp != null) rp.objectReferenceValue = assets[t];
                S("lodBias", new[] { 0.8f, 1.2f, 1.8f, 2.5f }[t]);
                S("maximumLODLevel", 0);
                S("anisotropicTextures", t >= 2 ? 2 : 1);                    // forced on High+ (roads at shallow angles)
                S("globalTextureMipmapLimit", t == 0 ? 1 : 0);
                S("vSyncCount", 1);
                S("realtimeReflectionProbes", true);
                S("softParticles", t >= 2);
                S("billboardsFaceCameraPosition", true);
                S("skinWeights", t == 0 ? 2 : 4);
                S("enableLODCrossFade", true);
                S("streamingMipmapsActive", true);
                S("streamingMipmapsMemoryBudget", new[] { 384f, 768f, 1536f, 3072f }[t]);
                var excluded = e.FindPropertyRelative("excludedTargetPlatforms");
                if (excluded != null && excluded.isArray) excluded.arraySize = 0;
            }
            var current = so.FindProperty("m_CurrentQuality");
            if (current != null) current.intValue = DefaultTier;
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform != null && perPlatform.isArray)
                for (int i = 0; i < perPlatform.arraySize; i++)
                {
                    var second = perPlatform.GetArrayElementAtIndex(i).FindPropertyRelative("second");
                    if (second != null) second.intValue = DefaultTier;
                }
            so.ApplyModifiedPropertiesWithoutUndo();
            report.Add("Quality levels: Low, Medium, High (default), Ultra - players choose in Settings > Graphics.");
        }

        // ------------------------------------------------------------------ BACKUP / RESTORE
        static void SaveBackup()
        {
            var b = new Backup { pipeline = Guid(GraphicsSettings.defaultRenderPipeline), current = QualitySettings.GetQualityLevel() };
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            var levels = qs ? new SerializedObject(qs).FindProperty("m_QualitySettings") : null;
            if (levels != null)
                for (int i = 0; i < levels.arraySize; i++)
                {
                    var e = levels.GetArrayElementAtIndex(i);
                    b.levels.Add(new Level
                    {
                        name = e.FindPropertyRelative("name")?.stringValue ?? "Level " + i,
                        pipeline = Guid(e.FindPropertyRelative("customRenderPipeline")?.objectReferenceValue),
                        lodBias = e.FindPropertyRelative("lodBias")?.floatValue ?? 1f,
                        aniso = e.FindPropertyRelative("anisotropicTextures")?.intValue ?? 1,
                        vSync = e.FindPropertyRelative("vSyncCount")?.intValue ?? 1,
                    });
                }
            File.WriteAllText(BackupPath, JsonUtility.ToJson(b, true));
        }

        public static string Restore()
        {
            if (!File.Exists(BackupPath)) return "Nothing to restore: realistic graphics were never applied here.";
            var b = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qs && b.levels.Count > 0)
            {
                var so = new SerializedObject(qs);
                var levels = so.FindProperty("m_QualitySettings");
                while (levels.arraySize < b.levels.Count) levels.InsertArrayElementAtIndex(levels.arraySize - 1);
                while (levels.arraySize > b.levels.Count) levels.DeleteArrayElementAtIndex(levels.arraySize - 1);
                var none = new List<string>();
                for (int i = 0; i < b.levels.Count; i++)
                {
                    var e = levels.GetArrayElementAtIndex(i);
                    var l = b.levels[i];
                    SetRel(e, "name", l.name, none);
                    var rp = e.FindPropertyRelative("customRenderPipeline");
                    if (rp != null) rp.objectReferenceValue = Load(l.pipeline);
                    SetRel(e, "lodBias", l.lodBias, none);
                    SetRel(e, "anisotropicTextures", l.aniso, none);
                    SetRel(e, "vSyncCount", l.vSync, none);
                }
                var current = so.FindProperty("m_CurrentQuality");
                if (current != null) current.intValue = Mathf.Clamp(b.current, 0, b.levels.Count - 1);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GraphicsSettings.defaultRenderPipeline = Load(b.pipeline) as RenderPipelineAsset;
            AssetDatabase.SaveAssets();
            return "Your original graphics settings are back. The Car Sales copies stay in " + Folder + " (delete them if you like).";
        }

        static string Guid(Object o) => o ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)) : "";
        static Object Load(string guid) => string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));

        // ------------------------------------------------------------------ SERIALIZED HELPERS
        static void Set(SerializedObject so, string path, object value, List<string> report)
        {
            var p = so.FindProperty(path);
            if (p == null) { report.Add("skipped " + path); return; }
            Assign(p, value, report);
        }

        static void SetRel(SerializedProperty parent, string path, object value, List<string> report)
        {
            var p = parent.FindPropertyRelative(path);
            if (p == null) { report.Add("skipped " + path); return; }
            Assign(p, value, report);
        }

        static void Assign(SerializedProperty p, object value, List<string> report)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Boolean: p.boolValue = Convert.ToBoolean(value); break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask: p.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.Float: p.floatValue = Convert.ToSingle(value); break;
                case SerializedPropertyType.String: p.stringValue = Convert.ToString(value); break;
                default: report.Add("skipped " + p.propertyPath); break;
            }
        }
    }
}
