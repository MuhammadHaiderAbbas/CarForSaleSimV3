using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 12: every toast is kept (latest 50) for the phone's Notifications app - "Vehicle sold, 2m ago".
    /// Not saved: it's this session's feed.
    /// </summary>
    public static class NotificationLog
    {
        public sealed class Entry { public ToastType type; public string title, message; public float realTime; public int day; public float hour; }
        const int Max = 50;
        static readonly List<Entry> entries = new List<Entry>();
        public static IReadOnlyList<Entry> Entries => entries;
        public static int Unseen { get; private set; }
        public static event System.Action OnChanged;

        public static void Add(ToastType type, string title, string message)
        {
            var day = CarSalesServices.DayCycle;
            entries.Insert(0, new Entry { type = type, title = title, message = message, realTime = Time.realtimeSinceStartup, day = day ? day.CurrentDay : 1, hour = day ? day.HourOfDay : 0f });
            if (entries.Count > Max) entries.RemoveAt(entries.Count - 1);
            Unseen++;
            OnChanged?.Invoke();
        }

        public static void MarkSeen() { Unseen = 0; OnChanged?.Invoke(); }
        public static void Clear() { entries.Clear(); Unseen = 0; OnChanged?.Invoke(); }

        /// <summary>"just now", "2m ago", "1h ago" (real time).</summary>
        public static string Ago(Entry e)
        {
            float s = Time.realtimeSinceStartup - e.realTime;
            if (s < 45f) return "just now";
            if (s < 3600f) return Mathf.RoundToInt(s / 60f) + "m ago";
            return Mathf.RoundToInt(s / 3600f) + "h ago";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { entries.Clear(); Unseen = 0; OnChanged = null; }
    }
}
