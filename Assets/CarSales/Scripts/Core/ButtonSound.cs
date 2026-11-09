using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Plays a click sound when the button on this GameObject is pressed.</summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSound : MonoBehaviour
    {
        void Awake() => GetComponent<Button>().onClick.AddListener(() => CarSalesServices.PlaySfx(Sfx.Click, 0.6f));
    }
}
