using TMPro;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// One world-space popup: rises, fades and turns itself off.
    /// <para>
    /// It runs its own animation in <c>Update</c> rather than through the central presentation pass.
    /// A popup is a fire-and-forget decoration with no simulation state behind it, so routing it
    /// through the game loop would add a subscription and a lifetime for no benefit.
    /// </para>
    /// </summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _lifetime = 1.1f;
        [SerializeField] private float _riseDistance = 2.4f;
        [SerializeField] private float _driftRadius = 0.35f;

        private Vector3 _origin;
        private Vector3 _drift;
        private float _elapsed;
        private Camera _camera;

        public bool IsFinished => _elapsed >= _lifetime;

        public void Play(string text, Color color, Vector3 worldPosition, Camera viewer)
        {
            _label.text = text;
            _label.color = color;

            _origin = worldPosition;
            _camera = viewer;
            _elapsed = 0f;

            // A small sideways offset per popup so several hits in the same spot do not stack into
            // one illegible smear.
            Vector2 jitter = Random.insideUnitCircle * _driftRadius;
            _drift = new Vector3(jitter.x, 0f, jitter.y);

            transform.position = _origin + _drift;
            Face();
        }

        private void Update()
        {
            if (IsFinished)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            // Decelerating rise and a late fade: the number is fully readable for most of its life
            // and only disappears once the eye has had time to land on it.
            transform.position = _origin + _drift + Vector3.up * (_riseDistance * Mathf.Sqrt(t));

            Color color = _label.color;
            color.a = 1f - Mathf.Clamp01((t - 0.55f) / 0.45f);
            _label.color = color;

            Face();
        }

        private void Face()
        {
            if (_camera != null)
            {
                transform.rotation = _camera.transform.rotation;
            }
        }
    }
}
