using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Drives the laser beam's brightness and opacity, and plays its power-up flicker.
    /// <para>
    /// Values are written through a <see cref="MaterialPropertyBlock"/> rather than by touching
    /// <c>Renderer.material</c>, which would clone the material at runtime and leak a copy per
    /// instance. The block also means several beams could share one material and still flicker
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
        [SerializeField, Min(0f), Tooltip("Brightness the beam settles at once it is up.")]
        private float _idleIntensity = 4f;

        [SerializeField, Range(0f, 1f), Tooltip("Opacity the beam settles at once it is up.")]
        private float _idleAlpha = 0.85f;

        [Header("Activation flicker")]
        [SerializeField, Min(0f), Tooltip("Brightness of the first strike. Later flashes fall off towards idle.")]
        private float _strikeIntensity = 18f;

        [SerializeField, Min(1), Tooltip("How many on/off pulses the start-up sequence plays.")]
        private int _flashCount = 5;

        [SerializeField, Min(0f)] private float _flashOnDuration = 0.055f;
        [SerializeField, Min(0f)] private float _flashOffDuration = 0.045f;

        [SerializeField, Min(0f), Tooltip("Seconds spent easing from the last flash down to idle.")]
        private float _settleDuration = 0.4f;

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

        /// <summary>Overrides the beam's colour. The alpha channel scales with <see cref="SetAlpha"/>.</summary>
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
        /// Plays the power-up sequence: a burst of on/off pulses whose brightness falls towards the
        /// idle level, then an ease down onto it. Calling it again restarts the sequence.
        /// </summary>
        public void PlayActivation()
        {
            CancelActivation();

            _activation = new CancellationTokenSource();
            RunActivationAsync(_activation.Token).Forget();
        }

        private float CurrentIntensity { get; set; }

        private float CurrentAlpha { get; set; }

        private async UniTaskVoid RunActivationAsync(CancellationToken cancellationToken)
        {
            try
            {
                int flashes = Mathf.Max(1, _flashCount);

                for (int i = 0; i < flashes; i++)
                {
                    // Each pulse is dimmer than the last, so the beam reads as struggling up to
                    // power rather than as a light switch being flipped repeatedly.
                    float t = flashes > 1 ? i / (float)(flashes - 1) : 1f;
                    float intensity = Mathf.Lerp(_strikeIntensity, _idleIntensity, t);

                    Write(intensity, _idleAlpha);
                    await UniTask.Delay(Seconds(_flashOnDuration), cancellationToken: cancellationToken);

                    Write(0f, 0f);
                    await UniTask.Delay(Seconds(_flashOffDuration), cancellationToken: cancellationToken);
                }

                await EaseToIdleAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Superseded by another activation, or the object went away.
            }
        }

        private async UniTask EaseToIdleAsync(CancellationToken cancellationToken)
        {
            float duration = Mathf.Max(0f, _settleDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Write(Mathf.Lerp(_strikeIntensity * 0.5f, _idleIntensity, t), Mathf.Lerp(0f, _idleAlpha, t));

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            Write(_idleIntensity, _idleAlpha);
        }

        private static TimeSpan Seconds(float seconds) => TimeSpan.FromSeconds(Mathf.Max(0f, seconds));

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
