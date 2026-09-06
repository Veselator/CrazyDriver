using CrazyDriver.Events;
using UnityEngine;

namespace CrazyDriver
{
    /// <summary>
    /// Pulses the car's scale when it pulls away, and stops when the run ends.
    /// <para>
    /// A ping-pong on the scale rather than a one-shot pop: the car is the only thing on screen that
    /// never changes shape, so a single squash reads as a glitch while a rhythm reads as an engine.
    /// The amplitude is per-axis, so the same component does a squash-and-stretch or a plain
    /// breathing pulse depending on what is filled in.
    /// </para>
    /// <para>
    /// It scales a child visual, never the object carrying the simulation: the collider, the turret
    /// mount and the muzzle all hang off the car's own transform, and scaling that would move them.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarScalingAnim : MonoBehaviour
    {
        [SerializeField, Tooltip("What gets scaled. Defaults to this object.")]
        private Transform _target;

        [SerializeField, Tooltip("Peak scale offset per axis, as a fraction of the base scale. " +
             "(-0.06, 0.1, 0) is a squash-and-stretch; (0.05, 0.05, 0.05) is a breathing pulse.")]
        private Vector3 _amplitude = new(-0.05f, 0.09f, 0f);

        [SerializeField, Min(0.01f), Tooltip("Seconds for one full out-and-back swing.")]
        private float _cycleDuration = 0.42f;

        [SerializeField, Min(0), Tooltip("How many swings to play when the car starts. Zero keeps " +
             "pulsing for the whole run.")]
        private int _cycles = 3;

        [SerializeField, Min(0f), Tooltip("Seconds for the pulse to fade back to nothing once the " +
             "run ends, so the car settles instead of snapping.")]
        private float _settleDuration = 0.35f;

        private Vector3 _baseScale = Vector3.one;
        private float _elapsed;
        private float _playDuration;
        private bool _playing;
        private bool _settling;
        private float _settleFrom;

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
            GameEvents.OnRunStarted += HandleStart;
            GameEvents.OnWin += HandleFinish;
            GameEvents.OnLose += HandleFinish;
        }

        private void OnDisable()
        {
            GameEvents.OnRunStarted -= HandleStart;
            GameEvents.OnWin -= HandleFinish;
            GameEvents.OnLose -= HandleFinish;

            _playing = false;
            _settling = false;

            Apply(0f);
        }

        private void HandleStart()
        {
            _elapsed = 0f;
            _playing = true;
            _settleFrom = 0f;

            // Zero cycles means "for as long as the run lasts"; HandleFinish is what ends it.
            _playDuration = _cycles > 0 ? _cycles * _cycleDuration : float.PositiveInfinity;
        }

        private void HandleFinish(RunResult result) => Stop();

        private void Stop()
        {
            if (!_playing)
            {
                return;
            }

            // Remembered so the settle fades out from wherever the swing happened to be, rather
            // than jumping to the peak and easing down from there.
            _settleFrom = Swing(_elapsed);
            _playing = false;
            _settling = true;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_playing)
            {
                _elapsed += Time.deltaTime;

                if (_elapsed >= _playDuration)
                {
                    Stop();
                    return;
                }

                Apply(Swing(_elapsed));
                return;
            }

            if (!_settling)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _settleDuration > 0f ? Mathf.Clamp01(_elapsed / _settleDuration) : 1f;

            Apply(Mathf.Lerp(_settleFrom, 0f, t));

            if (t >= 1f)
            {
                _settling = false;
            }
        }

        /// <summary>Position in the ping-pong, -1 to 1, as a smooth swing rather than a triangle.</summary>
        private float Swing(float time) => Mathf.Sin(time / _cycleDuration * 2f * Mathf.PI);

        private void Apply(float swing)
        {
            if (_target == null)
            {
                return;
            }

            _target.localScale = new Vector3(
                _baseScale.x * (1f + _amplitude.x * swing),
                _baseScale.y * (1f + _amplitude.y * swing),
                _baseScale.z * (1f + _amplitude.z * swing));
        }
    }
}
