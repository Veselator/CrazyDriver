using CrazyDriver.Events;
using UnityEngine;

namespace CrazyDriver.UI
{
    /// <summary>
    /// The "Tap to start" prompt: breathes while the player is deciding, and fades out the moment
    /// they commit.
    /// <para>
    /// It leaves on <see cref="GameEvents.OnRunStarting"/>, not on <c>OnRunStarted</c>. The second
    /// one lands several seconds later, after the camera has swung and the countdown has run, and a
    /// prompt still saying "tap to start" through all of that reads as an input that was missed.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TapToStartView : MonoBehaviour
    {
        private const float Tau = 2f * Mathf.PI;

        [SerializeField] private CanvasGroup _group;

        [SerializeField, Tooltip("What gets scaled. Defaults to this object.")]
        private Transform _target;

        [SerializeField, Range(0f, 1f), Tooltip("Peak size change, as a fraction of the base scale.")]
        private float _amplitude = 0.09f;

        [SerializeField, Min(0f), Tooltip("Swings per second.")]
        private float _pulseSpeed = 0.85f;

        [SerializeField, Min(0f), Tooltip("Seconds to appear once a level is ready.")]
        private float _fadeInDuration = 0.4f;

        [SerializeField, Min(0f), Tooltip("Seconds to disappear once the player taps.")]
        private float _fadeOutDuration = 0.3f;

        private Vector3 _baseScale = Vector3.one;

        private float _phase;
        private bool _pulsing;

        private float _fadeFrom;
        private float _fadeTo;
        private float _fadeElapsed;
        private float _fadeDuration;

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform;
            }

            _baseScale = _target.localScale;
        }

        private void OnEnable()
        {
            GameEvents.OnRunPrepared += Show;
            GameEvents.OnRunStarting += Hide;

            // A level may already be prepared by the time this enables, so the prompt starts from
            // its own idle state rather than waiting for an event that has already gone past.
            SetAlpha(0f);
            Show();
        }

        private void OnDisable()
        {
            GameEvents.OnRunPrepared -= Show;
            GameEvents.OnRunStarting -= Hide;
        }

        private void Show()
        {
            _phase = 0f;
            _pulsing = true;

            Fade(1f, _fadeInDuration);
            Apply(0f);
        }

        private void Hide() => Fade(0f, _fadeOutDuration);

        private void Fade(float to, float duration)
        {
            _fadeFrom = _group != null ? _group.alpha : 1f;
            _fadeTo = to;
            _fadeElapsed = 0f;
            _fadeDuration = Mathf.Max(0f, duration);

            if (_fadeDuration <= 0f)
            {
                SetAlpha(to);
            }
        }

        private void Update()
        {
            TickFade();

            if (!_pulsing)
            {
                return;
            }

            _phase += Tau * _pulseSpeed * Time.deltaTime;
            Apply(Mathf.Sin(_phase));
        }

        private void TickFade()
        {
            if (_fadeDuration <= 0f)
            {
                return;
            }

            _fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);

            SetAlpha(Mathf.Lerp(_fadeFrom, _fadeTo, Mathf.SmoothStep(0f, 1f, t)));

            if (t < 1f)
            {
                return;
            }

            _fadeDuration = 0f;

            // The pulse stops only once the prompt is invisible; stopping it at the tap would snap
            // the label back to its base size in full view.
            if (_fadeTo <= 0f)
            {
                _pulsing = false;
                Apply(0f);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_group != null)
            {
                _group.alpha = alpha;
            }
        }

        private void Apply(float swing)
        {
            if (_target != null)
            {
                _target.localScale = _baseScale * (1f + _amplitude * swing);
            }
        }
    }
}
