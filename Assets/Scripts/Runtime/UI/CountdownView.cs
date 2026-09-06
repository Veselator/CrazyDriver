using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CrazyDriver.UI
{
    /// <summary>
    /// The "3, 2, 1, Go" that plays between the camera settling and the gate opening.
    /// <para>
    /// Awaited rather than fired and forgotten, so the run's start sequence stays one readable
    /// method in <c>GameRunner</c> instead of a chain of callbacks.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountdownView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _label;

        [SerializeField, Tooltip("Shown in order, one per beat. The last one is the go signal.")]
        private string[] _steps = { "3", "2", "1", "Go!" };

        [SerializeField, Min(0.01f)] private float _stepDuration = 0.6f;

        [SerializeField, Tooltip("Scale the text punches out from at the start of each beat.")]
        private float _punchScale = 1.6f;

        [SerializeField] private Color _countColor = Color.white;
        [SerializeField] private Color _goColor = new(0.35f, 0.95f, 0.4f);

        private void Awake() => Hide();

        public void Hide()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
            }
        }

        /// <summary>Plays the whole count and returns once the last beat has finished.</summary>
        public async UniTask PlayAsync(CancellationToken cancellationToken)
        {
            if (_label == null || _steps == null || _steps.Length == 0)
            {
                return;
            }

            try
            {
                for (int i = 0; i < _steps.Length; i++)
                {
                    _label.text = _steps[i];
                    _label.color = i == _steps.Length - 1 ? _goColor : _countColor;

                    await PlayStepAsync(cancellationToken);
                }
            }
            finally
            {
                // Even a cancelled countdown must not leave a number frozen on screen.
                Hide();
            }
        }

        private async UniTask PlayStepAsync(CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < _stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _stepDuration);

                // Punches to full size quickly, then fades over the rest of the beat, so each number
                // lands hard and clears itself before the next one arrives.
                float scale = Mathf.Lerp(_punchScale, 1f, Mathf.Clamp01(t * 4f));
                _label.transform.localScale = Vector3.one * scale;

                if (_group != null)
                {
                    _group.alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
