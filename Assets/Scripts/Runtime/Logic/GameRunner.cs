using System;
using System.Threading;
using CrazyDriver.Config;
using CrazyDriver.Events;
using CrazyDriver.Input;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.Progression;
using CrazyDriver.UI;
using CrazyDriver.View;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    public enum GameState
    {
        /// <summary>Level generated, car parked behind the closed gate. A tap starts the run.</summary>
        Ready,

        Playing,

        /// <summary>Reached the end of the map. A tap rerolls and returns to <see cref="Ready"/>.</summary>
        Won,

        /// <summary>Ran out of hit points. A tap rerolls and returns to <see cref="Ready"/>.</summary>
        Lost
    }

    /// <summary>
    /// Owns the run: generates a level, waits for a tap, drives, and resolves into a win or a loss.
    /// <para>
    /// The simulation components each have their own Update, so this does not tick them. Instead it
    /// switches them on and off, which is the Unity-native way to express "the world is not running
    /// right now" and means no component has to carry a state check of its own.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.GameRunner)]
    public sealed class GameRunner : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PointerInput _input;

        [Header("Simulation")]
        [SerializeField] private PathTracker _path;
        [SerializeField] private CarMotor _car;
        [SerializeField] private TurretAim _turret;
        [SerializeField] private AutoCannon _cannon;
        [SerializeField] private CarHealth _health;
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private EnemySpawner _enemies;
        [SerializeField] private BonusSpawner _bonuses;
        [SerializeField] private ProjectileController _projectiles;
        [SerializeField] private MapObjectsManager _mapObjects;
        [SerializeField] private VisualPath _road;

        [Header("Presentation")]
        [SerializeField] private CameraRig _camera;
        [SerializeField] private GateView _gate;
        [SerializeField] private CountdownView _countdown;

        [Header("Debug")]
        [SerializeField, Tooltip("Leave at 0 to roll a fresh layout every run. Any other value " +
             "replays the same layout, sway and map choice, which is what you want when chasing a bug.")]
        private int _fixedSeed;

        [SerializeField, Tooltip("Starts the run without waiting for a tap. For capture and testing.")]
        private bool _autoStart;

        private GameConstantsSO _constants;
        private LevelSO _level;
        private PlayerProfileService _profiles;

        private LevelGenerator _generator;
        private CancellationTokenSource _lifetime;
        private bool _isStarting;

        [Inject]
        public void Construct(GameConstantsSO constants, LevelSO level, PlayerProfileService profiles)
        {
            _constants = constants;
            _level = level;
            _profiles = profiles;
        }

        public GameState State { get; private set; } = GameState.Ready;

        public LevelPlan Plan { get; private set; }

        /// <summary>Fraction of the map completed, 0 to 1.</summary>
        public float Progress =>
            Plan is { Length: > 0f } ? Mathf.Clamp01(_path.Distance / Plan.Length) : 0f;

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
            _generator = new LevelGenerator(_constants.Road, _constants.Car, _level.Enemies);
        }

        private void OnEnable()
        {
            _input.Tapped += OnTapped;
            _input.DragStarted += _turret.BeginDrag;
            _input.Dragged += OnDragged;

            _health.Died += OnCarDestroyed;
        }

        private void OnDisable()
        {
            _input.Tapped -= OnTapped;
            _input.DragStarted -= _turret.BeginDrag;
            _input.Dragged -= OnDragged;

            _health.Died -= OnCarDestroyed;
        }

        private void Start()
        {
            Prepare();

            // Raised after the first level exists, so a listener can safely read Plan.
            GameEvents.RaiseGameStarted();

            if (_autoStart)
            {
                StartRunAsync(_lifetime.Token).Forget();
            }
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private void Update()
        {
            if (State != GameState.Playing || Plan == null)
            {
                return;
            }

            if (_path.Distance >= Plan.Length)
            {
                Finish(won: true);
            }
        }

        /// <summary>Generates a fresh level and returns to the ready state.</summary>
        public void Prepare()
        {
            int seed = _fixedSeed != 0 ? _fixedSeed : Environment.TickCount;
            MapConfig map = _level.PickMap(seed);

            Plan = _generator.Generate(map, seed);

            // The curve goes in before anything is reset: the car recomputes its pose against the
            // path the moment it is placed back at zero.
            _path.SetPath(Plan.Path);
            _path.ResetProgress();

            _car.Reseed(seed);
            _turret.ResetAim();
            _cannon.ResetCadence();
            _wallet.ResetCoins();
            _health.ResetTo(_level.CarMaxHealth);

            _projectiles.Clear();
            _enemies.Load(Plan);
            _bonuses.Load(Plan);
            _mapObjects?.Load(Plan);
            _road.Load(Plan);

            _gate.Close(_path);
            _countdown?.Hide();
            _camera.SetPlaying(false);
            _camera.Snap(_car);

            State = GameState.Ready;
            GameEvents.RaiseRunPrepared();

            Debug.Log($"[CrazyDriver] Map '{Plan.MapName}', {Plan.Length:0}m, " +
                      $"{Plan.Enemies.Length} enemies, {Plan.Bonuses.Length} bonuses, seed {seed}.");
        }

        private void OnTapped()
        {
            switch (State)
            {
                case GameState.Ready:
                    StartRunAsync(_lifetime.Token).Forget();
                    break;

                case GameState.Won:
                case GameState.Lost:
                    Prepare();
                    break;
            }
        }

        private void OnDragged(float normalizedOffset)
        {
            // Guarded here rather than inside the turret: the turret is a dumb smoother, and the
            // rule that you cannot aim before the run begins belongs to the run.
            if (State == GameState.Playing)
            {
                _turret.Drag(normalizedOffset);
            }
        }

        private async UniTaskVoid StartRunAsync(CancellationToken cancellationToken)
        {
            // A second tap while the gate is swinging must not queue a second start.
            if (_isStarting || State != GameState.Ready)
            {
                return;
            }

            _isStarting = true;

            try
            {
                // Three beats, in order, each waiting on the one before: the camera swings to its
                // driving framing, the countdown runs to Go, and only then does the gate open.
                _camera.SetPlaying(true);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_constants.Camera.StateBlendDuration),
                    cancellationToken: cancellationToken);

                if (_countdown != null)
                {
                    await _countdown.PlayAsync(cancellationToken);
                }

                // The gate keeps opening while the car pulls away, rather than the player waiting
                // for the animation to finish. The delay is only long enough that the car never
                // appears to drive through a closed gate.
                _gate.OpenAsync(cancellationToken).Forget();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_constants.Gate.CarStartDelay),
                    cancellationToken: cancellationToken);
                State = GameState.Playing;

                // Guarded because a static bus hands one listener the power to strand the whole
                // run: an exception thrown in any OnRunStarted handler would unwind through
                // Invoke, skip Drive, and leave the car parked with the gate open and no error
                // that points at the cause. Logged, not swallowed -- the console still names the
                // broken listener.
                try
                {
                    GameEvents.RaiseRunStarted();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }

                _car.Drive();
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

        private void OnCarDestroyed()
        {
            if (State == GameState.Playing)
            {
                Finish(won: false);
            }
        }

        private void Finish(bool won)
        {
            State = won ? GameState.Won : GameState.Lost;

            _car.Brake();
            _enemies.Clear();
            _bonuses.Clear();
            _projectiles.Clear();

            // Banked before the event, so anything listening already sees the updated totals.
            _profiles.RecordRun(won, _wallet.Coins, _enemies.KillCount, _path.Distance);

            var result = new RunResult(_path.Distance, Plan.Length, _enemies.KillCount, _wallet.Coins);

            if (won)
            {
                GameEvents.RaiseWin(result);
            }
            else
            {
                GameEvents.RaiseLose(result);
            }
        }
    }
}
