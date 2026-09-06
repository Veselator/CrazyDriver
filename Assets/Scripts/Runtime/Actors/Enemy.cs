using System;
using CrazyDriver.Combat;
using CrazyDriver.Config;
using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver.Actors
{
    public enum EnemyState
    {
        /// <summary>Standing on the map, waiting for the car to come within activation distance.</summary>
        Idle,

        /// <summary>Running at the car's projected position.</summary>
        Chasing,

        /// <summary>Reached the car this frame. Destroyed by the impact immediately after.</summary>
        Colliding,

        Dead
    }

    /// <summary>
    /// One enemy: its state, its health and its movement. The matching <see cref="EnemyView"/> reads
    /// <see cref="State"/> and drives the visuals, and never touches any of this.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Enemies)]
    public sealed class Enemy : MonoBehaviour, IDamageable
    {
        private EnemySettings _settings;
        private CarMotor _car;
        private PathTracker _path;
        private CarHealth _carHealth;

        private float _health;

        /// <summary>
        /// Raised when the enemy leaves play, carrying true when gunfire killed it rather than the
        /// bumper or the streaming window.
        /// <para>
        /// The spawner subscribes once, when the pool builds this instance, and never unsubscribes.
        /// Wiring it per rent instead is how a pooled object ends up with a dead subscriber list.
        /// </para>
        /// </summary>
        public event Action<Enemy, bool> Released;

        public EnemyState State { get; private set; } = EnemyState.Dead;

        /// <summary>Distance along the path this enemy was generated at.</summary>
        public float SpawnDistance { get; private set; }

        public bool IsAlive => State != EnemyState.Dead;

        /// <summary>Called once by the pool when the instance is created.</summary>
        public void Bind(EnemySettings settings, CarMotor car, PathTracker path, CarHealth carHealth)
        {
            _settings = settings;
            _car = car;
            _path = path;
            _carHealth = carHealth;
        }

        public void Activate(Vector3 position, Quaternion rotation, float spawnDistance)
        {
            transform.SetPositionAndRotation(position, rotation);

            SpawnDistance = spawnDistance;
            _health = _settings.MaxHealth;
            State = EnemyState.Idle;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            _health -= amount;
            if (_health > 0f)
            {
                return;
            }

            State = EnemyState.Dead;
            Released?.Invoke(this, true);
        }

        /// <summary>Retire without counting as a kill: it fell behind, or the run ended.</summary>
        public void Retire()
        {
            if (!IsAlive)
            {
                return;
            }

            State = EnemyState.Dead;
            Released?.Invoke(this, false);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            float carDistance = _path.Distance;

            if (State == EnemyState.Idle)
            {
                // Activation is a distance comparison along the path, not a physics trigger: it
                // costs two floats, is frame-rate independent, and needs no collider or rigidbody.
                if (SpawnDistance - carDistance > _settings.ActivationDistance)
                {
                    return;
                }

                State = EnemyState.Chasing;
            }

            Vector3 toCar = _car.Position - transform.position;
            float distanceToCar = toCar.magnitude;

            if (distanceToCar <= _settings.AttackRange)
            {
                Collide();
                return;
            }

            State = EnemyState.Chasing;
            Chase(distanceToCar);
            FaceCar();

            if (HasFallenBehind(carDistance))
            {
                Retire();
            }
        }

        private void Chase(float distanceToCar)
        {
            // Chasing the car's *current* position is a tail chase the enemy can never win, because
            // the car is faster. Aiming at where the car will be by the time we arrive turns the
            // same speed budget into an interception.
            float timeToReach = _settings.MoveSpeed > 0f ? distanceToCar / _settings.MoveSpeed : 0f;
            Vector3 aimPoint = _car.Position + _car.Velocity * (timeToReach * _settings.InterceptLead);

            Vector3 step = (aimPoint - transform.position).normalized * (_settings.MoveSpeed * Time.deltaTime);
            transform.position += step;
        }

        private void Collide()
        {
            // The enemy is destroyed by its own impact, so the damage lands exactly once. A
            // sustained damage-per-second would instead leave the survivor jogging alongside the
            // car draining it, which reads as a bug rather than as a hit.
            State = EnemyState.Colliding;
            FaceCar();

            _carHealth.TakeDamage(_settings.CollisionDamage);

            State = EnemyState.Dead;
            Released?.Invoke(this, false);
        }

        private bool HasFallenBehind(float carDistance)
        {
            // Project the offset from the car onto the path's forward axis. Straight-line distance
            // would keep an enemy alive forever while it ran alongside the car.
            Vector3 forward = _car.PathRotation * Vector3.forward;
            float along = Vector3.Dot(transform.position - _car.Position, forward);

            return -along > _settings.DespawnDistanceBehind;
        }

        private void FaceCar()
        {
            Vector3 flat = _car.Position - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
            }
        }
    }
}
