using System;
using UnityEngine;

namespace CrazyDriver.Core.Path
{
    /// <summary>
    /// The single source of truth for how far the car has travelled, in absolute meters.
    /// <para>
    /// Everything that needs to know "where are we" reads this rather than a Transform position:
    /// enemy activation, road streaming, bonus placement and the win condition. Keeping it as one
    /// scalar is what lets those systems be O(1) instead of scanning the scene.
    /// </para>
    /// </summary>
    public sealed class PathProgress
    {
        private float _distance;

        /// <summary>Raised every time the distance changes, carrying the new absolute distance.</summary>
        public event Action<float> DistanceChanged;

        /// <summary>Raised once per whole meter crossed, for cheap UI updates.</summary>
        public event Action<int> MeterPassed;

        private int _lastWholeMeter;

        public float Distance => _distance;

        public float Advance(float delta)
        {
            if (delta <= 0f)
            {
                return _distance;
            }

            _distance += delta;
            DistanceChanged?.Invoke(_distance);

            int wholeMeter = Mathf.FloorToInt(_distance);
            if (wholeMeter != _lastWholeMeter)
            {
                _lastWholeMeter = wholeMeter;
                MeterPassed?.Invoke(wholeMeter);
            }

            return _distance;
        }

        public void Reset()
        {
            _distance = 0f;
            _lastWholeMeter = 0;
            DistanceChanged?.Invoke(_distance);
            MeterPassed?.Invoke(0);
        }
    }
}
