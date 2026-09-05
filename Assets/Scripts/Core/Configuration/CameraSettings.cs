using System;
using UnityEngine;

namespace CrazyDriver.Core.Configuration
{
    /// <summary>
    /// Chase camera. Both states frame the car from behind, as the brief's screenshot requires; the
    /// only difference is the offset, which blends when the run starts.
    /// </summary>
    [Serializable]
    public sealed class CameraSettings
    {
        [SerializeField] private Vector3 _readyOffset = new(0f, 5.4f, -8.2f);
        [SerializeField] private Vector3 _playOffset = new(0f, 7.6f, -10.5f);
        [SerializeField] private Vector3 _lookAtOffset = new(0f, 1.4f, 6f);
        [SerializeField, Min(0f)] private float _stateBlendDuration = 1.1f;
        [SerializeField, Min(0f)] private float _positionSmoothingTime = 0.22f;
        [SerializeField, Min(0f)] private float _rotationSmoothingTime = 0.16f;
        [SerializeField, Range(0f, 1f)] private float _swayFollow = 0.35f;

        /// <summary>Offset from the car while waiting for the starting tap.</summary>
        public Vector3 ReadyOffset => _readyOffset;

        /// <summary>Offset from the car during the run.</summary>
        public Vector3 PlayOffset => _playOffset;

        /// <summary>Point the camera aims at, relative to the car.</summary>
        public Vector3 LookAtOffset => _lookAtOffset;

        public float StateBlendDuration => _stateBlendDuration;
        public float PositionSmoothingTime => _positionSmoothingTime;
        public float RotationSmoothingTime => _rotationSmoothingTime;

        /// <summary>
        /// How much of the car's lateral sway the camera copies. Following it fully makes the world
        /// feel static; ignoring it entirely makes the car slide across the frame. Partial is best.
        /// </summary>
        public float SwayFollow => _swayFollow;
    }
}
