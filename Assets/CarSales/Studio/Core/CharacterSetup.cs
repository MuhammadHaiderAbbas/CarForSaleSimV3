using System;
using System.Collections.Generic;

namespace CarSales.Studio
{
    /// <summary>
    /// Every animation a character can play (Phase 11). Saved as numbers in the Animator ("Action" parameter):
    /// only add new values at the end.
    /// </summary>
    public enum CharacterAnim
    {
        Idle = 0, Walk = 1, Run = 2, Talking = 3, Wave = 4, Point = 5, LookAround = 6, Kneel = 7, RaiseHand = 8,
        Clap = 9, Cheer = 10, Angry = 11, Thinking = 12, SitIdle = 13, Typing = 14, PhoneCall = 15,
    }

    /// <summary>Who plays an animation (flags).</summary>
    [Flags]
    public enum CharacterUse { None = 0, Player = 1, Customer = 2, Pedestrian = 4, Staff = 8, Seller = 16, Auction = 32, All = 63 }

    /// <summary>One animation the game asks you to download from Mixamo.</summary>
    public sealed class AnimRequirement
    {
        public CharacterAnim Anim;
        public string Label;
        public string MixamoSearch;     // what to type into mixamo.com
        public bool Required;           // the game can't animate characters without it
        public bool Loop;               // locomotion, talking, sitting, typing...
        public bool InPlace;            // tick "In Place" on Mixamo (walk / run)
        public bool OneShot => !Loop;
        public CharacterUse UsedBy;
        public string Why;
    }

    /// <summary>
    /// Phase 11 character rules (engine-free, tested): the Mixamo animation list, matching downloaded files to it,
    /// what to play when an animation is missing, and the look of generated AI characters.
    /// </summary>
    public static class CharacterSetup
    {
        /// <summary>Bump when the required animation list changes: the editor asks you again after an update.</summary>
        public const int SetupVersion = 1;
        public const string LibraryResource = "CarSalesCharacterLibrary";
        public const string AnimationsFolder = "Assets/CarSales/Art/Characters/Animations";
        public const string CharactersFolder = "Assets/CarSales/Art/Characters/Models";
        public const string GeneratedFolder = "Assets/CarSales/Art/Characters/Generated";

        /// <summary>Walking speed (m/s) used as the walk threshold in the locomotion blend.</summary>
        public const float WalkSpeed = 1.4f, RunSpeed = 4f;

        public static readonly AnimRequirement[] Animations =
        {
            new AnimRequirement { Anim = CharacterAnim.Idle, Label = "Idle", MixamoSearch = "Breathing Idle", Required = true, Loop = true, UsedBy = CharacterUse.All, Why = "Standing still." },
            new AnimRequirement { Anim = CharacterAnim.Walk, Label = "Walk", MixamoSearch = "Walking", Required = true, Loop = true, InPlace = true, UsedBy = CharacterUse.All, Why = "Everyone walking." },
            new AnimRequirement { Anim = CharacterAnim.Run, Label = "Run", MixamoSearch = "Running", Required = true, Loop = true, InPlace = true, UsedBy = CharacterUse.Player | CharacterUse.Pedestrian, Why = "You sprinting, people in a hurry." },
            new AnimRequirement { Anim = CharacterAnim.Talking, Label = "Talking", MixamoSearch = "Talking", Required = true, Loop = true, UsedBy = CharacterUse.Customer | CharacterUse.Staff | CharacterUse.Seller | CharacterUse.Auction, Why = "Negotiating, the auctioneer, sellers, salespeople." },
            new AnimRequirement { Anim = CharacterAnim.Wave, Label = "Wave", MixamoSearch = "Waving", UsedBy = CharacterUse.Customer | CharacterUse.Seller | CharacterUse.Staff, Why = "A customer calling you over, sellers greeting you." },
            new AnimRequirement { Anim = CharacterAnim.Point, Label = "Point", MixamoSearch = "Pointing", UsedBy = CharacterUse.Customer | CharacterUse.Auction, Why = "Pointing at a car; the auctioneer taking a bid." },
            new AnimRequirement { Anim = CharacterAnim.LookAround, Label = "Look around", MixamoSearch = "Looking Around", Loop = true, UsedBy = CharacterUse.Customer | CharacterUse.Staff, Why = "Looking over a car, looking inside." },
            new AnimRequirement { Anim = CharacterAnim.Kneel, Label = "Kneel / inspect", MixamoSearch = "Kneeling Pointing", Loop = true, UsedBy = CharacterUse.Customer | CharacterUse.Staff, Why = "Checking the tyres; mechanics and detailers at work." },
            new AnimRequirement { Anim = CharacterAnim.RaiseHand, Label = "Raise hand (bid)", MixamoSearch = "Raising Hand", UsedBy = CharacterUse.Auction, Why = "Bidders bidding in the sale hall." },
            new AnimRequirement { Anim = CharacterAnim.Clap, Label = "Clap", MixamoSearch = "Clapping", UsedBy = CharacterUse.Auction, Why = "The room when a lot is sold." },
            new AnimRequirement { Anim = CharacterAnim.Cheer, Label = "Happy", MixamoSearch = "Happy", UsedBy = CharacterUse.Customer, Why = "A customer who just bought a car." },
            new AnimRequirement { Anim = CharacterAnim.Angry, Label = "Angry", MixamoSearch = "Angry", UsedBy = CharacterUse.Customer, Why = "A customer walking away from a bad deal." },
            new AnimRequirement { Anim = CharacterAnim.Thinking, Label = "Thinking", MixamoSearch = "Thinking", Loop = true, UsedBy = CharacterUse.Customer | CharacterUse.Staff, Why = "Deciding about a car, the manager." },
            new AnimRequirement { Anim = CharacterAnim.SitIdle, Label = "Sitting", MixamoSearch = "Sitting Idle", Loop = true, UsedBy = CharacterUse.Auction | CharacterUse.Staff, Why = "Bidders on the benches, staff at desks." },
            new AnimRequirement { Anim = CharacterAnim.Typing, Label = "Typing", MixamoSearch = "Typing", Loop = true, UsedBy = CharacterUse.Staff, Why = "The receptionist at the desk." },
            new AnimRequirement { Anim = CharacterAnim.PhoneCall, Label = "On the phone", MixamoSearch = "Talking On Phone", Loop = true, UsedBy = CharacterUse.Staff | CharacterUse.Pedestrian, Why = "Staff on calls, people on the street." },
        };

