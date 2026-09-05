using CrazyDriver.Core.Car;
using CrazyDriver.Core.Configuration;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// The chase camera. Frames the car from behind in both states, as the brief's screenshot
    /// requires, and blends between the parked and driving offsets when the run starts.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private CameraSettings _settings;
        private Vector3 _positionVelocity;
        private float _blend;
        private bool _isPlaying;

        public Camera Camera => _camera;

        private void Reset() => _camera = GetComponent<Camera>();

        /// <summary>
        /// Injected by the container while it builds, which is strictly before any entry point
        /// ticks. Calling this by hand from a startable instead left a window in which the first
        /// LateTick ran against unset settings.
        /// </summary>
        [Inject]
        public void Construct(CameraSettings settings)
        {
            _settings = settings;
        }

        public void SetPlaying(bool isPlaying) => _isPlaying = isPlaying;

        /// <summary>Jump straight to the framing with no smoothing, used when a run is prepared.</summary>
        public void Snap(CarModel car)
        {
            _blend = 0f;
            _isPlaying = false;
            _positionVelocity = Vector3.zero;

            GetTargets(car, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Render(CarModel car, float deltaTime)
        {
            float blendRate = _settings.StateBlendDuration > 0f ? deltaTime / _settings.StateBlendDuration : 1f;
            _blend = Mathf.Clamp01(_blend + (_isPlaying ? blendRate : -blendRate));

            GetTargets(car, out Vector3 targetPosition, out Quaternion targetRotation);

            Vector3 position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _positionVelocity,
                _settings.PositionSmoothingTime,
                Mathf.Infinity,
                deltaTime);

            // An exponential approach rather than a fixed slerp factor, so the rotation settles at
            // the same rate regardless of frame time.
            float rotationBlend = _settings.RotationSmoothingTime > 0f
                ? 1f - Mathf.Exp(-deltaTime / _settings.RotationSmoothingTime)
                : 1f;

            transform.SetPositionAndRotation(
                position,
                Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend));
        }

        private void GetTargets(CarModel car, out Vector3 position, out Quaternion rotation)
        {
            Quaternion pathRotation = car.PathRotation;

            // Anchor on the path, not on the car body. Copying the full sway would freeze the car
            // dead-centre in frame and make the wander invisible; ignoring it entirely would slide
            // the car off the edge of the screen. Following a fraction of it keeps both readable.
            Vector3 pathRight = pathRotation * Vector3.right;
            Vector3 anchor = car.Position - pathRight * (car.LateralOffset * (1f - _settings.SwayFollow));

            Vector3 offset = Vector3.Lerp(_settings.ReadyOffset, _settings.PlayOffset, Mathf.SmoothStep(0f, 1f, _blend));

            position = anchor + pathRotation * offset;

            Vector3 lookAt = anchor + pathRotation * _settings.LookAtOffset;
            rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
        }
    }
}
