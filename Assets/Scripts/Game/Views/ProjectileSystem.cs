using System.Collections.Generic;
using CrazyDriver.Config;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Fires, moves and resolves every projectile.
    /// <para>
    /// Each shot is swept with a sphere cast along the segment it covers this frame rather than
    /// being given a collider and a Rigidbody. At these speeds a collider-based bullet passes clean
    /// through a stickman between two fixed-update steps; a sweep cannot miss, costs one cast per
    /// live shot, and needs no physics body at all.
    /// </para>
    /// </summary>
    public sealed class ProjectileSystem
    {
        private struct Projectile
        {
            public ProjectileView View;
            public Vector3 Position;
            public Vector3 Direction;
            public float RemainingLifetime;
        }

        private readonly WeaponSettings _settings;
        private readonly LayerMask _hitMask;
        private readonly ViewPool<ProjectileView> _pool;
        private readonly List<Projectile> _live = new(32);
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public ProjectileSystem(WeaponSettings settings, LayerMask hitMask, ProjectileView prefab, Transform root)
        {
            _settings = settings;
            _hitMask = hitMask;
            _pool = new ViewPool<ProjectileView>(prefab, root, prewarm: 24);
        }

        public void Fire(Vector3 origin, Quaternion rotation)
        {
            ProjectileView view = _pool.Rent();
            view.OnRented(origin, rotation);

            _live.Add(new Projectile
            {
                View = view,
                Position = origin,
                Direction = rotation * Vector3.forward,
                RemainingLifetime = _settings.ProjectileLifetime
            });
        }

        public void Tick(float deltaTime)
        {
            float step = _settings.ProjectileSpeed * deltaTime;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _live[i];

                projectile.RemainingLifetime -= deltaTime;
                if (projectile.RemainingLifetime <= 0f)
                {
                    Release(i, projectile);
                    continue;
                }

                if (TryResolveHit(projectile, step, out Vector3 hitPoint))
                {
                    projectile.Position = hitPoint;
                    Release(i, projectile);
                    continue;
                }

                projectile.Position += projectile.Direction * step;
                projectile.View.transform.position = projectile.Position;
                _live[i] = projectile;
            }
        }

        public void Clear()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Release(i, _live[i]);
            }
        }

        private bool TryResolveHit(Projectile projectile, float step, out Vector3 hitPoint)
        {
            hitPoint = projectile.Position;

            int count = Physics.SphereCastNonAlloc(
                projectile.Position,
                _settings.HitRadius,
                projectile.Direction,
                _hits,
                step,
                _hitMask,
                QueryTriggerInteraction.Collide);

            // NonAlloc does not sort its results, and punching through the enemy you aimed at to
            // hit the one behind would feel broken, so take the nearest of whatever the sweep found.
            DamageReceiver nearestReceiver = null;
            float nearestDistance = float.MaxValue;
            Vector3 nearestPoint = projectile.Position;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance)
                {
                    continue;
                }

                if (!_hits[i].collider.TryGetComponent(out DamageReceiver receiver))
                {
                    continue;
                }

                nearestReceiver = receiver;
                nearestDistance = _hits[i].distance;

                // A zero-distance hit means the sweep started already overlapping the collider, in
                // which case the reported contact point is not meaningful.
                nearestPoint = _hits[i].distance > 0f ? _hits[i].point : projectile.Position;
            }

            if (nearestReceiver == null)
            {
                return false;
            }

            nearestReceiver.Apply(_settings.Damage);
            hitPoint = nearestPoint;
            return true;
        }

        private void Release(int index, Projectile projectile)
        {
            _pool.Return(projectile.View);

            int last = _live.Count - 1;
            _live[index] = _live[last];
            _live.RemoveAt(last);
        }
    }
}
