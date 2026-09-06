using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Draws the turret and exposes the muzzle projectiles are launched from.
    /// <para>
    /// The pivot's rotation is written in world space, derived from the path rather than from the
    /// car body it is parented to. That cancels the car's sway out of the aim: the barrel stays
    /// exactly where the player pointed it while the chassis wanders underneath.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretView : MonoBehaviour
    {
        [SerializeField] private CarMotor _motor;
        [SerializeField] private TurretAim _aim;
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _muzzle;

        public Transform Muzzle => _muzzle != null ? _muzzle : transform;

        private void Reset() => _pivot = transform;

        private void LateUpdate()
        {
            if (_motor == null || _aim == null)
            {
                return;
            }

            Transform target = _pivot != null ? _pivot : transform;
            target.rotation = _aim.GetWorldRotation(_motor.PathRotation);
        }
    }
}
