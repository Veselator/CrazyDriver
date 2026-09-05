using System;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using CrazyDriver.Game.Views;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// Turns the weapon model's cadence into actual projectiles.
    /// <para>
    /// The launch direction is taken from the turret <em>model</em> rather than from the muzzle
    /// transform, because views are written in LateTick and would therefore be one frame stale --
    /// which is exactly the frame in which the player was sweeping the turret fastest.
    /// </para>
    /// </summary>
    public sealed class ProjectileLauncher : IInitializable, IDisposable
    {
        private readonly WeaponModel _weapon;
        private readonly TurretModel _turret;
        private readonly CarModel _car;
        private readonly CarView _carView;
        private readonly ProjectileSystem _projectiles;

        public ProjectileLauncher(
            WeaponModel weapon,
            TurretModel turret,
            CarModel car,
            CarView carView,
            ProjectileSystem projectiles)
        {
            _weapon = weapon;
            _turret = turret;
            _car = car;
            _carView = carView;
            _projectiles = projectiles;
        }

        public void Initialize() => _weapon.ShotFired += OnShotFired;

        public void Dispose() => _weapon.ShotFired -= OnShotFired;

        private void OnShotFired() =>
            _projectiles.Fire(_carView.Turret.Muzzle.position, _turret.GetWorldRotation(_car.PathRotation));
    }
}
