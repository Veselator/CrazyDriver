using CrazyDriver.Core.Configuration;
using UnityEngine;

namespace CrazyDriver.Core.Combat
{
    /// <summary>
    /// The turret's aim angle, held relative to the <em>path</em> rather than to the car body.
    /// <para>
    /// This is the single most important decision for how the game feels. If the turret were simply
    /// a child of the car, the car's sway yaw would rotate the barrel while the player's finger was
    /// still, and aiming would feel drunk. Storing the angle against the path and letting the view
    /// cancel out the car's rotation keeps the crosshair exactly where the player put it.
    /// </para>
    /// </summary>
    public sealed class TurretModel
    {
        private readonly TurretSettings _settings;

        private float _targetAngle;
        private float _angle;
        private float _smoothingVelocity;
        private float _dragOriginAngle;

        public TurretModel(TurretSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Smoothed aim angle in degrees, relative to the path's forward direction.</summary>
        public float Angle => _angle;

        public float NormalizedAngle => _settings.MaxAngle > 0f ? _angle / _settings.MaxAngle : 0f;

        /// <summary>
        /// Anchors a new drag. Everything that follows is measured from wherever the finger landed
        /// and from the angle the turret already held, which is what makes the control feel like
        /// grabbing the turret rather than nudging it.
        /// </summary>
        public void BeginDrag() => _dragOriginAngle = _targetAngle;

        /// <summary>
        /// Aims from the drag anchor. The offset is a fraction of the screen width rather than a
        /// pixel count, so the sensitivity is identical on every density and aspect ratio.
        /// </summary>
        public void Drag(float normalizedOffsetFromOrigin)
        {
            _targetAngle = Mathf.Clamp(
                _dragOriginAngle + normalizedOffsetFromOrigin * _settings.DegreesPerScreenWidth,
                -_settings.MaxAngle,
                _settings.MaxAngle);
        }

        public void Tick(float deltaTime)
        {
            _angle = Mathf.SmoothDamp(_angle, _targetAngle, ref _smoothingVelocity, _settings.SmoothingTime, Mathf.Infinity, deltaTime);
        }

        public void Reset()
        {
            _angle = 0f;
            _targetAngle = 0f;
            _dragOriginAngle = 0f;
            _smoothingVelocity = 0f;
        }

        /// <summary>World-space aim rotation, given the path's rotation at the car's position.</summary>
        public Quaternion GetWorldRotation(Quaternion pathRotation) =>
            pathRotation * Quaternion.Euler(0f, _angle, 0f);
    }
}
