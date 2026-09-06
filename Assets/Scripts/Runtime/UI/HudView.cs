using CrazyDriver.Logic;
using TMPro;
using UnityEngine;

namespace CrazyDriver.UI
{
    /// <summary>
    /// The in-run overlay. Every field is driven by an event from the simulation rather than polled
    /// in Update, so the UI only does work when something has actually changed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private ProgressBarView _healthBar;
        [SerializeField] private ProgressBarView _progressBar;
        [SerializeField] private CoinCounterView _coins;
        [SerializeField] private TMP_Text _distanceLabel;

        [Header("Sources")]
        [SerializeField] private CarHealth _health;
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private PathTracker _path;
        [SerializeField] private GameRunner _run;

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _path.MeterPassed += OnMeterPassed;

            OnHealthChanged(_health.Current, _health.Max);
            OnMeterPassed(0);

            // Snapped rather than animated on the first frame: a bar sweeping up from empty as the
            // run is prepared reads as damage being undone, and a counter rolling from zero to zero
            // flashes for no reason.
            _healthBar?.SnapTo(_health.Normalized);
            _progressBar?.SnapTo(0f);
            _coins?.SnapTo(_wallet.Coins);
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _wallet.CoinsChanged -= OnCoinsChanged;
            _path.MeterPassed -= OnMeterPassed;
        }

        public void SetVisible(bool visible)
        {
            if (_group != null)
            {
                _group.alpha = visible ? 1f : 0f;
            }
        }

        private void OnHealthChanged(float current, float max) =>
            _healthBar?.SetNormalized(max > 0f ? current / max : 0f);

        private void OnCoinsChanged(int coins) => _coins?.SetValue(coins);

        private void OnMeterPassed(int meters)
        {
            // SetText with a format argument rather than string interpolation: this fires once per
            // meter and interpolation would allocate a string every time.
            _distanceLabel?.SetText("{0}m", meters);
            _progressBar?.SetNormalized(_run.Progress);
        }
    }
}
