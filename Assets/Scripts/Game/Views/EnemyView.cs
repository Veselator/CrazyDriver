using CrazyDriver.Core.Actors;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Draws one enemy. Bound to an agent on spawn and unbound on release, so a pooled instance
    /// never keeps a reference to a retired agent.
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private DamageReceiver _damageReceiver;
        [SerializeField] private Animator _animator;

        private static readonly int RunningHash = Animator.StringToHash("IsRunning");

        private EnemyAgent _agent;

        public EnemyAgent Agent => _agent;

        public void Bind(EnemyAgent agent)
        {
            _agent = agent;

            if (_damageReceiver != null)
            {
                _damageReceiver.Target = agent;
            }

            Render();
        }

        public void Unbind()
        {
            _agent = null;

            if (_damageReceiver != null)
            {
                _damageReceiver.Target = null;
            }
        }

        public void Render()
        {
            if (_agent == null)
            {
                return;
            }

            transform.SetPositionAndRotation(_agent.Position, _agent.Rotation);

            // Left in place for a rigged stickman: the model is static for now, but the state the
            // animation needs is already being produced.
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetBool(RunningHash, _agent.State == EnemyState.Chasing);
            }
        }
    }
}
