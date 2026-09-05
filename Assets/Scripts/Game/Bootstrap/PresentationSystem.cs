using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using CrazyDriver.Game.Views;
using UnityEngine;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// Copies model state onto transforms, once per frame, after the simulation has run.
    /// <para>
    /// Running in LateTick rather than Tick is what guarantees the camera frames the position the
    /// car actually ended the frame at, instead of trailing it by one frame.
    /// </para>
    /// </summary>
    public sealed class PresentationSystem : ILateTickable
    {
        private readonly CarModel _car;
        private readonly TurretModel _turret;
        private readonly CarView _carView;
        private readonly CameraRig _camera;
        private readonly EnemyViewBinder _enemies;
        private readonly BonusViewBinder _bonuses;

        public PresentationSystem(
            CarModel car,
            TurretModel turret,
            CarView carView,
            CameraRig camera,
            EnemyViewBinder enemies,
            BonusViewBinder bonuses)
        {
            _car = car;
            _turret = turret;
            _carView = carView;
            _camera = camera;
            _enemies = enemies;
            _bonuses = bonuses;
        }

        public void LateTick()
        {
            _carView.Render(_car);
            _carView.Turret.Render(_turret, _car);

            _enemies.Render();
            _bonuses.Render();

            // Camera last: it reads the transforms everything above has just written.
            _camera.Render(_car, Time.deltaTime);
        }
    }
}
