using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Turns a hit on the car into something the player feels: a number over the bonnet and a shake.
    /// <para>
    /// It listens to <see cref="CarHealth"/> rather than to the enemy that did it. The spawner used
    /// to own this, which meant the feedback knew one specific enemy type's collision damage; now
    /// anything that can hurt the car -- a new enemy, a hazard, an explosion -- gets the same
    /// treatment for free, and reads the damage that was actually applied.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarDamageFeedback : MonoBehaviour
    {
        private static readonly Color DamageColor = new(1f, 0.35f, 0.3f);

        [SerializeField] private CarHealth _health;
        [SerializeField] private Transform _car;
        [SerializeField] private PopupPool _popups;
        [SerializeField] private CameraShake _shake;

        [SerializeField, Tooltip("Where the number appears, relative to the car.")]
        private Vector3 _popupOffset = new(0f, 2.6f, 0f);

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged(float amount)
        {
            Vector3 at = (_car != null ? _car.position : transform.position) + _popupOffset;

            _popups?.Play($"-{Mathf.RoundToInt(amount)}", DamageColor, at, 1.2f);
            _shake?.Shake();
        }
    }
}
