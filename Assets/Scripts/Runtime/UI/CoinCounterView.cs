using TMPro;
using UnityEngine;

namespace CrazyDriver.UI
{
    /// <summary>
    /// The coin readout. Rolls the digits towards a new total and flashes green when it goes up,
    /// red when it goes down.
    /// <para>
    /// Two independent animations run side by side on purpose. The roll says how much changed, the
    /// flash says which direction, and they have different natural durations -- tying them together
    /// would make a one-coin pickup take as long to read as a fifty-coin one.
    /// </para>
    /// </summary>
    public sealed class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        [Header("Colours")]
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField] private Color _gainColor = new(0.35f, 0.95f, 0.4f);
        [SerializeField] private Color _lossColor = new(0.95f, 0.3f, 0.28f);

        [Header("Flash")]
        [SerializeField, Min(0f), Tooltip("Seconds from the base colour to the flash colour.")]
        private float _flashInDuration = 0.5f;

        [SerializeField, Min(0f), Tooltip("Seconds back from the flash colour to the base colour.")]
        private float _flashOutDuration = 0.5f;

        [Header("Digits")]
        [SerializeField, Min(0f), Tooltip("Seconds for the number to count to its new value.")]
        private float _rollDuration = 0.4f;

        private int _target;
        private float _rollFrom;
        private float _rollElapsed = -1f;

        private Color _flashColor;
        private float _flashElapsed = -1f;

        private int _shownValue = int.MinValue;

        /// <summary>The value the counter is heading towards, whatever it is currently displaying.</summary>
        public int Value => _target;

        private void Awake()
        {
            if (_label != null)
            {
                _label.color = _baseColor;
            }
        }

        /// <summary>Animates to <paramref name="value"/>, flashing in the direction of the change.</summary>
        public void SetValue(int value)
        {
            if (value == _target)
            {
                return;
            }

            // A change arriving mid-flash restarts it rather than queueing, so a burst of pickups
            // reads as one continuous glow instead of a stutter.
            _flashColor = value > _target ? _gainColor : _lossColor;
            _flashElapsed = 0f;

            _rollFrom = CurrentDisplayedValue;
            _rollElapsed = 0f;
            _target = value;
        }

        /// <summary>Jumps straight to <paramref name="value"/> with no roll and no flash.</summary>
        public void SnapTo(int value)
        {
            _target = value;
            _rollFrom = value;
            _rollElapsed = -1f;
            _flashElapsed = -1f;

            Show(value);

            if (_label != null)
            {
                _label.color = _baseColor;
            }
        }

        private float CurrentDisplayedValue
        {
            get
            {
                if (_rollElapsed < 0f || _rollDuration <= 0f)
                {
                    return _target;
                }

                float t = Mathf.Clamp01(_rollElapsed / _rollDuration);
                return Mathf.Lerp(_rollFrom, _target, t);
            }
        }

        private void Update()
        {
            TickRoll();
            TickFlash();
        }

        private void TickRoll()
        {
            if (_rollElapsed < 0f)
            {
                return;
            }

            _rollElapsed += Time.deltaTime;

            if (_rollDuration <= 0f || _rollElapsed >= _rollDuration)
            {
                _rollElapsed = -1f;
                Show(_target);
                return;
            }

            // Eased so the number decelerates onto its final value instead of stopping dead.
            float t = Mathf.SmoothStep(0f, 1f, _rollElapsed / _rollDuration);
            Show(Mathf.RoundToInt(Mathf.Lerp(_rollFrom, _target, t)));
        }

        private void TickFlash()
        {
            if (_flashElapsed < 0f || _label == null)
            {
                return;
            }

            _flashElapsed += Time.deltaTime;

            float total = _flashInDuration + _flashOutDuration;
            if (_flashElapsed >= total)
            {
                _flashElapsed = -1f;
                _label.color = _baseColor;
                return;
            }

            _label.color = _flashElapsed < _flashInDuration
                ? Color.Lerp(_baseColor, _flashColor, Safe(_flashElapsed, _flashInDuration))
                : Color.Lerp(_flashColor, _baseColor, Safe(_flashElapsed - _flashInDuration, _flashOutDuration));
        }

        private static float Safe(float elapsed, float duration) =>
            duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        private void Show(int value)
        {
            if (_label == null || value == _shownValue)
            {
                return;
            }

            _shownValue = value;

            // SetText with an argument rather than interpolation: this runs every frame while the
            // digits roll, and interpolation would allocate a string each time.
            _label.SetText("{0}", value);
        }
    }
}
