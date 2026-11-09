using CarSales.Studio;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11: plays the Character Setup animations on any character. Speed drives idle/walk/run; <see cref="Play"/>
    /// fires a one-shot (wave, point, clap...); <see cref="Loop"/> holds a looping action (talking, sitting, typing...)
    /// until <see cref="StopLoop"/>. Missing animations use the closest one that exists (CharacterSetup.Resolve).
    /// Also works with older characters whose Animator only has "Speed".
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public const string SpeedParam = "Speed", ActionParam = "Action", ActTrigger = "Act", LoopParam = "Loop";
        Animator animator;
        bool hasActions;
        CharacterAnim? looping;

        public Animator Animator => animator;
        public CharacterAnim? Looping => looping;

        public static CharacterAnimator On(GameObject root)
        {
            if (!root) return null;
            var c = root.GetComponent<CharacterAnimator>();
            if (!c) c = root.AddComponent<CharacterAnimator>();
            c.Bind();
            return c;
        }

        void Awake() => Bind();

        void Bind()
        {
            if (animator) return;
            animator = GetComponentInChildren<Animator>(true);
            if (!animator) return;
            animator.applyRootMotion = false;
            foreach (var p in animator.parameters) if (p.name == ActionParam) hasActions = true;
        }

        static bool Available(CharacterAnim a)
        {
            var lib = CharacterLibrary.Load();
            return lib && lib.Has(a);
        }

        public void SetSpeed(float metresPerSecond)
        {
            if (animator && animator.isActiveAndEnabled) animator.SetFloat(SpeedParam, metresPerSecond);
        }

        /// <summary>A one-shot gesture; returns to walking/standing by itself.</summary>
        public void Play(CharacterAnim anim)
        {
            if (!animator || !hasActions || !animator.isActiveAndEnabled) return;
            var a = CharacterSetup.Resolve(anim, Available);
            if (a == null || a == CharacterAnim.Idle || a == CharacterAnim.Walk || a == CharacterAnim.Run) return;
            var req = CharacterSetup.Requirement(a.Value);
            if (req != null && req.Loop) { StartCoroutine(LoopFor(a.Value, 3f)); return; }   // a looping clip played once: hold it briefly
            looping = null;
            animator.SetBool(LoopParam, false);
            animator.SetInteger(ActionParam, (int)a.Value);
            animator.SetTrigger(ActTrigger);
        }

        /// <summary>Holds a looping action (talking, sitting, typing, kneeling...).</summary>
        public void Loop(CharacterAnim anim)
        {
            if (!animator || !hasActions || !animator.isActiveAndEnabled) return;
            var a = CharacterSetup.Resolve(anim, Available);
            if (a == null || a == CharacterAnim.Idle) { StopLoop(); return; }
            if (looping == a) return;
            looping = a;
            animator.SetInteger(ActionParam, (int)a.Value);
            animator.SetBool(LoopParam, true);
            animator.SetTrigger(ActTrigger);
        }

        System.Collections.IEnumerator LoopFor(CharacterAnim a, float seconds)
        {
            Loop(a);
            yield return new WaitForSeconds(seconds);
            if (looping == a) StopLoop();
        }

        public void StopLoop()
        {
            looping = null;
            if (animator && hasActions && animator.isActiveAndEnabled) animator.SetBool(LoopParam, false);
        }
    }
}
