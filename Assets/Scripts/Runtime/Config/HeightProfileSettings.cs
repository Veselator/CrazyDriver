using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Gentle vertical undulation of the road. Height is a function of the horizontal distance
    /// travelled, which keeps arc length equal to horizontal distance and removes the need for an
    /// arc-length lookup table.
    /// </summary>
    [Serializable]
    public sealed class HeightProfileSettings
    {
        [SerializeField, Min(0f)] private float _amplitude = 0.85f;
        [SerializeField, Min(0.01f)] private float _wavelength = 46f;
        [SerializeField, Range(1, 4)] private int _octaves = 2;
        [SerializeField, Range(0f, 1f)] private float _persistence = 0.5f;
        [SerializeField, Min(1f)] private float _lacunarity = 2.1f;

        /// <summary>Peak height deviation in meters. Keep small: the camera amplifies pitch.</summary>
        public float Amplitude => _amplitude;

        public float Wavelength => _wavelength;
        public int Octaves => _octaves;
        public float Persistence => _persistence;
        public float Lacunarity => _lacunarity;
    }
}
