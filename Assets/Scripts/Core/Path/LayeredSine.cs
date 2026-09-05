using CrazyDriver.Core.Configuration;
using UnityEngine;
using Random = System.Random;

namespace CrazyDriver.Core.Path
{
    /// <summary>
    /// A sum of sine octaves with seeded phases, evaluated against distance.
    /// <para>
    /// Used for both the road's height profile and the car's lateral sway. A single sine reads as
    /// mechanical and a per-frame random offset is not continuous; summing a few octaves with fixed
    /// random phases keeps the curve C-infinity smooth while looking unrepeating. Because the input
    /// is distance rather than time, the result is identical for a given seed regardless of frame
    /// rate or current speed.
    /// </para>
    /// </summary>
    public sealed class LayeredSine
    {
        private readonly float[] _amplitudes;
        private readonly float[] _angularFrequencies;
        private readonly float[] _phases;

        public LayeredSine(float amplitude, float wavelength, int octaves, float persistence, float lacunarity, int seed)
        {
            octaves = Mathf.Max(1, octaves);
            wavelength = Mathf.Max(0.01f, wavelength);
            lacunarity = Mathf.Max(1f, lacunarity);

            _amplitudes = new float[octaves];
            _angularFrequencies = new float[octaves];
            _phases = new float[octaves];

            // System.Random keeps generation reproducible without touching UnityEngine.Random's
            // global state, which any other system is free to consume between our calls.
            var random = new Random(seed);

            float rawAmplitude = 1f;
            float frequency = 1f;
            float amplitudeSum = 0f;

            for (int i = 0; i < octaves; i++)
            {
                _amplitudes[i] = rawAmplitude;
                _angularFrequencies[i] = 2f * Mathf.PI * frequency / wavelength;
                _phases[i] = (float)random.NextDouble() * 2f * Mathf.PI;

                amplitudeSum += rawAmplitude;
                rawAmplitude *= persistence;
                frequency *= lacunarity;
            }

            // Normalise so the summed octaves peak at exactly the requested amplitude.
            float normalisation = amplitudeSum > 0f ? amplitude / amplitudeSum : 0f;
            for (int i = 0; i < octaves; i++)
            {
                _amplitudes[i] *= normalisation;
            }
        }

        public static LayeredSine FromSettings(SwaySettings settings, int seed) =>
            new(settings.Amplitude, settings.Wavelength, settings.Octaves, settings.Persistence, settings.Lacunarity, seed);

        public static LayeredSine FromSettings(HeightProfileSettings settings, int seed) =>
            new(settings.Amplitude, settings.Wavelength, settings.Octaves, settings.Persistence, settings.Lacunarity, seed);

        public float Evaluate(float x)
        {
            float sum = 0f;
            for (int i = 0; i < _amplitudes.Length; i++)
            {
                sum += _amplitudes[i] * Mathf.Sin(_angularFrequencies[i] * x + _phases[i]);
            }

            return sum;
        }

        /// <summary>Exact analytic derivative with respect to distance.</summary>
        public float EvaluateDerivative(float x)
        {
            float sum = 0f;
            for (int i = 0; i < _amplitudes.Length; i++)
            {
                sum += _amplitudes[i] * _angularFrequencies[i] * Mathf.Cos(_angularFrequencies[i] * x + _phases[i]);
            }

            return sum;
        }
    }
}
