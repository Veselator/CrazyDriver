using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Draws the turret and exposes the muzzle projectiles are launched from.
    /// <para>
    /// The pivot's rotation is written in world space, derived from the path rather than from the
    /// car body it is parented to. That cancels the car's sway out of the aim: the barrel stays
    /// exactly where the player pointed it while the chassis wanders underneath.
    /// </para>
    /// </summary>
    public sealed class TurretView : MonoBehaviour
    {
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _muzzle;

        public Transform Muzzle => _muzzle != null ? _muzzle : transform;

        private void Reset() => _pivot = transform;

        public void Render(TurretModel turret, CarModel car)
        {
            Transform target = _pivot != null ? _pivot : transform;
            target.rotation = turret.GetWorldRotation(car.PathRotation);
        }
    }
}
