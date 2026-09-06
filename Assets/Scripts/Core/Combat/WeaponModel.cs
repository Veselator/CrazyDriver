using System;
using CrazyDriver.Config;

namespace CrazyDriver.Core.Combat
{
    /// <summary>
    /// Auto-fire cadence. The player only aims; the turret shoots on its own, as in the reference.
    /// </summary>
    public sealed class WeaponModel
    {
        private readonly WeaponSettings _settings;

        private float _cooldown;
        private bool _isFiring;

        public WeaponModel(WeaponSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Raised once per shot. The view turns this into a pooled projectile.</summary>
        public event Action ShotFired;

        public void StartFiring() => _isFiring = true;

        public void StopFiring() => _isFiring = false;

        public void Tick(float deltaTime)
        {
            if (!_isFiring)
            {
                return;
            }

            _cooldown -= deltaTime;

            // A while loop rather than an if: at high rates of fire a long frame owes more than one
            // shot, and swallowing the surplus would make the cadence frame-rate dependent.
            while (_cooldown <= 0f)
            {
                _cooldown += _settings.ShotInterval;
                ShotFired?.Invoke();
            }
        }

        public void Reset()
        {
            _isFiring = false;
            _cooldown = 0f;
        }
    }
}
