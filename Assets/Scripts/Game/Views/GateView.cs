using System.Threading;
using CrazyDriver.Core.Configuration;
using CrazyDriver.Core.Path;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// The gate the car sets off through. Its wings slide apart when the run starts, which is what
    /// covers the first road tiles streaming in behind it.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {
        [SerializeField] private Transform _leftWing;
        [SerializeField] private Transform _rightWing;

        private GateSettings _settings;
        private Vector3 _leftClosed;
        private Vector3 _rightClosed;

        [Inject]
        public void Construct(GateSettings settings)
        {
            _settings = settings;

            if (_leftWing != null)
            {
                _leftClosed = _leftWing.localPosition;
            }

            if (_rightWing != null)
            {
                _rightClosed = _rightWing.localPosition;
            }
        }

        /// <summary>Places the gate on the path and snaps the wings shut.</summary>
        public void Close(IPathEvaluator path)
        {
            PathSample sample = path.Evaluate(_settings.DistanceAhead);
            transform.SetPositionAndRotation(sample.Position, sample.Rotation);

            gameObject.SetActive(true);
            SetOpenAmount(0f);
        }

        /// <summary>
        /// Slides the wings open. Awaited rather than fired and forgotten, so the caller can give
        /// the gate a head start before the car pulls away.
        /// </summary>
        public async UniTask OpenAsync(CancellationToken cancellationToken)
        {
            float duration = Mathf.Max(0.01f, _settings.OpenDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetOpenAmount(Mathf.SmoothStep(0f, 1f, elapsed / duration));

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            SetOpenAmount(1f);
        }

        private void SetOpenAmount(float amount)
        {
            float travel = _settings.OpenTravel * amount;

            if (_leftWing != null)
            {
                _leftWing.localPosition = _leftClosed + Vector3.left * travel;
            }

            if (_rightWing != null)
            {
                _rightWing.localPosition = _rightClosed + Vector3.right * travel;
            }
        }
    }
}
