using CrazyDriver.Events;
using UnityEngine;

namespace CrazyDriver
{
    /// <summary>
    /// Pulses the car's scale for the whole run: a short, larger burst as it pulls away, then a
    /// quieter pulse that keeps going until the run ends.
    /// <para>
    /// Two amplitudes on one continuous phase. The burst is the engine catching; the cruise pulse is
    /// it running. Keeping a single phase accumulator is what makes the handover invisible -- the
    /// burst ends on a whole number of swings, where the scale is exactly the base scale, so the
    /// amplitude can change there without anything jumping.
    /// </para>
    /// <para>
    /// It scales a child visual, never the object carrying the simulation: the collider, the turret
    /// mount and the muzzle all hang off the car's own transform, and scaling that would move them.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarScalingAnim : MonoBehaviour
    {
        private const float Tau = 2f * Mathf.PI;

        [SerializeField, Tooltip("What gets scaled. Defaults to this object.")]
        private Transform _target;

        [Header("Ignition")]
        [SerializeField, Tooltip("Peak scale offset per axis during the opening burst, as a " +
             "fraction of the base scale. (-0.06, 0.1, 0) is a squash-and-stretch; equal values " +
             "are a plain breathing pulse.")]
        private Vector3 _amplitude = new(-0.05f, 0.09f, 0f);

        [SerializeField, Min(0.01f), Tooltip("Seconds for one full out-and-back swing of the burst.")]
        private float _cycleDuration = 0.42f;

        [SerializeField, Min(0), Tooltip("How many swings the opening burst lasts. Zero skips it " +
             "and goes straight to the cruise pulse.")]
        private int _cycles = 3;

        [Header("While moving")]
        [SerializeField, Tooltip("Peak scale offset per axis for the rest of the run. Keep it well " +
             "under the ignition amplitude -- this one is on screen for the whole drive.")]
        private Vector3 _amplitudeWhileMoving = new(-0.015f, 0.03f, 0f);

        [SerializeField, Min(0f), Tooltip("Swings per second while driving.")]
        private float _speedWhileMoving = 2.2f;

        [Header("Stop")]
        [SerializeField, Min(0f), Tooltip("Seconds for the pulse to fade back to nothing once the " +
             "run ends, so the car settles instead of snapping.")]
        private float _settleDuration = 0.35f;

        private Vector3 _baseScale = Vector3.one;

        private float _phase;
        private bool _playing;
        private bool _inBurst;

        private bool _settling;
        private float _settleElapsed;
        private Vector3 _settleFrom;

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

            Apply(0f, _amplitude);
        }

        private void HandleStart()
        {
            _phase = 0f;
            _playing = true;
            _inBurst = _cycles > 0;
            _settling = false;
        }

        private void HandleFinish(RunResult result)
        {
            if (!_playing)
            {
                return;
            }

            // Captured as the scale actually on screen, not as a swing value: the two phases use
            // different amplitudes, so easing a scalar back to zero would settle to the wrong size.
            _settleFrom = _target != null ? _target.localScale : _baseScale;

            _playing = false;
            _settling = true;
            _settleElapsed = 0f;
        }

        private void Update()
        {
            if (_playing)
            {
                Tick();
                return;
            }

            if (_settling)
            {
                Settle();
            }
        }

        private void Tick()
        {
            float rate = _inBurst
                ? Tau / _cycleDuration
                : Tau * _speedWhileMoving;

            _phase += rate * Time.deltaTime;

            if (_inBurst && _phase >= _cycles * Tau)
            {
                // Restarted from zero rather than carried over: sin is the same at both, and the
                // cruise pulse then begins from a clean phase whatever the burst length was.
                _inBurst = false;
                _phase = 0f;
            }

            Apply(Mathf.Sin(_phase), _inBurst ? _amplitude : _amplitudeWhileMoving);
        }

        private void Settle()
        {
            _settleElapsed += Time.deltaTime;
            float t = _settleDuration > 0f ? Mathf.Clamp01(_settleElapsed / _settleDuration) : 1f;

            if (_target != null)
            {
                _target.localScale = Vector3.Lerp(_settleFrom, _baseScale, Mathf.SmoothStep(0f, 1f, t));
            }

            if (t >= 1f)
            {
                _settling = false;
            }
        }

        private void Apply(float swing, Vector3 amplitude)
        {
            if (_target == null)
            {
                return;
            }

            _target.localScale = new Vector3(
                _baseScale.x * (1f + amplitude.x * swing),
                _baseScale.y * (1f + amplitude.y * swing),
                _baseScale.z * (1f + amplitude.z * swing));
        }
    }
}
