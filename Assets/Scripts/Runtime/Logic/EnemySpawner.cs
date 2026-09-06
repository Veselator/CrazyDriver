using System.Collections.Generic;
using CrazyDriver.Actors;
using CrazyDriver.Config;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.Pooling;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Streams enemies in ahead of the car and recycles them once they are irrelevant.
    /// <para>
    /// Each enemy then runs itself: this only decides who exists, and of which kind. Placement and
    /// activation are both distance comparisons against the path, never physics queries.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Enemies)]
    public sealed class EnemySpawner : RunPhaseBehaviour
    {
        private static readonly Color DeathColor = new(0.9f, 0.18f, 0.18f);
        private static readonly Color HitColor = new(1f, 0.93f, 0.55f);

        [SerializeField] private PathTracker _path;
        [SerializeField] private Transform _root;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private PopupPool _popups;

        [SerializeField, Min(0), Tooltip("Instances of each enemy kind built before the run starts.")]
        private int _prewarmPerKind = 8;

        private readonly List<PrefabPool<Enemy>> _pools = new();
        private readonly Dictionary<Enemy, int> _poolOf = new(64);
        private readonly List<Enemy> _active = new(32);

        private LevelSO _level;
        private CarMotor _car;
        private CarHealth _carHealth;

        private SpawnStream<EnemySpawnPoint> _stream;

        [Inject]
        public void Construct(LevelSO level, CarMotor car, CarHealth carHealth)
        {
            _level = level;
            _car = car;
            _carHealth = carHealth;
        }

        /// <summary>Enemies destroyed by the player this run. Bumper kills do not count.</summary>
        public int KillCount { get; private set; }

        protected override void OnAwake()
        {
            if (_root == null)
            {
                _root = transform;
            }
        }

        public void Load(LevelPlan plan)
        {
            Clear();
            KillCount = 0;

            // Built here rather than in Awake: the pools need the injected level asset, and
            // container injection is not guaranteed to have run by the time Awake does.
            BuildPools();

            // Streamed in further out than the furthest-waking kind, so every model is already
            // standing in the world by the time it is allowed to start running.
            float lookAhead = MaxActivationDistance() + 40f;
            _stream = new SpawnStream<EnemySpawnPoint>(plan.Enemies, static point => point.Distance, lookAhead);
        }

        public void Clear()
        {
            // Backwards: Retire fires Released, which removes the enemy from this very list.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Retire();
            }

            _active.Clear();
            _stream?.Rewind();
        }

        private void Update()
        {
            _stream?.Advance(_path.Distance, SpawnAt);
        }

        private void BuildPools()
        {
            if (_pools.Count > 0)
            {
                return;
            }

            foreach (EnemyEntry entry in _level.Enemies)
            {
                if (entry.enemyPrefab == null)
                {
                    // Kept as a null slot rather than skipped, so pool indices still line up with
                    // the roster indices the generator wrote into its spawn points.
                    _pools.Add(null);
                    continue;
                }

                int index = _pools.Count;
                var pool = new PrefabPool<Enemy>(entry.enemyPrefab, _root, enemy => OnCreated(enemy, index));

                pool.Prewarm(_prewarmPerKind);
                _pools.Add(pool);
            }
        }

        private float MaxActivationDistance()
        {
            float max = 0f;

            foreach (EnemyEntry entry in _level.Enemies)
            {
                if (entry.enemyPrefab != null)
                {
                    max = Mathf.Max(max, entry.enemyPrefab.ActivationDistance);
                }
            }

            return max;
        }

        private void SpawnAt(EnemySpawnPoint point)
        {
            if (point.EntryIndex < 0 || point.EntryIndex >= _pools.Count)
            {
                return;
            }

            PrefabPool<Enemy> pool = _pools[point.EntryIndex];
            if (pool == null)
            {
                return;
            }

            PathSample sample = _path.Evaluate(point.Distance, point.LateralOffset);

            Enemy enemy = pool.Rent();
            enemy.Activate(sample.Position, sample.Rotation, point.Distance);

            _active.Add(enemy);
        }

        private void OnCreated(Enemy enemy, int poolIndex)
        {
            enemy.Bind(_car, _path, _carHealth);
            _poolOf[enemy] = poolIndex;

            // Subscribed once, for the life of the instance. Wiring this per rent is how a pooled
            // object ends up carrying a subscriber that outlives the thing it belonged to.
            enemy.Released += OnReleased;
            enemy.Damaged += OnDamaged;
        }

        /// <summary>
        /// A hit that did not kill. The number is thrown above the enemy so the player can see the
        /// weapon working -- without it a tough enemy is indistinguishable from a missed shot.
        /// </summary>
        private void OnDamaged(Enemy enemy, float amount)
        {
            bool fatal = !enemy.IsAlive || enemy.Health <= 0f;

            _popups?.Play(
                Mathf.RoundToInt(amount).ToString(),
                fatal ? DeathColor : HitColor,
                enemy.transform.position + Vector3.up * 2.1f,
                fatal ? 1.35f : 1f);
        }

        private void OnReleased(Enemy enemy, ReleaseReason reason)
        {
            Vector3 at = enemy.transform.position + Vector3.up;

            if (reason == ReleaseReason.Shot)
            {
                KillCount++;
                _vfx?.Play(at, DeathColor);
            }
            else if (reason == ReleaseReason.Impact)
            {
                // The damage number and the shake come from CarHealth, which is the one place that
                // knows what the car actually lost -- whatever kind of enemy did it.
                _vfx?.Play(at, DeathColor);
            }

            _active.Remove(enemy);

            _pools[_poolOf[enemy]].Return(enemy);
        }

        protected override void OnDestroyed()
        {
            foreach (PrefabPool<Enemy> pool in _pools)
            {
                if (pool == null)
                {
                    continue;
                }

                foreach (Enemy enemy in pool.Created)
                {
                    enemy.Released -= OnReleased;
                    enemy.Damaged -= OnDamaged;
                }
            }
        }
    }
}
