using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CarSales.Studio;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Release Checklist &gt; Car brands &gt; Rename: rewrites the displayName/brand of every car that uses a real
    /// maker's name to a fictional make, so the game can legally be sold. The 3D model itself is untouched — you
    /// must still remove or paint over the maker's badges/logos on the car textures (or get the maker's licence).
    /// Ticks nothing and grants nothing: review the Console list afterwards.
    /// </summary>
    public static class RenameTrademarkCars
    {
        // Real word -> fictional replacement. Applied whole-word, case-insensitive.
        internal static readonly (string real, string fictional)[] Map =
        {
            ("mercedes-benz", "Meridian"), ("mercedes", "Meridian"), ("mersedes", "Meridian"),
            ("benz", "Meridian"), ("amg", "Mistral"), ("brabus", "Mistral"), ("maybach", "Meridian"),
            ("dodge", "Redbrook"), ("jeep", "Redbrook"), ("chrysler", "Redbrook"), ("ram", "Redbrook"),
            ("bmw", "Altius"), ("alpina", "Altius"),
            ("audi", "Velder"), ("volkswagen", "Velder"), ("vw", "Velder"), ("porsche", "Surridge"),
            ("ford", "Kingsley"), ("vauxhall", "Kingsley"), ("opel", "Kingsley"),
            ("ferrari", "Savoy"), ("lamborghini", "Savoy"), ("maserati", "Savoy"), ("bugatti", "Savoy"),
            ("mclaren", "Savoy"), ("rolls", "Kingswood"), ("bentley", "Kingswood"),
            ("aston martin", "Kingswood"), ("jaguar", "Harlan"), ("land rover", "Harlan"), ("range rover", "Harlan"),
            ("chevrolet", "Calloway"), ("chevy", "Calloway"), ("cadillac", "Calloway"), ("gmc", "Calloway"),
            ("toyota", "Nakata"), ("lexus", "Nakata"), ("honda", "Kiryu"), ("nissan", "Kiryu"),
            ("kia", "Sura"), ("hyundai", "Sura"), ("mazda", "Sura"), ("subaru", "Sura"),
            ("volvo", "Sten"), ("renault", "Velcourt"), ("peugeot", "Velcourt"),
        };

        internal static string CleanName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            string s = Regex.Replace(raw, @"(?i)www\.[a-z0-9\-.]+", " ");
            s = s.Replace("_", " ").Replace("-", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            foreach (var (real, fictional) in Map)
                s = Regex.Replace(s, $@"(?i)\b{Regex.Escape(real)}\b", fictional);
            s = Regex.Replace(s, @"\s+", " ").Trim(' ', '-');
            // Title-case words that are all lower case (leave GT, AMG-less acronyms as typed).
            var words = s.Split(' ').Select(w => w.Length > 0 && w.All(char.IsLower) ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w);
            s = string.Join(" ", words);
            s = Regex.Replace(s, @"(?i)\bimported\b\s*\bimported\b", "Imported");
            return s;
        }

        [MenuItem("Tools/Car Sales/Release Checklist/Rename Trademarked Cars", priority = 41)]
        public static void Run()
        {
            var renamed = new List<string>();

            // 1) The loaded car database (the cars actually in the game).
            var db = CarDatabase.LoadDefault();
            if (db && db.cars != null)
                foreach (var c in db.cars)
                {
                    if (!c) continue;
                    string b = TrademarkCheck.Brands(c.brand).Count > 0 ? CleanName(c.brand) : c.brand;
                    string d = TrademarkCheck.Brands(c.displayName).Count > 0 ? CleanName(c.displayName) : c.displayName;
                    if (b == c.brand && d == c.displayName) continue;
                    renamed.Add($"db: '{c.brand} {c.displayName}' -> '{b} {d}'");
                    c.brand = b; c.displayName = d;
                    EditorUtility.SetDirty(c);
                }

            // 2) CarDefinition assets (the rich per-model definitions the database is built from).
            foreach (var guid in AssetDatabase.FindAssets("t:CarDefinition", new[] { StudioPaths.CarsFolder }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CarDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (!c) continue;
                var b0 = c.brand; var d0 = c.displayName;
                c.brand = TrademarkCheck.Brands(c.brand).Count > 0 ? CleanName(c.brand) : c.brand;
                c.displayName = TrademarkCheck.Brands(c.displayName).Count > 0 ? CleanName(c.displayName) : c.displayName;
                if (c.brand == b0 && c.displayName == d0) continue;
                renamed.Add($"asset: '{b0} {d0}' -> '{c.brand} {c.displayName}'");
                EditorUtility.SetDirty(c);
            }

            // 3) The Asset Studio vehicle registry (imported GLBs, e.g. the vecarz.com ones).
            var reg = StudioPaths.Registry();
            foreach (var r in reg.records.Where(r => r != null && r.category == AssetCategory.Vehicle))
            {
                if (TrademarkCheck.Brands(r.displayName).Count == 0) continue;
                string n = CleanName(r.displayName);
                renamed.Add($"studio: '{r.displayName}' -> '{n}'");
                r.displayName = n; EditorUtility.SetDirty(reg);
            }

            if (db) EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            if (renamed.Count == 0)
            {
                Debug.Log("[CarSales] Rename Trademarked Cars: nothing to rename - no real-maker words found on any car.");
                EditorUtility.DisplayDialog("Rename Trademarked Cars", "Nothing to rename - the checklist should already be clear.", "OK");
                return;
            }
            foreach (var line in renamed) Debug.Log("[CarSales] Renamed " + line);
            Debug.Log($"[CarSales] Rename Trademarked Cars: {renamed.Count} entr(y/ies) renamed. IMPORTANT: the 3D models are untouched - remove or paint over the maker's badges/logos on the textures before selling, or tick 'I have the makers' licences' only if true.");
            EditorUtility.DisplayDialog("Rename Trademarked Cars",
                renamed.Count + " entr(y/ies) renamed:\n\n" + string.Join("\n", renamed.Take(12)) +
                (renamed.Count > 12 ? $"\n... and {renamed.Count - 12} more (see Console)" : "") +
                "\n\nThe 3D models themselves are unchanged. You MUST still remove or paint over the badges/logos on the car textures before selling (or use the tick-box only if you actually hold the makers' licences).",
                "OK");
        }
    }
}
