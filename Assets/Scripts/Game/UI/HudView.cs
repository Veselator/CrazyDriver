using CrazyDriver.Core.Combat;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CrazyDriver.Game.UI
{
    /// <summary>
    /// The in-run overlay. Every field is driven by an event from the core rather than polled in
    /// Update, so the UI only does work when something has actually changed.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField] private TMP_Text _distanceLabel;

        private Health _health;
        private Wallet _wallet;
        private PathProgress _progress;
        private RunController _run;

        [Inject]
        public void Construct(Health health, Wallet wallet, PathProgress progress, RunController run)
        {
            _health = health;
            _wallet = wallet;
            _progress = progress;
            _run = run;

            _health.Changed += OnHealthChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _progress.MeterPassed += OnMeterPassed;

            OnHealthChanged(_health.Current, _health.Max);
            OnCoinsChanged(_wallet.Coins);
            OnMeterPassed(0);
        }

        public void SetVisible(bool visible)
        {
            if (_group != null)
            {
                _group.alpha = visible ? 1f : 0f;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnHealthChanged;
            }

            if (_wallet != null)
            {
                _wallet.CoinsChanged -= OnCoinsChanged;
            }

            if (_progress != null)
            {
                _progress.MeterPassed -= OnMeterPassed;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_healthFill != null)
            {
                _healthFill.fillAmount = max > 0f ? current / max : 0f;
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (_coinsLabel != null)
            {
                _coinsLabel.SetText("{0}", coins);
            }
        }

        private void OnMeterPassed(int meters)
        {
            // SetText with a format argument rather than string interpolation: this fires once per
            // meter and interpolation would allocate a string every time.
            if (_distanceLabel != null)
            {
                _distanceLabel.SetText("{0}m", meters);
            }

            if (_progressFill != null)
            {
                _progressFill.fillAmount = _run.Progress;
            }
        }
    }
}
