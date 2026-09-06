using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>Streaming window for the tiled road geometry.</summary>
    [Serializable]
    public sealed class RoadSettings
    {
        [SerializeField, Min(0.1f)] private float _tileLength = 20f;
        [SerializeField, Min(0f)] private float _distanceAhead = 140f;
        [SerializeField, Min(0f)] private float _distanceBehind = 40f;
        [SerializeField, Min(0f)] private float _halfWidth = 6.5f;

        /// <summary>Length of one road prefab along the path, in meters.</summary>
        public float TileLength => _tileLength;

        /// <summary>How far in front of the car tiles are kept alive.</summary>
        public float DistanceAhead => _distanceAhead;

        /// <summary>How far behind the car tiles are kept alive before returning to the pool.</summary>
        public float DistanceBehind => _distanceBehind;

        /// <summary>Half the drivable width, used to clamp generated spawn offsets.</summary>
        public float HalfWidth => _halfWidth;
    }
}
