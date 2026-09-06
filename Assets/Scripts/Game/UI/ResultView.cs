using CrazyDriver.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyDriver.Game.UI
{
    /// <summary>
    /// The win and loss screen. Shows what the run earned alongside the persisted totals, so the
    /// player can see progress accumulating rather than each run vanishing.
    /// </summary>
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _backdrop;
        [SerializeField] private Image _banner;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _runStatsLabel;
        [SerializeField] private TMP_Text _profileStatsLabel;
        [SerializeField] private TMP_Text _promptLabel;

        [Header("Presentation")]
        [SerializeField] private string _winTitle = "You win";
        [SerializeField] private string _loseTitle = "You lose";
        [SerializeField] private Color _winColor = new(0.24f, 0.68f, 0.35f);
        [SerializeField] private Color _loseColor = new(0.78f, 0.24f, 0.24f);
        [SerializeField] private float _fadeInDuration = 0.35f;

        private float _fade;
        private bool _isShown;

        public void Hide()
        {
            _isShown = false;
            _fade = 0f;
            ApplyFade();
        }

        public void Show(bool won, RunSummary summary, PlayerProfile profile)
        {
            _isShown = true;
            _fade = 0f;

            if (_titleLabel != null)
            {
                _titleLabel.text = won ? _winTitle : _loseTitle;
            }

            if (_banner != null)
            {
                _banner.color = won ? _winColor : _loseColor;
            }

            if (_runStatsLabel != null)
            {
                _runStatsLabel.text =
                    $"Distance   {summary.Distance:0} m / {summary.Length:0} m\n" +
                    $"Enemies    {summary.Kills}\n" +
                    $"Coins      +{summary.Coins}";
            }

            if (_profileStatsLabel != null && profile != null)
            {
                _profileStatsLabel.text =
                    $"Best {profile.BestDistance:0} m     " +
                    $"Wins {profile.RunsWon}/{profile.RunsPlayed}     " +
                    $"Bank {profile.TotalCoins}";
            }

            if (_promptLabel != null)
            {
                _promptLabel.text = "Tap to play again";
            }

            ApplyFade();
        }

        private void Update()
        {
            if (!_isShown || _fade >= 1f)
            {
                return;
            }

            _fade = _fadeInDuration > 0f
                ? Mathf.Clamp01(_fade + Time.deltaTime / _fadeInDuration)
                : 1f;

            ApplyFade();
        }

        private void ApplyFade()
        {
            if (_group == null)
            {
                return;
            }

            _group.alpha = Mathf.SmoothStep(0f, 1f, _fade);

            // Never blocks raycasts: the brief asks for a tap anywhere on screen to restart, and an
            // interactive overlay would swallow exactly that tap.
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
    }

    /// <summary>What a finished run produced, passed to the result screen as one value.</summary>
    public readonly struct RunSummary
    {
        public readonly float Distance;
        public readonly float Length;
        public readonly int Kills;
        public readonly int Coins;

        public RunSummary(float distance, float length, int kills, int coins)
        {
            Distance = distance;
            Length = length;
            Kills = kills;
            Coins = coins;
        }
    }
}
