using System.Linq;
using UnityEditor;
using UnityEngine;
#if CARSALES_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace CarSales.EditorTools
{
    /// <summary>
    /// Lighting pass (spec Phase 4): the newest sky imported with the Asset Studio (Poly Haven HDRI) becomes the
    /// scene sky, and URP post-processing (ACES tonemapping, bloom, colour grading, vignette, SMAA) is added to
    /// the game, menu and test-track cameras.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        /// <summary>Night and rain grading volumes made by the last SetUpLooks (the Realism Director blends them in).</summary>
        static Behaviour lastNightVolume, lastRainVolume;

        static void SetUpLooks(Camera cam)
        {
            ApplyImportedSky();   // simple procedural sky (no HDRI)
            lastNightVolume = lastRainVolume = null;
            if (cam && cam.farClipPlane < 1200f) cam.farClipPlane = 1200f;   // the haze, not a wall, hides the far end of town
#if CARSALES_URP
            if (!cam) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var dso = new SerializedObject(data);
            var aaq = dso.FindProperty("m_AntialiasingQuality");
            if (aaq != null) aaq.intValue = 2;   // SMAA high
            var shadows = dso.FindProperty("m_RenderShadows");
            if (shadows != null) shadows.boolValue = true;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = PostProfile();
            // Phase 13b: graded looks for night and rain, blended in by the Realism Director.
            lastNightVolume = GradeVolume(go.transform, "PostNight", 1f, NightProfile());
            lastRainVolume = GradeVolume(go.transform, "PostRain", 2f, RainProfile());
#endif
        }

        /// <summary>
        /// Box-projected reflection probe rendered once when the scene starts (so glossy paint and floors reflect
        /// the room they are in rather than the sky).
        /// </summary>
        static ReflectionProbe AddReflectionProbe(Transform parent, string name, Vector3 localCentre, Vector3 size, float intensity = 1f)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = localCentre;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.boxProjection = true;
            probe.size = size;
            probe.resolution = 128;
            probe.intensity = intensity;
            probe.blendDistance = 1f;
            probe.cullingMask = ~((1 << StudioLayer) | (1 << MinimapLayer) | (1 << 5));
            return probe;
        }

        static void ApplyImportedSky()
        {
            // Harbour City: a simple built-in sky only - no HDRI package dependency.
            // Unity's built-in Default-Skybox.mat is the procedural sky (blue atmosphere + sun disc);
            // assigning it explicitly also clears any HDRI material a previous build baked into the scene.
            var sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (sky) RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }

#if CARSALES_URP
        static VolumeProfile PostProfile()
        {
            string path = $"{Gen}/Settings/PostProcessing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing) return existing;
            EnsureFolder(Gen + "/Settings");
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);

            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.65f);
            var color = p.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);   // ACES darkens slightly
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            var vignette = p.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);
            // Phase 13b: a filmic finish - cool shadows, warm highlights, a touch of grain, clean bloom.
            Fx(p, "Bloom", ("highQualityFiltering", true));
            Fx(p, "ShadowsMidtonesHighlights", ("shadows", new Vector4(0.96f, 0.99f, 1.05f, 0f)), ("highlights", new Vector4(1.04f, 1.01f, 0.96f, 0f)));
            Fx(p, "FilmGrain", ("type", 0), ("intensity", 0.1f), ("response", 0.8f));
            Fx(p, "WhiteBalance", ("temperature", 3f));
            foreach (var c in p.components) if (!AssetDatabase.IsSubAsset(c)) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        /// <summary>Night: the eye adapts (brighter exposure), colours cool and desaturate, lights bloom.</summary>
        static VolumeProfile NightProfile() => GradeProfile("PostNight",
            ("ColorAdjustments", new (string, object)[] { ("postExposure", 0.75f), ("saturation", -12f), ("contrast", 14f) }),
            ("WhiteBalance", new (string, object)[] { ("temperature", -16f), ("tint", 4f) }),
            ("Bloom", new (string, object)[] { ("intensity", 1.1f), ("threshold", 0.85f), ("scatter", 0.72f) }),
            ("Vignette", new (string, object)[] { ("intensity", 0.3f) }));

        /// <summary>Rain and grey skies: flatter and less saturated.</summary>
        static VolumeProfile RainProfile() => GradeProfile("PostRain",
            ("ColorAdjustments", new (string, object)[] { ("saturation", -24f), ("contrast", -6f) }),
            ("Vignette", new (string, object)[] { ("intensity", 0.24f) }));

        static VolumeProfile GradeProfile(string name, params (string type, (string, object)[] values)[] parts)
        {
            string path = $"{Gen}/Settings/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing) return existing;
            EnsureFolder(Gen + "/Settings");
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            foreach (var (type, values) in parts) Fx(p, type, values);
            foreach (var c in p.components) if (!AssetDatabase.IsSubAsset(c)) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        static Behaviour GradeVolume(Transform parent, string name, float priority, VolumeProfile profile)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = priority;
            v.weight = 0f;
            v.sharedProfile = profile;
            return v;
        }

        /// <summary>
        /// Adds (or reuses) a URP post effect by name and overrides the given parameters through serialization
        /// (m_Value / m_OverrideState), so a URP version without an effect or a parameter simply skips it.
        /// </summary>
        static void Fx(VolumeProfile p, string typeName, params (string field, object value)[] values)
        {
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEngine.Rendering.Universal." + typeName, false)).FirstOrDefault(t => t != null);
            if (type == null) { Debug.Log($"[CarSales] Post effect {typeName} not in this URP version - skipped."); return; }
            var comp = p.components.FirstOrDefault(c => c && c.GetType() == type) ?? p.Add(type, false);
            if (!comp) return;
            var so = new SerializedObject(comp);
            foreach (var (field, value) in values)
            {
                var prop = so.FindProperty(field);
                var v = prop?.FindPropertyRelative("m_Value");
                if (v == null) continue;
                switch (value)
                {
                    case bool b: v.boolValue = b; break;
                    case int i: if (v.propertyType == SerializedPropertyType.Enum) v.enumValueIndex = i; else v.intValue = i; break;
                    case float f: v.floatValue = f; break;
                    case Color c: v.colorValue = c; break;
                    case Vector4 q: v.vector4Value = q; break;
                    default: continue;
                }
                var o = prop.FindPropertyRelative("m_OverrideState");
                if (o != null) o.boolValue = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
#endif

        /// <summary>Phase 13b: puts the Realism Director in the town (night windows, lamp lights, headlights, night/rain grading).</summary>
        static void AddRealismDirector(Mats m)
        {
            var director = new GameObject("RealismDirector").AddComponent<RealismDirector>();
            director.nightWindows = new[] { m.facadeOffice, m.facadeApartment, m.facadeBrick }.Where(x => x).ToArray();
            director.nightGlow = new[] { m.lampGlow, m.signWhite, m.windowsGlow, m.ledStrip }.Where(x => x).ToArray();
            director.dayWindows = new[] { m.daylight }.Where(x => x).ToArray();
            director.nightVolume = lastNightVolume;
            director.rainVolume = lastRainVolume;
        }
    }
}
