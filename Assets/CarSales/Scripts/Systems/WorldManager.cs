using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Foundation;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// World simulation (spec §6.13): hourly weather (rain, fog, overcast...) with rain particles, fog, dimmer sun,
    /// shiny wet roads, less grip and fewer customers; Sunday hours and bank holidays; a daily fuel price; random
    /// events (police checks, breakdowns, road works, theft attempts) and open days you can book.
    /// Saved as module "world". Added by CarSalesBootstrap if missing.
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        WorldState state = new WorldState();
        readonly System.Random rng = new System.Random();
        ParticleSystem rain;
        readonly List<Material> roadMats = new List<Material>();
        readonly List<float> roadDry = new List<float>();
        readonly List<Color> roadDryColour = new List<Color>();
        /// <summary>Outdoor materials that get darker and shinier in the rain (phase 13: roads, pavements, roofs, brick).</summary>
        static readonly string[] WettableMaterials = { "M_Asphalt", "M_Sidewalk", "M_Concrete", "M_Roof", "M_RoofTiles", "M_RoofSlate", "M_Brick", "M_FieldSoil", "M_LotLine", "M_YellowLine" };
        float lastHour = -1f;
        float wetness, sunFactor = 1f, policeHour = -1f;
        int policeDay = -1;

        public WorldState State => state;
        public Weather Weather => state.Weather;
        public float FuelIndex => state.fuelIndex;
        public float Grip => WorldSim.Grip(state.Weather);
        public float SunFactor => sunFactor;
        public event Action OnChanged;

        /// <summary>Customers turning up, from weather, the day of the week, holidays, road works and open days.</summary>
        public float Footfall
        {
            get
            {
                int day = CarRecords.Today;
                float f = WorldSim.Footfall(state.Weather) * WorldSim.DayFootfall(day);
                if (state.roadWorksUntil >= day) f *= WorldSim.RoadWorksFootfall;
                if (state.openDay == day) f *= WorldSim.OpenDayFootfall;
                return f;
            }
        }

        void Awake() => CarSalesServices.World = this;
        void OnEnable() => EventBus.Subscribe<DayStarted>(OnDayStarted);
        void OnDisable() => EventBus.Unsubscribe<DayStarted>(OnDayStarted);

        void Start()
        {
            if (CarSalesServices.DayCycle) CarSalesServices.DayCycle.OnTimeChanged += OnTime;
            CollectRoads();
        }

        void OnDestroy()
        {
            if (CarSalesServices.DayCycle) CarSalesServices.DayCycle.OnTimeChanged -= OnTime;
            foreach (var m in roadMats) if (m) Destroy(m);
            roadMats.Clear(); roadDry.Clear(); roadDryColour.Clear();
        }

        public void SetState(WorldState s)
        {
            state = s ?? new WorldState();
            if (state.fuelIndex <= 0f) state.fuelIndex = 1f;
            ApplyWeather(instant: true);
            OnChanged?.Invoke();
        }

        public void ResetState() => SetState(new WorldState());

        /// <summary>Debug (F4): switch straight to the next kind of weather to check how the town looks in it.</summary>
        public void DebugNextWeather()
        {
            state.weather = ((int)state.Weather + 1) % 6;
            ApplyWeather(false);
            CarSalesServices.UI.Notify("Weather: " + WorldSim.Name(state.Weather));
        }

        // ------------------------------------------------------------------ WEATHER
        void OnTime(float hour)
        {
            int key = CarRecords.Today * 24 + Mathf.FloorToInt(hour % 24f);
            if (key != state.weatherHour)
            {
                state.weatherHour = key;
                var before = state.Weather;
                state.weather = (int)WorldSim.Next(state.Weather, GameCalendar.QuarterInYear(CarRecords.Today), Mathf.FloorToInt(hour % 24f), rng);
                if (state.Weather != before)
                {
                    ApplyWeather(instant: false);
                    if (WorldSim.IsWet(state.Weather) && !WorldSim.IsWet(before)) CarSalesServices.UI?.Notify("It's started to rain: roads are slippery and fewer customers come out.");
                    OnChanged?.Invoke();
                }
                // Rain makes the cars on your lots dirty.
                if (WorldSim.IsWet(state.Weather) && CarSalesServices.Inventory)
                    foreach (var c in CarSalesServices.Inventory.OwnedCars.Where(x => x.location == CarLocation.Spot))
                        c.cleanliness = Mathf.Max(0f, c.cleanliness - (state.Weather == Weather.HeavyRain ? 2f : 1f));
            }
            CheckPolice(hour % 24f);
        }

        void ApplyWeather(bool instant)
        {
            foreach (var w in UnityCompat.FindAll<WheelVehicleController>()) w.SetEffects(w.Effects);
            if (instant) { wetness = WorldSim.IsWet(state.Weather) ? 1f : 0f; sunFactor = WorldSim.SunLight(state.Weather); }
        }

        void CollectRoads()
        {
            // Wet ground: one runtime copy of each outdoor material, made darker and shinier when it rains.
            // Includes hidden building levels so a rebuilt dealership gets wet too.
            var copies = new Dictionary<Material, Material>();
            foreach (var r in UnityCompat.FindAll<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var shared = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (!m || !IsWettable(m.name)) continue;
                    if (!copies.TryGetValue(m, out var copy))
                    {
                        copy = new Material(m) { name = m.name + " (wet)" };
                        copies[m] = copy;
                        roadMats.Add(copy);
                        roadDry.Add(copy.HasProperty("_Smoothness") ? copy.GetFloat("_Smoothness") : 0.3f);
                        roadDryColour.Add(copy.HasProperty("_BaseColor") ? copy.GetColor("_BaseColor") : Color.white);
                    }
                    shared[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = shared;
            }
        }

        static bool IsWettable(string name)
        {
            foreach (var w in WettableMaterials)
                if (name == w || name.StartsWith(w + " ")) return true;
            return false;
        }

        void Update()
        {
            bool wet = WorldSim.IsWet(state.Weather);
            float hour = CarSalesServices.DayCycle ? CarSalesServices.DayCycle.HourOfDay : 12f;
            // Wetness follows game time (soaked in ~20 game minutes, dry in ~2 hours); falls back to real time with no clock.
            float gameHours = lastHour < 0f ? 0f : Mathf.Repeat(hour - lastHour, 24f);
            if (gameHours > 6f) gameHours = 0f;   // slept / loaded: jump
            lastHour = hour;
            float target = RealismSim.WetTarget(wet, state.Weather == Weather.HeavyRain);
            wetness = CarSalesServices.DayCycle ? RealismSim.StepWetness(wetness, target, gameHours)
                                                : Mathf.MoveTowards(wetness, target, Time.deltaTime / (wet ? 60f : 240f));
            sunFactor = Mathf.MoveTowards(sunFactor, WorldSim.SunLight(state.Weather), Time.deltaTime * 0.05f);
            for (int i = 0; i < roadMats.Count; i++)
            {
                var m = roadMats[i];
                if (!m) continue;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", RealismSim.WetSmoothness(roadDry[i], wetness));
                if (m.HasProperty("_BaseColor"))
                {
                    var c = roadDryColour[i] * RealismSim.WetDarken(wetness);
                    c.a = roadDryColour[i].a;
                    m.SetColor("_BaseColor", c);
                }
            }

            // Fog: the weather's fog or the time of day's haze (whichever is thicker), coloured for the time of day.
            // Indoors (walk-in home) the air is clear.
            float fog = InteriorState.Inside ? 0f : RealismSim.FogDensity(WorldSim.FogDensity(state.Weather), hour);
            RenderSettings.fog = fog > 0f || RenderSettings.fogDensity > 0.00005f;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, fog, Time.deltaTime * 0.002f);
            var fc = RealismSim.FogColour(hour, sunFactor);
            RenderSettings.fogColor = new Color(fc.r, fc.g, fc.b);

            UpdateRain(wet && !InteriorState.Inside);
        }

        void UpdateRain(bool wet)
        {
            var cam = Camera.main;
            if (!cam) return;
            if (!rain)
            {
                if (!wet) return;
                rain = CreateRain();
            }
            rain.transform.position = cam.transform.position + Vector3.up * 12f;
            var em = rain.emission;
            em.rateOverTime = !wet ? 0f : state.Weather == Weather.HeavyRain ? 2600f : state.Weather == Weather.Rain ? 1100f : 0f;
            if (!wet && rain.particleCount > 0 && InteriorState.Inside) rain.Clear();
        }

        static ParticleSystem CreateRain()
        {
            var go = new GameObject("Rain");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 18f;
            main.startSize = 0.04f;
            main.maxParticles = 6000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.8f, 0.9f, 0.45f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 0.5f, 40f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 6f;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            r.sharedMaterial = new Material(shader) { name = "RainDrops" };
            ps.Play();
            return ps;
        }

        // ------------------------------------------------------------------ DAILY & EVENTS
        void OnDayStarted(DayStarted e)
        {
            state.fuelIndex = WorldSim.StepFuel(state.fuelIndex, rng);
            state.fuelHistory.Add(state.fuelIndex);
            while (state.fuelHistory.Count > 60) state.fuelHistory.RemoveAt(0);
            if (WorldSim.IsBankHoliday(e.Day)) Log("Bank holiday: expect a busy day!", toast: true);
            else if (GameCalendar.WeekdayOf(e.Day) == Weekday.Sunday) Log($"Sunday: dealerships open {CarSalesServices.DayCycle.OpenHour:00}:00-{CarSalesServices.DayCycle.CloseHour:00}:00.", toast: false);

            if (rng.NextDouble() < WorldSim.EventChance(WorldEventType.PoliceCheck)) { policeDay = e.Day; policeHour = 9f + (float)rng.NextDouble() * 9f; }
            if (rng.NextDouble() < WorldSim.EventChance(WorldEventType.Breakdown)) Breakdown();
            if (state.roadWorksUntil < e.Day && rng.NextDouble() < WorldSim.EventChance(WorldEventType.RoadWorks))
            {
                state.roadWorksUntil = e.Day + 1;
                Log("Road works on the main road today and tomorrow: fewer customers get through (-20%).", toast: true);
            }
            float theft = CarSalesServices.Business ? CarSalesServices.Business.TheftFactor : 1f;   // phase 9: CCTV & alarm
            if (rng.NextDouble() < WorldSim.EventChance(WorldEventType.TheftAttempt) * theft) Theft();
            if (rng.NextDouble() < 0.03f * theft) Vandalism();
            if (state.openDay == e.Day) Log("It's your open day: balloons up, expect lots of visitors!", toast: true);
            OnChanged?.Invoke();
        }

        void Log(string msg, bool toast)
        {
            state.eventLog.Insert(0, $"Day {CarRecords.Today}: {msg}");
            while (state.eventLog.Count > 30) state.eventLog.RemoveAt(state.eventLog.Count - 1);
            if (toast) CarSalesServices.UI?.Toast(ToastType.Info, "Around town", msg);
        }

        /// <summary>A car in stock with a severe fault breaks down: it can't be sold until it's fixed.</summary>
        void Breakdown()
        {
            var inv = CarSalesServices.Inventory;
            var car = inv ? inv.OwnedCars.Where(c => !c.personal && c.faults != null && c.faults.Any(f => f.severity >= 0.6f)).OrderBy(_ => rng.Next()).FirstOrDefault() : null;
            if (car == null) return;
            var f = car.faults.OrderByDescending(x => x.severity).First();
            f.known = true;
            f.severity = 1f;
            FaultModel.ApplyHealthCaps(car.components, car.faults);
            CarParts.Recalculate(car);
            if (car.state == CarState.Listed) car.state = CarState.NeedsRepair;
            var d = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.GetDisplay(car.instanceId) : null;
            if (d) d.Refresh();
            Log($"The {car.Name} broke down on your lot: {f.Def?.Name}. It's off sale until it's fixed.", toast: true);
        }

        /// <summary>Someone tries to break into a car on your lots: a bit of body damage unless a staff member is around.</summary>
        void Theft()
        {
            bool staff = CarSalesServices.Staff && CarSalesServices.Staff.Employees.Count > 0;
            var inv = CarSalesServices.Inventory;
            var car = inv ? inv.OwnedCars.Where(c => c.location == CarLocation.Spot).OrderBy(_ => rng.Next()).FirstOrDefault() : null;
            if (car == null) return;
            if (staff && rng.NextDouble() < 0.6) { Log($"Your staff scared off someone trying to break into the {car.Name}.", toast: true); return; }
            int before = CarValueCalculator.RepairCost(car);
            CarParts.Damage(car, ImpactZone.Left, 18f);
            var d = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.GetDisplay(car.instanceId) : null;
            if (d) d.Refresh();
            Log($"Someone tried to break into the {car.Name} overnight: a damaged door.", toast: true);
            if (CarSalesServices.Business) CarSalesServices.Business.Claim(Mathf.Max(0, CarValueCalculator.RepairCost(car) - before), false, $"break-in damage to the {car.Name}");
        }

        /// <summary>Phase 9: forecourt vandalism (scratched paint) - comprehensive insurance covers it.</summary>
        void Vandalism()
        {
            var inv = CarSalesServices.Inventory;
            var car = inv ? inv.OwnedCars.Where(c => c.location == CarLocation.Spot && !c.personal).OrderBy(_ => rng.Next()).FirstOrDefault() : null;
            if (car == null) return;
            int before = CarValueCalculator.RepairCost(car);
            CarParts.Damage(car, ImpactZone.Right, 12f);
            var d = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.GetDisplay(car.instanceId) : null;
            if (d) d.Refresh();
            Log($"Vandals scratched the {car.Name} on your forecourt overnight.", toast: true);
            if (CarSalesServices.Business) CarSalesServices.Business.Claim(Mathf.Max(0, CarValueCalculator.RepairCost(car) - before), true, $"vandalism on the {car.Name}");
        }

        /// <summary>Police check while you're driving: no valid MOT costs a fine.</summary>
        void CheckPolice(float hour)
        {
            if (policeDay != CarRecords.Today || hour < policeHour) return;
            var drive = CarSalesServices.TestDrive;
            if (!drive || !drive.IsDriving || !drive.CurrentCar) return;
            policeDay = -1;
            var car = drive.CurrentCar.Data;
            int fine = WorldSim.PoliceFineFor(CarRecords.MotValid(car));
            if (fine > 0)
            {
                CarSalesServices.Economy.ForceSpend(fine, TxCategory.Fines, $"Police: no MOT - {car.Name}", car.instanceId);
                Log($"Police check: the {car.Name} has no valid MOT. Fined {UIFactory.Money(fine)}.", toast: true);
            }
            else Log("Police check: all in order, drive on.", toast: true);
        }

        /// <summary>Book an open day for tomorrow: +60% customers for the fee.</summary>
        public bool BookOpenDay()
        {
            if (state.openDay > CarRecords.Today) { CarSalesServices.UI.Notify($"Your open day is already booked for day {state.openDay}."); return false; }
            if (!CarSalesServices.Economy.TrySpend(WorldSim.OpenDayCost, TxCategory.Other, "Open day (balloons, adverts, coffee)")) { CarSalesServices.UI.Error("Not enough money."); return false; }
            state.openDay = CarRecords.Today + 1;
            CarSalesServices.UI.Success("Open day booked", $"Tomorrow (day {state.openDay}) expect about 60% more customers. Have cars listed!");
            OnChanged?.Invoke();
            return true;
        }

        public string Describe()
        {
            int day = CarRecords.Today;
            return WorldSim.Name(state.Weather) + (WorldSim.IsBankHoliday(day) ? "  -  bank holiday" : "") + (state.openDay == day ? "  -  open day" : "") +
                   (state.roadWorksUntil >= day ? "  -  road works" : "");
        }
    }
}
