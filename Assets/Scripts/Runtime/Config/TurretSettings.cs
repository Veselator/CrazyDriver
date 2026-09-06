using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Turret aiming. The aim angle is stored in world space and is deliberately decoupled from the
    /// car's own yaw, so the car's sway never drags the crosshair around under the player's finger.
    /// </summary>
    [Serializable]
    public sealed class TurretSettings
    {
        [SerializeField, Min(0f)] private float _degreesPerScreenWidth = 150f;
        [SerializeField, Min(0f)] private float _maxAngle = 70f;
        [SerializeField, Min(0f)] private float _smoothingTime = 0.05f;

        /// <summary>
        /// Rotation applied when the finger travels the full width of the screen. Expressed against
        /// normalized screen width so the feel is identical on every device density.
        /// </summary>
        public float DegreesPerScreenWidth => _degreesPerScreenWidth;

        /// <summary>Symmetric clamp around the path's forward direction, in degrees.</summary>
        public float MaxAngle => _maxAngle;

        /// <summary>Critically damped smoothing applied to the aim angle, in seconds.</summary>
        public float SmoothingTime => _smoothingTime;
    }
}
