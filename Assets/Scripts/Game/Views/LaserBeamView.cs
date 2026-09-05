using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Drives the laser beam's brightness and opacity, and fades it up when it powers on.
    /// <para>
    /// Values are written through a <see cref="MaterialPropertyBlock"/> rather than by touching
    /// <c>Renderer.material</c>, which would clone the material at runtime and leak a copy per
    /// instance. The block also means several beams could share one material and still be driven
    /// independently.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class LaserBeamView : MonoBehaviour
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _renderer;

        [Header("Steady state")]
        [SerializeField, Min(0f), Tooltip("Brightness the beam holds once it is up.")]
        private float _idleIntensity = 4f;

        [SerializeField, Range(0f, 1f), Tooltip("Opacity the beam holds once it is up.")]
        private float _idleAlpha = 0.85f;

        [Header("Activation")]
        [SerializeField, Min(0f), Tooltip("Seconds for the beam to fade up from nothing.")]
        private float _fadeInDuration = 0.9f;

        [SerializeField, Tooltip("Shape of the fade. The default eases in slowly and settles gently.")]
        private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private MaterialPropertyBlock _block;
        private CancellationTokenSource _activation;

        private void Reset() => _renderer = GetComponent<Renderer>();

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            _block = new MaterialPropertyBlock();
            SetVisible(false);
        }

        private void OnDestroy() => CancelActivation();

        /// <summary>Sets brightness directly. Higher values bloom harder.</summary>
        public void SetIntensity(float intensity) => Write(intensity, CurrentAlpha);

        /// <summary>Sets opacity directly, in the 0..1 range.</summary>
        public void SetAlpha(float alpha) => Write(CurrentIntensity, alpha);

        /// <summary>Overrides the beam's colour. Its alpha channel scales with <see cref="SetAlpha"/>.</summary>
        public void SetColor(Color color)
        {
            EnsureBlock();

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>Snaps the beam fully on at its idle values, or fully off.</summary>
        public void SetVisible(bool visible)
        {
            CancelActivation();
            Write(visible ? _idleIntensity : 0f, visible ? _idleAlpha : 0f);
        }

        /// <summary>
        /// Fades the beam up from nothing to its idle glow. Calling it again restarts the fade.
        /// </summary>
        public void PlayActivation()
        {
            CancelActivation();

            _activation = new CancellationTokenSource();
            FadeInAsync(_activation.Token).Forget();
        }

        private float CurrentIntensity { get; set; }

        private float CurrentAlpha { get; set; }

        private async UniTaskVoid FadeInAsync(CancellationToken cancellationToken)
        {
            try
            {
                float duration = Mathf.Max(0f, _fadeInDuration);
                float elapsed = 0f;

                Write(0f, 0f);

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;

                    // Intensity and alpha ride the same curve, so the beam gains presence and
                    // brightness together instead of appearing solid before it starts glowing.
                    float t = _fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                    Write(_idleIntensity * t, _idleAlpha * t);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                Write(_idleIntensity, _idleAlpha);
            }
            catch (OperationCanceledException)
            {
                // Superseded by another activation, or the object went away.
            }
        }

        private void CancelActivation()
        {
            _activation?.Cancel();
            _activation?.Dispose();
            _activation = null;
        }

        private void Write(float intensity, float alpha)
        {
            CurrentIntensity = Mathf.Max(0f, intensity);
            CurrentAlpha = Mathf.Clamp01(alpha);

            EnsureBlock();

            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(IntensityId, CurrentIntensity);
            _block.SetFloat(AlphaId, CurrentAlpha);
            _renderer.SetPropertyBlock(_block);
        }

        private void EnsureBlock()
        {
            _block ??= new MaterialPropertyBlock();

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
        }
    }
}
