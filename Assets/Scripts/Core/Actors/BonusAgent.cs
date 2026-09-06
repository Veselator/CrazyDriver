using CrazyDriver.Core.Combat;
using CrazyDriver.Combat;
using UnityEngine;

namespace CrazyDriver.Core.Actors
{
    /// <summary>
    /// A shootable pickup. Any single projectile destroys it and pays out its coins -- there is no
    /// health to whittle down, so it reads as an instant reward rather than a second enemy.
    /// </summary>
    public sealed class BonusAgent : IDamageable
    {
        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>Distance along the path this bonus was generated at.</summary>
        public float SpawnDistance { get; private set; }

        public int CoinReward { get; private set; }

        public bool IsAlive { get; private set; }

        /// <summary>
        /// True once a projectile destroyed this bonus, as opposed to it being retired unshot. The
        /// director polls this rather than subscribing, for the same pooling reason as
        /// <see cref="EnemyAgent"/>.
        /// </summary>
        public bool WasShot { get; private set; }

        public void Activate(Vector3 position, Quaternion rotation, float spawnDistance, int coinReward)
        {
            Position = position;
            Rotation = rotation;
            SpawnDistance = spawnDistance;
            CoinReward = coinReward;
            IsAlive = true;
            WasShot = false;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            IsAlive = false;
            WasShot = true;
        }

        /// <summary>Retire without paying out, e.g. when it falls behind the car unshot.</summary>
        public void Retire() => IsAlive = false;
    }
}
