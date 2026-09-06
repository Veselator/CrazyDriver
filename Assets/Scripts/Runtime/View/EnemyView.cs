using CrazyDriver.Actors;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Draws one enemy. It reads <see cref="Enemy.State"/> and nothing else: no movement, no
    /// health, no decisions. Deleting it would leave the game playing identically and invisibly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        private static readonly int RunningHash = Animator.StringToHash("IsRunning");

        [SerializeField] private Enemy _enemy;
        [SerializeField] private Animator _animator;
        [SerializeField] private FlashOnHit _flash;

        private void Reset()
        {
            _enemy = GetComponent<Enemy>();
            _flash = GetComponent<FlashOnHit>();
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged() => _flash?.Flash();

        private void LateUpdate()
        {
            // Left in place for a rigged stickman: the supplied model has no rig, but the state an
            // animation would need is already being produced.
            if (_animator == null || _animator.runtimeAnimatorController == null || _enemy == null)
            {
                return;
            }

            _animator.SetBool(RunningHash, _enemy.State == EnemyState.Chasing);
        }
    }
}
