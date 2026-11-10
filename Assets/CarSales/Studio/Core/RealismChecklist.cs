using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSales.Studio
{
    /// <summary>What the editor found in the project (counted by the Realism Checklist window).</summary>
    public struct RealismInputs
    {
        public bool GraphicsApplied;
        public int Skies;
        public int ImportedCars;
        public int ScannedKeySurfaces;
        public bool MainCharacter;
        public int MissingAnimations;
        public int TownKindsReplaced;        // buildings, trees, lamps, benches
        public int FurnitureKindsReplaced;   // sofas, chairs, tables...
        public int AmbienceClips;            // street, showroom, workshop, auction hall, rain
        public bool Music;
        public int LicenceBlockers;
    }

    public sealed class RealismItem
    {
        public string Id = "", Title = "", Why = "", Next = "", Tool = "";
        /// <summary>0..1 done.</summary>
        public float Progress;
        /// <summary>How much this step changes how real the game looks (weights the overall score).</summary>
        public int Weight = 1;
        public bool Done => Progress >= 0.999f;
    }

    /// <summary>
    /// Phase 13d: the steps from "generated placeholder town" to "looks real", in order of how much each one shows, with
    /// how far along the project is. Pure logic (unit-tested); Tools > Car Sales > Realism > Realism Checklist shows it.
    /// </summary>
    public static class RealismChecklist
    {
        public const int CarTarget = 6;            // the six Sketchfab cars on your list
        public const int KeySurfaces = 8;          // asphalt, paving, concrete, brick, render, grass, floor tiles, wood
        public const int TownKinds = 4;
        public const int FurnitureKinds = 9;
        public const int AmbienceSlots = 5;

        static float Ratio(int have, int want) => want <= 0 ? 1f : Math.Max(0f, Math.Min(1f, have / (float)want));

        public static List<RealismItem> Evaluate(RealismInputs i)
        {
            return new List<RealismItem>
            {
                new RealismItem
                {
                    Id = "graphics", Title = "Graphics levels (shadows, ambient occlusion, lights)", Weight = 3, Tool = "graphics",
                    Progress = i.GraphicsApplied ? 1f : 0f,
                    Why = "Soft long shadows, contact shadows in corners and real street-lamp and headlight light are most of the 'real' look.",
                    Next = "Tools > Car Sales > Realism > Apply Realistic Graphics (Build Everything does it the first time).",
                },
                new RealismItem
                {
                    Id = "sky", Title = "Simple built-in sky", Weight = 1, Tool = "graphics",
                    Progress = 1f,
                    Why = "Harbour City ships with a simple procedural sky - blue atmosphere and a sun disc. No HDRI package required; it lights the town and is reflected in car paint.",
                    Next = "Nothing to do - the built-in procedural sky is applied by Build Everything / Build Harbour City.",
                },
                new RealismItem
                {
                    Id = "cars", Title = $"Real car models ({Math.Min(i.ImportedCars, CarTarget)} of {CarTarget})", Weight = 4, Tool = "studio",
                    Progress = Ratio(i.ImportedCars, CarTarget),
                    Why = "Cars are what the player looks at most. Box cars break the illusion instantly.",
                    Next = "Asset Studio > Online: paste your Sketchfab links, download each with your Sketchfab token, approve it as a Vehicle (pick paint and wheels).",
                },
                new RealismItem
                {
                    Id = "surfaces", Title = $"Scanned textures ({Math.Min(i.ScannedKeySurfaces, KeySurfaces)} of {KeySurfaces} key surfaces)", Weight = 2, Tool = "surfaces",
                    Progress = Ratio(i.ScannedKeySurfaces, KeySurfaces),
                    Why = "The generated textures are good; photo-scanned asphalt, paving, brick and grass are better close up.",
                    Next = "Asset Studio > Online > Poly Haven > Textures (CC0), then Tools > Car Sales > Realism > Surface Library: drop each in and Apply.",
                },
                new RealismItem
                {
                    Id = "characters", Title = "People (Mixamo character + animations)", Weight = 3, Tool = "characters",
                    Progress = (i.MainCharacter ? 0.5f : 0f) + (i.MainCharacter ? 0.5f * (1f - Math.Min(1f, i.MissingAnimations / 6f)) : 0f),
                    Why = "Animated, varied people walking, talking and haggling make the world feel alive.",
                    Next = "Tools > Car Sales > Character Setup: add a Mixamo character and its animations; the game makes the variants.",
                },
                new RealismItem
                {
                    Id = "town", Title = $"Town models ({i.TownKindsReplaced} of {TownKinds}: buildings, trees, lamps, benches)", Weight = 3, Tool = "town",
                    Progress = Ratio(i.TownKindsReplaced, TownKinds),
                    Why = "Real trees and modelled buildings replace the last primitive shapes on the skyline.",
                    Next = "Import models as Building / Prop in the Asset Studio, then Asset Studio > Town: tick them and Apply.",
                },
                new RealismItem
                {
                    Id = "furniture", Title = $"Furniture models ({i.FurnitureKindsReplaced} of {FurnitureKinds} kinds)", Weight = 1, Tool = "town",
                    Progress = Ratio(i.FurnitureKindsReplaced, FurnitureKinds),
                    Why = "The built-in furniture is detailed; scanned sofas, plants and chairs finish the interiors.",
                    Next = "Import furniture as Interior in the Asset Studio, then Asset Studio > Town: tick it under Sofa, Chair, Plant... and Apply.",
                },
                new RealismItem
                {
                    Id = "sound", Title = $"Sound ({Math.Min(i.AmbienceClips, AmbienceSlots)} of {AmbienceSlots} ambiences{(i.Music ? ", music" : "")})", Weight = 2, Tool = "audio",
                    Progress = Ratio(i.AmbienceClips, AmbienceSlots) * 0.8f + (i.Music ? 0.2f : 0f),
                    Why = "Real recordings of traffic, a busy showroom and rain sell the scene as much as the graphics do.",
                    Next = "Put CC0 recordings (freesound.org, filter by CC0) in Assets/CarSales/Data/AudioLibrary.asset.",
                },
                new RealismItem
                {
                    Id = "licences", Title = "Licences clear for release", Weight = 1, Tool = "release",
                    Progress = i.LicenceBlockers == 0 ? 1f : 0f,
                    Why = "Every downloaded asset must be allowed in a paid game, with credits where the licence asks for them.",
                    Next = i.LicenceBlockers == 0 ? "All imported assets are cleared." : $"{i.LicenceBlockers} asset(s) need attention: Tools > Car Sales > Release Checklist.",
                },
            };
        }

        /// <summary>Overall progress 0..100, weighted by how much each step shows.</summary>
        public static int Score(IEnumerable<RealismItem> items)
        {
            var list = items.ToList();
            int total = list.Sum(x => x.Weight);
            return total == 0 ? 0 : (int)Math.Round(100.0 * list.Sum(x => x.Progress * x.Weight) / total);
        }

        /// <summary>The most valuable unfinished step (highest weight, least done), or null when everything is done.</summary>
        public static RealismItem NextStep(IEnumerable<RealismItem> items) =>
            items.Where(x => !x.Done).OrderByDescending(x => x.Weight * (1f - x.Progress)).FirstOrDefault();
    }
}
