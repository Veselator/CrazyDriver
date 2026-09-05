using CrazyDriver.Core.Car;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Draws the car. It holds no movement logic at all -- it copies the pose the model already
    /// computed, which is why the car's motion is identical whether or not anything is rendered.
    /// </summary>
    public sealed class CarView : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private TurretView _turret;
        [SerializeField] private Transform _damageEffectAnchor;

        public TurretView Turret => _turret;

        public Transform DamageEffectAnchor => _damageEffectAnchor != null ? _damageEffectAnchor : transform;

        private void Reset() => _body = transform;

        public void Render(CarModel car)
        {
            Transform target = _body != null ? _body : transform;
            target.SetPositionAndRotation(car.Position, car.Rotation);
        }
    }
}
