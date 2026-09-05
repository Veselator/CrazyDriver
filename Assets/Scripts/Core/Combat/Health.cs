using System;
using UnityEngine;

namespace CrazyDriver.Core.Combat
{
    /// <summary>
    /// Plain hit points. Shared verbatim by the car and by every enemy -- there is no reason for
    /// two implementations of subtraction.
    /// </summary>
    public sealed class Health : IDamageable
    {
        private float _max;
        private float _current;

        public Health(float max)
        {
            _max = Mathf.Max(1f, max);
            _current = _max;
        }

        /// <summary>Raised on every change, carrying current and maximum hit points.</summary>
        public event Action<float, float> Changed;

        public event Action Died;

        public float Current => _current;

        public float Max => _max;

        public float Normalized => _max > 0f ? _current / _max : 0f;

        public bool IsAlive => _current > 0f;

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            _current = Mathf.Max(0f, _current - amount);
            Changed?.Invoke(_current, _max);

            if (_current <= 0f)
            {
                Died?.Invoke();
            }
        }

        /// <summary>Refill, optionally redefining the maximum -- used when a run restarts.</summary>
        public void Reset(float? max = null)
        {
            if (max.HasValue)
            {
                _max = Mathf.Max(1f, max.Value);
            }

            _current = _max;
            Changed?.Invoke(_current, _max);
        }
    }
}
