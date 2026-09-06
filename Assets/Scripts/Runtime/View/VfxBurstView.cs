using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>A one-shot particle burst, tinted per use.</summary>
    [DisallowMultipleComponent]
    public sealed class VfxBurstView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private ParticleSystemRenderer _renderer;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>How long a burst needs before it is safe to recycle, in seconds.</summary>
        public float Duration
        {
            get
            {
                if (_particles == null)
                {
                    return 0f;
                }

                ParticleSystem.MainModule main = _particles.main;
                return main.duration + main.startLifetime.constantMax;
            }
        }

        private void Reset()
        {
            _particles = GetComponent<ParticleSystem>();
            _renderer = GetComponent<ParticleSystemRenderer>();
        }

        public void Play(Vector3 position, Color tint)
        {
            transform.position = position;

            if (_particles == null)
            {
                return;
            }

            Tint(tint);

            // Clear as well as play: a pooled system still holds the previous burst's particles and
            // would otherwise replay them at the new location on the first frame.
            _particles.Clear(withChildren: true);
            _particles.Play(withChildren: true);
        }

        /// <summary>
        /// Colours the burst through a property block rather than the particle system's start
        /// colour.
        /// <para>
        /// A start colour only reaches the shader as a vertex colour, which mesh particles do not
        /// carry unless the colour vertex stream is enabled -- so every burst came out the
        /// material's own white. A property block sets the material colour directly, works with any
        /// URP shader, and still does not create a material instance per pooled object.
        /// </para>
        /// </summary>
        private void Tint(Color tint)
        {
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, tint);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
