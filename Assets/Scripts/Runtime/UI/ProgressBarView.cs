using UnityEngine;
using UnityEngine.UI;

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
    /// <para>
    /// The bar also owns its own visibility, because the two things are one animation: it fades in
    /// and sweeps up together at the start of a run, and splitting that across two components would
    /// mean keeping two timers in step for no gain.
    /// </para>
    /// </summary>
    public sealed class ProgressBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("The masked area. Its width is what one full bar measures.")]
        private RectTransform _viewport;

        [SerializeField, Tooltip("The visual sliding under the mask. Sized to fill the viewport.")]
        private Image _fill;

        [SerializeField, Tooltip("Faded to hide the bar outside a run. Optional.")]
        private CanvasGroup _group;

        [SerializeField, Min(0f), Tooltip("Seconds for the bar to catch up. Zero snaps.")]
        private float _smoothingTime = 0.12f;

        private float _target = 1f;
        private float _displayed = 1f;
        private float _velocity;

        // The intro sweep. While it runs it overrides the smoothing above, so the bar rises on a
        // fixed, readable curve instead of the ease-out that ordinary value changes use.
        private float _sweepFrom;
        private float _sweepElapsed;
        private float _sweepDuration;

        private float _fadeFrom;
        private float _fadeTo = 1f;
        private float _fadeElapsed;
        private float _fadeDuration;

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
            _sweepDuration = 0f;

            Apply();
        }

        /// <summary>Hides the bar instantly. Used while the game sits in the menu.</summary>
        public void Hide()
        {
            _fadeDuration = 0f;
            _fadeTo = 0f;

            ApplyAlpha(0f);
        }

        /// <summary>
        /// Fades in and sweeps from <paramref name="from"/> up to the current value.
        /// <para>
        /// The sweep tracks <see cref="SetNormalized"/> rather than a captured end value, so a hit
        /// landing mid-animation still lands the bar in the right place instead of overwriting it a
        /// moment later.
        /// </para>
        /// </summary>
        public void PlayIntro(float from, float sweepDuration, float fadeDuration)
        {
            _sweepFrom = Mathf.Clamp01(from);
            _sweepElapsed = 0f;
            _sweepDuration = Mathf.Max(0f, sweepDuration);

            _displayed = _sweepFrom;
            _velocity = 0f;

            _fadeFrom = _group != null ? _group.alpha : 1f;
            _fadeTo = 1f;
            _fadeElapsed = 0f;
            _fadeDuration = Mathf.Max(0f, fadeDuration);

            if (_fadeDuration <= 0f)
            {
                ApplyAlpha(1f);
            }

            Apply();
        }

        private void Update()
        {
            TickFade();
            TickValue();
        }

        private void TickFade()
        {
            if (_fadeDuration <= 0f)
            {
                return;
            }

            _fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);

            ApplyAlpha(Mathf.Lerp(_fadeFrom, _fadeTo, t));

            if (t >= 1f)
            {
                _fadeDuration = 0f;
            }
        }

        private void TickValue()
        {
            if (_sweepDuration > 0f)
            {
                _sweepElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_sweepElapsed / _sweepDuration);

                _displayed = Mathf.Lerp(_sweepFrom, _target, Mathf.SmoothStep(0f, 1f, t));
                Apply();

                if (t >= 1f)
                {
                    _sweepDuration = 0f;
                    _velocity = 0f;
                }

                return;
            }

            if (Mathf.Approximately(_displayed, _target))
            {
                return;
            }

            _displayed = _smoothingTime > 0f
                ? Mathf.SmoothDamp(_displayed, _target, ref _velocity, _smoothingTime)
                : _target;

            Apply();
        }

        private void ApplyAlpha(float alpha)
        {
            if (_group != null)
            {
                _group.alpha = alpha;
            }
        }

        private void Apply()
        {
            if (_fill == null)
            {
                return;
            }

            // Slide left by the hidden fraction; the mask clips whatever leaves the viewport.
            _fill.fillAmount = _displayed;
        }
    }
}