        public static AnimRequirement Requirement(CharacterAnim a) => Array.Find(Animations, r => r.Anim == a);

        /// <summary>What to play instead when an animation wasn't downloaded (first available wins).</summary>
        static readonly Dictionary<CharacterAnim, CharacterAnim[]> Fallbacks = new Dictionary<CharacterAnim, CharacterAnim[]>
        {
            { CharacterAnim.Run, new[] { CharacterAnim.Walk } },
            { CharacterAnim.Wave, new[] { CharacterAnim.RaiseHand, CharacterAnim.Talking } },
            { CharacterAnim.Point, new[] { CharacterAnim.Talking } },
            { CharacterAnim.LookAround, new[] { CharacterAnim.Thinking, CharacterAnim.Idle } },
            { CharacterAnim.Kneel, new[] { CharacterAnim.LookAround, CharacterAnim.Idle } },
            { CharacterAnim.RaiseHand, new[] { CharacterAnim.Wave, CharacterAnim.Point } },
            { CharacterAnim.Clap, new[] { CharacterAnim.Cheer } },
            { CharacterAnim.Cheer, new[] { CharacterAnim.Clap, CharacterAnim.Wave } },
            { CharacterAnim.Angry, new[] { CharacterAnim.Thinking } },
            { CharacterAnim.Thinking, new[] { CharacterAnim.LookAround, CharacterAnim.Idle } },
            { CharacterAnim.SitIdle, new[] { CharacterAnim.Idle } },
            { CharacterAnim.Typing, new[] { CharacterAnim.SitIdle, CharacterAnim.Idle } },
            { CharacterAnim.PhoneCall, new[] { CharacterAnim.Talking, CharacterAnim.Idle } },
            { CharacterAnim.Talking, new[] { CharacterAnim.Idle } },
        };

        /// <summary>
        /// The animation to actually play for <paramref name="wanted"/>, given which ones exist; null if nothing fits
        /// (one-shots then just don't play, loops stay in idle).
        /// </summary>
        public static CharacterAnim? Resolve(CharacterAnim wanted, Func<CharacterAnim, bool> available)
        {
            if (available(wanted)) return wanted;
            var seen = new HashSet<CharacterAnim> { wanted };
            var queue = new Queue<CharacterAnim>();
            if (Fallbacks.TryGetValue(wanted, out var first)) foreach (var f in first) queue.Enqueue(f);
            while (queue.Count > 0)
            {
                var a = queue.Dequeue();
                if (!seen.Add(a)) continue;
                if (available(a)) return a;
                if (Fallbacks.TryGetValue(a, out var more)) foreach (var f in more) queue.Enqueue(f);
            }
            return null;
        }

