using UnityEngine;

namespace CrazyDriver.Core.Path
{
    /// <summary>
    /// Maps a travelled distance to a world pose. Everything positional in the game -- the car, the
    /// road tiles, enemies and bonuses -- is authored against this single abstraction, so swapping
    /// the straight corridor for a curved spline later touches one implementation and nothing else.
    /// </summary>
    public interface IPathEvaluator
    {
        /// <summary>Pose at <paramref name="distance"/> meters along the path centerline.</summary>
        PathSample Evaluate(float distance);

        /// <summary>
        /// Pose at <paramref name="distance"/>, displaced <paramref name="lateralOffset"/> meters
        /// along the local right axis.
        /// </summary>
        PathSample Evaluate(float distance, float lateralOffset);
    }
}
