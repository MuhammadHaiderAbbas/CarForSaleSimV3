using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Finds every object of a type, on any Unity 6 version. Newer versions deprecate the sort-mode overload of
    /// FindObjectsByType, older ones don't have the replacement yet; this one call works on both without warnings.
    /// </summary>
    public static class UnityCompat
    {
        public static T[] FindAll<T>(bool includeInactive = false) where T : Object
        {
#pragma warning disable CS0618
            return includeInactive
                ? Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                : Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#pragma warning restore CS0618
        }
    }
}