        /// <summary>
        /// Which slot a downloaded clip or file is for, from its name ("Breathing Idle.fbx", "Sitting Idle", "Walking (1)",
        /// "mixamo.com" clips take the file name). Null if it doesn't look like any of them.
        /// </summary>
        public static CharacterAnim? Match(string name)
        {
            string n = Normalise(name);
            if (n.Length == 0 || n.Contains("preview")) return null;
            // Most specific first: "sitting idle" is sitting, "talking on phone" is the phone, "kneeling pointing" is kneeling.
            if (Has(n, "phone", "texting", "cell")) return CharacterAnim.PhoneCall;
            if (Has(n, "typing", "keyboard")) return CharacterAnim.Typing;
            if (Has(n, "sit", "seated")) return CharacterAnim.SitIdle;
            if (Has(n, "kneel", "crouch", "squat", "inspect")) return CharacterAnim.Kneel;
            if (Has(n, "raising hand", "raise hand", "hand raise", "bid")) return CharacterAnim.RaiseHand;
            if (Has(n, "wave", "waving", "greeting", "hello")) return CharacterAnim.Wave;
            if (Has(n, "point")) return CharacterAnim.Point;
            if (Has(n, "clap", "applau")) return CharacterAnim.Clap;
            if (Has(n, "cheer", "happy", "victory", "celebrat", "excited")) return CharacterAnim.Cheer;
            if (Has(n, "angry", "mad", "annoyed", "frustrat", "dismiss")) return CharacterAnim.Angry;
            if (Has(n, "think", "thoughtful", "ponder")) return CharacterAnim.Thinking;
            if (Has(n, "look around", "looking around", "looking", "search")) return CharacterAnim.LookAround;
            if (Has(n, "talk", "conversation", "explain", "argu")) return CharacterAnim.Talking;
            if (Has(n, "run", "jog", "sprint")) return CharacterAnim.Run;
            if (Has(n, "walk")) return CharacterAnim.Walk;
            if (Has(n, "idle", "breath", "standing", "stand")) return CharacterAnim.Idle;
            return null;
        }

        static bool Has(string n, params string[] words) { foreach (var w in words) if (n.Contains(w)) return true; return false; }

        static string Normalise(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\\', '/');
            int slash = s.LastIndexOf('/');
            if (slash >= 0) s = s.Substring(slash + 1);
            int dot = s.LastIndexOf('.');
            if (dot > 0) s = s.Substring(0, dot);
            var sb = new System.Text.StringBuilder();
            foreach (char c in s.ToLowerInvariant()) sb.Append(char.IsLetter(c) ? c : ' ');
            return " " + string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) + " ";
        }

        /// <summary>Slots still empty that the game can't do without.</summary>
        public static List<CharacterAnim> MissingRequired(Func<CharacterAnim, bool> available)
        {
            var list = new List<CharacterAnim>();
            foreach (var r in Animations) if (r.Required && !available(r.Anim)) list.Add(r.Anim);
            return list;
        }

        /// <summary>
        /// Show the "set up your characters" popup? Yes while setup isn't complete (no library yet, or a required animation
        /// missing) unless you said "don't ask again" for this version. Bumping <see cref="SetupVersion"/> (new animations
        /// in an update) asks again.
        /// </summary>
        public static bool ShouldPrompt(int dismissedVersion, bool libraryExists, int missingRequired) =>
            dismissedVersion < SetupVersion && (!libraryExists || missingRequired > 0);

