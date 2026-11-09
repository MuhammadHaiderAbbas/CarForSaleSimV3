using System;

namespace CarSales.Sim
{
    /// <summary>
    /// Phase 13b: the rules behind the realistic look - how dark it is, how hazy the air is, what colour the haze is,
    /// when street lights and windows come on and how wet the ground is. Pure maths so it is unit-tested.
    /// </summary>
    public static class RealismSim
    {
        public const float Sunrise = 6f, Sunset = 20.5f;
        /// <summary>Half-length of twilight in hours (light fades over ~1.5 h around sunrise and sunset).</summary>
        public const float Twilight = 0.75f;

        static float Smooth(float e0, float e1, float x)
        {
            float t = (x - e0) / (e1 - e0);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - 2f * t);
        }

        static float Wrap(float hour) { hour %= 24f; return hour < 0f ? hour + 24f : hour; }

        /// <summary>0 = full daylight, 1 = night.</summary>
        public static float Darkness(float hour)
        {
            hour = Wrap(hour);
            float up = Smooth(Sunrise - Twilight, Sunrise + Twilight, hour);
            float down = Smooth(Sunset - Twilight, Sunset + Twilight, hour);
            return 1f - up * (1f - down);
        }

        /// <summary>Golden-hour strength (0..1): peaks as the sun rises and sets.</summary>
        public static float GoldenHour(float hour)
        {
            float d = Darkness(hour);
            return 4f * d * (1f - d);
        }

        /// <summary>
        /// Atmospheric haze as exponential fog density: misty at dawn, clearest in the afternoon, a little thicker at
        /// night so distant buildings melt into the dark. Values keep a 1 km town readable.
        /// </summary>
        public static float Haze(float hour)
        {
            hour = Wrap(hour);
            float morning = Smooth(4.5f, 6.5f, hour) * (1f - Smooth(7.5f, 10.5f, hour));   // dawn mist
            float night = Darkness(hour);
            return 0.0007f + morning * 0.0016f + night * 0.0006f;
        }

        /// <summary>Fog density for the weather and time (whichever is thicker).</summary>
        public static float FogDensity(float weatherFog, float hour) => Math.Max(weatherFog, Haze(hour));

        /// <summary>Haze colour: blue-grey by day, warm at golden hour, deep blue at night, dimmed by cloud.</summary>
        public static (float r, float g, float b) FogColour(float hour, float sunFactor)
        {
            float d = Darkness(hour), gold = GoldenHour(hour);
            float bright = 0.35f + 0.65f * Math.Max(0f, Math.Min(1f, sunFactor));
            float r = 0.64f * bright, g = 0.7f * bright, b = 0.78f * bright;
            // Golden hour: warm the haze.
            r += (0.78f - r) * gold * 0.6f; g += (0.6f - g) * gold * 0.6f; b += (0.48f - b) * gold * 0.6f;
            // Night: deep blue-black.
            r += (0.035f - r) * d; g += (0.045f - g) * d; b += (0.075f - b) * d;
            return (r, g, b);
        }

        /// <summary>Street lights switch on at dusk (earlier in fog or heavy rain) and off after dawn.</summary>
        public static bool StreetLightsOn(float darkness, bool gloomyWeather) => darkness > (gloomyWeather ? 0.12f : 0.3f);

        /// <summary>How brightly lit windows glow (0..1).</summary>
        public static float WindowGlow(float darkness) => Smooth(0.2f, 0.75f, darkness);

        /// <summary>
        /// Moves ground wetness towards the weather: soaked in about 20 game minutes of rain, dry again in about two hours.
        /// <paramref name="target"/> is 1 in heavy rain, 0.8 in rain, 0 otherwise.
        /// </summary>
        public static float StepWetness(float wetness, float target, float gameHours)
        {
            if (gameHours <= 0f) return wetness;
            float rate = target > wetness ? 3f : 0.5f;   // per game hour
            float next = target > wetness ? Math.Min(target, wetness + rate * gameHours) : Math.Max(target, wetness - rate * gameHours);
            return Math.Max(0f, Math.Min(1f, next));
        }

        public static float WetTarget(bool raining, bool heavy) => heavy ? 1f : raining ? 0.8f : 0f;

        /// <summary>Wet surfaces: albedo multiplier (darker) and smoothness (shinier, puddle-like).</summary>
        public static float WetDarken(float wetness) => 1f - 0.32f * wetness;
        public static float WetSmoothness(float drySmoothness, float wetness) => drySmoothness + (0.9f - drySmoothness) * wetness;
    }
}
