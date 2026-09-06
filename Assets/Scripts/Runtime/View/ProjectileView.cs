using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// A projectile's visuals only. It carries no collider and no Rigidbody: movement and hit
    /// detection are owned by <see cref="ProjectileSystem"/>, which sweeps the shot's path instead.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;

        public void OnRented(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            // A pooled trail still remembers where the previous shot died and would draw a streak
            // across the level on its first frame.
            if (_trail != null)
            {
                _trail.Clear();
            }
        }
    }
}
