using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CarSales.Foundation.Graphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 13a: real textures on every surface. Generates tiling PBR texture sets (Foundation SurfaceGen) once into
    /// Assets/CarSales/GeneratedArt/Surfaces, puts them on the town's materials (or your scanned ones from the Surface
    /// Library), and swaps primitive meshes for copies whose UVs are in metres so every texture is shown at its real size.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        public const string SurfaceArtFolder = "Assets/CarSales/GeneratedArt/Surfaces";
        const string MetricMeshPath = Gen + "/Meshes/MetricMeshes.asset";
        /// <summary>Metres covered by the large-scale variation map laid over big ground surfaces (hides tiling).</summary>
        const float MacroMetres = 70f;

        sealed class SurfaceSet { public Texture2D albedo, normal, mask, emission; public Color average = Color.white; }
        static readonly Dictionary<SurfaceId, SurfaceSet> surfaceSets = new Dictionary<SurfaceId, SurfaceSet>();
        /// <summary>Town material name → its surface and wanted colour (filled while the materials are made).</summary>
        static readonly Dictionary<string, (SurfaceId id, Color wanted)> surfaceMaterials = new Dictionary<string, (SurfaceId, Color)>();

        static bool UsesMacroDetail(SurfaceId id) =>
            id == SurfaceId.Grass || id == SurfaceId.Soil || id == SurfaceId.Wheat || id == SurfaceId.Asphalt ||
            id == SurfaceId.Concrete || id == SurfaceId.Paving;

        // ------------------------------------------------------------------ TEXTURES
        /// <summary>Makes sure every surface's textures exist (generated once, then reused; rebuilt when the generator changes).</summary>
        public static void EnsureSurfaceTextures()
        {
            string stampPath = SurfaceArtFolder + "/surfaces.txt";
            string version = "v" + SurfaceGen.Version;
            var averages = new Dictionary<SurfaceId, Color>();
            bool fresh = false;
            if (File.Exists(stampPath))
            {
                var lines = File.ReadAllLines(stampPath);
                fresh = lines.Length > 0 && lines[0] == version;
                if (fresh)
                    foreach (var l in lines.Skip(1))
                    {
                        var p = l.Split(' ');
                        if (p.Length == 4 && System.Enum.TryParse(p[0], out SurfaceId id))
                            averages[id] = new Color(F(p[1]), F(p[2]), F(p[3]));
                    }
            }

            Directory.CreateDirectory(SurfaceArtFolder);
            bool wrote = false;
            foreach (var info in SurfaceGen.All)
            {
                string b = $"{SurfaceArtFolder}/{info.Id}";
                bool have = fresh && averages.ContainsKey(info.Id) && File.Exists(b + "_Albedo.png") && File.Exists(b + "_Normal.png") &&
                            File.Exists(b + "_Mask.png") && (!info.Emission || File.Exists(b + "_Emission.png"));
                if (have) continue;
                EditorUtility.DisplayProgressBar("Car Sales Simulator", "Generating textures: " + info.Name, 0.1f);
                var maps = SurfaceGen.Generate(info.Id);
                WritePng(b + "_Albedo.png", maps.Size, maps.Albedo);
                WritePng(b + "_Normal.png", maps.Size, maps.Normal);
                WritePng(b + "_Mask.png", maps.Size, maps.Mask);
                if (maps.Emission.Length > 0) WritePng(b + "_Emission.png", maps.Size, maps.Emission);
                var a = maps.AverageAlbedo();
                averages[info.Id] = new Color(a.r, a.g, a.b);
                wrote = true;
            }
            if (wrote) EditorUtility.ClearProgressBar();
            if (wrote || !fresh)
            {
                var outLines = new List<string> { version };
                foreach (var kv in averages)
                    outLines.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0000} {2:0.0000} {3:0.0000}", kv.Key, kv.Value.r, kv.Value.g, kv.Value.b));
                File.WriteAllLines(stampPath, outLines);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            surfaceSets.Clear();
            foreach (var info in SurfaceGen.All)
            {
                string b = $"{SurfaceArtFolder}/{info.Id}";
                surfaceSets[info.Id] = new SurfaceSet
                {
                    albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(b + "_Albedo.png"),
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(b + "_Normal.png"),
                    mask = AssetDatabase.LoadAssetAtPath<Texture2D>(b + "_Mask.png"),
                    emission = info.Emission ? AssetDatabase.LoadAssetAtPath<Texture2D>(b + "_Emission.png") : null,
                    average = averages.TryGetValue(info.Id, out var avg) ? avg : Color.white,
                };
            }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        static void WritePng(string path, int size, byte[] rgba)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                t.SetPixelData(rgba, 0);
                t.Apply(false);
                File.WriteAllBytes(path, t.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(t); }
        }

        static SurfaceSet SetFor(SurfaceId id)
        {
            if (!surfaceSets.TryGetValue(id, out var s) || !s.albedo) { EnsureSurfaceTextures(); surfaceSets.TryGetValue(id, out s); }
            return s;
        }

        /// <summary>The generated albedo of a surface (for previews; null until Build Everything has made the textures).</summary>
        public static Texture2D SurfacePreview(SurfaceId id) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{SurfaceArtFolder}/{id}_Albedo.png");

        // ------------------------------------------------------------------ MATERIALS
        /// <summary>Puts a surface on one of the town's materials. <paramref name="wanted"/> = the colour it should read as (tintable surfaces).</summary>
        static Material Surf(Material m, SurfaceId id, Color? wanted = null)
        {
            var w = wanted ?? Color.white;
            surfaceMaterials[m.name] = (id, w);
            if (loadMaterialsOnly && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")) return m;   // already done
            ApplySurface(m, id, w);
            return m;
        }

        /// <summary>Applies the surface's textures (scanned from the Surface Library, else generated) to <paramref name="m"/>.</summary>
        public static void ApplySurface(Material m, SurfaceId id, Color wanted)
        {
            if (!m || !m.HasProperty("_BaseMap")) return;
            var info = SurfaceGen.Info(id);
            var lib = SurfaceLibrary.Load(false);
            var entry = lib ? lib.For(id) : null;
            Texture albedo, normal, mask, occlusion, emission = null;
            float tileU, tileV, smoothness;
            Color tint;
            if (entry != null && entry.scanned)
            {
                var s = entry.scanned;
                albedo = Tex(s, "_BaseMap") ?? Tex(s, "_MainTex");
                normal = Tex(s, "_BumpMap");
                mask = Tex(s, "_MetallicGlossMap");
                occlusion = Tex(s, "_OcclusionMap");
                tileU = tileV = Mathf.Max(0.1f, entry.metres);
                smoothness = s.HasProperty("_Smoothness") ? s.GetFloat("_Smoothness") : 0.4f;
                tint = Color.white;   // a photo already has the real colour
            }
            else
            {
                var set = SetFor(id);
                if (set == null || !set.albedo) return;
                albedo = set.albedo; normal = set.normal; mask = set.mask; occlusion = set.mask; emission = set.emission;
                tileU = info.TileU; tileV = info.TileV; smoothness = info.Smoothness;
                tint = info.Tintable ? TintFor(wanted, set.average) : Color.white;
            }

            var scale = new Vector2(1f / tileU, 1f / tileV);
            m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", scale);
            if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", albedo); m.SetTextureScale("_MainTex", scale); }
            m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            Map(m, "_BumpMap", normal, "_NORMALMAP");
            if (normal && m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
            Map(m, "_MetallicGlossMap", mask, "_METALLICSPECGLOSSMAP");
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_SmoothnessTextureChannel")) m.SetFloat("_SmoothnessTextureChannel", 0f);
            Map(m, "_OcclusionMap", occlusion, "_OCCLUSIONMAP");
            if (occlusion && m.HasProperty("_OcclusionStrength")) m.SetFloat("_OcclusionStrength", 1f);
            if (emission && m.HasProperty("_EmissionMap"))
            {
                // Night-time windows: the Realism Director turns the colour up after dusk (dark by day).
                m.SetTexture("_EmissionMap", emission);
                m.SetColor("_EmissionColor", Color.black);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (UsesMacroDetail(id) && m.HasProperty("_DetailAlbedoMap"))
            {
                // URP applies the detail tiling on top of the base tiling.
                var macro = SetFor(SurfaceId.GrassMacro);
                m.SetTexture("_DetailAlbedoMap", macro?.albedo);
                m.SetTextureScale("_DetailAlbedoMap", Vector2.one * (tileU / MacroMetres));
                if (m.HasProperty("_DetailAlbedoMapScale")) m.SetFloat("_DetailAlbedoMapScale", 1f);
                if (macro?.albedo) m.EnableKeyword("_DETAIL_MULX2"); else m.DisableKeyword("_DETAIL_MULX2");
            }
            EditorUtility.SetDirty(m);
        }

        static Texture Tex(Material m, string p) => m.HasProperty(p) ? m.GetTexture(p) : null;

        static void Map(Material m, string property, Texture t, string keyword)
        {
            if (!m.HasProperty(property)) return;
            m.SetTexture(property, t);
            if (t) m.EnableKeyword(keyword); else m.DisableKeyword(keyword);
        }

        /// <summary>The base colour that makes a neutral texture (average <paramref name="avg"/>) read as <paramref name="wanted"/>.</summary>
        static Color TintFor(Color wanted, Color avg) =>
            new Color(Mathf.Clamp01(wanted.r / Mathf.Max(0.05f, avg.r)), Mathf.Clamp01(wanted.g / Mathf.Max(0.05f, avg.g)),
                      Mathf.Clamp01(wanted.b / Mathf.Max(0.05f, avg.b)), 1f);

        /// <summary>Re-applies every surface (after changing the Surface Library) to the town's saved materials. Returns how many changed.</summary>
        public static int ReapplySurfaces()
        {
            if (!AssetDatabase.IsValidFolder(Gen + "/Materials")) return -1;
            LoadMats();   // fills surfaceMaterials
            EnsureSurfaceTextures();
            int n = 0;
            foreach (var kv in surfaceMaterials)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{Gen}/Materials/{kv.Key}.mat");
                if (!m) continue;
                ApplySurface(m, kv.Value.id, kv.Value.wanted);
                n++;
            }
            AssetDatabase.SaveAssets();
            return n;
        }

        /// <summary>Clear-coat car paint (URP Complex Lit) - the glossy lacquer layer real paint has. Falls back to Lit.</summary>
        static Material ClearCoat(Material m, float coatSmoothness = 0.96f)
        {
            if (loadMaterialsOnly && m.shader && m.shader.name.Contains("Complex")) return m;
            var complex = Shader.Find("Universal Render Pipeline/Complex Lit");
            if (!complex) return m;
            var c = m.GetColor("_BaseColor");
            float smooth = m.GetFloat("_Smoothness"), metal = m.GetFloat("_Metallic");
            m.shader = complex;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_ClearCoatMask")) m.SetFloat("_ClearCoatMask", 1f);
            if (m.HasProperty("_ClearCoatSmoothness")) m.SetFloat("_ClearCoatSmoothness", coatSmoothness);
            m.EnableKeyword("_CLEARCOAT");
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ METRIC UVS
        static bool IsTextured(Material m) => m && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap");

        /// <summary>
        /// Swaps Unity's cube / plane / cylinder on every textured object in <paramref name="scene"/> for a copy whose UVs
        /// are in metres for that object's size. Meshes are shared by size and saved in Generated/Meshes. Returns the count.
        /// </summary>
        static int ApplyMetricUVs(Scene scene)
        {
            // The exact meshes CreatePrimitive uses (whatever this Unity version calls them).
            var cube = BuiltinMesh(PrimitiveType.Cube);
            var plane = BuiltinMesh(PrimitiveType.Plane);
            var cylinder = BuiltinMesh(PrimitiveType.Cylinder);
            EnsureFolder(Gen + "/Meshes");
            var bank = new Dictionary<string, Mesh>();
            foreach (var existing in AssetDatabase.LoadAllAssetsAtPath(MetricMeshPath).OfType<Mesh>())
                if (!bank.ContainsKey(existing.name)) bank[existing.name] = existing;

            int swapped = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var src = mf.sharedMesh;
                    if (!src) continue;
                    string kind = src == cube ? "box" : src == plane ? "plane" : src == cylinder ? "cyl" : null;
                    if (kind == null) continue;
                    var r = mf.GetComponent<MeshRenderer>();
                    if (!r || !r.sharedMaterials.Any(IsTextured)) continue;
                    var s = mf.transform.lossyScale;
                    float sx = Mathf.Abs(s.x), sy = Mathf.Abs(s.y), sz = Mathf.Abs(s.z);
                    if (Mathf.Max(sx, Mathf.Max(sy, sz)) < 0.35f) continue;   // cushions, legs, handles: texture scale doesn't show
                    bool split = kind == "box" && r.sharedMaterials.Length > 1;
                    string key = MetricMesh.Key(kind, sx, sy, sz, split);
                    if (!bank.TryGetValue(key, out var mesh))
                    {
                        var data = kind == "box" ? MetricMesh.Box(sx, sy, sz, split)
                                 : kind == "plane" ? MetricMesh.Plane(sx, sz)
                                 : MetricMesh.Cylinder(sx, sy, sz);
                        mesh = ToMesh(data, key);
                        if (!AssetDatabase.LoadMainAssetAtPath(MetricMeshPath)) AssetDatabase.CreateAsset(mesh, MetricMeshPath);
                        else AssetDatabase.AddObjectToAsset(mesh, MetricMeshPath);
                        bank[key] = mesh;
                    }
                    mf.sharedMesh = mesh;
                    swapped++;
                }
            AssetDatabase.SaveAssets();
            return swapped;
        }

        static Mesh BuiltinMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            try { return tmp.GetComponent<MeshFilter>().sharedMesh; }
            finally { Object.DestroyImmediate(tmp); }
        }

        static Mesh ToMesh(MeshData d, string name)
        {
            int n = d.VertexCount;
            var pos = new Vector3[n]; var nor = new Vector3[n]; var tan = new Vector4[n]; var uv = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                pos[i] = new Vector3(d.Positions[i * 3], d.Positions[i * 3 + 1], d.Positions[i * 3 + 2]);
                nor[i] = new Vector3(d.Normals[i * 3], d.Normals[i * 3 + 1], d.Normals[i * 3 + 2]);
                tan[i] = new Vector4(d.Tangents[i * 4], d.Tangents[i * 4 + 1], d.Tangents[i * 4 + 2], d.Tangents[i * 4 + 3]);
                uv[i] = new Vector2(d.Uvs[i * 2], d.Uvs[i * 2 + 1]);
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = pos;
            mesh.normals = nor;
            mesh.tangents = tan;
            mesh.uv = uv;
            mesh.subMeshCount = d.Submeshes.Count;
            for (int s = 0; s < d.Submeshes.Count; s++) mesh.SetTriangles(d.Submeshes[s], s);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>Import settings for the generated surface textures (normal maps, linear masks, anisotropic filtering, streaming).</summary>
    class SurfaceTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CarSalesSetupWizard.SurfaceArtFolder)) return;
            var ti = (TextureImporter)assetImporter;
            string n = Path.GetFileNameWithoutExtension(assetPath);
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 8;          // roads and floors are seen at a shallow angle
            ti.streamingMipmaps = true;
            if (n.EndsWith("_Normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (n.EndsWith("_Mask"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;   // metallic / occlusion / smoothness are data, not colour
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = false;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.None;
            }
        }
    }
}
