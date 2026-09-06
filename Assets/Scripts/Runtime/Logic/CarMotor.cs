using CrazyDriver.Config;
using CrazyDriver.Paths;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// The car's motion. Pure numbers on a component: it drives no Transform of its own, and
    /// <see cref="CarView"/> reads the pose it publishes.
    /// <para>
    /// The car never steers. It advances along the path at a fixed cruise speed and is displaced
    /// sideways by a seeded sway curve, so its lateral position is a function of distance rather
    /// than of anything the player does.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.CarMotor)]
    public sealed class CarMotor : MonoBehaviour
    {
        [SerializeField] private PathTracker _path;

        private CarSettings _settings;
        private LayeredSine _sway;
        private float _speed;
        private float _targetSpeed;

        [Inject]
        public void Construct(GameConstantsSO constants)
        {
            _settings = constants.Car;
            Reseed(0);
        }

        public Vector3 Position { get; private set; }

        /// <summary>Full visual rotation, including the yaw the car takes into its own sway.</summary>
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Rotation of the path at the car's position, with no sway applied. The turret aims
        /// against this so the car's wander never drags the crosshair under the player's finger.
        /// </summary>
        public Quaternion PathRotation { get; private set; }

        /// <summary>World velocity including the sideways component. Enemies lead their chase with it.</summary>
        public Vector3 Velocity { get; private set; }

        public float LateralOffset { get; private set; }

        public float Speed => _speed;

        public float NormalizedSpeed => _settings != null && _settings.Speed > 0f ? _speed / _settings.Speed : 0f;

        /// <summary>Begin accelerating towards the cruise speed.</summary>
        public void Drive() => _targetSpeed = _settings.Speed;

        /// <summary>Begin decelerating to a standstill, as on level completion.</summary>
        public void Brake() => _targetSpeed = 0f;

        /// <summary>
        /// Returns the car to a standstill and rerolls its sway. The seed comes from the run so a
        /// replayed seed reproduces the wander, not just the enemy layout.
        /// </summary>
        public void Reseed(int seed)
        {
            _speed = 0f;
            _targetSpeed = 0f;
            _sway = LayeredSine.FromSettings(_settings.Sway, seed);

            RecomputePose();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            UpdateSpeed(deltaTime);

            if (_speed > 0f)
            {
                _path.Advance(_speed * deltaTime);
            }

            RecomputePose();
        }

        private void UpdateSpeed(float deltaTime)
        {
            // Linear ramps rather than a spring: the player can predict them, and "reaches cruise
            // speed in N seconds" is a number a designer can reason about.
            float rampTime = _targetSpeed > _speed ? _settings.AccelerationTime : _settings.BrakingTime;
            if (rampTime <= 0f)
            {
                _speed = _targetSpeed;
                return;
            }

            _speed = Mathf.MoveTowards(_speed, _targetSpeed, _settings.Speed / rampTime * deltaTime);
        }

        private void RecomputePose()
        {
            float distance = _path.Distance;

            LateralOffset = _sway.Evaluate(distance);
            float lateralSlope = _sway.EvaluateDerivative(distance);

            PathSample sample = _path.Evaluate(distance, LateralOffset);
            Position = sample.Position;
            PathRotation = sample.Rotation;

            // d(position)/dt = d(position)/d(distance) * speed. Both derivatives are analytic, so
            // this is exact rather than a one-frame finite difference.
            Vector3 forwardTangent = PathRotation * Vector3.forward;
            Vector3 lateralTangent = PathRotation * Vector3.right * lateralSlope;
            Velocity = (forwardTangent + lateralTangent) * _speed;

            // The car must point where it is actually going. Without this term the sway reads as
            // the car sliding sideways on ice.
            float swayYaw = Mathf.Atan(lateralSlope) * Mathf.Rad2Deg * _settings.Sway.YawResponse;
            Rotation = PathRotation * Quaternion.Euler(0f, swayYaw, 0f);
        }
    }
}
