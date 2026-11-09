using System.Collections.Generic;
using System.Reflection;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 13b: the town after dark and in the rain. Lights windows on façades at dusk, dims lamp heads and signs by day,
    /// moves a pool of real lights to the street lamps nearest the camera, switches on the headlights of the car you drive
    /// and blends the night / rain post-processing in. Works on runtime copies so no material asset is ever changed.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class RealismDirector : MonoBehaviour
    {
        [Tooltip("Façade materials with an emission map (lit windows at night).")]
        public Material[] nightWindows = new Material[0];
        [Tooltip("Lamp heads and illuminated signs: full brightness at night, faint by day.")]
        public Material[] nightGlow = new Material[0];
        [Tooltip("Frosted windows inside the walk-in homes: daylight by day, dark blue at night.")]
        public Material[] dayWindows = new Material[0];
        [Tooltip("URP Volumes blended in at night and in the rain (weight set here).")]
        public Behaviour nightVolume, rainVolume;
        [Range(0f, 6f)] public float windowIntensity = 2.2f;
        [Range(0f, 1f)] public float glowByDay = 0.06f;

        [Header("Street lamps")]
        [Tooltip("Real lights moved to the lamps nearest the camera after dark (more = nicer, slower).")]
        [Range(0, 64)] public int lampLights = 24;
        public float lampRange = 18f, lampIntensity = 2.4f;
        public Color lampColour = new Color(1f, 0.8f, 0.58f);

        [Header("Headlights")]
        public float headlightRange = 45f, headlightIntensity = 3.2f;
        [Range(10f, 120f)] public float headlightAngle = 62f;

        /// <summary>0 = day, 1 = night (for other scripts).</summary>
        public static float Darkness { get; private set; }

        readonly List<Material> windows = new List<Material>();
        readonly List<(Material copy, Color emission)> glow = new List<(Material, Color)>();
        readonly List<(Material copy, Color emission)> daylight = new List<(Material, Color)>();
        readonly List<(Transform slot, Vector3 point)> lampPoints = new List<(Transform, Vector3)>();
        readonly List<Light> pool = new List<Light>();
        readonly Dictionary<WheelVehicleController, Light[]> headlights = new Dictionary<WheelVehicleController, Light[]>();
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static FieldInfo weightField;
        float darkness = -1f, lampTimer, carTimer;
        bool lightsOn;

        void Start()
        {
            var map = new Dictionary<Material, Material>();
            foreach (var m in nightWindows)
                if (m && !map.ContainsKey(m)) { var c = new Material(m) { name = m.name + " (night)" }; map[m] = c; windows.Add(c); }
            foreach (var m in nightGlow)
                if (m && !map.ContainsKey(m) && m.HasProperty(EmissionId))
                {
                    var c = new Material(m) { name = m.name + " (night)" };
                    map[m] = c;
                    glow.Add((c, m.GetColor(EmissionId)));
                }
            foreach (var m in dayWindows)
                if (m && !map.ContainsKey(m) && m.HasProperty(EmissionId))
                {
                    var c = new Material(m) { name = m.name + " (daylight)" };
                    map[m] = c;
                    daylight.Add((c, m.GetColor(EmissionId)));
                }
            if (map.Count > 0)
                foreach (var r in UnityCompat.FindAll<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    var shared = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < shared.Length; i++)
                        if (shared[i] && map.TryGetValue(shared[i], out var copy)) { shared[i] = copy; changed = true; }
                    if (changed) r.sharedMaterials = shared;
                }

            // Street lamps: the lamp head (or the top of an imported lamp model).
            foreach (var slot in UnityCompat.FindAll<PlaceholderSlot>(true))
            {
                if (slot.kind != PlaceholderKind.StreetLamp) continue;
                var head = slot.transform.Find("LampHead");
                lampPoints.Add((slot.transform, head ? head.position - Vector3.up * 0.2f : slot.transform.position + Vector3.up * 5.6f));
            }
            // Fewer real lamp lights on lower graphics levels.
            int levels = QualitySettings.names.Length, q = QualitySettings.GetQualityLevel();
            float tier = levels > 1 ? q / (float)(levels - 1) : 1f;
            int count = Mathf.RoundToInt(Mathf.Lerp(Mathf.Min(6, lampLights), lampLights, tier));
            for (int i = 0; i < Mathf.Min(count, lampPoints.Count); i++)
            {
                var go = new GameObject("StreetLampLight");
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 120f;
                l.innerSpotAngle = 60f;
                l.range = lampRange;
                l.intensity = lampIntensity;
                l.color = lampColour;
                l.shadows = LightShadows.None;
                l.enabled = false;
                pool.Add(l);
            }
            Update();
        }

        void Update()
        {
            HomeInterior.Track();
            float hour = CarSalesServices.DayCycle ? CarSalesServices.DayCycle.HourOfDay : 12f;
            float target = RealismSim.Darkness(hour);
            darkness = darkness < 0f ? target : Mathf.MoveTowards(darkness, target, Time.deltaTime * 0.5f);
            Darkness = darkness;
            var world = CarSalesServices.World;
            var weather = world ? world.Weather : Weather.Clear;
            bool gloomy = weather == Weather.HeavyRain || weather == Weather.Fog;
            bool on = RealismSim.StreetLightsOn(darkness, gloomy) && !InteriorState.Inside;

            float windowGlow = RealismSim.WindowGlow(darkness);
            var windowColour = Color.white * (windowIntensity * windowGlow);
            foreach (var w in windows) if (w) w.SetColor(EmissionId, windowColour);
            float g = on ? 1f : Mathf.Lerp(glowByDay, 1f, darkness * 0.5f);
            foreach (var (copy, emission) in glow) if (copy) copy.SetColor(EmissionId, emission * g);
            var weatherLight = world ? Mathf.Lerp(0.45f, 1f, world.SunFactor) : 1f;
            foreach (var (copy, emission) in daylight)
                if (copy) copy.SetColor(EmissionId, Color.Lerp(emission * weatherLight, new Color(0.02f, 0.03f, 0.06f), darkness));

            SetWeight(nightVolume, InteriorState.Inside ? darkness * 0.3f : darkness);   // indoors the room lights do the work
            float rain = weather == Weather.HeavyRain ? 1f : weather == Weather.Rain ? 0.7f : weather == Weather.Overcast ? 0.35f : weather == Weather.Fog ? 0.5f : 0f;
            SetWeight(rainVolume, Mathf.MoveTowards(GetWeight(rainVolume), rain, Time.deltaTime * 0.2f));

            lampTimer -= Time.deltaTime;
            if (lampTimer <= 0f || on != lightsOn) { lampTimer = 0.5f; lightsOn = on; PlaceLampLights(on); }
            carTimer -= Time.deltaTime;
            if (carTimer <= 0f) { carTimer = 1f; UpdateHeadlights(darkness > 0.25f || gloomy); }
        }

        /// <summary>Moves the light pool to the lamps nearest the camera (sorted by distance, cheap: a few hundred lamps).</summary>
        void PlaceLampLights(bool on)
        {
            var cam = Camera.main;
            if (!on || !cam || pool.Count == 0) { foreach (var l in pool) if (l) l.enabled = false; return; }
            Vector3 c = cam.transform.position;
            var nearest = new List<(float d, int i)>(lampPoints.Count);
            for (int i = 0; i < lampPoints.Count; i++)
                if (lampPoints[i].slot && lampPoints[i].slot.gameObject.activeInHierarchy)   // not on a hidden building level
                    nearest.Add(((lampPoints[i].point - c).sqrMagnitude, i));
            nearest.Sort((a, b) => a.d.CompareTo(b.d));
            for (int k = 0; k < pool.Count; k++)
            {
                var l = pool[k];
                if (!l) continue;
                bool use = k < nearest.Count && nearest[k].d < 220f * 220f;
                l.enabled = use;
                if (use) l.transform.position = lampPoints[nearest[k].i].point;
            }
        }

        void UpdateHeadlights(bool needed)
        {
            foreach (var car in UnityCompat.FindAll<WheelVehicleController>())
                if (car && !headlights.ContainsKey(car)) headlights[car] = MakeHeadlights(car);
            var gone = new List<WheelVehicleController>();
            foreach (var kv in headlights)
            {
                if (!kv.Key) { gone.Add(kv.Key); continue; }
                bool active = needed && kv.Key.isActiveAndEnabled;
                foreach (var l in kv.Value) if (l) l.enabled = active;
            }
            foreach (var k in gone) headlights.Remove(k);
        }

        Light[] MakeHeadlights(WheelVehicleController car)
        {
            var b = car.BodyBounds;
            float halfW = b.size.x > 0.1f ? b.size.x * 0.32f : 0.6f;
            float front = b.size.z > 0.1f ? b.center.z + b.size.z * 0.5f : 2.2f;
            float y = b.size.y > 0.1f ? b.min.y + b.size.y * 0.38f : 0.7f;
            var lights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "Headlight_L" : "Headlight_R");
                go.transform.SetParent(car.transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -halfW : halfW, y, front);
                go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);   // dipped beam
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = headlightAngle;
                l.innerSpotAngle = headlightAngle * 0.45f;
                l.range = headlightRange;
                l.intensity = headlightIntensity;
                l.color = new Color(1f, 0.96f, 0.88f);
                l.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;   // one shadow is enough
                l.enabled = false;
                lights[i] = l;
            }
            return lights;
        }

        // URP's Volume.weight, set by reflection so the runtime code does not depend on the URP package.
        static void SetWeight(Behaviour volume, float w)
        {
            if (!volume) return;
            var f = WeightField(volume);
            if (f != null) f.SetValue(volume, Mathf.Clamp01(w));
        }

        static float GetWeight(Behaviour volume)
        {
            if (!volume) return 0f;
            var f = WeightField(volume);
            return f != null ? (float)f.GetValue(volume) : 0f;
        }

        static FieldInfo WeightField(Behaviour volume)
        {
            if (weightField == null || weightField.DeclaringType != volume.GetType())
                weightField = volume.GetType().GetField("weight", BindingFlags.Public | BindingFlags.Instance);
            return weightField;
        }

        void OnDestroy()
        {
            foreach (var w in windows) if (w) Destroy(w);
            foreach (var (copy, _) in glow) if (copy) Destroy(copy);
            foreach (var (copy, _) in daylight) if (copy) Destroy(copy);
            Darkness = 0f;
        }
    }

    /// <summary>Phase 13c: true while the player is inside a walk-in interior (no rain, fog or street lights there).</summary>
    public static class InteriorState
    {
        public static bool Inside { get; set; }
        public static string PropertyId { get; set; } = "";
    }
}
