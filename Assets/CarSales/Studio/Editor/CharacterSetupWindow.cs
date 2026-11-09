using System.IO;
using System.Linq;
using CarSales.Studio;
using UnityEditor;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 11 Character Setup (Tools > Car Sales > Character Setup): your main character, the Mixamo animations the
    /// game needs (found automatically in Art/Characters/Animations), the AI cast made from your character, and one
    /// Build button. Opened by the update popup.
    /// </summary>
    public class CharacterSetupWindow : EditorWindow
    {
        CharacterSetupData data;
        Vector2 scroll;
        string status = "";

        [MenuItem("Tools/Car Sales/Character Setup", priority = 28)]
        public static void Open() => GetWindow<CharacterSetupWindow>("Character Setup").minSize = new Vector2(620f, 560f);

        public static CharacterSetupData Data()
        {
            var d = AssetDatabase.LoadAssetAtPath<CharacterSetupData>(CharacterSetupData.AssetPath);
            if (d) return d;
            StudioPaths.Ensure(Path.GetDirectoryName(CharacterSetupData.AssetPath).Replace('\\', '/'));
            d = CreateInstance<CharacterSetupData>();
            AssetDatabase.CreateAsset(d, CharacterSetupData.AssetPath);
            return d;
        }

        void OnEnable()
        {
            data = Data();
            StudioPaths.Ensure(CharacterSetup.AnimationsFolder);
            StudioPaths.Ensure(CharacterSetup.CharactersFolder);
            if (CharacterBuilder.AutoFind(data) > 0) status = "Found animations in the Animations folder.";
        }

        public void Rescan() { if (data && CharacterBuilder.AutoFind(data) > 0) { status = "Found new animations."; Repaint(); } }

        void OnGUI()
        {
            if (!data) data = Data();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(
                "1. On mixamo.com pick a character → Download: FBX for Unity, With Skin. Put it in " + CharacterSetup.CharactersFolder + ".\n" +
                "2. For each animation below: search it, select it, Download: FBX for Unity, Without Skin, 30 fps (tick In Place for Walk and Run). " +
                "Put the files in " + CharacterSetup.AnimationsFolder + " - they are found automatically.\n" +
                "3. Press Build. Your character becomes the player (V switches first/third person) and the AI cast is made from it.",
                UnityEditor.MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open mixamo.com")) Application.OpenURL("https://www.mixamo.com");
                if (GUILayout.Button("Show Animations folder")) EditorUtility.RevealInFinder(CharacterSetup.AnimationsFolder);
                if (GUILayout.Button("Find animations")) { int n = CharacterBuilder.AutoFind(data); status = n > 0 ? $"Found {n} animation(s)." : "No new animations found."; }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Your main character", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.mainCharacter = (GameObject)EditorGUILayout.ObjectField("Character model", data.mainCharacter, typeof(GameObject), false);
            if (data.mainCharacter && PrefabUtility.GetPrefabAssetType(data.mainCharacter) != PrefabAssetType.Model)
                EditorGUILayout.HelpBox("Pick the character's model file (.fbx), not a scene object.", UnityEditor.MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            foreach (var r in CharacterSetup.Animations)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var clip = data.Clip(r.Anim);
                    string mark = clip ? "✓" : r.Required ? "✗" : "–";
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = clip ? new Color(0.35f, 0.8f, 0.35f) : r.Required ? new Color(1f, 0.45f, 0.35f) : Color.gray } };
                    GUILayout.Label(mark, style, GUILayout.Width(16));
                    GUILayout.Label(new GUIContent(r.Label + (r.Required ? " *" : ""), r.Why), GUILayout.Width(130));
                    if (GUILayout.Button(new GUIContent("\"" + r.MixamoSearch + "\"", "Copy the Mixamo search"), EditorStyles.miniButton, GUILayout.Width(150)))
                        EditorGUIUtility.systemCopyBuffer = r.MixamoSearch;
                    var picked = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                    if (picked != clip) data.SetClip(r.Anim, picked);
                }
            }
            EditorGUILayout.LabelField("* required. Missing extras fall back to a similar animation (e.g. Clap → Happy → Wave).", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI characters", EditorStyles.boldLabel);
            data.variantCount = EditorGUILayout.IntSlider(new GUIContent("Look-alikes to make", "Customers, staff, sellers, pedestrians and bidders. A quarter wear workwear."), data.variantCount, 4, 40);
            using (new EditorGUILayout.HorizontalScope())
            {
                data.seed = EditorGUILayout.IntField("Seed", data.seed);
                if (GUILayout.Button("New mix", GUILayout.Width(80))) data.seed = Random.Range(1, 99999);
            }
            EditorGUILayout.HelpBox("Look-alikes use your character's body with different heights, builds, skin tones, hair and clothes colours, " +
                                    "optional parts (hats, glasses, beards) and body blend shapes. For different faces, add more Mixamo characters here:",
                                    UnityEditor.MessageType.None);
            for (int i = 0; i < data.extraCharacters.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    data.extraCharacters[i] = (GameObject)EditorGUILayout.ObjectField(data.extraCharacters[i], typeof(GameObject), false);
                    if (GUILayout.Button("Remove", GUILayout.Width(70))) { data.extraCharacters.RemoveAt(i); break; }
                }
            }
            if (GUILayout.Button("+ Add another character")) data.extraCharacters.Add(null);
            data.author = EditorGUILayout.TextField(new GUIContent("Credit as", "Licence record / credits"), data.author);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(data);

            EditorGUILayout.Space();
            var missing = CharacterSetup.MissingRequired(a => data.Clip(a));
            using (new EditorGUI.DisabledScope(!data.mainCharacter || missing.Count > 0))
                if (GUILayout.Button("Build characters", GUILayout.Height(34)))
                {
                    status = CharacterBuilder.Build(data);
                    AssetDatabase.SaveAssets();
                }
            if (!data.mainCharacter) EditorGUILayout.HelpBox("Choose your main character.", UnityEditor.MessageType.Warning);
            else if (missing.Count > 0) EditorGUILayout.HelpBox("Still needed: " + string.Join(", ", missing.Select(m => CharacterSetup.Requirement(m).Label)), UnityEditor.MessageType.Warning);
            var lib = CharacterBuilder.LoadLibrary();
            if (lib) EditorGUILayout.LabelField($"Built: player {(lib.player ? "✓" : "✗")}, {lib.variants.Count} AI characters, {lib.hasAnimation.Count(x => x)} animations.");
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, UnityEditor.MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>The popup after an update: asks for the Mixamo files until characters are set up (or you say "don't ask").</summary>
    [InitializeOnLoad]
    static class CharacterSetupPrompt
    {
        static string Key => "CarSales.CharacterSetupDismissed." + Application.dataPath.GetHashCode();

        static CharacterSetupPrompt() => EditorApplication.delayCall += Check;

        static void Check()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("CarSales.CharacterPromptShown", false)) return;
            var lib = CharacterBuilder.LoadLibrary();
            int missing = lib ? CharacterSetup.MissingRequired(a => lib.Has(a)).Count + (lib.setupVersion < CharacterSetup.SetupVersion ? 1 : 0) : 0;
            if (!CharacterSetup.ShouldPrompt(EditorPrefs.GetInt(Key, 0), lib, missing)) return;
            SessionState.SetBool("CarSales.CharacterPromptShown", true);
            int choice = EditorUtility.DisplayDialogComplex("Car Sales - set up your characters", CharacterSetup.PromptText(), "Open Character Setup", "Later", "Don't ask again");
            if (choice == 0) CharacterSetupWindow.Open();
            else if (choice == 2) EditorPrefs.SetInt(Key, CharacterSetup.SetupVersion);
        }
    }

    /// <summary>New files in the character folders: refresh the open Character Setup window.</summary>
    class CharacterFolderWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Concat(moved).Any(p => p.StartsWith("Assets/CarSales/Art/Characters/") && !p.StartsWith(CharacterSetup.GeneratedFolder))) return;
            EditorApplication.delayCall += () => { if (EditorWindow.HasOpenInstances<CharacterSetupWindow>()) EditorWindow.GetWindow<CharacterSetupWindow>().Rescan(); };
        }
    }
}
