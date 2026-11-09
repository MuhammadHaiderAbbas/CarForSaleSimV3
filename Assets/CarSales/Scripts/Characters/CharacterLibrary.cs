using System;
using System.Collections.Generic;
using CarSales.Studio;
using UnityEngine;

namespace CarSales
{
    /// <summary>One AI character made by Character Setup (a look-alike of your character, or another imported one).</summary>
    [Serializable]
    public class CharacterVariant
    {
        public string name = "";
        public GameObject prefab;
        public CharacterUse roles = CharacterUse.Customer | CharacterUse.Pedestrian | CharacterUse.Seller | CharacterUse.Auction;
    }

    /// <summary>
    /// Phase 11: the characters the game uses, written by Tools > Car Sales > Character Setup into
    /// Assets/CarSales/Art/Characters/Generated/Resources. Without it everyone keeps the old look.
    /// </summary>
    [CreateAssetMenu(menuName = "Car Sales/Character Library", fileName = CharacterSetup.LibraryResource)]
    public class CharacterLibrary : ScriptableObject
    {
        public int setupVersion;
        public GameObject player;
        public RuntimeAnimatorController controller;
        public List<CharacterVariant> variants = new List<CharacterVariant>();
        [Tooltip("Which CharacterAnim slots have a clip (index = CharacterAnim).")]
        public List<bool> hasAnimation = new List<bool>();

        static CharacterLibrary cached;
        static bool looked;

        public static CharacterLibrary Load()
        {
            if (!looked) { cached = Resources.Load<CharacterLibrary>(CharacterSetup.LibraryResource); looked = true; }
            return cached;
        }

        public bool Has(CharacterAnim a) => (int)a < hasAnimation.Count && hasAnimation[(int)a];

        /// <summary>A random character for this job (staff get the ones in workwear when there are any).</summary>
        public GameObject Pick(CharacterUse role)
        {
            if (variants.Count == 0) return null;
            var fits = variants.FindAll(v => v.prefab && (v.roles & role) != 0);
            var from = fits.Count > 0 ? fits : variants.FindAll(v => v.prefab);
            return from.Count == 0 ? null : from[UnityEngine.Random.Range(0, from.Count)].prefab;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; looked = false; }
    }
}
