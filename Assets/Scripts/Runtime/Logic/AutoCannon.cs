using CrazyDriver.Config;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Auto-fire cadence. The player only aims; the turret shoots on its own, as in the reference.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Weapon)]
    public sealed class AutoCannon : MonoBehaviour
    {
        [SerializeField] private CarMotor _car;
        [SerializeField] private TurretAim _turret;
        [SerializeField] private TurretView _turretView;
        [SerializeField] private ProjectileController _projectiles;

        private WeaponSettings _settings;
        private float _cooldown;

        [Inject]
        public void Construct(GameConstantsSO constants) => _settings = constants.Weapon;

        public void ResetCadence() => _cooldown = 0f;

        private void Update()
        {
            _cooldown -= Time.deltaTime;

            // A while loop rather than an if: at high rates of fire a long frame owes more than one
            // shot, and swallowing the surplus would make the cadence frame-rate dependent.
            while (_cooldown <= 0f)
            {
                _cooldown += _settings.ShotInterval;
                Fire();
            }
        }

        private void Fire()
        {
            // The direction comes from the turret model, not the muzzle transform: views are written
            // in LateUpdate and would be a frame stale -- which is exactly the frame in which the
            // player was sweeping the turret fastest.
            Quaternion rotation = _turret.GetWorldRotation(_car.PathRotation);
            _projectiles.Fire(_turretView.Muzzle.position, rotation);
        }
    }
}
