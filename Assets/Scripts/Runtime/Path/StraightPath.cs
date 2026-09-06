using CrazyDriver.Config;
using UnityEngine;

namespace CrazyDriver.Paths
{
    /// <summary>
    /// The looped straight corridor the brief asks for: the centerline runs along world +Z and only
    /// its height varies.
    /// <para>
    /// Treating distance as the <em>horizontal</em> coordinate rather than true arc length is the
    /// deliberate simplification that keeps this class trivial. With gentle slopes the two differ by
    /// well under a percent, so no arc-length lookup table is needed and the car's fixed speed stays
    /// genuinely fixed. A curved path would not have that luxury -- which is exactly why the
    /// abstraction sits behind <see cref="IPathEvaluator"/>.
    /// </para>
    /// </summary>
    public sealed class StraightPath : IPathEvaluator
    {
        private readonly LayeredSine _height;

        public StraightPath(HeightProfileSettings settings, int seed)
        {
            _height = LayeredSine.FromSettings(settings, seed);
        }

        public PathSample Evaluate(float distance) => Evaluate(distance, 0f);

        public PathSample Evaluate(float distance, float lateralOffset)
        {
            float height = _height.Evaluate(distance);
            float slope = _height.EvaluateDerivative(distance);

            // Tangent of the centerline in the ZY plane; X stays flat because the road is straight.
            Vector3 forward = new Vector3(0f, slope, 1f).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            Vector3 position = new(lateralOffset, height, distance);
            return new PathSample(position, rotation);
        }

        /// <summary>Height of the centerline at <paramref name="distance"/>, in meters.</summary>
        public float GetHeight(float distance) => _height.Evaluate(distance);
    }
}
