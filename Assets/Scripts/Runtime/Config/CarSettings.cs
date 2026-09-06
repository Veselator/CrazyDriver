using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Movement of the car along the path. The car never steers: it advances at a fixed
    /// speed and is displaced sideways by <see cref="Sway"/> only.
    /// </summary>
    [Serializable]
    public sealed class CarSettings
    {
        [SerializeField, Min(0f)] private float _speed = 12f;
        [SerializeField, Min(0f)] private float _accelerationTime = 0.9f;
        [SerializeField, Min(0f)] private float _brakingTime = 1.4f;
        [SerializeField] private SwaySettings _sway = new();

        /// <summary>Cruise speed in meters per second.</summary>
        public float Speed => _speed;

        /// <summary>Seconds to blend from a standstill to <see cref="Speed"/>.</summary>
        public float AccelerationTime => _accelerationTime;

        /// <summary>Seconds to blend from <see cref="Speed"/> to a standstill on level completion.</summary>
        public float BrakingTime => _brakingTime;

        public SwaySettings Sway => _sway;
    }
}
