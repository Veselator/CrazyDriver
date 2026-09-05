using System;
using CrazyDriver.Core.Actors;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using UnityEngine;

namespace CrazyDriver.Core.Run
{
    /// <summary>
    /// The game loop, expressed without a single Unity callback.
    /// <para>
    /// It owns the run's lifecycle -- generate, wait for a tap, drive, resolve -- and drives every
    /// other core system in a fixed order. The Unity layer contributes exactly two things: it calls
    /// <see cref="Tick"/> once per frame and it renders whatever state this produces.
    /// </para>
    /// </summary>
    public sealed class RunController
    {
        private readonly GameStateMachine _stateMachine;
        private readonly LevelGenerator _generator;
        private readonly IMapProvider _maps;
        private readonly PathProgress _progress;
        private readonly ActivePath _path;
        private readonly CarModel _car;
        private readonly TurretModel _turret;
        private readonly WeaponModel _weapon;
        private readonly Health _carHealth;
        private readonly EnemyDirector _enemies;
        private readonly BonusDirector _bonuses;
        private readonly Wallet _wallet;
        private readonly Func<int> _seedSource;

        public RunController(
            GameStateMachine stateMachine,
            LevelGenerator generator,
            IMapProvider maps,
            PathProgress progress,
            ActivePath path,
            CarModel car,
            TurretModel turret,
            WeaponModel weapon,
            Health carHealth,
            EnemyDirector enemies,
            BonusDirector bonuses,
            Wallet wallet,
            Func<int> seedSource)
        {
            _stateMachine = stateMachine;
            _generator = generator;
            _maps = maps;
            _progress = progress;
            _path = path;
            _car = car;
            _turret = turret;
            _weapon = weapon;
            _carHealth = carHealth;
            _enemies = enemies;
            _bonuses = bonuses;
            _wallet = wallet;
            _seedSource = seedSource;

            _enemies.CarDamaged += OnCarDamaged;
            _carHealth.Died += OnCarDestroyed;
        }

        /// <summary>Raised once a fresh level plan exists, before the player taps to start.</summary>
        public event Action<LevelPlan> RunPrepared;

        /// <summary>Raised when the player taps to begin driving.</summary>
        public event Action RunStarted;

        /// <summary>Raised on win or loss, carrying true when the level was completed.</summary>
        public event Action<bool> RunFinished;

        /// <summary>
        /// Raised when an enemy actually landed damage on the car, carrying the amount and the
        /// world position of the impact. Re-raised here rather than consumed straight from the
        /// enemy director so that hit feedback inherits the same "only while playing" guard as the
        /// damage itself.
        /// </summary>
        public event Action<float, Vector3> CarDamaged;

        public LevelPlan Plan { get; private set; }

        /// <summary>Fraction of the map completed, in the range 0 to 1.</summary>
        public float Progress => Plan is { Length: > 0f } ? Mathf.Clamp01(_progress.Distance / Plan.Length) : 0f;

        public int KillCount => _enemies.KillCount;

        /// <summary>
        /// Generates a new level and returns to the ready state. Called at boot and again after
        /// every win or loss, since the brief asks for a freshly rolled layout each time.
        /// </summary>
        public void Prepare()
        {
            int seed = _seedSource();
            MapConfig map = _maps.PickMap(seed);

            Plan = _generator.Generate(map, seed);

            // Publish the new curve before resetting anything: the car recomputes its pose against
            // the path the moment it is reset, and the directors place spawns from it.
            _path.Set(Plan.Path);

            _progress.Reset();
            _car.Reset(seed);
            _turret.Reset();
            _weapon.Reset();
            _wallet.Reset();
            _carHealth.Reset(_maps.CarMaxHealth);

            _enemies.Load(Plan);
            _bonuses.Load(Plan);

            _stateMachine.Set(GameState.Ready);
            RunPrepared?.Invoke(Plan);
        }

        /// <summary>The starting tap. Ignored unless the run is waiting in <see cref="GameState.Ready"/>.</summary>
        public void StartRun()
        {
            if (_stateMachine.Current != GameState.Ready)
            {
                return;
            }

            _stateMachine.Set(GameState.Playing);
            _car.Drive();
            _weapon.StartFiring();
            RunStarted?.Invoke();
        }

        /// <summary>The tap on a result screen. Ignored while a run is in progress.</summary>
        public void RestartAfterResult()
        {
            if (!_stateMachine.IsFinished)
            {
                return;
            }

            Prepare();
        }

        /// <summary>Anchors a new aim drag. Ignored outside of play.</summary>
        public void BeginAim()
        {
            if (_stateMachine.IsPlaying)
            {
                _turret.BeginDrag();
            }
        }

        /// <summary>
        /// Aim input, as a fraction of screen width measured from where the drag started. Ignored
        /// outside of play, which is what keeps the turret locked in the menu state.
        /// </summary>
        public void Aim(float normalizedOffsetFromOrigin)
        {
            if (_stateMachine.IsPlaying)
            {
                _turret.Drag(normalizedOffsetFromOrigin);
            }
        }

        public void Tick(float deltaTime)
        {
            switch (_stateMachine.Current)
            {
                case GameState.Playing:
                    TickPlaying(deltaTime);
                    break;

                case GameState.Won:
                case GameState.Lost:
                    // Still ticked so the car can roll to a stop and enemies can finish their
                    // animations, but no new damage or progress is possible.
                    _car.Tick(deltaTime);
                    break;
            }
        }

        private void TickPlaying(float deltaTime)
        {
            // Order matters: the car moves first so that everything downstream reacts to this
            // frame's position rather than the previous one.
            _car.Tick(deltaTime);
            _turret.Tick(deltaTime);
            _weapon.Tick(deltaTime);
            _enemies.Tick(deltaTime);

            // The car can have died inside the enemy tick, in which case the run is already over
            // and crossing the finish line must not turn that loss into a win.
            if (!_stateMachine.IsPlaying)
            {
                return;
            }

            _bonuses.Tick();

            if (_progress.Distance >= Plan.Length)
            {
                Finish(won: true);
            }
        }

        private void OnCarDamaged(float amount, Vector3 position)
        {
            if (!_stateMachine.IsPlaying)
            {
                return;
            }

            _carHealth.TakeDamage(amount);
            CarDamaged?.Invoke(amount, position);
        }

        private void OnCarDestroyed()
        {
            if (_stateMachine.IsPlaying)
            {
                Finish(won: false);
            }
        }

        private void Finish(bool won)
        {
            _stateMachine.Set(won ? GameState.Won : GameState.Lost);
            _car.Brake();
            _weapon.StopFiring();
            _enemies.Clear();
            _bonuses.Clear();

            RunFinished?.Invoke(won);
        }
    }
}
