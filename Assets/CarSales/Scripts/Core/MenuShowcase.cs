using UnityEngine;

namespace CarSales
{
    /// <summary>Slowly spins a car model and changes its paint colour (main menu decoration).</summary>
    public class MenuShowcase : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 20f;
        [SerializeField] float colorChangeSeconds = 4f;
        [SerializeField] Color[] colors = { new Color(0.75f, 0.05f, 0.05f), new Color(0.05f, 0.2f, 0.6f), new Color(0.95f, 0.6f, 0.1f), Color.white };
        CarVisual visual;
        float timer;
        int index;

        void Start()
        {
            visual = GetComponentInChildren<CarVisual>();
            if (visual && colors.Length > 0) visual.Apply(colors[0], 0f);
        }

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
            if (!visual || colors.Length < 2) return;
            timer += Time.deltaTime;
            if (timer < colorChangeSeconds) return;
            timer = 0f;
            index = (index + 1) % colors.Length;
            visual.Apply(colors[index], 0f);
        }
    }
}
