using CrazyDriver.Core.Configuration;
using CrazyDriver.Core.Path;
using UnityEngine;

namespace CrazyDriver.Core.Car
{
    /// <summary>
    /// The car's motion, expressed purely as numbers. No Transform, no Rigidbody, no MonoBehaviour:
    /// the view reads <see cref="Position"/> and <see cref="Rotation"/> once per frame and does
    /// nothing else. The car never steers -- it advances along the path at a fixed cruise speed and
    /// is displaced sideways by a seeded sway curve.
    /// </summary>
    public sealed class CarModel
    {
        private readonly CarSettings _settings;
        private readonly IPathEvaluator _path;
        private readonly PathProgress _progress;

        private LayeredSine _sway;
        private float _speed;
        private float _targetSpeed;

        public CarModel(CarSettings settings, IPathEvaluator path, PathProgress progress)
        {
            _settings = settings;
            _path = path;
            _progress = progress;

            Reset(seed: 0);
        }

        public Vector3 Position { get; private set; }

        /// <summary>Full visual rotation, including the yaw the car takes into its own sway.</summary>
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Rotation of the path itself at the car's position, with no sway applied. The turret aims
        /// against this so the car's wander never drags the crosshair under the player's finger.
        /// </summary>
        public Quaternion PathRotation { get; private set; }

        public float LateralOffset { get; private set; }

        public float Speed => _speed;

        /// <summary>
        /// World-space velocity, including the sideways component contributed by the sway. Enemies
        /// lead their chase with this, so it has to be the real velocity rather than speed times
        /// forward.
        /// </summary>
        public Vector3 Velocity { get; private set; }

        public float NormalizedSpeed => _settings.Speed > 0f ? _speed / _settings.Speed : 0f;

        /// <summary>Begin accelerating towards the configured cruise speed.</summary>
        public void Drive() => _targetSpeed = _settings.Speed;

        /// <summary>Begin decelerating to a standstill, as on level completion.</summary>
        public void Brake() => _targetSpeed = 0f;

        /// <summary>Stop dead, without a deceleration ramp.</summary>
        public void HaltImmediately()
        {
            _targetSpeed = 0f;
            _speed = 0f;
        }

        public void Tick(float deltaTime)
        {
            UpdateSpeed(deltaTime);

            if (_speed > 0f)
            {
                _progress.Advance(_speed * deltaTime);
            }

            RecomputePose();
        }

        /// <summary>
        /// Returns the car to a standstill and rerolls its sway curve. The seed comes from the run
        /// so that a replayed seed reproduces the wander exactly, not just the enemy layout.
        /// </summary>
        public void Reset(int seed)
        {
            _speed = 0f;
            _targetSpeed = 0f;
            _sway = LayeredSine.FromSettings(_settings.Sway, seed);

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

            float rate = _settings.Speed / rampTime;
            _speed = Mathf.MoveTowards(_speed, _targetSpeed, rate * deltaTime);
        }

        private void RecomputePose()
        {
            float distance = _progress.Distance;

            LateralOffset = _sway.Evaluate(distance);
            float lateralSlope = _sway.EvaluateDerivative(distance);

            PathSample sample = _path.Evaluate(distance, LateralOffset);
            Position = sample.Position;
            PathRotation = sample.Rotation;

            // d(position)/dt = d(position)/d(distance) * speed. Both derivatives are analytic, so
            // the velocity is exact rather than a one-frame finite difference.
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
