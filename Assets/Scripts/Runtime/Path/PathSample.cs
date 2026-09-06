using UnityEngine;

namespace CrazyDriver.Paths
{
    /// <summary>
    /// A pose on the path at a given distance.
    /// <para>
    /// This is deliberately a value type rather than a <see cref="Transform"/>: a Transform is a
    /// component bound to a live GameObject and cannot be produced for an arbitrary distance.
    /// </para>
    /// </summary>
    public readonly struct PathSample
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public PathSample(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Forward => Rotation * Vector3.forward;

        public Vector3 Right => Rotation * Vector3.right;

        public Vector3 Up => Rotation * Vector3.up;
    }
}
