using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Adds a decaying positional jolt on top of whatever the chase camera framed.
    /// <para>
    /// It runs after <see cref="CameraRig"/> and offsets the transform the rig has already written,
    /// rather than feeding the rig a target. Doing it the other way round would make the rig's
    /// smoothing chase the shake and turn a sharp jolt into a slow wobble.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _amplitude = 0.35f;
        [SerializeField, Min(0f)] private float _duration = 0.28f;

        [SerializeField, Min(0f), Tooltip("Shakes per second. Higher reads as a sharper impact.")]
        private float _frequency = 26f;

        private float _remaining;
        private Vector3 _seed;

        /// <summary>Starts a shake, or restarts one already running.</summary>
        public void Shake(float scale = 1f)
        {
            _remaining = _duration * Mathf.Max(0.01f, scale);

            // A fresh offset into the noise per shake, so two hits in a row do not trace the same
            // path and read as one long rattle.
            _seed = new Vector3(Random.value, Random.value, Random.value) * 100f;
        }

        private void LateUpdate()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;

            float falloff = _duration > 0f ? Mathf.Clamp01(_remaining / _duration) : 0f;
            float strength = _amplitude * falloff * falloff;
            float t = Time.time * _frequency;

            // Perlin rather than white noise: neighbouring frames stay related, so the camera
            // vibrates instead of teleporting.
            var offset = new Vector3(
                Mathf.PerlinNoise(_seed.x + t, 0f) * 2f - 1f,
                Mathf.PerlinNoise(_seed.y + t, 0f) * 2f - 1f,
                Mathf.PerlinNoise(_seed.z + t, 0f) * 2f - 1f);

            transform.position += offset * strength;
        }
    }
}
