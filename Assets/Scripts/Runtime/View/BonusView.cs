using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>Spins the pickup so it reads as collectable rather than as scenery.</summary>
    [DisallowMultipleComponent]
    public sealed class BonusView : MonoBehaviour
    {
        [SerializeField] private Transform _spinner;
        [SerializeField] private float _spinSpeed = 90f;

        private void LateUpdate()
        {
            if (_spinner != null)
            {
                _spinner.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
