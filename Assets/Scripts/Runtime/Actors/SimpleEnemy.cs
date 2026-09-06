using UnityEngine;

namespace CrazyDriver.Actors
{
    /// <summary>
    /// The ground runner: wakes up, sprints at where the car is going to be, and destroys itself on
    /// impact. The default enemy, and the reference for what a subclass has to provide.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimpleEnemy : Enemy
    {
        [Header("Chase")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;

        [SerializeField, Min(0f), Tooltip("Distance from the car's origin at which contact is " +
             "registered. The body is over four meters long, so anything much under three puts the " +
             "attacker inside the bodywork.")]
        private float _attackRange = 3.2f;

        [SerializeField, Min(0f), Tooltip("How strongly the enemy aims at where the car will be " +
             "rather than where it is. Zero produces a tail chase a slower enemy can never win.")]
        private float _interceptLead = 0.65f;

        [SerializeField, Min(0f), Tooltip("Dealt once, by the impact that destroys this enemy.")]
        private float _collisionDamage = 25f;

        public float MoveSpeed => _moveSpeed;

        public float CollisionDamage => _collisionDamage;

        protected override void Tick(float carDistance)
        {
            Vector3 toCar = Car.Position - transform.position;
            float distanceToCar = toCar.magnitude;

            if (distanceToCar <= _attackRange)
            {
                Collide();
                return;
            }

            Chase(distanceToCar);
            FaceCar();
        }

        private void Chase(float distanceToCar)
        {
            // Chasing the car's *current* position is a tail chase the enemy can never win, because
            // the car is faster. Aiming at where the car will be by the time we arrive turns the
            // same speed budget into an interception.
            float timeToReach = _moveSpeed > 0f ? distanceToCar / _moveSpeed : 0f;
            Vector3 aimPoint = Car.Position + Car.Velocity * (timeToReach * _interceptLead);

            Vector3 step = (aimPoint - transform.position).normalized * (_moveSpeed * Time.deltaTime);
            transform.position += step;
        }

        private void Collide()
        {
            // The enemy is destroyed by its own impact, so the damage lands exactly once. A
            // sustained damage-per-second would instead leave the survivor jogging alongside the
            // car draining it, which reads as a bug rather than as a hit.
            State = EnemyState.Colliding;
            FaceCar();

            CarHealth.TakeDamage(_collisionDamage);

            Release(ReleaseReason.Impact);
        }

        private void FaceCar()
        {
            Vector3 flat = Car.Position - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
            }
        }

        public override bool CanIntercept(float lateralOffset, float carSpeed)
        {
            if (carSpeed <= 0f)
            {
                return true;
            }

            float secondsUntilPassed = ActivationDistance / carSpeed;
            float reachable = _moveSpeed * secondsUntilPassed;

            return reachable >= Mathf.Abs(lateralOffset) - _attackRange;
        }
    }
}
