using System;
using CrazyDriver.Combat;
using CrazyDriver.Config;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// One shot. It flies itself and resolves its own hit, then announces that it is finished and
    /// leaves the pooling to whoever created it.
    /// <para>
    /// It carries no collider and no Rigidbody. Each frame it sweeps a sphere along the segment it
    /// is about to cover, because at 90 m/s a collider-based bullet passes clean through a stickman
    /// between two fixed-update steps. A sweep cannot miss, costs one cast per live shot -- exactly
    /// what a physics body would have cost -- and the hit still resolves from a collider, as the
    /// brief requires. It is only the bullet that has none.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileView : MonoBehaviour
    {
        /// <summary>
        /// Shared across every projectile. The sweep is synchronous, so no two shots are ever
        /// mid-cast at the same moment, and a buffer per pooled instance would be pure waste.
        /// </summary>
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        [SerializeField] private TrailRenderer _trail;

        /// <summary>
        /// Raised when the shot is spent -- it hit something, or it ran out of lifetime.
        /// <para>
        /// The pool subscribes once when it builds the instance and never unsubscribes. Wiring this
        /// per rent instead is how a pooled object ends up with a dead subscriber list.
        /// </para>
        /// </summary>
        public event Action<ProjectileView> OnProjectileDied;

        private WeaponSettings _weapon;
        private LayerMask _hitMask;
        private Vector3 _direction;
        private float _remainingLifetime;

        public bool IsAlive { get; private set; }

        /// <summary>Called once by the pool when the instance is created.</summary>
        public void Bind(WeaponSettings weapon, LayerMask hitMask)
        {
            _weapon = weapon;
            _hitMask = hitMask;
        }

        public void Launch(Vector3 origin, Quaternion rotation)
        {
            transform.SetPositionAndRotation(origin, rotation);

            _direction = rotation * Vector3.forward;
            _remainingLifetime = _weapon.ProjectileLifetime;
            IsAlive = true;

            // A pooled trail still remembers where the previous shot died and would draw a streak
            // across the level on its first frame.
            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        /// <summary>Ends the shot early, as when a run finishes. Safe to call twice.</summary>
        public void Kill()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            OnProjectileDied?.Invoke(this);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            _remainingLifetime -= deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Kill();
                return;
            }

            float step = _weapon.ProjectileSpeed * deltaTime;

            if (TryResolveHit(step))
            {
                Kill();
                return;
            }

            // Moved only after the sweep has cleared the segment, so the shot is never drawn one
            // frame beyond something it was about to hit.
            transform.position += _direction * step;
        }

        private bool TryResolveHit(float step)
        {
            int count = Physics.SphereCastNonAlloc(
                transform.position,
                _weapon.HitRadius,
                _direction,
                Hits,
                step,
                _hitMask,
                QueryTriggerInteraction.Collide);

            // NonAlloc does not sort its results, and punching through the enemy you aimed at to hit
            // the one behind would feel broken, so take the nearest of whatever the sweep found.
            IDamageable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (Hits[i].distance >= nearestDistance)
                {
                    continue;
                }

                if (!Hits[i].collider.TryGetComponent(out IDamageable target) || !target.IsAlive)
                {
                    continue;
                }

                nearest = target;
                nearestDistance = Hits[i].distance;
            }

            if (nearest == null)
            {
                return false;
            }

            nearest.TakeDamage(_weapon.Damage);
            return true;
        }
    }
}
