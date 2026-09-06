using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>Opening animation of the starting gate.</summary>
    [Serializable]
    public sealed class GateSettings
    {
        [SerializeField, Min(0f)] private float _distanceAhead = 16f;
        [SerializeField, Min(0f)] private float _openDuration = 0.85f;
        [SerializeField, Min(0f)] private float _openTravel = 3.2f;
        [SerializeField, Min(0f)] private float _carStartDelay = 0.25f;

        /// <summary>Where the gate stands relative to the car's starting distance.</summary>
        public float DistanceAhead => _distanceAhead;

        public float OpenDuration => _openDuration;

        /// <summary>How far each wing slides sideways when opening, in meters.</summary>
        public float OpenTravel => _openTravel;

        /// <summary>Head start given to the gate before the car begins to accelerate.</summary>
        public float CarStartDelay => _carStartDelay;
    }
}
