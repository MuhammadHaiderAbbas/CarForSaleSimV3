using System.Collections.Generic;
using CarSales.Sim;
using Unity.AI.Navigation;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 11b: the three looks of one building. Build Everything makes "Level1", "Level2" and "Level3" children
    /// (completely different buildings) and this switches between them as you rebuild; while the builders are in,
    /// scaffolding, fences, a crane and a sign go up around it. After a switch the site's NavMesh is rebuilt so
    /// customers walk round the new walls.
    /// </summary>
    public class BuildingLevelVisual : MonoBehaviour
    {
        public static readonly List<BuildingLevelVisual> All = new List<BuildingLevelVisual>();
        public string buildingId;
        public Transform[] levels = new Transform[3];
        [Tooltip("Ground footprint of the largest level (local), used for the building site.")]
        public Vector3 siteCentre = new Vector3(0f, 0f, 17f), siteSize = new Vector3(30f, 8f, 18f);

        GameObject construction;
        int shownLevel = -1;
        static Material siteMat, poleMat, signMat;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);
        void Start() => Apply(instant: true);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); siteMat = poleMat = signMat = null; }

        public void Apply(bool instant)
        {
            var bm = CarSalesServices.Buildings;
            int level = bm ? bm.Level(buildingId) : 1;
            bool building = bm && bm.Constructing(buildingId);
            for (int i = 0; i < levels.Length; i++) if (levels[i]) levels[i].gameObject.SetActive(i == level - 1);
            if (levels.Length >= level && !levels[level - 1] && levels.Length > 0 && levels[0]) levels[0].gameObject.SetActive(true);   // a missing look: keep level 1
            ShowConstruction(building);
            if (shownLevel != level)
            {
                bool rebake = shownLevel > 0;
                shownLevel = level;
                if (rebake) RebakeNavMesh();
            }
        }

        void RebakeNavMesh()
        {
            foreach (var s in GetComponentsInChildren<NavMeshSurface>(true))
            {
                s.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;   // imported meshes may not be readable at runtime
                s.BuildNavMesh();
            }
        }

        static Material Mat(ref Material m, Color c)
        {
            if (m) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        GameObject Box(Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Scaffolding on the front, a fence round the plot, a crane and an "under construction" board.</summary>
        void ShowConstruction(bool on)
        {
            if (!on) { if (construction) Destroy(construction); construction = null; return; }
            if (construction) return;
            var fence = Mat(ref siteMat, new Color(0.95f, 0.55f, 0.1f));
            var pole = Mat(ref poleMat, new Color(0.55f, 0.57f, 0.6f));
            var board = Mat(ref signMat, new Color(0.95f, 0.85f, 0.15f));
            construction = new GameObject("Construction");
            var t = construction.transform;
            t.SetParent(transform, false);
            var c = siteCentre; var s = siteSize;
            float front = c.z - s.z / 2f - 1.5f, back = c.z + s.z / 2f + 1f, left = c.x - s.x / 2f - 1f, right = c.x + s.x / 2f + 1f;
            // Fence (orange panels with gaps for the door).
            for (float x = left; x <= right; x += 2.2f)
                if (Mathf.Abs(x - c.x) > 2.5f) Box(t, new Vector3(x, 0.9f, front), new Vector3(2f, 1.8f, 0.06f), fence, true);
            for (float z = front; z <= back; z += 2.2f) { Box(t, new Vector3(left, 0.9f, z), new Vector3(0.06f, 1.8f, 2f), fence, true); Box(t, new Vector3(right, 0.9f, z), new Vector3(0.06f, 1.8f, 2f), fence, true); }
            // Scaffolding across the front: poles and planks every 2 m up.
            float h = Mathf.Max(4f, s.y);
            for (float x = c.x - s.x / 2f; x <= c.x + s.x / 2f + 0.01f; x += 3f) Box(t, new Vector3(x, h / 2f, front + 0.9f), new Vector3(0.08f, h, 0.08f), pole);
            for (float y = 2f; y < h; y += 2f) Box(t, new Vector3(c.x, y, front + 0.9f), new Vector3(s.x, 0.06f, 0.7f), board);
            // Crane behind the building.
            var crane = new Vector3(right - 2f, 0f, back + 3f);
            Box(t, crane + new Vector3(0f, h + 6f, 0f), new Vector3(1f, 2f * h + 12f, 1f), board);
            Box(t, crane + new Vector3(-s.x * 0.4f, 2f * h + 11.5f, 0f), new Vector3(s.x * 0.9f, 0.6f, 0.6f), board);
            Box(t, crane + new Vector3(-s.x * 0.75f, h + 6f, 0f), new Vector3(0.05f, 2f * h + 10f, 0.05f), pole);
            // Sign.
            Box(t, new Vector3(c.x + 4.5f, 1.6f, front - 0.2f), new Vector3(3f, 1.2f, 0.08f), board);
            var label = new GameObject("Label").AddComponent<TMPro.TextMeshPro>();
            label.transform.SetParent(t, false);
            label.transform.localPosition = new Vector3(c.x + 4.5f, 1.6f, front - 0.26f);
            label.text = "UNDER CONSTRUCTION\nReady tomorrow";
            label.fontSize = 3f; label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = Color.black;
            label.rectTransform.sizeDelta = new Vector2(2.8f, 1f);
        }
    }
}
