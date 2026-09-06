using System;
using CrazyDriver.Combat;
using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver.Actors
{
    public enum EnemyState
    {
        /// <summary>Standing on the map, waiting for the car to come within activation distance.</summary>
        Idle,

        /// <summary>Awake and acting on the car.</summary>
        Chasing,

        /// <summary>Reached the car this frame. Destroyed by the impact immediately after.</summary>
        Colliding,

        Dead
    }

    /// <summary>Why an enemy left play. The spawner reacts differently to each.</summary>
    public enum ReleaseReason
    {
        /// <summary>Shot down. Counts as a kill and bursts.</summary>
        Shot,

        /// <summary>Reached the car and destroyed itself on it. Damages the car.</summary>
        Impact,

        /// <summary>Fell behind, or the run ended. Silent.</summary>
        Retired
    }

    /// <summary>
    /// What every enemy has regardless of how it behaves: health, the activation rule, the retire
    /// rule, and the two events the spawner listens to. Behaviour is left to the subclass.
    /// <para>
    /// Everything here is tuned on the prefab rather than in a shared settings block, because the
    /// numbers only mean anything next to the behaviour that reads them. A flier's climb rate and a
    /// runner's ground speed are not the same quantity, and one shared asset holding both would
    /// grow a field per enemy type that every other type ignores.
    /// </para>
    /// <para>
    /// The matching <see cref="View.EnemyView"/> reads <see cref="State"/> and drives the visuals,
    /// and never touches any of this.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Enemies)]
    public abstract class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Enemy")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        [SerializeField, Min(0f), Tooltip("How far ahead of the car, in meters of travelled " +
             "distance, this enemy wakes up. A pure distance comparison, not a physics trigger.")]
        private float _activationDistance = 66.6f;

        [SerializeField, Min(0f), Tooltip("Meters behind the car after which the enemy is pooled again.")]
        private float _despawnDistanceBehind = 25f;

        /// <summary>
        /// Raised when the enemy leaves play, carrying why.
        /// <para>
        /// The spawner subscribes once, when the pool builds this instance, and never unsubscribes.
        /// Wiring it per rent instead is how a pooled object ends up with a dead subscriber list.
        /// </para>
        /// </summary>
        public event Action<Enemy, ReleaseReason> Released;

        /// <summary>
        /// Raised on every hit that lands, carrying the damage actually applied -- clamped to the
        /// health remaining, so the number on screen is never larger than the life it took.
        /// </summary>
        public event Action<Enemy, float> Damaged;

        /// <summary>The car this enemy is acting on. Available from <see cref="Bind"/> onwards.</summary>
        protected CarMotor Car { get; private set; }

        /// <summary>Distance and curve for the run. Activation is measured against this.</summary>
        protected PathTracker Path { get; private set; }

        /// <summary>The car's hit points, for enemies that damage it.</summary>
        protected CarHealth CarHealth { get; private set; }

        public EnemyState State { get; protected set; } = EnemyState.Dead;

        /// <summary>Distance along the path this enemy was generated at.</summary>
        public float SpawnDistance { get; private set; }

        public float Health { get; private set; }

        public float MaxHealth => _maxHealth;

        /// <summary>Read by the spawner to decide how far ahead of the car to stream enemies in.</summary>
        public float ActivationDistance => _activationDistance;

        public bool IsAlive => State != EnemyState.Dead;

        [SerializeField] private int _coinsBonusPerKill = 4;
        public int CoinsBonusPerKill => _coinsBonusPerKill;

        /// <summary>Called once by the pool when the instance is created.</summary>
        public void Bind(CarMotor car, PathTracker path, CarHealth carHealth)
        {
            Car = car;
            Path = path;
            CarHealth = carHealth;
        }

        public virtual void Activate(Vector3 position, Quaternion rotation, float spawnDistance)
        {
            transform.SetPositionAndRotation(position, rotation);

            SpawnDistance = spawnDistance;
            Health = _maxHealth;
            State = EnemyState.Idle;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            // Clamped to what is left, so the popup over a dying enemy reads the life it actually
            // took rather than the weapon's nominal damage.
            float applied = Mathf.Min(amount, Health);
            Health -= applied;

            Damaged?.Invoke(this, applied);

            if (Health > 0f)
            {
                return;
            }

            Release(ReleaseReason.Shot);
        }

        /// <summary>Retire without counting as a kill: it fell behind, or the run ended.</summary>
        public void Retire() => Release(ReleaseReason.Retired);

        /// <summary>
        /// The activation and retirement rules, which are the same for every enemy. What happens in
        /// between belongs to the subclass, in <see cref="Tick"/>.
        /// </summary>
        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            float carDistance = Path.Distance;

            if (State == EnemyState.Idle)
            {
                // Activation is a distance comparison along the path, not a physics trigger: it
                // costs two floats, is frame-rate independent, and needs no collider or rigidbody.
                if (SpawnDistance - carDistance > _activationDistance)
                {
                    return;
                }

                State = EnemyState.Chasing;
                OnActivated();
            }

            Tick(carDistance);

            if (IsAlive && HasFallenBehind())
            {
                Retire();
            }
        }

        /// <summary>One frame of behaviour, called only while awake and alive.</summary>
        protected abstract void Tick(float carDistance);

        /// <summary>Called on the frame the enemy wakes up. Nothing to do by default.</summary>
        protected virtual void OnActivated()
        {
        }

        /// <summary>Leaves play for the given reason. Safe to call twice.</summary>
        protected void Release(ReleaseReason reason)
        {
            if (!IsAlive)
            {
                return;
            }

            State = EnemyState.Dead;
            Released?.Invoke(this, reason);
        }

        /// <summary>
        /// The height of the road surface under a world position.
        /// <para>
        /// A chase computed in the XZ plane keeps whatever height the enemy spawned at, and the road
        /// rises and falls with distance, so after a few strides the enemy is buried in a hill or
        /// walking above one. This resolves the height the same way everything else does: by asking
        /// the path, rather than by casting a ray at geometry that is only an approximation of it.
        /// </para>
        /// <para>
        /// The position is converted back into a distance and a lateral offset by projecting onto
        /// the path's axes at the car -- the same projection <see cref="HasFallenBehind"/> uses.
        /// </para>
        /// </summary>
        protected float GroundHeightAt(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - Car.Position;
            Vector3 forward = Car.PathRotation * Vector3.forward;
            Vector3 right = Car.PathRotation * Vector3.right;

            float distance = Path.Distance + Vector3.Dot(offset, forward);
            float lateral = Car.LateralOffset + Vector3.Dot(offset, right);

            return Path.Evaluate(distance, lateral).Position.y;
        }

        /// <summary>
        /// True once the car has driven far enough past. Projects the offset onto the path's forward
        /// axis: straight-line distance would keep an enemy alive forever while it ran alongside.
        /// </summary>
        protected virtual bool HasFallenBehind()
        {
            Vector3 forward = Car.PathRotation * Vector3.forward;
            float along = Vector3.Dot(transform.position - Car.Position, forward);

            return -along > _despawnDistanceBehind;
        }

        /// <summary>
        /// Whether this enemy, woken at its activation distance, can reach a car travelling at
        /// <paramref name="carSpeed"/> from <paramref name="lateralOffset"/> meters off the
        /// centerline. The level generator asks the prefab before placing one, so an enemy that
        /// could only ever stand and watch is pulled inwards until it can engage.
        /// </summary>
        public virtual bool CanIntercept(float lateralOffset, float carSpeed) => true;
    }
}
