using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Studio;
using UnityEditor;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Release Checklist &gt; Cars &gt; Wire All Cars: turns every downloaded car GLB from the Harbour City
    /// manifest (docs/HARBOUR_CITY_ASSETS.md) into a real, buyable, drivable car in one go — the same pipeline
    /// the Asset Studio's "Approve as Vehicle" button runs (measure, wheel detection, paint detection, clear-coat,
    /// prefab, CarDefinition in Assets/CarSales/Data/Cars, registered in every CarDatabase).
    ///
    /// Display cars already placed in the world (POI dressing) are unaffected. Idempotent: a car whose file is
    /// already imported (a Vehicle record with the same source path) is skipped.
    /// The badge/logo caveat still applies: renaming is done for you here, but the 3D model textures themselves
    /// may still carry the maker's badges — strip them (or hold licences) before selling.
    /// </summary>
    public static class WireAllCars
    {
        const string ManifestPath = "Assets/CarSales/HarbourCity/harbour-city-assets.json";

        struct Entry { public string key, category, uid, name, author, licence; public string importPath; public bool needsReview; }

        static List<Entry> ReadCarManifest()
        {
            var cars = new List<Entry>();
            if (!File.Exists(ManifestPath)) return cars;
            var root = CarSales.Foundation.MiniJson.Parse(File.ReadAllText(ManifestPath));
            foreach (var item in root["assets"]?.Items ?? new List<CarSales.Foundation.JsonNode>())
            {
                string cat = item["category"]?.AsString("") ?? "";
                if (!cat.StartsWith("car_", StringComparison.Ordinal)) continue;
                cars.Add(new Entry
                {
                    key = item["key"]?.AsString("") ?? "",
                    category = cat,
                    uid = item["uid"]?.AsString("") ?? "",
                    name = item["name"]?.AsString("(unnamed)") ?? "(unnamed)",
                    author = item["author"]?.AsString("") ?? "",
                    licence = item["licence"]?.AsString("") ?? "",
                    importPath = item["importPath"]?.AsString("") ?? "",
                    needsReview = item["needsReview"]?.AsBool(false) ?? false,
                });
            }
            return cars;
        }

        static CarCategory CategoryFor(string manifestCategory)
        {
            switch (manifestCategory)
            {
                case "car_hatchback": return CarCategory.Hatchback;
                case "car_sedan": return CarCategory.Sedan;
                case "car_suv": return CarCategory.SUV;
                case "car_sports": return CarCategory.Sports;
                case "car_pickup": return CarCategory.Pickup;
                case "car_luxury": return CarCategory.Luxury;
                default: return CarCategory.Sedan;
            }
        }

        static int DefaultPrice(CarCategory c)
        {
            switch (c)
            {
                case CarCategory.Hatchback: return 14000;
                case CarCategory.SUV: return 30000;
                case CarCategory.Sports: return 52000;
                case CarCategory.Pickup: return 27000;
                case CarCategory.Luxury: return 65000;
                default: return 22000;
            }
        }

        [MenuItem("Tools/Car Sales/Release Checklist/Wire All Cars Into The Game", priority = 40)]
        public static void Run()
        {
            var cars = ReadCarManifest();
            if (cars.Count == 0)
            {
                EditorUtility.DisplayDialog("Wire All Cars", "No car_* entries in the Harbour City manifest (" + ManifestPath + ").", "OK");
                return;
            }

            var registry = StudioPaths.Registry();
            int wired = 0, already = 0, missing = 0;
            var failures = new List<string>();
            try
            {
                for (int i = 0; i < cars.Count; i++)
                {
                    var e = cars[i];
                    EditorUtility.DisplayProgressBar("Wire All Cars", e.name, (float)i / cars.Count);

                    if (string.IsNullOrEmpty(e.importPath) || !File.Exists(e.importPath)) { missing++; continue; }   // not downloaded yet
                    bool known = registry.records.Any(r => r != null && r.category == AssetCategory.Vehicle &&
                                                           string.Equals(r.sourcePath, e.importPath, StringComparison.OrdinalIgnoreCase));
                    if (known) { already++; continue; }   // already imported and wired

                    string clean = RenameTrademarkCars.CleanName(e.name);   // strip real-maker words -> fictional makes
                    var cat = CategoryFor(e.category);
                    var licence = e.needsReview ? LicenceType.Unknown : LicencePolicy.FromSketchfabSlug(e.licence);
                    var job = new ImportJob
                    {
                        modelPath = e.importPath,
                        category = AssetCategory.Vehicle,
                        displayName = clean,
                        brand = "Imported",
                        carCategory = cat,
                        year = 2019,
                        basePrice = DefaultPrice(cat),
                        fuelType = cat == CarCategory.Pickup ? "Diesel" : "Petrol",
                        transmission = cat == CarCategory.Sports ? "Manual" : "Automatic",
                        licence = new LicenceInfo
                        {
                            source = "Sketchfab",
                            sourceUrl = string.IsNullOrEmpty(e.uid) ? "" : "https://sketchfab.com/3d-models/" + e.uid,
                            author = e.author,
                            licence = licence,
                            licenceUrl = LicencePolicy.DefaultUrl(licence),
                            dateAcquired = DateTime.Today.ToString("yyyy-MM-dd"),
                            notes = e.needsReview ? "Flagged needsReview in the Harbour City manifest - verify the licence before release." : "",
                        },
                    };
                    try
                    {
                        AssetPipeline.Configure(job.modelPath, job.category);
                        AssetPipeline.Analyse(job, suggestScale: true, detectParts: true);
                        foreach (var issue in job.issues.Where(x => x.Level == IssueLevel.Error))
                            throw new InvalidOperationException(issue.Message);
                        AssetPipeline.FixMaterials(job);
                        AssetPipeline.Approve(job);
                        wired++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CarSales] Wire All Cars: '{e.name}' failed - {ex.Message}");
                        failures.Add($"{e.name}: {ex.Message}");
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                $"Wired into the game: {wired}\n" +
                $"Already imported (skipped): {already}\n" +
                $"Not downloaded yet: {missing}\n" +
                (failures.Count > 0 ? $"Failed: {failures.Count} (see Console)\n" : "") +
                "\nEvery wired car is a CarDefinition in Assets/CarSales/Data/Cars, registered in the CarDatabase - buyable and drivable.\n" +
                "Their display names were trademark-cleaned. The 3D models may still carry maker badges - strip those before selling (or hold licences).";
            foreach (var f in failures) Debug.LogError("[CarSales] Wire All Cars failed: " + f);
            Debug.Log("[CarSales] Wire All Cars summary:\n" + summary);
            EditorUtility.DisplayDialog("Wire All Cars", summary, "OK");
        }
    }
}
