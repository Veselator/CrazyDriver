using System;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>Auto-fire cadence and projectile behaviour.</summary>
    [Serializable]
    public sealed class WeaponSettings
    {
        [SerializeField, Min(0.01f)] private float _shotsPerSecond = 5f;
        [SerializeField, Min(0f)] private float _damage = 34f;
        [SerializeField, Min(0f)] private float _projectileSpeed = 90f;
        [SerializeField, Min(0f)] private float _projectileLifetime = 2.5f;
        [SerializeField, Min(0f)] private float _hitRadius = 0.35f;

        public float ShotsPerSecond => _shotsPerSecond;
        public float ShotInterval => 1f / Mathf.Max(0.01f, _shotsPerSecond);
        public float Damage => _damage;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;

        /// <summary>
        /// Radius of the sphere cast swept along each frame's travel segment. Sweeping instead of
        /// relying on a projectile collider is what keeps fast bullets from tunnelling through
        /// thin targets.
        /// </summary>
        public float HitRadius => _hitRadius;
    }
}
