using CrazyDriver.Events;
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
        [SerializeField] private TMP_Text[] _distanceLabels;

        [Header("Sources")]
        [SerializeField] private CarHealth _health;
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private PathTracker _path;
        [SerializeField] private GameRunner _run;

        [Header("Intro")]
        [SerializeField, Min(0f), Tooltip("Seconds the bars take to fade in when a run begins.")]
        private float _fadeDuration = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds the health bar takes to sweep from empty to full.")]
        private float _healthSweepDuration = 0.7f;

        [SerializeField, Min(0f), Tooltip("Seconds the progress bar takes to appear. It sweeps " +
             "from nothing, so it only needs the fade.")]
        private float _progressSweepDuration = 0.35f;

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _path.MeterPassed += OnMeterPassed;

            // The bars belong to the run, not to the menu: hidden while the player is waiting to
            // tap, revealed when the car actually pulls away.
            GameEvents.OnRunPrepared += OnRunPrepared;
            GameEvents.OnRunStarted += OnRunStarted;

            OnHealthChanged(_health.Current, _health.Max);
            OnMeterPassed(0);

            // Snapped rather than animated on the first frame: a bar sweeping up from empty as the
            // run is prepared reads as damage being undone, and a counter rolling from zero to zero
            // flashes for no reason. The intentional sweep comes later, on OnRunStarted.
            _healthBar?.SnapTo(_health.Normalized);
            _progressBar?.SnapTo(0f);
            _coins?.SnapTo(_wallet.Coins);

            OnRunPrepared();
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _wallet.CoinsChanged -= OnCoinsChanged;
            _path.MeterPassed -= OnMeterPassed;

            GameEvents.OnRunPrepared -= OnRunPrepared;
            GameEvents.OnRunStarted -= OnRunStarted;
        }

        public void SetVisible(bool visible)
        {
            if (_group != null)
            {
                _group.alpha = visible ? 1f : 0f;
            }
        }

        private void OnRunPrepared()
        {
            _healthBar?.Hide();
            _progressBar?.Hide();
        }

        private void OnRunStarted()
        {
            // The health bar sweeps up from empty as it appears -- the run's opening statement that
            // this is how much there is to lose. The progress bar has nothing to sweep, so it only
            // fades in and starts from zero.
            _healthBar?.SnapTo(_health.Normalized);
            _healthBar?.PlayIntro(0f, _healthSweepDuration, _fadeDuration);

            _progressBar?.SnapTo(0f);
            _progressBar?.PlayIntro(0f, _progressSweepDuration, _fadeDuration);
        }

        private void OnHealthChanged(float current, float max) =>
            _healthBar?.SetNormalized(max > 0f ? current / max : 0f);

        private void OnCoinsChanged(int coins) => _coins?.SetValue(coins);

        private void OnMeterPassed(int meters)
        {
            // SetText with a format argument rather than string interpolation: this fires once per
            // meter and interpolation would allocate a string every time.
            foreach (TMP_Text label in _distanceLabels)
            {
                if (label != null)
                {
                    label.SetText("{0}m", meters);
                }
            }

            _progressBar?.SetNormalized(_run.Progress);
        }
    }
}
