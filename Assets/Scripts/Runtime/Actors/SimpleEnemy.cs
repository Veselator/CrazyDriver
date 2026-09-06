using UnityEngine;
using Random = System.Random;

namespace CrazyDriver.Actors
{
    /// <summary>
    /// The ground runner: wakes up, sprints at where the car is going to be, and destroys itself on
    /// impact. The default enemy, and the reference for what a subclass has to provide.
    /// <para>
    /// A perfect interception curve looks like a guided missile, not like a person. Three cheap
    /// sources of noise fix that without touching the maths that makes the chase work: the aim point
    /// is only refreshed a few times a second, the heading wanders on smooth noise, and both the
    /// running speed and its pace vary per enemy and over time.
    /// </para>
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

        [SerializeField, Min(0f), Tooltip("Degrees per second the body can turn. Low values make " +
             "the enemy lean into its turns instead of snapping to face the car.")]
        private float _turnSpeed = 420f;

        [Header("Randomness")]
        [SerializeField, Range(0f, 0.6f), Tooltip("How much running speed differs between one " +
             "enemy and the next, as a fraction. 0.2 means anywhere from 80% to 120% of the speed above.")]
        private float _speedVariance = 0.22f;

        [SerializeField, Range(0f, 1f), Tooltip("How much each enemy's own speed surges and eases " +
             "while it runs.")]
        private float _surgeAmount = 0.28f;

        [SerializeField, Min(0f), Tooltip("Surges per second. Slow values read as stamina, fast " +
             "ones as panic.")]
        private float _surgeRate = 0.6f;

        [SerializeField, Min(0f), Tooltip("How far off a straight line the enemy wanders, in " +
             "meters of sideways drift at full swing.")]
        private float _weaveAmplitude = 1.6f;

        [SerializeField, Min(0f), Tooltip("How quickly the wander changes direction.")]
        private float _weaveRate = 0.7f;

        [SerializeField, Tooltip("Seconds between re-aims, rolled fresh each time. This is the " +
             "enemy's reaction time: it runs at where it last saw the car going, not at a " +
             "continuously perfect solution.")]
        private Vector2 _reactionInterval = new(0.16f, 0.42f);

        [SerializeField, Min(0f), Tooltip("Meters of scatter on the aim point, so a crowd converges " +
             "on the car rather than on one shared pixel.")]
        private float _aimScatter = 1.4f;

        private Random _random = new(0);
        private float _speedScale = 1f;
        private float _weaveSeed;
        private float _surgeSeed;
        private Vector3 _aimScatterOffset;
        private Vector3 _aimPoint;
        private float _reactionTimer;

        public float MoveSpeed => _moveSpeed;

        public float CollisionDamage => _collisionDamage;

        public override void Activate(Vector3 position, Quaternion rotation, float spawnDistance)
        {
            base.Activate(position, rotation, spawnDistance);

            // Seeded from where this enemy stands, so a replayed seed replays the same crowd rather
            // than a differently-shaped one. UnityEngine.Random would have made the run only
            // approximately reproducible, which is worse than not reproducible at all.
            _random = new Random(spawnDistance.GetHashCode());

            _speedScale = 1f + Signed() * _speedVariance;
            _weaveSeed = (float)_random.NextDouble() * 100f;
            _surgeSeed = (float)_random.NextDouble() * 100f;

            _aimScatterOffset = new Vector3(Signed() * _aimScatter, 0f, Signed() * _aimScatter);

            _reactionTimer = 0f;
            _aimPoint = position;
        }

        protected override void Tick(float carDistance)
        {
            Vector3 toCar = Car.Position - transform.position;
            float distanceToCar = toCar.magnitude;

            if (distanceToCar <= _attackRange)
            {
                Collide();
                return;
            }

            _reactionTimer -= Time.deltaTime;
            if (_reactionTimer <= 0f)
            {
                RefreshAim(distanceToCar);
            }

            Chase(distanceToCar);
        }

        /// <summary>
        /// Recomputes where to run, then goes quiet for a fraction of a second.
        /// <para>
        /// Solving the interception every frame is what made the movement look mechanical: the
        /// enemy was correcting continuously against a target that also moves continuously, which
        /// traces a perfectly smooth arc. Holding the last solution for a beat leaves the small
        /// corrections a runner actually makes.
        /// </para>
        /// </summary>
        private void RefreshAim(float distanceToCar)
        {
            float speed = _moveSpeed * _speedScale;
            float timeToReach = speed > 0f ? distanceToCar / speed : 0f;

            _aimPoint = Car.Position
                        + Car.Velocity * (timeToReach * _interceptLead)
                        + _aimScatterOffset;

            _reactionTimer = Mathf.Max(
                0.01f,
                Mathf.Lerp(_reactionInterval.x, _reactionInterval.y, (float)_random.NextDouble()));
        }

        /// <summary>A random number in -1..1 from this enemy's own seeded stream.</summary>
        private float Signed() => (float)_random.NextDouble() * 2f - 1f;

        private void Chase(float distanceToCar)
        {
            Vector3 toAim = _aimPoint - transform.position;
            toAim.y = 0f;

            if (toAim.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 forward = toAim.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, forward);

            // Perlin rather than a sine: a sine makes every enemy weave to the same rhythm, and a
            // crowd of them pulses in unison like a screensaver. Sampled against time with a
            // per-instance offset, the noise is smooth but never repeats between two of them.
            float wander = Mathf.PerlinNoise(_weaveSeed, Time.time * _weaveRate) * 2f - 1f;

            // Damped as the enemy closes in, or the last stride would swerve past the bumper.
            float approach = Mathf.Clamp01(distanceToCar / 14f);
            Vector3 heading = (forward + side * (wander * _weaveAmplitude * 0.12f * approach)).normalized;

            float surge = 1f + (Mathf.PerlinNoise(Time.time * _surgeRate, _surgeSeed) * 2f - 1f) * _surgeAmount;
            float speed = _moveSpeed * _speedScale * Mathf.Max(0.1f, surge);

            Vector3 next = transform.position + heading * (speed * Time.deltaTime);

            // The stride is computed flat, so the height has to be put back afterwards or the enemy
            // walks straight into the next rise in the road.
            next.y = GroundHeightAt(next);

            transform.position = next;
            Face(heading);
        }

        private void Collide()
        {
            // The enemy is destroyed by its own impact, so the damage lands exactly once. A
            // sustained damage-per-second would instead leave the survivor jogging alongside the
            // car draining it, which reads as a bug rather than as a hit.
            State = EnemyState.Colliding;
            Face(Car.Position - transform.position);

            CarHealth.TakeDamage(_collisionDamage);

            Release(ReleaseReason.Impact);
        }

        /// <summary>
        /// Turns towards where the enemy is actually running, at a limited rate. Facing the car
        /// directly would hide the wander entirely -- the body would point at the target while the
        /// feet went sideways.
        /// </summary>
        private void Face(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = _turnSpeed > 0f
                ? Quaternion.RotateTowards(transform.rotation, wanted, _turnSpeed * Time.deltaTime)
                : wanted;
        }

        public override bool CanIntercept(float lateralOffset, float carSpeed)
        {
            if (carSpeed <= 0f)
            {
                return true;
            }

            // Judged on the slowest an enemy of this kind can roll, so the generator never places
            // one that only the lucky end of the variance could reach.
            float slowest = _moveSpeed * (1f - _speedVariance);
            float secondsUntilPassed = ActivationDistance / carSpeed;
            float reachable = slowest * secondsUntilPassed;

            return reachable >= Mathf.Abs(lateralOffset) - _attackRange;
        }
    }
}
