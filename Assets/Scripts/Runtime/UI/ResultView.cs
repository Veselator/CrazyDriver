using CrazyDriver.Events;
using CrazyDriver.Input;
using CrazyDriver.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CrazyDriver.UI
{
    /// <summary>
    /// The win and loss screen. Shows what the run earned alongside the persisted totals, so the
    /// player can see progress accumulating rather than each run vanishing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _backdrop;
        [SerializeField] private Image _banner;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _runStatsLabel;
        [SerializeField] private TMP_Text _profileStatsLabel;
        [SerializeField] private TMP_Text _promptLabel;
        [SerializeField] private PointerInput _input;

        [Header("Presentation")]
        [SerializeField] private string _winTitle = "You win";
        [SerializeField] private string _loseTitle = "You lose";
        [SerializeField] private Color _winColor = new(0.24f, 0.68f, 0.35f);
        [SerializeField] private Color _loseColor = new(0.78f, 0.24f, 0.24f);
        [SerializeField] private float _fadeInDuration = 0.35f;

        private PlayerProfileService _profiles;
        private float _fade;
        private bool _isShown;

        [Inject]
        public void Construct(PlayerProfileService profiles) => _profiles = profiles;

        private void OnEnable()
        {
            GameEvents.OnWin += OnWin;
            GameEvents.OnLose += OnLose;
            _input.Tapped += OnTapped;

            Hide();
        }

        private void OnDisable()
        {
            GameEvents.OnWin -= OnWin;
            GameEvents.OnLose -= OnLose;
            _input.Tapped -= OnTapped;
        }

        private void OnWin(RunResult result) => Show(true, result);

        private void OnLose(RunResult result) => Show(false, result);

        /// <summary>
        /// The screen dismisses itself on the next tap rather than waiting for an event saying a
        /// new run was prepared. The same tap is what tells the runner to reroll, so tying both to
        /// it keeps them in step without inventing a fifth event.
        /// </summary>
        private void OnTapped()
        {
            if (_isShown)
            {
                Hide();
            }
        }

        private void Hide()
        {
            _isShown = false;
            _fade = 0f;
            ApplyFade();
        }

        private void Show(bool won, RunResult result)
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
                    $"Distance   {result.Distance:0} m / {result.Length:0} m\n" +
                    $"Enemies    {result.Kills}\n" +
                    $"Coins      +{result.Coins}";
            }

            PlayerProfile profile = _profiles?.Profile;
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

            _fade = _fadeInDuration > 0f ? Mathf.Clamp01(_fade + Time.deltaTime / _fadeInDuration) : 1f;
            ApplyFade();
        }

        private void ApplyFade()
        {
            if (_group == null)
            {
                return;
            }

            _group.alpha = _isShown ? Mathf.SmoothStep(0f, 1f, _fade) : 0f;

            // Never blocks raycasts: the brief asks for a tap anywhere on screen to restart, and an
            // interactive overlay would swallow exactly that tap.
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
    }
}
