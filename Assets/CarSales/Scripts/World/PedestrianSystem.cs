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
        // Phase 12: people walk the pavements of the master-map town (TownPlan), turning round at the ends.
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

        void Spawn(int pavement, float d)
        {
            var root = new GameObject("Pedestrian").transform;
            float speed = Random.Range(1.1f, 1.6f);
            if (Random.value < 0.08f) speed = Random.Range(2.8f, 3.6f);   // phase 11: someone in a hurry (runs if there's a run clip)
            var character = CharacterSpawn.Attach(root, CarSales.Studio.CharacterUse.Pedestrian);
            if (character) character.SetSpeed(speed);
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(root, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                var r = body.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                var mpb = new MaterialPropertyBlock();
                var col = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.4f, 0.9f);
                mpb.SetColor("_BaseColor", col); mpb.SetColor("_Color", col);
                r.SetPropertyBlock(mpb);
            }
            walkers.Add(new Walker { t = root, pavement = pavement, d = d, dir = Random.value < 0.5f ? -1f : 1f, speed = speed });
        }
    }
}
