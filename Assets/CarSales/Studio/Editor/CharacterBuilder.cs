using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Studio;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 11: turns Character Setup's choices into game assets - Humanoid rigs, looping/in-place clips, one shared
    /// Animator for everyone, your player prefab, a cast of AI look-alikes and the CharacterLibrary the game loads.
    /// </summary>
    public static class CharacterBuilder
    {
        static string Gen => CharacterSetup.GeneratedFolder;
        public static string LibraryPath => $"{Gen}/Resources/{CharacterSetup.LibraryResource}.asset";
        public static string ControllerPath => $"{Gen}/CarSalesCharacters.controller";

        public static CharacterLibrary LoadLibrary() => AssetDatabase.LoadAssetAtPath<CharacterLibrary>(LibraryPath);

        // ------------------------------------------------------------------ FINDING CLIPS
        public static IEnumerable<AnimationClip> ClipsIn(string assetPath) =>
            AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__"));

        /// <summary>Fills empty slots from the Animations folder (and the main character's folder). Returns how many were found.</summary>
        public static int AutoFind(CharacterSetupData data)
        {
            int found = 0;
            var folders = new List<string>();
            if (AssetDatabase.IsValidFolder(CharacterSetup.AnimationsFolder)) folders.Add(CharacterSetup.AnimationsFolder);
            if (data.mainCharacter)
            {
                string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(data.mainCharacter))?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && AssetDatabase.IsValidFolder(dir) && !folders.Contains(dir)) folders.Add(dir);
            }
            if (folders.Count == 0) return 0;
            var files = AssetDatabase.FindAssets("t:Model t:AnimationClip", folders.ToArray()).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
            foreach (var f in files)
                foreach (var clip in ClipsIn(f))
                {
                    // Mixamo names every clip "mixamo.com": then the file name tells us what it is.
                    var a = CharacterSetup.Match(clip.name) ?? CharacterSetup.Match(Path.GetFileNameWithoutExtension(f));
                    if (a == null || data.Clip(a.Value)) continue;
                    data.SetClip(a.Value, clip);
                    found++;
                }
            EditorUtility.SetDirty(data);
            return found;
        }

        // ------------------------------------------------------------------ BUILD
        public static string Build(CharacterSetupData data)
        {
            if (!data.mainCharacter) return "Choose your main character first.";
            var missing = CharacterSetup.MissingRequired(a => data.Clip(a));
            if (missing.Count > 0) return "Missing required animations: " + string.Join(", ", missing.Select(m => CharacterSetup.Requirement(m).Label)) + ".";
            try
            {
                AssetDatabase.StartAssetEditing();
                StudioPaths.Ensure(Gen + "/Resources");
                StudioPaths.Ensure(Gen + "/AI");
                StudioPaths.Ensure(Gen + "/Materials");
            }
            finally { AssetDatabase.StopAssetEditing(); }

            EditorUtility.DisplayProgressBar("Character Setup", "Rigging characters (Humanoid)", 0.1f);
            try
            {
                var characters = new List<GameObject> { data.mainCharacter };
                characters.AddRange(data.extraCharacters.Where(c => c && c != data.mainCharacter));
                foreach (var c in characters) PrepareModel(AssetDatabase.GetAssetPath(c));

                EditorUtility.DisplayProgressBar("Character Setup", "Setting up animation clips", 0.3f);
                foreach (var r in CharacterSetup.Animations)
                {
                    var clip = data.Clip(r.Anim);
                    if (clip) PrepareClip(clip, r);
                }
                // Re-load after re-import (clip objects are replaced).
                var clips = new Dictionary<CharacterAnim, AnimationClip>();
                foreach (var r in CharacterSetup.Animations)
                {
                    var old = data.Clip(r.Anim);
                    if (!old) continue;
                    string p = AssetDatabase.GetAssetPath(old);
                    var fresh = ClipsIn(p).FirstOrDefault(c => c.name == old.name) ?? ClipsIn(p).FirstOrDefault();
                    if (fresh) { clips[r.Anim] = fresh; data.SetClip(r.Anim, fresh); }
                }

                EditorUtility.DisplayProgressBar("Character Setup", "Building the animator", 0.5f);
                var controller = BuildController(clips);

                EditorUtility.DisplayProgressBar("Character Setup", "Your character", 0.6f);
                var player = SaveCharacterPrefab(data.mainCharacter, controller, $"{Gen}/Player_Character.prefab", null);

                EditorUtility.DisplayProgressBar("Character Setup", "Making the AI cast", 0.7f);
                var variants = BuildVariants(data, characters, controller);

                var lib = LoadLibrary();
                if (!lib) { lib = ScriptableObject.CreateInstance<CharacterLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
                lib.setupVersion = CharacterSetup.SetupVersion;
                lib.player = player;
                lib.controller = controller;
                lib.variants = variants;
                lib.hasAnimation = Enumerable.Range(0, System.Enum.GetValues(typeof(CharacterAnim)).Length).Select(i => clips.ContainsKey((CharacterAnim)i)).ToList();
                EditorUtility.SetDirty(lib);

                // Licence records (spec §12): Mixamo characters and animations are royalty-free with an Adobe account.
                var licence = new LicenceInfo { source = "Mixamo", sourceUrl = "https://www.mixamo.com", author = data.author, licence = LicenceType.RoyaltyFreePurchased,
                                                purchaseRef = "Mixamo (Adobe account, royalty-free)", dateAcquired = System.DateTime.UtcNow.ToString("yyyy-MM-dd"),
                                                notes = "Set up with Character Setup. Change this if your character came from somewhere else." };
                foreach (var c in characters)
                    if (StudioPaths.Registry().Find("char_" + StudioNames.SafeName(c.name)) == null)
                        AssetPipeline.RegisterAsset(AssetCategory.Character, "char_" + StudioNames.SafeName(c.name), c.name, c, licence);
                if (StudioPaths.Registry().Find("mixamo_animations") == null)
                    AssetPipeline.RegisterAsset(AssetCategory.Character, "mixamo_animations", "Character animations", controller, licence);

                AssetDatabase.SaveAssets();
                return $"Done: your character, {variants.Count} AI characters and {clips.Count} animations. Press Play.";
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static void PrepareModel(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) return;
            if (mi.animationType != ModelImporterAnimationType.Human) AssetPipeline.MakeHumanoid(path);
            // Pink materials in URP: extract and convert.
            AssetPipeline.FixMaterials(new ImportJob { modelPath = path, category = AssetCategory.Character, displayName = Path.GetFileNameWithoutExtension(path) });
        }

        /// <summary>Humanoid rig, loop if needed, root motion baked in place (the game moves characters itself).</summary>
        static void PrepareClip(AnimationClip clip, AnimRequirement r)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) return;   // a standalone .anim: use as it is
            bool changed = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            var clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                if (clips.Length > 1 && c.name != clip.name) continue;
                c.loopTime = r.Loop; c.loopPose = r.Loop;
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                changed = true;
            }
            if (!changed) return;
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        /// <summary>
        /// One controller for everyone: "Speed" blends idle → walk → run; "Act" + "Action" plays any other clip
        /// (loops stay while "Loop" is on; one-shots return by themselves).
        /// </summary>
        static AnimatorController BuildController(Dictionary<CharacterAnim, AnimationClip> clips)
        {
            if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter(CharacterAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            ac.AddParameter(CharacterAnimator.ActionParam, AnimatorControllerParameterType.Int);
            ac.AddParameter(CharacterAnimator.ActTrigger, AnimatorControllerParameterType.Trigger);
            ac.AddParameter(CharacterAnimator.LoopParam, AnimatorControllerParameterType.Bool);
            var sm = ac.layers[0].stateMachine;

            var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = CharacterAnimator.SpeedParam;
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips[CharacterAnim.Idle], 0f);
            tree.AddChild(clips[CharacterAnim.Walk], CharacterSetup.WalkSpeed);
            tree.AddChild(clips.TryGetValue(CharacterAnim.Run, out var run) ? run : clips[CharacterAnim.Walk], CharacterSetup.RunSpeed);
            sm.defaultState = loco;

            int i = 0;
            foreach (var r in CharacterSetup.Animations)
            {
                if (r.Anim == CharacterAnim.Idle || r.Anim == CharacterAnim.Walk || r.Anim == CharacterAnim.Run) continue;
                if (!clips.TryGetValue(r.Anim, out var clip)) continue;
                var st = sm.AddState(r.Label, new Vector3(300f, i++ * 60f, 0f));
                st.motion = clip;
                var enter = sm.AddAnyStateTransition(st);
                enter.canTransitionToSelf = false;
                enter.hasExitTime = false;
                enter.duration = 0.2f;
                enter.AddCondition(AnimatorConditionMode.If, 0f, CharacterAnimator.ActTrigger);
                enter.AddCondition(AnimatorConditionMode.Equals, (int)r.Anim, CharacterAnimator.ActionParam);
                var back = st.AddTransition(loco);
                back.duration = 0.25f;
                if (r.Loop) { back.hasExitTime = false; back.AddCondition(AnimatorConditionMode.IfNot, 0f, CharacterAnimator.LoopParam); }
                else { back.hasExitTime = true; back.exitTime = 0.9f; }
            }
            EditorUtility.SetDirty(ac);
            return ac;
        }

        static Avatar AvatarOf(GameObject model) =>
            AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<Avatar>().FirstOrDefault(a => a.isHuman) ??
            AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<Avatar>().FirstOrDefault();

        static GameObject SaveCharacterPrefab(GameObject model, RuntimeAnimatorController controller, string path, VariantSpec spec)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = Path.GetFileNameWithoutExtension(path);
                go.transform.position = Vector3.zero;
                var animator = go.GetComponentInChildren<Animator>() ?? go.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (!animator.avatar) animator.avatar = AvatarOf(model);
                if (spec != null) ApplyVariant(go, spec);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static void ApplyVariant(GameObject go, VariantSpec v)
        {
            go.transform.localScale = new Vector3(v.Height * v.Build, v.Height, v.Height * v.Build);
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (v.HiddenParts.Contains(t.name)) t.gameObject.SetActive(false);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (!mesh) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    if (v.BlendShapes.TryGetValue(mesh.GetBlendShapeName(i), out float w)) smr.SetBlendShapeWeight(i, w);
            }
            int k = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (!src) continue;
                    var role = VariantGenerator.Classify(src.name);
                    if (role == MaterialRole.Other) role = VariantGenerator.Classify(r.name);
                    var tint = VariantGenerator.TintFor(v, role);
                    if (tint == null) continue;
                    var m = new Material(src) { name = $"{v.Name}_{src.name}" };
                    var c = new Color(tint.Value.r, tint.Value.g, tint.Value.b, 1f);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                    AssetDatabase.CreateAsset(m, $"{Gen}/Materials/{v.Name}_{k++}.mat");
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        static List<CharacterVariant> BuildVariants(CharacterSetupData data, List<GameObject> sources, RuntimeAnimatorController controller)
        {
            // Clear the last cast so materials don't pile up.
            foreach (var f in new[] { Gen + "/AI", Gen + "/Materials" })
                foreach (var guid in AssetDatabase.FindAssets("", new[] { f })) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            var list = new List<CharacterVariant>();
            // Every imported character as it is (more faces), then look-alikes spread over all of them.
            for (int s = 1; s < sources.Count; s++)
            {
                var p = SaveCharacterPrefab(sources[s], controller, $"{Gen}/AI/Original_{StudioNames.SafeName(sources[s].name)}.prefab", null);
                list.Add(new CharacterVariant { name = sources[s].name, prefab = p });
            }
            for (int s = 0; s < sources.Count; s++)
            {
                var model = sources[s];
                var tmp = (GameObject)PrefabUtility.InstantiatePrefab(model);
                var parts = tmp.GetComponentsInChildren<Transform>(true).Where(t => t != tmp.transform && VariantGenerator.IsOptionalPart(t.name)).Select(t => t.name).Distinct().ToList();
                var blends = tmp.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.sharedMesh)
                                .SelectMany(r => Enumerable.Range(0, r.sharedMesh.blendShapeCount).Select(i => r.sharedMesh.GetBlendShapeName(i)))
                                .Where(VariantGenerator.IsShapeBlend).Distinct().ToList();
                Object.DestroyImmediate(tmp);
                int count = Mathf.Max(1, Mathf.RoundToInt(data.variantCount / (float)sources.Count));
                foreach (var spec in VariantGenerator.Generate(count, data.seed + s * 101, parts, blends))
                {
                    spec.Name = $"{StudioNames.SafeName(model.name)}_{spec.Name}";
                    var p = SaveCharacterPrefab(model, controller, $"{Gen}/AI/{spec.Name}.prefab", spec);
                    list.Add(new CharacterVariant { name = spec.Name, prefab = p, roles = spec.Role });
                }
            }
            return list;
        }
    }
}
