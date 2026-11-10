using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// People walking along the pavements (spec §6.13). Busier by day, fewer in the rain. Walking past an open
    /// dealership, some of them go in and become customers.
    /// </summary>
    public class PedestrianSystem : MonoBehaviour
    {
        // People walk the pavements of Harbour City, turning round at the ends.
        class Walker { public Transform t; public int pavement; public float d, dir, speed; public bool turnedIn; }
        List<(V2 a, V2 b)> pavements;
        readonly List<Walker> walkers = new List<Walker>();
        float spawnTimer, walkInTimer;
        public int Count => walkers.Count;
        Material mat;

        void Start()
        {
            pavements = CarSales.Sim.Town.Pavements();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = "Pedestrian" };
        }

        void OnDestroy() { if (mat) Destroy(mat); }

        void Update()
        {
            var cam = Camera.main;
            if (!cam) return;
            float hour = CarSalesServices.DayCycle ? CarSalesServices.DayCycle.HourOfDay : 12f;
            float weather = CarSalesServices.World ? WorldSim.Footfall(CarSalesServices.World.Weather) : 1f;
            int wanted = Mathf.RoundToInt(TrafficRules.MaxPedestrians * TrafficRules.Density(hour) * weather * GameSettings.TrafficMultiplier * PerformanceGovernor.Scale);

            if (walkers.Count > wanted + 2 && walkers.Count > 0) { var last = walkers[walkers.Count - 1]; if (last.t) Destroy(last.t.gameObject); walkers.RemoveAt(walkers.Count - 1); }
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                var w = walkers[i];
                if (!w.t) { walkers.RemoveAt(i); continue; }
                var (pa, pb) = pavements[w.pavement];
                float len = V2.Distance(pa, pb);
                w.d += w.dir * w.speed * Time.deltaTime;
                if (w.d > len || w.d < 0f) { w.dir = -w.dir; w.d = Mathf.Clamp(w.d, 0f, len); }   // end of the pavement: turn round
                var p = pa + (pb - pa) * (w.d / Mathf.Max(0.01f, len));
                var forward = (pb - pa).Normalized * w.dir;
                w.t.position = new Vector3(p.x, 0.12f, p.z);
                w.t.rotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.z));
                if (Vector3.Distance(w.t.position, cam.transform.position) > 200f) { Destroy(w.t.gameObject); walkers.RemoveAt(i); }
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && walkers.Count < wanted && pavements.Count > 0)
            {
                spawnTimer = 1.2f;
                // A random pavement within sight range of the camera.
                for (int tries = 0; tries < 6; tries++)
                {
                    int k = Random.Range(0, pavements.Count);
                    var (pa, pb) = pavements[k];
                    float len = V2.Distance(pa, pb);
                    float d = Random.Range(0f, len);
                    var p = pa + (pb - pa) * (d / Mathf.Max(0.01f, len));
                    float dist = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(cam.transform.position.x, cam.transform.position.z));
                    if (dist < 25f || dist > 150f) continue;
                    Spawn(k, d);
                    break;
                }
            }

            // Walk-ins: someone passing an open dealership goes in.
            walkInTimer -= Time.deltaTime;
            if (walkInTimer > 0f) return;
            walkInTimer = 4f;
            foreach (var w in walkers.Where(x => !x.turnedIn))
                foreach (var s in CustomerSpawner.All)
                {
                    if (!s || !s.Dealer.IsOpenNow) continue;
                    var p = s.transform.position;
                    if (w.t && Vector3.Distance(w.t.position, p) < 45f && Random.value < 0.15f)
                    {
                        w.turnedIn = true;
                        s.SpawnWalkIn();
                        Destroy(w.t.gameObject);
                        return;
                    }
                }
        }

        // ------------------------------------------------------------------ POPULATION
        // Six archetypes so the street never shows the same person twice (spec: old, young, small, tall, boys, men, women).
        // When Character Setup has imported models they are used (random); otherwise a stylised primitive body is built.
        enum Archetype { Man, Woman, Senior, Young, Child, Tall }

        static Archetype RollArchetype()
        {
            float r = Random.value;
            if (r < 0.30f) return Archetype.Man;
            if (r < 0.55f) return Archetype.Woman;
            if (r < 0.68f) return Archetype.Senior;
            if (r < 0.82f) return Archetype.Young;
            if (r < 0.92f) return Archetype.Child;
            return Archetype.Tall;
        }

        // (height, build width scale, speed range, patience hair colour brightness)
        static void ArchetypeTraits(Archetype a, out float height, out float width, out float speedMin, out float speedMax)
        {
            switch (a)
            {
                case Archetype.Man:    height = Random.Range(1.70f, 1.90f); width = Random.Range(0.85f, 1.25f); speedMin = 1.1f; speedMax = 1.6f; break;
                case Archetype.Woman:  height = Random.Range(1.58f, 1.78f); width = Random.Range(0.75f, 1.05f); speedMin = 1.1f; speedMax = 1.6f; break;
                case Archetype.Senior: height = Random.Range(1.50f, 1.72f); width = Random.Range(0.95f, 1.25f); speedMin = 0.65f; speedMax = 1.05f; break;
                case Archetype.Young:  height = Random.Range(1.60f, 1.85f); width = Random.Range(0.70f, 1.00f); speedMin = 1.35f; speedMax = 2.1f; break;
                case Archetype.Child:  height = Random.Range(1.00f, 1.35f); width = Random.Range(0.70f, 0.95f); speedMin = 1.4f; speedMax = 2.3f; break;
                default:               height = Random.Range(1.90f, 2.10f); width = Random.Range(0.95f, 1.20f); speedMin = 1.15f; speedMax = 1.55f; break;
            }
        }

        static readonly Color[] SkinTones =
        {
            new Color(0.98f, 0.87f, 0.75f), new Color(0.93f, 0.78f, 0.62f), new Color(0.83f, 0.64f, 0.47f),
            new Color(0.66f, 0.47f, 0.33f), new Color(0.45f, 0.30f, 0.22f), new Color(0.30f, 0.20f, 0.14f),
        };
        static readonly Color[] Hair = { new Color(0.12f, 0.09f, 0.07f), new Color(0.25f, 0.17f, 0.1f), new Color(0.42f, 0.3f, 0.18f), new Color(0.5f, 0.5f, 0.5f), new Color(0.85f, 0.85f, 0.82f), new Color(0.55f, 0.25f, 0.1f) };
        static readonly Color[] Menswear = { new Color(0.2f, 0.25f, 0.45f), new Color(0.25f, 0.25f, 0.25f), new Color(0.35f, 0.4f, 0.3f), new Color(0.45f, 0.35f, 0.2f), new Color(0.55f, 0.55f, 0.6f), new Color(0.3f, 0.15f, 0.15f) };
        static readonly Color[] Womenswear = { new Color(0.6f, 0.25f, 0.35f), new Color(0.45f, 0.2f, 0.5f), new Color(0.2f, 0.4f, 0.5f), new Color(0.7f, 0.4f, 0.55f), new Color(0.3f, 0.45f, 0.35f), new Color(0.6f, 0.5f, 0.2f) };
        static readonly Color[] Seniorwear = { new Color(0.55f, 0.52f, 0.45f), new Color(0.45f, 0.45f, 0.5f), new Color(0.6f, 0.6f, 0.55f), new Color(0.4f, 0.35f, 0.3f) };
        static readonly Color[] Kidswear = { new Color(0.85f, 0.35f, 0.2f), new Color(0.2f, 0.5f, 0.85f), new Color(0.85f, 0.7f, 0.15f), new Color(0.3f, 0.7f, 0.35f), new Color(0.6f, 0.4f, 0.8f) };
        static readonly Color[] Youngwear = { new Color(0.3f, 0.5f, 0.35f), new Color(0.4f, 0.3f, 0.6f), new Color(0.2f, 0.35f, 0.6f), new Color(0.55f, 0.55f, 0.3f) };
        static readonly Color[] Tallwear = { new Color(0.22f, 0.28f, 0.4f), new Color(0.3f, 0.3f, 0.3f), new Color(0.18f, 0.35f, 0.25f) };

        static Color Pick(Color[] palette) => palette[Random.Range(0, palette.Length)];

        void ApplyColor(Renderer r, Color c)
        {
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
            r.sharedMaterial = mat;
        }

        GameObject PrimitivePerson(Transform parent, Archetype a, float height, float width)
        {
            bool senior = a == Archetype.Senior, child = a == Archetype.Child;
            float torso = height * 0.46f, leg = height * 0.40f, head = height * 0.115f;
            float tR = 0.155f * width * height / 1.75f;         // torso half-width

            var root = new GameObject("Body").transform;
            root.SetParent(parent, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, leg + torso * 0.5f, 0f);
            body.transform.localScale = new Vector3(tR * 2.4f, torso * 0.62f, tR * 1.7f);
            Color[] torsoPalette = a == Archetype.Man ? Menswear
                : a == Archetype.Woman ? Womenswear
                : a == Archetype.Young ? Youngwear
                : senior ? Seniorwear
                : a == Archetype.Tall ? Tallwear
                : Kidswear;
            ApplyColor(body.GetComponent<Renderer>(), Pick(torsoPalette));

            foreach (float side in new[] { -1f, 1f })
            {
                var l = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(l.GetComponent<Collider>());
                l.transform.SetParent(root, false);
                l.transform.localPosition = new Vector3(side * tR * 1.05f, leg * 0.5f, 0f);
                l.transform.localScale = new Vector3(0.085f * width, leg * 0.48f, 0.085f * width);
                ApplyColor(l.GetComponent<Renderer>(), new Color(0.18f, 0.17f, 0.2f));
            }

            var headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(headGo.GetComponent<Collider>());
            headGo.transform.SetParent(root, false);
            headGo.transform.localPosition = new Vector3(0f, leg + torso + head * 0.55f, 0f);
            headGo.transform.localScale = Vector3.one * head;
            ApplyColor(headGo.GetComponent<Renderer>(), Pick(SkinTones));

            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(hair.GetComponent<Collider>());
            hair.transform.SetParent(root, false);
            hair.transform.localPosition = new Vector3(0f, leg + torso + head * 0.86f, -0.012f);
            hair.transform.localScale = new Vector3(head * 1.02f, head * 0.55f, head * 1.02f);
            ApplyColor(hair.GetComponent<Renderer>(), senior ? Pick(new[] { Hair[3], Hair[4] }) : Pick(Hair));

            // Hats: beret-style flat cap for some adults, sun hat geometry for some women/seniors.
            if (Random.value < (senior ? 0.4f : 0.3f))
            {
                var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(hat.GetComponent<Collider>());
                hat.transform.SetParent(root, false);
                hat.transform.localPosition = new Vector3(0f, leg + torso + head * 1.12f, 0f);
                hat.transform.localScale = new Vector3(head * 0.92f, 0.035f, head * 0.92f);
                ApplyColor(hat.GetComponent<Renderer>(), Pick(new[] { new Color(0.75f, 0.7f, 0.6f), new Color(0.3f, 0.3f, 0.33f), new Color(0.5f, 0.3f, 0.15f), new Color(0.15f, 0.2f, 0.35f) }));
            }
            // A cane for some seniors.
            if (senior && Random.value < 0.45f)
            {
                var cane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(cane.GetComponent<Collider>());
                cane.transform.SetParent(root, false);
                float side = Random.value < 0.5f ? -1f : 1f;
                cane.transform.localPosition = new Vector3(side * (tR + 0.09f), leg * 0.52f, 0.06f);
                cane.transform.localScale = new Vector3(0.022f, leg * 0.52f, 0.022f);
                ApplyColor(cane.GetComponent<Renderer>(), new Color(0.35f, 0.25f, 0.12f));
            }
            return root.gameObject;
        }

        void Spawn(int pavement, float d)
        {
            var root = new GameObject("Pedestrian").transform;
            var archetype = RollArchetype();
            ArchetypeTraits(archetype, out float height, out float width, out float speedMin, out float speedMax);
            float speed = Random.Range(speedMin, speedMax);
            if (archetype == Archetype.Young && Random.value < 0.25f) speed = Random.Range(2.6f, 3.6f);   // jogging
            var character = CharacterSpawn.Attach(root, CarSales.Studio.CharacterUse.Pedestrian);
            if (character)
            {
                character.transform.localScale = Vector3.one * (height / 1.8f);
                character.SetSpeed(speed);
            }
            else
            {
                PrimitivePerson(root, archetype, height, width);
            }
            walkers.Add(new Walker { t = root, pavement = pavement, d = d, dir = Random.value < 0.5f ? -1f : 1f, speed = speed });
        }
    }
}
