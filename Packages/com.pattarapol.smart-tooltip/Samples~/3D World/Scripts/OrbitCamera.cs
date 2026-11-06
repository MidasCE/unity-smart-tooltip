using UnityEngine;

namespace SmartTooltip.Samples
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        [SerializeField] Vector3 center;
        [SerializeField] float radius = 12f;
        [SerializeField] float height = 5f;
        [SerializeField] float degreesPerSecond = 12f;

        float angle;

        // Update, not LateUpdate, so the overlay reads the final camera position.
        void Update()
        {
            angle += degreesPerSecond * Time.deltaTime;
            var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, height, -radius);
            transform.position = center + offset;
            transform.LookAt(center);
        }
    }
}