        public static string PromptText()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Characters can now use real animations: you, customers, staff, sellers, pedestrians and the auction room.\n\n");
            sb.Append("1. Your main character: download one character from mixamo.com (Format: FBX for Unity, Skin: With Skin).\n");
            sb.Append("2. These animations (Skin: Without Skin, 30 fps; tick In Place for walk and run):\n");
            foreach (var r in Animations)
                sb.Append("   ").Append(r.Required ? "* " : "  ").Append(r.Label).Append("  -  search \"").Append(r.MixamoSearch).Append('"').Append('\n');
            sb.Append("(* = required; the others make characters livelier and fall back to similar ones if missing.)\n\n");
            sb.Append("Put the files in ").Append(AnimationsFolder).Append(", then open Character Setup: it finds them, builds the animator, ");
            sb.Append("and makes your AI cast from your character.");
            return sb.ToString();
        }
    }

    // ------------------------------------------------------------------ AI LOOK-ALIKES
    public enum MaterialRole { Other = 0, Skin = 1, Hair = 2, Eyes = 3, Top = 4, Bottom = 5, Shoes = 6, Accessory = 7 }

    /// <summary>One generated AI character: proportions, colours per material role, which optional parts it wears.</summary>
    public sealed class VariantSpec
    {
        public int Index;
        public string Name = "";
        public float Height = 1f, Build = 1f;              // uniform scale and width (x/z) multipliers
        public readonly Dictionary<MaterialRole, (float r, float g, float b)> Tints = new Dictionary<MaterialRole, (float, float, float)>();
        public readonly HashSet<string> HiddenParts = new HashSet<string>();
        public readonly Dictionary<string, float> BlendShapes = new Dictionary<string, float>();
        public CharacterUse Role = CharacterUse.Customer | CharacterUse.Pedestrian | CharacterUse.Seller | CharacterUse.Auction;
        public float SkinTone = 1f;                        // multiplier on skin (1 = as modelled)
    }

    /// <summary>
    /// Makes a varied cast from your character (and any other characters you import): different heights and builds,
    /// clothes and hair colours, optional parts (hats, glasses, beards...) switched on and off, body blend shapes,
    /// and work outfits for staff. The body mesh stays yours; add more Mixamo characters for more faces.
    /// </summary>
    public static class VariantGenerator
    {
        public static MaterialRole Classify(string materialOrMeshName)
        {
            string n = (materialOrMeshName ?? "").ToLowerInvariant();
            if (n.Contains("eye") || n.Contains("lash") || n.Contains("brow") || n.Contains("teeth") || n.Contains("tongue") || n.Contains("cornea")) return MaterialRole.Eyes;
            if (n.Contains("hair") || n.Contains("beard") || n.Contains("moustache") || n.Contains("mustache")) return MaterialRole.Hair;
            if (n.Contains("skin") || n.Contains("body") || n.Contains("face") || n.Contains("head") || n.Contains("hand") || n.Contains("arm")) return MaterialRole.Skin;
            if (n.Contains("shoe") || n.Contains("boot") || n.Contains("sneaker") || n.Contains("feet") || n.Contains("foot")) return MaterialRole.Shoes;
            if (n.Contains("pant") || n.Contains("trouser") || n.Contains("jean") || n.Contains("short") || n.Contains("skirt") || n.Contains("bottom") || n.Contains("leg")) return MaterialRole.Bottom;
            if (n.Contains("shirt") || n.Contains("top") || n.Contains("jacket") || n.Contains("coat") || n.Contains("hood") || n.Contains("sweater") || n.Contains("vest") || n.Contains("cloth") || n.Contains("suit") || n.Contains("dress")) return MaterialRole.Top;
            if (n.Contains("hat") || n.Contains("cap") || n.Contains("glass") || n.Contains("bag") || n.Contains("watch") || n.Contains("belt") || n.Contains("scarf") || n.Contains("tie")) return MaterialRole.Accessory;
            return MaterialRole.Other;
        }

        /// <summary>Parts that can be switched off for variety (a mesh named like this is optional).</summary>
        public static bool IsOptionalPart(string meshName)
        {
            string n = (meshName ?? "").ToLowerInvariant();
            foreach (var w in new[] { "hat", "cap", "glasses", "sunglasses", "beard", "moustache", "mustache", "bag", "backpack", "earring", "necklace", "scarf", "watch", "helmet", "headphone", "mask", "tie" })
                if (n.Contains(w)) return true;
            return false;
        }

        /// <summary>Blend shapes that change the body or face shape (not expressions or visemes).</summary>
        public static bool IsShapeBlend(string blendName)
        {
            string n = (blendName ?? "").ToLowerInvariant();
            if (n.Contains("viseme") || n.Contains("blink") || n.Contains("smile") || n.Contains("mouth") || n.Contains("eye")) return false;
            foreach (var w in new[] { "fat", "thin", "muscle", "weight", "heavy", "slim", "nose", "jaw", "cheek", "chin", "belly", "age" })
                if (n.Contains(w)) return true;
            return false;
        }

        static readonly (float, float, float)[] HairColours =
        {
            (0.08f, 0.06f, 0.05f), (0.22f, 0.14f, 0.08f), (0.38f, 0.24f, 0.13f), (0.62f, 0.45f, 0.26f), (0.85f, 0.72f, 0.48f),
            (0.55f, 0.24f, 0.12f), (0.6f, 0.6f, 0.6f), (0.9f, 0.9f, 0.88f),
        };

        // Work clothes: tops for each staff job (bottoms darker).
        static readonly Dictionary<CharacterUse, (float, float, float)[]> Workwear = new Dictionary<CharacterUse, (float, float, float)[]>
        {
            { CharacterUse.Staff, new[] { (0.12f, 0.16f, 0.28f), (0.18f, 0.18f, 0.2f), (0.1f, 0.25f, 0.45f) } },
        };

        static (float, float, float) Hsv(double h, double s, double v)
        {
            h = (h % 1.0 + 1.0) % 1.0;
            double c = v * s, x = c * (1 - Math.Abs(h * 6 % 2 - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            switch ((int)(h * 6))
            {
                case 0: r = c; g = x; break;
                case 1: r = x; g = c; break;
                case 2: g = c; b = x; break;
                case 3: g = x; b = c; break;
                case 4: r = x; b = c; break;
                default: r = c; b = x; break;
            }
            return ((float)(r + m), (float)(g + m), (float)(b + m));
        }

        /// <summary>
        /// Generates <paramref name="count"/> look-alikes. The same seed always gives the same cast (so re-running
        /// Character Setup doesn't reshuffle everyone). <paramref name="staffShare"/> of them wear workwear.
        /// </summary>
        public static List<VariantSpec> Generate(int count, int seed, IList<string> optionalParts, IList<string> shapeBlends, float staffShare = 0.25f)
        {
            var rng = new Random(seed);
            var list = new List<VariantSpec>();
            int staff = (int)Math.Round(count * staffShare);
            for (int i = 0; i < count; i++)
            {
                var v = new VariantSpec { Index = i, Name = $"AI_{i + 1:00}" };
                v.Height = 0.92f + (float)rng.NextDouble() * 0.16f;          // about 1.65 m - 1.95 m for a 1.8 m model
                v.Build = 0.92f + (float)rng.NextDouble() * 0.18f;
                v.SkinTone = 0.55f + (float)rng.NextDouble() * 0.45f;        // multiplies the skin texture: lighter to darker
                v.Tints[MaterialRole.Hair] = HairColours[rng.Next(HairColours.Length)];
                bool isStaff = i < staff;
                if (isStaff)
                {
                    var work = Workwear[CharacterUse.Staff];
                    v.Tints[MaterialRole.Top] = work[rng.Next(work.Length)];
                    v.Tints[MaterialRole.Bottom] = (0.12f, 0.12f, 0.14f);
                    v.Role = CharacterUse.Staff;
                    v.Name += "_Staff";
                }
                else
                {
                    v.Tints[MaterialRole.Top] = Hsv(rng.NextDouble(), 0.25 + rng.NextDouble() * 0.55, 0.35 + rng.NextDouble() * 0.6);
                    v.Tints[MaterialRole.Bottom] = Hsv(rng.NextDouble(), 0.1 + rng.NextDouble() * 0.4, 0.15 + rng.NextDouble() * 0.45);
                }
                v.Tints[MaterialRole.Shoes] = Hsv(rng.NextDouble(), rng.NextDouble() * 0.3, 0.1 + rng.NextDouble() * 0.5);
                v.Tints[MaterialRole.Accessory] = Hsv(rng.NextDouble(), 0.3 + rng.NextDouble() * 0.5, 0.3 + rng.NextDouble() * 0.6);
                if (optionalParts != null)
                    foreach (var p in optionalParts) if (rng.NextDouble() < 0.5) v.HiddenParts.Add(p);
                if (shapeBlends != null)
                    foreach (var b in shapeBlends) v.BlendShapes[b] = (float)Math.Round(rng.NextDouble() * 60.0, 1);
                list.Add(v);
            }
            return list;
        }

        /// <summary>Colour for a material of this role (null = leave it as modelled).</summary>
        public static (float r, float g, float b)? TintFor(VariantSpec v, MaterialRole role)
        {
            if (role == MaterialRole.Eyes || role == MaterialRole.Other) return null;
            if (role == MaterialRole.Skin) return (v.SkinTone, Math.Min(1f, v.SkinTone * 0.95f + 0.03f), Math.Min(1f, v.SkinTone * 0.9f + 0.05f));
            return v.Tints.TryGetValue(role, out var c) ? c : ((float r, float g, float b)?)null;
        }
    }
}
