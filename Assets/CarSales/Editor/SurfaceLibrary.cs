using System;
using System.Collections.Generic;
using System.IO;
using CarSales.Foundation.Graphics;
using UnityEditor;
using UnityEngine;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 13a: your choice of scanned material per town surface (e.g. a Poly Haven asphalt downloaded with the Asset
    /// Studio). Empty = the built-in generated texture. Stored in Assets/CarSales/Data/SurfaceLibrary.asset and applied by
    /// Build Everything, or straight away with Tools > Car Sales > Realism > Surface Library > Apply.
    /// </summary>
    public class SurfaceLibrary : ScriptableObject
    {
        public const string AssetPath = "Assets/CarSales/Data/SurfaceLibrary.asset";

        [Serializable]
        public class Entry
        {
            public SurfaceId surface;
            [Tooltip("A material with real textures (Asset Studio > Online > Poly Haven textures, or any URP Lit material). Empty = generated.")]
            public Material scanned;
            [Tooltip("How many metres one copy of the texture covers. Poly Haven lists this on each texture's page (most are 1-4 m).")]
            public float metres = 2f;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry For(SurfaceId id)
        {
            foreach (var e in entries) if (e != null && e.surface == id) return e;
            return null;
        }

        public Entry GetOrAdd(SurfaceId id)
        {
            var e = For(id);
            if (e != null) return e;
            e = new Entry { surface = id, metres = Math.Max(1f, SurfaceGen.Info(id).TileU) };
            entries.Add(e);
            return e;
        }

        public static SurfaceLibrary Load(bool create)
        {
            var lib = AssetDatabase.LoadAssetAtPath<SurfaceLibrary>(AssetPath);
            if (lib || !create) return lib;
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            if (!AssetDatabase.IsValidFolder("Assets/CarSales/Data")) AssetDatabase.CreateFolder("Assets/CarSales", "Data");
            lib = CreateInstance<SurfaceLibrary>();
            AssetDatabase.CreateAsset(lib, AssetPath);
            AssetDatabase.SaveAssets();
            return lib;
        }

        /// <summary>How many surfaces use a scanned material.</summary>
        public int ScannedCount()
        {
            int n = 0;
            foreach (var e in entries) if (e != null && e.scanned) n++;
            return n;
        }
    }
}
