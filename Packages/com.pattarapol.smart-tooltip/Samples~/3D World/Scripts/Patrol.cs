using UnityEngine;

namespace SmartTooltip.Samples
{
    public sealed class Patrol : MonoBehaviour
    {
        [SerializeField] Vector3 from;
        [SerializeField] Vector3 to;
        [SerializeField] float secondsPerTrip = 4f;

        void Update()
        {
            var t = Mathf.PingPong(Time.time / secondsPerTrip, 1f);
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
        }
    }
}
