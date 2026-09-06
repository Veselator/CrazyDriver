using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Lateral wander of the car. Evaluated as a function of travelled distance (not time) so the
    /// motion is reproducible for a given seed and stays independent of the current speed.
    /// </summary>
    [Serializable]
    public sealed class SwaySettings
    {
        [SerializeField, Min(0f)] private float _amplitude = 1.15f;
        [SerializeField, Min(0.01f)] private float _wavelength = 34f;
        [SerializeField, Range(1, 4)] private int _octaves = 3;
        [SerializeField, Range(0f, 1f)] private float _persistence = 0.45f;
        [SerializeField, Min(1f)] private float _lacunarity = 2.3f;
        [SerializeField, Min(0f)] private float _yawResponse = 1.6f;

        /// <summary>Peak lateral offset in meters.</summary>
        public float Amplitude => _amplitude;

        /// <summary>Distance in meters covered by one full oscillation of the base octave.</summary>
        public float Wavelength => _wavelength;

        /// <summary>Number of summed sine octaves. More octaves read as less mechanical.</summary>
        public int Octaves => _octaves;

        /// <summary>Amplitude falloff applied per octave.</summary>
        public float Persistence => _persistence;

        /// <summary>Frequency growth applied per octave.</summary>
        public float Lacunarity => _lacunarity;

        /// <summary>
        /// Scales how much the car yaws into its own lateral drift. Zero makes the car slide
        /// sideways; values near one make it look like it is actually steering.
        /// </summary>
        public float YawResponse => _yawResponse;
    }
}
