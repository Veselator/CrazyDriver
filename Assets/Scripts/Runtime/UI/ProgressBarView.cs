using UnityEngine;

namespace CrazyDriver.UI
{
    /// <summary>
    /// A bar that fills by sliding a full-size visual underneath a mask.
    /// <para>
    /// The obvious alternative, <c>Image.fillAmount</c>, squeezes the sprite as the value drops, so
    /// any detail on the fill -- a gradient, a bevel, a pattern -- distorts as the bar empties. Here
    /// the fill keeps its true size at all times and the mask simply reveals less of it, which also
    /// means the bar can take any silhouette the mask sprite has rather than being stuck as a
    /// rectangle.
    /// </para>
    /// </summary>
    public sealed class ProgressBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("The masked area. Its width is what one full bar measures.")]
        private RectTransform _viewport;

        [SerializeField, Tooltip("The visual sliding under the mask. Sized to fill the viewport.")]
        private RectTransform _fill;

        [SerializeField, Min(0f), Tooltip("Seconds for the bar to catch up. Zero snaps.")]
        private float _smoothingTime = 0.12f;

        private float _target = 1f;
        private float _displayed = 1f;
        private float _velocity;

        /// <summary>Sets the value the bar animates towards, clamped to 0..1.</summary>
        public void SetNormalized(float value)
        {
            _target = Mathf.Clamp01(value);
        }

        /// <summary>Sets the value with no animation, for a fresh run.</summary>
        public void SnapTo(float value)
        {
            _target = Mathf.Clamp01(value);
            _displayed = _target;
            _velocity = 0f;

            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(_displayed, _target))
            {
                return;
            }

            _displayed = _smoothingTime > 0f
                ? Mathf.SmoothDamp(_displayed, _target, ref _velocity, _smoothingTime)
                : _target;

            Apply();
        }

        private void Apply()
        {
            if (_viewport == null || _fill == null)
            {
                return;
            }

            // Slide left by the hidden fraction; the mask clips whatever leaves the viewport.
            float width = _viewport.rect.width;
            Vector2 position = _fill.anchoredPosition;
            position.x = -(1f - _displayed) * width;
            _fill.anchoredPosition = position;
        }
    }
}
