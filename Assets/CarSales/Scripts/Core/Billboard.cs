using UnityEngine;

namespace CarSales
{
    /// <summary>Keeps world-space text (price tags, speech bubbles) facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
