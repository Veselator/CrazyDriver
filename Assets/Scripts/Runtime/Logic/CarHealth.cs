using System;
using CrazyDriver.Combat;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// The car's hit points.
    /// <para>
    /// Enemies carry their own health inside <see cref="Actors.Enemy"/> rather than sharing this
    /// component: an enemy's health is private to it and never observed, while the car's drives a
    /// bar on screen and the lose condition, so only this one needs to broadcast changes.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarHealth : MonoBehaviour, IDamageable
    {
        private float _max = 1f;
        private float _current = 1f;

        /// <summary>Raised on every change, carrying current and maximum hit points.</summary>
        public event Action<float, float> Changed;

        /// <summary>
        /// Raised on a damaging hit, carrying the hit points actually lost. Separate from
        /// <see cref="Changed"/> because feedback needs the size of the hit, which a before-and-after
        /// pair of totals does not give you once the bar is already empty.
        /// </summary>
        public event Action<float> Damaged;

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

            float applied = Mathf.Min(amount, _current);

            _current -= applied;
            Changed?.Invoke(_current, _max);
            Damaged?.Invoke(applied);

            if (_current <= 0f)
            {
                Died?.Invoke();
            }
        }

        /// <summary>Refills to full, redefining the maximum. Called when a run is prepared.</summary>
        public void ResetTo(float max)
        {
            _max = Mathf.Max(1f, max);
            _current = _max;

            Changed?.Invoke(_current, _max);
        }
    }
}
