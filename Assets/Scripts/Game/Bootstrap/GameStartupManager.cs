using System;
using CrazyDriver.Core.Actors;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Progression;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Data;
using CrazyDriver.Game.Input;
using CrazyDriver.Game.UI;
using CrazyDriver.Game.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// The game's single entry point: the only MonoBehaviour that composes anything.
    /// <para>
    /// It derives from <see cref="LifetimeScope"/> rather than sitting beside one. VContainer's
    /// scope is already a MonoBehaviour with a container and a start-up hook, so a separate manager
    /// duplicating that would be two entry points pretending to be one.
    /// </para>
    /// <para>
    /// Everything registered below is a plain C# object. The scene contributes only views, which are
    /// registered as instances so the systems that drive them never look anything up.
    /// </para>
    /// </summary>
    public sealed class GameStartupManager : LifetimeScope
    {
        [Header("Configuration")]
        [SerializeField] private GameConstantsSO _constants;
        [SerializeField] private LevelSO _level;

        [SerializeField, Tooltip("Leave at 0 to roll a fresh layout every run. Any other value " +
             "replays the same layout, sway and map choice, which is what you want when chasing a bug.")]
        private int _fixedSeed;

        [Header("Scene views")]
        [SerializeField] private CarView _carView;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private VisualPath _visualPath;
        [SerializeField] private GateView _gateView;
        [SerializeField] private HudView _hudView;
        [SerializeField] private ResultView _resultView;

        [SerializeField, Tooltip("Optional. Leave empty and the laser systems simply are not registered.")]
        private LaserBeamView _laserView;

        [Header("Feedback prefabs")]
        [SerializeField] private FloatingTextView _floatingTextPrefab;
        [SerializeField] private VfxBurstView _burstPrefab;

        [Header("Pool roots")]
        [SerializeField] private Transform _enemyRoot;
        [SerializeField] private Transform _bonusRoot;
        [SerializeField] private Transform _projectileRoot;
        [SerializeField] private Transform _feedbackRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSettings(builder);
            RegisterSceneViews(builder);
            RegisterSimulation(builder);
            RegisterPresentation(builder);
            RegisterEntryPoints(builder);
        }

        private void RegisterSettings(IContainerBuilder builder)
        {
            // Registered one group at a time rather than as a single settings blob, so each system
            // asks for exactly the numbers it uses and its dependencies are visible in its
            // constructor.
            builder.RegisterInstance(_constants.Car);
            builder.RegisterInstance(_constants.Turret);
            builder.RegisterInstance(_constants.Weapon);
            builder.RegisterInstance(_constants.Enemy);
            builder.RegisterInstance(_constants.Camera);
            builder.RegisterInstance(_constants.Road);
            builder.RegisterInstance(_constants.Gate);

            builder.RegisterInstance(_level);
            builder.RegisterInstance<IMapProvider>(_level);

            Func<int> seedSource = _fixedSeed != 0
                ? () => _fixedSeed
                : () => Environment.TickCount;

            builder.RegisterInstance(seedSource);
        }

        private void RegisterSceneViews(IContainerBuilder builder)
        {
            builder.RegisterComponent(_carView);
            builder.RegisterComponent(_cameraRig);
            builder.RegisterComponent(_visualPath);
            builder.RegisterComponent(_gateView);
            builder.RegisterComponent(_hudView);
            builder.RegisterComponent(_resultView);

            // Optional: the beam is authored into the scene rather than generated, so the container
            // must tolerate a project where nobody has added one yet.
            if (_laserView != null)
            {
                builder.RegisterComponent(_laserView);
            }
        }

        private void RegisterSimulation(IContainerBuilder builder)
        {
            builder.Register<ActivePath>(Lifetime.Singleton).AsSelf().As<IPathEvaluator>();
            builder.Register<PathProgress>(Lifetime.Singleton);
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<Wallet>(Lifetime.Singleton);
            builder.Register<CarModel>(Lifetime.Singleton);
            builder.Register<TurretModel>(Lifetime.Singleton);
            builder.Register<WeaponModel>(Lifetime.Singleton);
            builder.Register<LevelGenerator>(Lifetime.Singleton);
            builder.Register<EnemyDirector>(Lifetime.Singleton);
            builder.Register<BonusDirector>(Lifetime.Singleton);
            builder.Register<RunController>(Lifetime.Singleton);

            builder.Register(_ => new Health(_level.CarMaxHealth), Lifetime.Singleton);

            // The core owns progression; only the storage implementation knows about files.
            builder.Register<IProfileStorage, JsonProfileStorage>(Lifetime.Singleton);
            builder.Register<PlayerProfileService>(Lifetime.Singleton);
        }

        private void RegisterPresentation(IContainerBuilder builder)
        {
            builder.Register(resolver => new ProjectileSystem(
                    _constants.Weapon,
                    _constants.ProjectileHitMask,
                    _level.ProjectilePrefab.GetComponent<ProjectileView>(),
                    _projectileRoot),
                Lifetime.Singleton);

            builder.Register(resolver => new EnemyViewBinder(
                    resolver.Resolve<EnemyDirector>(),
                    _level.EnemyPrefab.GetComponent<EnemyView>(),
                    _enemyRoot),
                Lifetime.Singleton);

            builder.Register(resolver => new BonusViewBinder(
                    resolver.Resolve<BonusDirector>(),
                    GetBonusPrefabs(),
                    _bonusRoot),
                Lifetime.Singleton);

            builder.Register(resolver => new FeedbackSystem(
                    resolver.Resolve<RunController>(),
                    resolver.Resolve<EnemyDirector>(),
                    resolver.Resolve<BonusDirector>(),
                    _cameraRig,
                    _floatingTextPrefab,
                    _burstPrefab,
                    _feedbackRoot),
                Lifetime.Singleton)
                .AsSelf()
                // Exposed as its own interfaces so the entry point dispatcher picks it up. The
                // dispatcher itself is already in the container because RegisterEntryPoint below
                // installs it; that -- not the interface list -- is what RegisterEntryPoint adds
                // over a plain registration, which is why a factory-built system can join in.
                .AsImplementedInterfaces();
        }

        private void RegisterEntryPoints(IContainerBuilder builder)
        {
            // RegisterEntryPoint rather than Register plus As<IInputService>: it also installs the
            // entry point dispatcher. Registering the same type twice would build two instances,
            // and only one of them would be receiving ticks.
            builder.RegisterEntryPoint<PointerInputService>().As<IInputService>();

            builder.RegisterEntryPoint<ProjectileLauncher>();
            builder.RegisterEntryPoint<GameLoop>();
            builder.RegisterEntryPoint<PresentationSystem>();

            if (_laserView != null)
            {
                builder.RegisterEntryPoint<LaserActivationSystem>();
            }

            builder.RegisterEntryPoint<GameBootstrap>();
        }

        private void OnValidate()
        {
            if (_constants == null || _level == null || _level.Maps == null)
            {
                return;
            }

            // Road pieces are rigid planes laid on the chord between tile boundaries, so a height
            // wave shorter than a tile leaves the car hovering over the middle of every piece. This
            // is the one cross-asset constraint in the project, and it is invisible in the inspector
            // of either asset alone -- which is exactly why the check lives here, where both meet.
            foreach (MapConfig map in _level.Maps)
            {
                if (map?.HeightProfile == null)
                {
                    continue;
                }

                float error = _constants.GetRoadChordError(map.HeightProfile);
                if (error > 0.25f)
                {
                    Debug.LogWarning(
                        $"[{name}] Map '{map.Name}' height profile deviates up to {error:0.00} m from the road " +
                        $"tiles ({_constants.Road.TileLength:0} m each). Lengthen the profile's wavelength.",
                        this);
                }
            }
        }

        private BonusView[] GetBonusPrefabs()
        {
            GameObject[] source = _level.BonusPrefabs;
            var views = new BonusView[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                views[i] = source[i].GetComponent<BonusView>();
            }

            return views;
        }
    }
}
