using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CarSales.Studio
{
    /// <summary>
    /// Phase 12: real car makers' names. A Sketchfab (or any) licence covers the 3D model's copyright, not the maker's
    /// trademarks: selling a game with "BMW M4" or Mercedes badges needs the maker's licence, or a rename and the
    /// badges removed. Used by the Asset Studio list and the Release Checklist.
    /// </summary>
    public static class TrademarkCheck
    {
        static readonly (string word, string brand)[] Words =
        {
            ("mercedes", "Mercedes-Benz"), ("mersedes", "Mercedes-Benz"), ("benz", "Mercedes-Benz"), ("amg", "Mercedes-AMG"), ("brabus", "Brabus"), ("maybach", "Maybach"),
            ("bmw", "BMW"), ("alpina", "Alpina"), ("mini cooper", "MINI"), ("rolls", "Rolls-Royce"), ("toyota", "Toyota"), ("lexus", "Lexus"),
            ("audi", "Audi"), ("volkswagen", "Volkswagen"), ("vw ", "Volkswagen"), ("golf gti", "Volkswagen"), ("porsche", "Porsche"), ("skoda", "Skoda"), ("seat ", "SEAT"),
            ("ford", "Ford"), ("vauxhall", "Vauxhall"), ("opel", "Opel"), ("honda", "Honda"), ("nissan", "Nissan"), ("mazda", "Mazda"), ("subaru", "Subaru"),
            ("mitsubishi", "Mitsubishi"), ("hyundai", "Hyundai"), ("kia", "Kia"), ("renault", "Renault"), ("peugeot", "Peugeot"), ("citroen", "Citroen"),
            ("fiat", "Fiat"), ("ferrari", "Ferrari"), ("lamborghini", "Lamborghini"), ("maserati", "Maserati"), ("alfa romeo", "Alfa Romeo"), ("bentley", "Bentley"),
            ("aston martin", "Aston Martin"), ("jaguar", "Jaguar"), ("land rover", "Land Rover"), ("range rover", "Land Rover"), ("volvo", "Volvo"), ("tesla", "Tesla"),
            ("chevrolet", "Chevrolet"), ("chevy", "Chevrolet"), ("dodge", "Dodge"), ("jeep", "Jeep"), ("cadillac", "Cadillac"), ("bugatti", "Bugatti"), ("mclaren", "McLaren"),
            ("lotus", "Lotus"), ("dacia", "Dacia"), ("suzuki", "Suzuki"),
        };

        /// <summary>Makers named in the text (empty when none). Matches whole words, so "Fordham" isn't Ford.</summary>
        public static List<string> Brands(string text)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(text)) return found;
            string t = " " + Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", " ") + " ";
            foreach (var (word, brand) in Words)
            {
                string w = word.Trim();
                if (Regex.IsMatch(t, "(^|\\s)" + Regex.Escape(w) + "(\\s|$)") && !found.Contains(brand)) found.Add(brand);
            }
            return found;
        }

        public const string Advice = "Real car brand: the model's licence covers the 3D model, not the maker's trademark. " +
                                     "Before selling the game, rename the car (fictional make and model) and remove badges and logos from its textures, " +
                                     "or get a licence from the maker.";
    }

    /// <summary>Sketchfab links: "https://sketchfab.com/3d-models/bmw-m4-f82-8e87379f40fd40dcac0a751e22c1a188" → its 32-character id.</summary>
    public static class SketchfabLinks
    {
        public static string Uid(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = Regex.Match(url.Trim(), "([0-9a-f]{32})(?:[/?#].*)?$", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
        }

        /// <summary>Every model id in a block of pasted text (one link per line, duplicates removed).</summary>
        public static List<string> Uids(string pasted) =>
            (pasted ?? "").Split(new[] { '\n', '\r', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Uid).Where(u => u != null).Distinct().ToList();
    }

    public enum CheckLevel { Pass = 0, Warning = 1, Blocker = 2 }

    /// <summary>One line of the Release Checklist.</summary>
    public sealed class ReleaseCheck
    {
        public string Title, Detail;
        public CheckLevel Level;
        public ReleaseCheck(string title, CheckLevel level, string detail) { Title = title; Level = level; Detail = detail; }
    }

    /// <summary>What the game needs before a release (Steam) build. The editor gathers the facts; these rules judge them.</summary>
    public static class ReleaseRules
    {
        public const int MinAiCharacters = 6;

        public static List<ReleaseCheck> Evaluate(bool hasPlayerCharacter, int aiCharacters, int requiredAnimationsMissing, int licenceBlockers,
                                                   IList<string> brandedCars, bool brandsAcknowledged, string companyName, string productName, string version,
                                                   bool hasNotices, int carModels)
        {
            var list = new List<ReleaseCheck>();
            list.Add(hasPlayerCharacter && aiCharacters >= MinAiCharacters && requiredAnimationsMissing == 0
                ? new ReleaseCheck("Characters", CheckLevel.Pass, $"Player character and {aiCharacters} AI characters with animations.")
                : new ReleaseCheck("Characters", CheckLevel.Blocker, $"Set up animated characters (Tools > Car Sales > Character Setup): player {(hasPlayerCharacter ? "done" : "missing")}, " +
                                   $"{aiCharacters}/{MinAiCharacters} AI characters, {requiredAnimationsMissing} required animation(s) missing. Without them people are plain shapes."));
            list.Add(licenceBlockers == 0
                ? new ReleaseCheck("Asset licences", CheckLevel.Pass, "Every imported asset can ship.")
                : new ReleaseCheck("Asset licences", CheckLevel.Blocker, $"{licenceBlockers} asset(s) have licences that don't allow a paid game (Asset Studio > Licences)."));
            if (brandedCars.Count == 0) list.Add(new ReleaseCheck("Car brands", CheckLevel.Pass, "No real car makers' names found."));
            else list.Add(new ReleaseCheck("Car brands", brandsAcknowledged ? CheckLevel.Warning : CheckLevel.Blocker,
                                           $"{brandedCars.Count} car(s) use real makers' names: {string.Join(", ", brandedCars.Take(6))}{(brandedCars.Count > 6 ? "..." : "")}. " + TrademarkCheck.Advice +
                                           (brandsAcknowledged ? " (You confirmed you have the rights.)" : "")));
            list.Add(carModels >= 6
                ? new ReleaseCheck("Cars", CheckLevel.Pass, $"{carModels} car models.")
                : new ReleaseCheck("Cars", CheckLevel.Warning, $"Only {carModels} car models - players will see the same cars a lot."));
            bool defaultIdentity = string.IsNullOrWhiteSpace(companyName) || companyName == "DefaultCompany" || string.IsNullOrWhiteSpace(productName) || productName == "CarSalesSim";
            list.Add(defaultIdentity
                ? new ReleaseCheck("Game name & company", CheckLevel.Blocker, "Set Company Name and Product Name in Edit > Project Settings > Player (they appear in the window title and save folder).")
                : new ReleaseCheck("Game name & company", CheckLevel.Pass, $"{productName} by {companyName}."));
            bool devVersion = string.IsNullOrWhiteSpace(version) || version == "0.1";
            list.Add(devVersion
                ? new ReleaseCheck("Version", CheckLevel.Warning, "Set a version number in Project Settings > Player (e.g. 0.9.0 for Early Access).")
                : new ReleaseCheck("Version", CheckLevel.Pass, "Version " + version + "."));
            list.Add(hasNotices
                ? new ReleaseCheck("Third-party notices", CheckLevel.Pass, "THIRD_PARTY_NOTICES.txt is shipped with the build.")
                : new ReleaseCheck("Third-party notices", CheckLevel.Blocker, "THIRD_PARTY_NOTICES.txt is missing from the project folder (the icon licence requires it)."));
            return list;
        }

        public static bool CanRelease(IEnumerable<ReleaseCheck> checks) => checks.All(c => c.Level != CheckLevel.Blocker);
    }
}
