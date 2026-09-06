using CrazyDriver.Config;
using CrazyDriver.Progression;
using CrazyDriver.UI;
using CrazyDriver.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// The scene's composition root.
    /// <para>
    /// Nothing is ticked from here and no systems are constructed here: every component owns its own
    /// lifecycle. This exists only so the components that need shared objects -- the two config
    /// assets, the profile service, and the handful of siblings referenced across the scene -- get
    /// them injected instead of hunting for them with <c>FindObjectOfType</c>.
    /// </para>
    /// <para>
    /// Each scene component is registered rather than merely referenced, because that is also what
    /// makes VContainer run its <c>[Inject]</c> method. Registration happens while the scope is
    /// building, which is strictly before any Awake or Update in the scene.
    /// </para>
    /// </summary>
    public sealed class GameScope : LifetimeScope
    {
        [Header("Configuration")]
        [SerializeField] private GameConstantsSO _constants;
        [SerializeField] private LevelSO _level;

        [Header("Simulation")]
        [SerializeField] private PathTracker _path;
        [SerializeField] private CarMotor _car;
        [SerializeField] private CarHealth _health;
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private TurretAim _turret;
        [SerializeField] private AutoCannon _cannon;
        [SerializeField] private EnemySpawner _enemies;
        [SerializeField] private BonusSpawner _bonuses;
        [SerializeField] private ProjectileController _projectiles;
        [SerializeField] private GameRunner _runner;

        [Header("Presentation")]
        [SerializeField] private CameraRig _camera;
        [SerializeField] private VisualPath _road;
        [SerializeField] private GateView _gate;
        [SerializeField] private ResultView _result;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_constants);
            builder.RegisterInstance(_level);

            // Only the storage implementation knows about files; the service above it does not.
            builder.Register<IProfileStorage, JsonProfileStorage>(Lifetime.Singleton);
            builder.Register<PlayerProfileService>(Lifetime.Singleton);

            RegisterComponent(builder, _path);
            RegisterComponent(builder, _car);
            RegisterComponent(builder, _health);
            RegisterComponent(builder, _wallet);
            RegisterComponent(builder, _turret);
            RegisterComponent(builder, _cannon);
            RegisterComponent(builder, _enemies);
            RegisterComponent(builder, _bonuses);
            RegisterComponent(builder, _projectiles);
            RegisterComponent(builder, _runner);

            RegisterComponent(builder, _camera);
            RegisterComponent(builder, _road);
            RegisterComponent(builder, _gate);
            RegisterComponent(builder, _result);
        }

        /// <summary>
        /// Registers a scene component, skipping any slot left empty.
        /// <para>
        /// Tolerating a null keeps the scope usable while the scene is being assembled: a missing
        /// reference produces one clear warning naming the slot, rather than an exception during
        /// container build that says only that something could not be resolved.
        /// </para>
        /// </summary>
        private void RegisterComponent<T>(IContainerBuilder builder, T component) where T : MonoBehaviour
        {
            if (component == null)
            {
                Debug.LogWarning($"[GameScope] No {typeof(T).Name} assigned; anything depending on it will fail.", this);
                return;
            }

            builder.RegisterComponent(component);
        }
    }
}
