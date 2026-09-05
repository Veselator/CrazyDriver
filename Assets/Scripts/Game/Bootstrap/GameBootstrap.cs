using System;
using System.Threading;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Configuration;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Progression;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Input;
using CrazyDriver.Game.UI;
using CrazyDriver.Game.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// Wires the scene to the simulation and owns the run's presentation flow.
    /// <para>
    /// This is the only class that knows both halves of the game. Everything above it is pure C#
    /// with no idea a scene exists; everything below it is a view that only knows how to draw.
    /// </para>
    /// </summary>
    public sealed class GameBootstrap : IAsyncStartable, IDisposable
    {
        private readonly RunController _run;
        private readonly GameStateMachine _state;
        private readonly IInputService _input;
        private readonly ActivePath _path;
        private readonly PathProgress _progress;
        private readonly CarModel _car;
        private readonly Wallet _wallet;
        private readonly PlayerProfileService _profiles;
        private readonly GateSettings _gateSettings;

        private readonly VisualPath _road;
        private readonly CameraRig _camera;
        private readonly GateView _gate;
        private readonly HudView _hud;
        private readonly ResultView _result;
        private readonly ProjectileSystem _projectiles;

        private CancellationTokenSource _lifetime;
        private bool _isStarting;

        public GameBootstrap(
            RunController run,
            GameStateMachine state,
            IInputService input,
            ActivePath path,
            PathProgress progress,
            CarModel car,
            Wallet wallet,
            PlayerProfileService profiles,
            GateSettings gateSettings,
            VisualPath road,
            CameraRig camera,
            GateView gate,
            HudView hud,
            ResultView result,
            ProjectileSystem projectiles)
        {
            _run = run;
            _state = state;
            _input = input;
            _path = path;
            _progress = progress;
            _car = car;
            _wallet = wallet;
            _profiles = profiles;
            _gateSettings = gateSettings;
            _road = road;
            _camera = camera;
            _gate = gate;
            _hud = hud;
            _result = result;
            _projectiles = projectiles;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

            // The views configure themselves through container injection, which happens while the
            // scope is still building. Doing it here instead raced the first LateTick.
            _input.Tapped += OnTapped;
            _input.DragStarted += _run.BeginAim;
            _input.Dragged += _run.Aim;

            _run.RunPrepared += OnRunPrepared;
            _run.RunFinished += OnRunFinished;

            _run.Prepare();

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _input.Tapped -= OnTapped;
            _input.DragStarted -= _run.BeginAim;
            _input.Dragged -= _run.Aim;

            _run.RunPrepared -= OnRunPrepared;
            _run.RunFinished -= OnRunFinished;

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private void OnRunPrepared(LevelPlan plan)
        {
            Debug.Log($"[CrazyDriver] Map '{plan.MapName}', {plan.Length:0}m, " +
                      $"{plan.Enemies.Length} enemies, {plan.Bonuses.Length} bonuses, seed {plan.Seed}.");

            _projectiles.Clear();
            _road.Load(plan);
            _road.UpdateStreaming(0f);

            _gate.Close(_path);
            _camera.SetPlaying(false);
            _camera.Snap(_car);

            _result.Hide();
            _hud.SetVisible(true);
        }

        private void OnRunFinished(bool won)
        {
            _projectiles.Clear();

            // Banked before the screen is built, so the totals it shows already include this run.
            _profiles.RecordRun(won, _wallet.Coins, _run.KillCount, _progress.Distance);

            var summary = new RunSummary(_progress.Distance, _run.Plan.Length, _run.KillCount, _wallet.Coins);
            _result.Show(won, summary, _profiles.Profile);
        }

        private void OnTapped()
        {
            switch (_state.Current)
            {
                case GameState.Ready:
                    StartRunAsync(_lifetime.Token).Forget();
                    break;

                case GameState.Won:
                case GameState.Lost:
                    _run.RestartAfterResult();
                    break;
            }
        }

        private async UniTaskVoid StartRunAsync(CancellationToken cancellationToken)
        {
            // A second tap while the gate is swinging must not queue a second start.
            if (_isStarting)
            {
                return;
            }

            _isStarting = true;

            try
            {
                _camera.SetPlaying(true);

                // The gate keeps opening while the car pulls away, rather than the player waiting
                // for the animation to finish. The delay is only long enough that the car never
                // appears to drive through a closed gate.
                _gate.OpenAsync(cancellationToken).Forget();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_gateSettings.CarStartDelay),
                    cancellationToken: cancellationToken);

                _run.StartRun();
            }
            catch (OperationCanceledException)
            {
                // Play mode ended mid-start; nothing to clean up.
            }
            finally
            {
                _isStarting = false;
            }
        }
    }
}
