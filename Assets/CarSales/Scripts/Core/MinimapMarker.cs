using UnityEngine;

namespace CarSales
{
    /// <summary>A coloured dot above an object that only the minimap camera sees (layer 22).</summary>
    public class MinimapMarker : MonoBehaviour
    {
        public const int MinimapLayer = 22;

        [SerializeField] Color color = Color.white;
        [SerializeField] float size = 2.4f;
        [SerializeField] float height = 30f;
        SpriteRenderer dot;

        public static MinimapMarker Attach(GameObject target, Color color, float size = 2.4f)
        {
            var m = target.GetComponent<MinimapMarker>();
            if (!m) m = target.AddComponent<MinimapMarker>();
            m.color = color;
            m.size = size;
            m.Apply();
            return m;
        }

        void Awake() => Create();

        void Create()
        {
            if (dot) return;
            var go = new GameObject("MinimapDot") { layer = MinimapLayer };
            go.transform.SetParent(transform, false);
            dot = go.AddComponent<SpriteRenderer>();
            dot.sprite = UIFactory.Circle;
            dot.sortingOrder = 10;
            Apply();
        }

        void Apply()
        {
            if (!dot) return;
            dot.color = color;
            dot.transform.localScale = Vector3.one * (size / 1.28f);   // sprite is 1.28 units wide
        }

        public void SetColor(Color c) { color = c; Apply(); }
        public void SetVisible(bool visible) { if (dot) dot.enabled = visible; }

        void LateUpdate()
        {
            if (!dot) return;
            dot.transform.position = transform.position + Vector3.up * height;
            dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
