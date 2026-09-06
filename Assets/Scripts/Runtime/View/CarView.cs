using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Draws the car. It holds no movement logic at all -- it copies the pose the motor already
    /// computed, which is why the car's motion is identical whether or not anything is rendered.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarView : MonoBehaviour
    {
        [SerializeField] private CarMotor _motor;
        [SerializeField] private Transform _body;
        [SerializeField] private TurretView _turret;
        [SerializeField] private Transform _damageEffectAnchor;

        public TurretView Turret => _turret;

        public Transform DamageEffectAnchor => _damageEffectAnchor != null ? _damageEffectAnchor : transform;

        private void Reset() => _body = transform;

        // LateUpdate, so the pose is whatever the simulation finished the frame with rather than
        // whatever it happened to hold when this component's turn came round in Update.
        private void LateUpdate()
        {
            if (_motor == null)
            {
                return;
            }

            Transform target = _body != null ? _body : transform;
            target.SetPositionAndRotation(_motor.Position, _motor.Rotation);
        }
    }
}
