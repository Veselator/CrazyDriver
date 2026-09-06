using System;
using UnityEngine;

namespace CrazyDriver.Level
{
    /// <summary>
    /// One piece of scenery placed by hand along the map: a sign, a barrier, a building.
    /// <para>
    /// Position is authored as a distance plus an offset rather than as a world position, for the
    /// same reason everything else in the game is: the road is a generated curve whose world
    /// coordinates are not known until a seed is rolled. A distance and an offset survive that,
    /// world coordinates do not.
    /// </para>
    /// </summary>
    [Serializable]
    public struct MapObjectData
    {
        public GameObject prefab;

        [Tooltip("Meters along the road where this object stands.")]
        [Min(0f)] public float distance;

        [Tooltip("Offset from the centerline, in road-local axes: X across, Y up, Z further along.")]
        public Vector3 offset;

        [Tooltip("Extra rotation around the road's up axis, in degrees.")]
        public float yaw;
    }
}
