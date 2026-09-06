using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Flashes a renderer towards a flat colour and fades back. Driven by whoever took the hit.
    /// <para>
    /// Written through a <see cref="MaterialPropertyBlock"/>, so a pool of enemies sharing one
    /// material can each flash independently without any of them cloning it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlashOnHit : MonoBehaviour
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashBrightnessId = Shader.PropertyToID("_FlashBrightness");

        [SerializeField] private Renderer[] _renderers;

        [SerializeField] private Color _color = Color.white;

        [SerializeField, Range(1f, 8f), Tooltip("Scales the flash colour past white so it can bloom.")]
        private float _brightness = 2.5f;

        [SerializeField, Min(0f), Tooltip("Seconds from full flash back to the normal surface.")]
        private float _duration = 0.12f;

        private MaterialPropertyBlock _block;
        private float _remaining;

        private void Reset() => _renderers = GetComponentsInChildren<Renderer>(true);

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _block = new MaterialPropertyBlock();
        }

        private void OnEnable() => Write(0f);

        /// <summary>Flashes to full and starts fading. Calling it again restarts the fade.</summary>
        public void Flash() => _remaining = _duration;

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            Write(_duration > 0f ? Mathf.Clamp01(_remaining / _duration) : 0f);
        }

        private void Write(float amount)
        {
            _block ??= new MaterialPropertyBlock();

            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_block);
                _block.SetColor(FlashColorId, _color);
                _block.SetFloat(FlashBrightnessId, _brightness);
                _block.SetFloat(FlashAmountId, amount);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
