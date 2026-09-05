using System;
using System.Collections.Generic;
using CrazyDriver.Core.Actors;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Views;
using UnityEngine;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// Turns simulation events into things the player can see: damage and coin popups, and a
    /// particle burst wherever something is destroyed.
    /// <para>
    /// All of it is pooled and all of it is driven from events the simulation already raised, so
    /// none of this feedback is on the hot path and removing it would change nothing about how the
    /// game plays.
    /// </para>
    /// </summary>
    public sealed class FeedbackSystem : IInitializable, ILateTickable, IDisposable
    {
        private static readonly Color DamageColor = new(1f, 0.35f, 0.3f);
        private static readonly Color CoinColor = new(1f, 0.82f, 0.25f);
        private static readonly Color EnemyBurstColor = new(0.9f, 0.18f, 0.18f);

        private const float PopupHeight = 1.9f;

        private readonly RunController _run;
        private readonly EnemyDirector _enemies;
        private readonly BonusDirector _bonuses;
        private readonly CameraRig _camera;

        private readonly ViewPool<FloatingTextView> _textPool;
        private readonly ViewPool<VfxBurstView> _burstPool;

        private readonly List<FloatingTextView> _liveText = new(16);
        private readonly List<VfxBurstView> _liveBursts = new(16);

        public FeedbackSystem(
            RunController run,
            EnemyDirector enemies,
            BonusDirector bonuses,
            CameraRig camera,
            FloatingTextView textPrefab,
            VfxBurstView burstPrefab,
            Transform root)
        {
            _run = run;
            _enemies = enemies;
            _bonuses = bonuses;
            _camera = camera;

            _textPool = new ViewPool<FloatingTextView>(textPrefab, root, prewarm: 12);
            _burstPool = new ViewPool<VfxBurstView>(burstPrefab, root, prewarm: 12);
        }

        public void Initialize()
        {
            _run.CarDamaged += OnCarDamaged;
            _enemies.Killed += OnEnemyKilled;
            _bonuses.Collected += OnBonusCollected;
        }

        public void Dispose()
        {
            _run.CarDamaged -= OnCarDamaged;
            _enemies.Killed -= OnEnemyKilled;
            _bonuses.Collected -= OnBonusCollected;
        }

        public void LateTick()
        {
            for (int i = _liveText.Count - 1; i >= 0; i--)
            {
                if (_liveText[i].IsFinished)
                {
                    _textPool.Return(_liveText[i]);
                    _liveText.RemoveAt(i);
                }
            }

            for (int i = _liveBursts.Count - 1; i >= 0; i--)
            {
                if (_liveBursts[i].IsFinished)
                {
                    _burstPool.Return(_liveBursts[i]);
                    _liveBursts.RemoveAt(i);
                }
            }
        }

        private void OnCarDamaged(float amount, Vector3 position)
        {
            ShowText($"-{Mathf.RoundToInt(amount)}", DamageColor, position + Vector3.up * PopupHeight);
            ShowBurst(position + Vector3.up, EnemyBurstColor);
        }

        private void OnEnemyKilled(EnemyAgent agent)
        {
            // Enemies that destroyed themselves on the bumper already produced a burst and a damage
            // number from the impact; a second one on top would just be noise.
            if (agent.DiedOnImpact)
            {
                return;
            }

            ShowBurst(agent.Position + Vector3.up, EnemyBurstColor);
        }

        private void OnBonusCollected(BonusAgent agent)
        {
            ShowText($"+{agent.CoinReward}", CoinColor, agent.Position + Vector3.up * PopupHeight);
            ShowBurst(agent.Position + Vector3.up, CoinColor);
        }

        private void ShowText(string text, Color color, Vector3 position)
        {
            FloatingTextView view = _textPool.Rent();
            view.Play(text, color, position, _camera.Camera);
            _liveText.Add(view);
        }

        private void ShowBurst(Vector3 position, Color tint)
        {
            VfxBurstView view = _burstPool.Rent();
            view.Play(position, tint);
            _liveBursts.Add(view);
        }
    }
}
