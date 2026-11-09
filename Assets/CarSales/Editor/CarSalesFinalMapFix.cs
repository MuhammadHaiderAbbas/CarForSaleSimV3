using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14 companion tooling for the Final Map GLBs (downloaded from Sketchfab).
    ///
    ///   Tools &gt; Car Sales &gt; Final Map &gt; Audit Final Map
    ///       Loads every GLB the Final Map knows about (via the manifest), force-reimporting the ones Unity failed
    ///       on, and writes final-map-audit.md to the project root with per-asset verdicts: imported OK / FAILED TO
    ///       IMPORT (Unity's asset import worker typically crashed - see the Console and Editor.log), no renderers,
    ///       or materials whose shader could not be found. Read it to know exactly which spots in the scene are
    ///       real models and which are stand-ins or broken, instead of eyeballing.
    ///
    ///   Tools &gt; Car Sales &gt; Final Map &gt; Repair Missing Materials (Open Scene)
    ///       For the open scene(s): every renderer that lost a material slot and every material whose shader
    ///       Unity could not locate (the classic "everything renders flat pink/white after a failed import") gets a
    ///       built-in URP/Lit grey material so the build is presentable while broken GLBs are fixed properly.
    ///       Save the scene afterwards (Ctrl+S).
    ///
    /// Per-asset orientation/scale/skip overrides live in Assets/CarSales/FinalMap/final-map-overrides.json
    /// (format documented in docs/FINAL_MAP.md); the Final Map builder applies them at placement time.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        const string OverridesPath = Root + "/FinalMap/final-map-overrides.json";

        // ------------------------------------------------------------------ OVERRIDES
        sealed class FinalMapOverride
        {
            public string uid, key;
            public float yawOffset, footprintScale = 1f;
            public bool skip;
        }

        static List<FinalMapOverride> overridesCache;

        static List<FinalMapOverride> FinalMapOverrides()
        {
            if (overridesCache != null) return overridesCache;
            overridesCache = new List<FinalMapOverride>();
            if (!File.Exists(OverridesPath)) return overridesCache;
            try
            {
                var root = CarSales.Foundation.MiniJson.Parse(File.ReadAllText(OverridesPath));
                foreach (var item in root["assets"].Items)
                {
                    var o = new FinalMapOverride
                    {
                        uid = item["uid"]?.AsString("") ?? "",
                        key = item["key"]?.AsString("") ?? "",
                        yawOffset = (float)(item["yawOffset"]?.Num ?? 0.0),
                        footprintScale = (float)(item["footprintScale"]?.Num ?? 1.0),
                        skip = item["skip"]?.AsBool(false) ?? false,
                    };
                    if (o.footprintScale <= 0.05f) o.footprintScale = 1f;
                    if (!string.IsNullOrEmpty(o.uid) || !string.IsNullOrEmpty(o.key)) overridesCache.Add(o);
                }
            }
            catch (Exception e) { Debug.LogError("[CarSales] Could not parse " + OverridesPath + ": " + e.Message); }
            return overridesCache;
        }

        static FinalMapOverride FindOverride(FinalMapAsset asset)
        {
            if (asset == null) return null;
            var all = FinalMapOverrides();
            return all.FirstOrDefault(o => !string.IsNullOrEmpty(o.uid) && o.uid == asset.uid)
                ?? all.FirstOrDefault(o => !string.IsNullOrEmpty(o.key) && o.key == asset.key)
                ?? all.FirstOrDefault(o => !string.IsNullOrEmpty(o.key) && o.key.Equals(asset.key, StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------------------------------------------ AUDIT
        [MenuItem("Tools/Car Sales/Final Map/Audit Final Map", priority = 13)]
        static void AuditFinalMapMenu()
        {
            var assets = FinalMapManifest();
            if (assets.Count == 0)
            {
                EditorUtility.DisplayDialog("Car Sales", "The manifest is empty or missing (Assets/CarSales/FinalMap/final-map-assets.json).", "OK");
                return;
            }
            var lines = new List<string>
            {
                "# Final Map asset audit",
                "",
                "Machine-readable verdict for every Sketchfab GLB the Final Map places. Re-run after every fix (Tools > Car Sales > Final Map > Audit Final Map).",
                "",
            };
            int ok = 0, missing = 0, broken = 0, empty = 0;
            for (int i = 0; i < assets.Count; i++)
            {
                var a = assets[i];
                EditorUtility.DisplayProgressBar("Audit Final Map", a.name, (float)i / assets.Count);
                string verdict;
                GameObject prefab = null;
                if (File.Exists(a.importPath))
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(a.importPath);
                    if (!prefab)
                    {
                        // The asset-import worker typically dies on the biggest GLBs; one forced synchronous reimport per asset gives it another chance.
                        try { AssetDatabase.ImportAsset(a.importPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); }
                        catch (Exception e) { Debug.LogWarning($"[CarSales] Reimport of '{a.importPath}' failed: {e.Message}"); }
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(a.importPath);
                    }
                }
                if (!File.Exists(a.importPath)) { missing++; verdict = "MISSING FILE - download it with tools/download_sketchfab_assets.py"; }
                else if (!prefab) { broken++; verdict = "FAILED TO IMPORT - Unity's asset import worker crashed on this GLB (see the Console). Repair steps in docs/FINAL_MAP.md; if it keeps crashing, delete the file so the Final Map uses a stand-in."; }
                else
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    try
                    {
                        var rs = go.GetComponentsInChildren<Renderer>(true);
                        int mats = rs.Sum(r => r.sharedMaterials?.Length ?? 0);
                        int missingMats = rs.Sum(r => (r.sharedMaterials ?? Array.Empty<Material>()).Count(m => m == null));
                        int nullShaders = rs.Sum(r => (r.sharedMaterials ?? Array.Empty<Material>()).Count(m => m != null && (m.shader == null || m.shader.name == "Legacy Shaders/Hidden/InternalErrorShader" || m.shader.name == "Hidden/InternalErrorShader" || m.shader.name == "Unlit/InternalErrorShader")));
                        long tris = go.GetComponentsInChildren<MeshFilter>(true).Sum(mf => mf.sharedMesh ? mf.sharedMesh.triangles.LongLength / 3 : 0);
                        if (rs.Length == 0) { empty++; verdict = "IMPORTED BUT HAS NO RENDERERS - the Final Map will skip it and use a stand-in"; }
                        else if (missingMats > 0 || nullShaders > 0) { ok++; verdict = $"IMPORTED WITH MISSING MATERIALS ({missingMats} empty slot(s), {nullShaders} broken shader(s)) - run 'Repair Missing Materials (Open Scene)' then fix the asset properly (see docs/FINAL_MAP.md). {rs.Length} renderer(s), {tris:N0} tris"; }
                        else { ok++; verdict = $"OK - {rs.Length} renderer(s), {mats} material slot(s), {tris:N0} tris"; }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                lines.Add($"- **{a.name}** ({a.category}/{a.uid}.glb, {a.licence?.ToUpperInvariant()}, {a.author}{(a.needsReview ? ", NEEDS REVIEW" : "")})");
                lines.Add($"  - key `{a.key}` · {verdict}");
            }
            EditorUtility.ClearProgressBar();
            var reportPath = Path.Combine(Application.dataPath, "..", "final-map-audit.md");
            File.WriteAllText(reportPath, string.Join("\n", lines) + "\n", System.Text.Encoding.UTF8);
            Debug.Log($"[CarSales] Final Map audit: {ok} OK, {missing} not downloaded, {broken} failed to import, {empty} with no renderers. Report: {Path.GetFullPath(reportPath)}");
            EditorUtility.DisplayDialog("Final Map Audit",
                $"{ok} OK · {missing} not downloaded · {broken} FAILED to import (import worker crashed) · {empty} with no renderers.\n\nFull report: {Path.GetFullPath(reportPath)}", "OK");
        }

        // ------------------------------------------------------------------ REPAIR
        [MenuItem("Tools/Car Sales/Final Map/Repair Missing Materials (Open Scene)", priority = 14)]
        static void RepairMaterialsMenu()
        {
            var fallback = FallbackMaterial();
            int slotsFixed = 0, scenesFixed = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                int fixedInScene = 0;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var r0 in root.GetComponentsInChildren<Renderer>(true))
                {
                    var shared = r0.sharedMaterials;
                    if (shared == null) continue;
                    bool changed = false;
                    for (int s = 0; s < shared.Length; s++)
                    {
                        var mat = shared[s];
                        bool broken = mat == null || mat.shader == null
                                      || mat.shader.name == "Hidden/InternalErrorShader"
                                      || mat.shader.name == "Unlit/InternalErrorShader"
                                      || mat.shader.name == "Legacy Shaders/Hidden/InternalErrorShader";
                        if (broken) { shared[s] = fallback; changed = true; fixedInScene++; }
                    }
                    if (changed) r0.sharedMaterials = shared;
                }
                if (fixedInScene > 0) { scenesFixed++; EditorSceneManager.MarkSceneDirty(scene); }
                slotsFixed += fixedInScene;
            }
            if (slotsFixed > 0)
                Debug.Log($"[CarSales] Repair Missing Materials: substituted {slotsFixed} broken/empty slot(s) across {scenesFixed} scene(s). Save the scene(s) (Ctrl+S).");
            else
                Debug.Log("[CarSales] Repair Missing Materials: nothing to fix in the open scene(s).");
            EditorUtility.DisplayDialog("Repair Missing Materials",
                slotsFixed > 0 ? $"Fixed {slotsFixed} material slot(s) across {scenesFixed} scene(s).\n\nSave now (Ctrl+S). Then re-run the audit to find the assets that still need proper imports." : "Nothing to fix in the open scene(s).",
                "OK");
        }

        static Material FallbackMaterial()
        {
            const string path = Gen + "/Materials/ImportRecovery.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = "ImportRecovery" };
            m.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.65f));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(m, path);
            AssetDatabase.SaveAssets();
            return m;
        }
    }
}
