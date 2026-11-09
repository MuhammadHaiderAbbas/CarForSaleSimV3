using System;
using System.Collections.Generic;
using CarSales.Studio;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>What you chose in Character Setup (kept in Assets/CarSales/Data/CharacterSetup.asset).</summary>
    public class CharacterSetupData : ScriptableObject
    {
        public const string AssetPath = "Assets/CarSales/Data/CharacterSetup.asset";

        [Serializable]
        public class Slot { public CharacterAnim anim; public AnimationClip clip; }

        public GameObject mainCharacter;
        public List<GameObject> extraCharacters = new List<GameObject>();
        public List<Slot> slots = new List<Slot>();
        public int variantCount = 16;
        public int seed = 1234;
        public string author = "Mixamo / Adobe";

        public AnimationClip Clip(CharacterAnim a) { var s = slots.Find(x => x.anim == a); return s != null ? s.clip : null; }

        public void SetClip(CharacterAnim a, AnimationClip c)
        {
            var s = slots.Find(x => x.anim == a);
            if (s == null) slots.Add(s = new Slot { anim = a });
            s.clip = c;
        }
    }
}
