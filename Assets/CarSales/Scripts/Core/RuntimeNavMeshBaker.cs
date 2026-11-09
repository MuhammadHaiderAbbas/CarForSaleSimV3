using Unity.AI.Navigation;
using UnityEngine;

namespace CarSales
{
    /// <summary>Safety net: bakes the NavMeshSurface at runtime if no baked data exists.</summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        void Awake()
        {
            var surface = GetComponent<NavMeshSurface>();
            if (surface.navMeshData == null) surface.BuildNavMesh();
        }
    }
}
