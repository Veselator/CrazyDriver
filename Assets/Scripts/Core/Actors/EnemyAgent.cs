using CrazyDriver.Core.Combat;
using CrazyDriver.Core.Configuration;
using UnityEngine;

namespace CrazyDriver.Core.Actors
{
    public enum EnemyState
    {
        /// <summary>Placed on the map, waiting for the car to come within activation distance.</summary>
        Idle,

        /// <summary>Running towards the car's projected position.</summary>
        Chasing,

        /// <summary>Reached the car this frame. Destroyed by the impact immediately after.</summary>
        Colliding,

        Dead
    }

    /// <summary>
    /// One enemy, as pure state. It owns no Transform: the pooled view copies
    /// <see cref="Position"/> and <see cref="Rotation"/> each frame and plays animations off
    /// <see cref="State"/>.
    /// </summary>
    public sealed class EnemyAgent : IDamageable
    {
        private readonly EnemySettings _settings;

        private float _health;

        public EnemyAgent(EnemySettings settings)
        {
            _settings = settings;
        }

        public EnemyState State { get; private set; } = EnemyState.Idle;

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>Distance along the path this enemy was generated at.</summary>
        public float SpawnDistance { get; private set; }

        public float HealthNormalized => _settings.MaxHealth > 0f ? _health / _settings.MaxHealth : 0f;

        public bool IsAlive => State != EnemyState.Dead && _health > 0f;

        /// <summary>True when this enemy was destroyed by ramming the car rather than by gunfire.</summary>
        public bool DiedOnImpact => State == EnemyState.Colliding;

        public void Activate(Vector3 position, Quaternion rotation, float spawnDistance)
        {
            Position = position;
            Rotation = rotation;
            SpawnDistance = spawnDistance;

            _health = _settings.MaxHealth;
            State = EnemyState.Idle;
        }

        /// <param name="carDistance">How far the car has travelled, in absolute meters.</param>
        /// <param name="carPosition">The car's current world position.</param>
        /// <param name="carVelocity">The car's current world velocity, used to lead the chase.</param>
        /// <returns>Damage this enemy dealt to the car during this tick, zero if none.</returns>
        /// <remarks>
        /// Returning the damage instead of raising an event is what keeps pooling cheap: a pooled
        /// agent that owned events would need every subscriber wired up on rent and torn down on
        /// release, and a single missed unsubscribe would resurrect a dead enemy's damage.
        /// </remarks>
        public float Tick(float deltaTime, float carDistance, Vector3 carPosition, Vector3 carVelocity)
        {
            if (State == EnemyState.Dead)
            {
                return 0f;
            }

            if (State == EnemyState.Idle)
            {
                // Activation is a distance comparison against the path, not a physics trigger:
                // it costs nothing, is frame-rate independent, and needs no collider.
                if (SpawnDistance - carDistance > _settings.ActivationDistance)
                {
                    return 0f;
                }

                State = EnemyState.Chasing;
            }

            Vector3 toCar = carPosition - Position;
            float distanceToCar = toCar.magnitude;

            if (distanceToCar <= _settings.AttackRange)
            {
                // The enemy is destroyed by its own impact, so the damage lands exactly once. A
                // sustained damage-per-second would instead leave the survivor jogging alongside
                // the car draining it, which reads as a bug rather than as a hit.
                State = EnemyState.Colliding;
                FaceTowards(carPosition);

                _health = 0f;
                return _settings.CollisionDamage;
            }

            State = EnemyState.Chasing;
            Chase(deltaTime, carPosition, carVelocity, distanceToCar);
            FaceTowards(carPosition);

            return 0f;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            _health -= amount;

            if (_health <= 0f)
            {
                _health = 0f;
                State = EnemyState.Dead;
            }
        }

        /// <summary>Silently retire the enemy, e.g. when it falls too far behind the car.</summary>
        public void Retire() => State = EnemyState.Dead;

        private void Chase(float deltaTime, Vector3 carPosition, Vector3 carVelocity, float distanceToCar)
        {
            // Chasing the car's *current* position is a tail chase the enemy can never win, because
            // the car is faster. Aiming at where the car will be by the time we arrive turns the
            // same speed budget into an interception.
            float timeToReach = _settings.MoveSpeed > 0f ? distanceToCar / _settings.MoveSpeed : 0f;
            Vector3 aimPoint = carPosition + carVelocity * (timeToReach * _settings.InterceptLead);

            Vector3 step = (aimPoint - Position).normalized * (_settings.MoveSpeed * deltaTime);
            Position += step;
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 flat = target - Position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.0001f)
            {
                Rotation = Quaternion.LookRotation(flat, Vector3.up);
            }
        }
    }
}
