using UnityEngine;

namespace CrazyDriver.Paths
{
    /// <summary>
    /// The path currently being played, behind a stable reference.
    /// <para>
    /// A new evaluator is built for every run, but the car, the directors and the road streamer are
    /// all constructed once at boot. Handing them this indirection instead of the concrete
    /// evaluator means a restart swaps the curve in one place rather than rebuilding the object
    /// graph -- and it removes any chance of one system still holding the previous run's path.
    /// </para>
    /// </summary>
    public sealed class ActivePath : IPathEvaluator
    {
        private IPathEvaluator _current;

        public bool IsLoaded => _current != null;

        public void Set(IPathEvaluator path) => _current = path;

        public PathSample Evaluate(float distance) => Evaluate(distance, 0f);

        public PathSample Evaluate(float distance, float lateralOffset) =>
            _current?.Evaluate(distance, lateralOffset)
            ?? new PathSample(new Vector3(lateralOffset, 0f, distance), Quaternion.identity);
    }
}
