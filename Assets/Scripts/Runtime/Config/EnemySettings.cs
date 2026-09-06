using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Enemy behaviour. Note the relationship between <see cref="ActivationDistance"/>,
    /// <see cref="MoveSpeed"/> and the car's cruise speed: an enemy can only ever reach the car if
    /// it is woken far enough ahead to close its lateral gap before the car drives past. See
    /// <c>EnemySettings.CanIntercept</c>.
    /// </summary>
    [Serializable]
    public sealed class EnemySettings
    {
        [SerializeField, Min(0f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _moveSpeed = 7.5f;
        [SerializeField, Min(0f)] private float _activationDistance = 55f;
        [SerializeField, Min(0f)] private float _despawnDistanceBehind = 25f;
        [SerializeField, Min(0f)] private float _attackRange = 3.2f;
        [SerializeField, Min(0f)] private float _collisionDamage = 34f;
        [SerializeField, Min(0f)] private float _interceptLead = 0.65f;

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// How far ahead of the car, in meters of travelled distance, an enemy wakes up and starts
        /// running. This is a pure distance comparison, not a physics trigger.
        /// </summary>
        public float ActivationDistance => _activationDistance;

        /// <summary>Distance behind the car after which an idle or chasing enemy is returned to the pool.</summary>
        public float DespawnDistanceBehind => _despawnDistanceBehind;

        /// <summary>
        /// Distance from the car's origin at which contact is registered. The body is over four
        /// meters long, so anything much under three puts the attacker inside the bodywork.
        /// </summary>
        public float AttackRange => _attackRange;

        /// <summary>
        /// Damage dealt by a single impact. The enemy is destroyed by the collision, so this lands
        /// exactly once per enemy rather than ticking for as long as one stays alongside.
        /// </summary>
        public float CollisionDamage => _collisionDamage;

        /// <summary>
        /// How strongly the enemy aims at where the car <em>will be</em> rather than where it is.
        /// Zero produces a tail chase that a slower enemy can never win.
        /// </summary>
        public float InterceptLead => _interceptLead;

        /// <summary>
        /// True when an enemy woken at <see cref="ActivationDistance"/> can physically cover
        /// <paramref name="lateralOffset"/> before the car passes it. Used by the level generator to
        /// reject spawn positions that would produce enemies that never engage.
        /// </summary>
        public bool CanIntercept(float lateralOffset, float carSpeed)
        {
            if (carSpeed <= 0f)
            {
                return true;
            }

            float secondsUntilPassed = _activationDistance / carSpeed;
            float reachable = _moveSpeed * secondsUntilPassed;
            return reachable >= Mathf.Abs(lateralOffset) - _attackRange;
        }
    }
}
