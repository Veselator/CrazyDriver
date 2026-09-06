using System;
using CrazyDriver.Combat;
using UnityEngine;

namespace CrazyDriver.Actors
{
    /// <summary>
    /// A shootable pickup. Any single projectile destroys it and pays out its coins -- there is no
    /// health to whittle down, so it reads as an instant reward rather than a second enemy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bonus : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// Raised when the bonus leaves play, carrying true when it was shot rather than retired
        /// unclaimed. Subscribed once by the pool, never per rent.
        /// </summary>
        public event Action<Bonus, bool> Released;

        public bool IsAlive { get; private set; }

        public float SpawnDistance { get; private set; }

        public int CoinReward { get; private set; }

        public void Activate(Vector3 position, Quaternion rotation, float spawnDistance, int coinReward)
        {
            transform.SetPositionAndRotation(position, rotation);

            SpawnDistance = spawnDistance;
            CoinReward = coinReward;
            IsAlive = true;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            IsAlive = false;
            Released?.Invoke(this, true);
        }

        /// <summary>Retire without paying out: it fell behind the car unshot, or the run ended.</summary>
        public void Retire()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Released?.Invoke(this, false);
        }
    }
}
