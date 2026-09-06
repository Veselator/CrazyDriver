using System;
using CrazyDriver.Paths;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// The single source of truth for how far the run has travelled, and for the curve it travels
    /// along.
    /// <para>
    /// Everything positional asks this rather than reading a Transform: enemy activation, road
    /// streaming, spawn placement and the win condition. Keeping it as one scalar plus one
    /// evaluator is what lets those systems be arithmetic instead of scene queries.
    /// </para>
    /// <para>
    /// It has no Update of its own. <see cref="CarMotor"/> advances it, which is what guarantees
    /// distance and the car's position can never disagree.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PathTracker : MonoBehaviour, IPathEvaluator
    {
        private IPathEvaluator _path;
        private float _distance;
        private int _lastWholeMeter;

        /// <summary>Raised whenever the distance changes, carrying the new absolute distance.</summary>
        public event Action<float> DistanceChanged;

        /// <summary>Raised once per whole meter crossed, for readouts that need not update per frame.</summary>
        public event Action<int> MeterPassed;

        /// <summary>Absolute distance travelled this run, in meters.</summary>
        public float Distance => _distance;

        public bool HasPath => _path != null;

        /// <summary>
        /// Installs the curve for a new run. Called before anything is reset, because the car
        /// recomputes its pose against the path the moment it is placed back at zero.
        /// </summary>
        public void SetPath(IPathEvaluator path) => _path = path;

        public void Advance(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }

            _distance += delta;
            DistanceChanged?.Invoke(_distance);

            int wholeMeter = Mathf.FloorToInt(_distance);
            if (wholeMeter != _lastWholeMeter)
            {
                _lastWholeMeter = wholeMeter;
                MeterPassed?.Invoke(wholeMeter);
            }
        }

        public void ResetProgress()
        {
            _distance = 0f;
            _lastWholeMeter = 0;

            DistanceChanged?.Invoke(_distance);
            MeterPassed?.Invoke(0);
        }

        public PathSample Evaluate(float distance) => Evaluate(distance, 0f);

        public PathSample Evaluate(float distance, float lateralOffset) =>
            _path?.Evaluate(distance, lateralOffset)
            ?? new PathSample(new Vector3(lateralOffset, 0f, distance), Quaternion.identity);
    }
}
