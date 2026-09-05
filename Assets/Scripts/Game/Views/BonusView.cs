using CrazyDriver.Core.Actors;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>Draws one shootable bonus.</summary>
    public sealed class BonusView : MonoBehaviour
    {
        [SerializeField] private DamageReceiver _damageReceiver;
        [SerializeField] private Transform _spinner;
        [SerializeField] private float _spinSpeed = 90f;

        private BonusAgent _agent;

        public BonusAgent Agent => _agent;

        public void Bind(BonusAgent agent)
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

            if (_spinner != null)
            {
                _spinner.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
