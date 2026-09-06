using System.Collections.Generic;
using CrazyDriver.Actors;
using CrazyDriver.Config;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Streams enemies in ahead of the car and recycles them once they are irrelevant.
    /// <para>
    /// Each enemy then runs itself: this only decides who exists. Placement and activation are both
    /// distance comparisons against the path, never physics queries.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Enemies)]
    public sealed class EnemySpawner : RunPhaseBehaviour
    {
        private static readonly Color DeathColor = new(0.9f, 0.18f, 0.18f);
        private static readonly Color DamageColor = new(1f, 0.35f, 0.3f);

        [SerializeField] private PathTracker _path;
        [SerializeField] private Transform _root;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private PopupPool _popups;
        [SerializeField] private CameraShake _shake;
        [SerializeField, Min(0)] private int _prewarm = 16;

        private readonly Stack<Enemy> _free = new();
        private readonly List<Enemy> _active = new(32);

        private GameConstantsSO _constants;
        private LevelSO _level;
        private CarMotor _car;
        private CarHealth _carHealth;

        private SpawnStream<EnemySpawnPoint> _stream;

        [Inject]
        public void Construct(GameConstantsSO constants, LevelSO level, CarMotor car, CarHealth carHealth)
        {
            _constants = constants;
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

            // Prewarmed here rather than in Awake: the pool needs the injected level asset, and
            // container injection is not guaranteed to have run by the time Awake does.
            while (_free.Count < _prewarm)
            {
                _free.Push(Create());
            }

            // Streamed in a little further out than they activate, so the model is already standing
            // in the world by the time it is allowed to start running.
            float lookAhead = _constants.Enemy.ActivationDistance + 40f;
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

        private void SpawnAt(EnemySpawnPoint point)
        {
            PathSample sample = _path.Evaluate(point.Distance, point.LateralOffset);

            Enemy enemy = Rent();
            enemy.gameObject.SetActive(true);
            enemy.Activate(sample.Position, sample.Rotation, point.Distance);

            _active.Add(enemy);
        }

        private Enemy Rent() => _free.Count > 0 ? _free.Pop() : Create();

        private Enemy Create()
        {
            Enemy enemy = Instantiate(_level.EnemyPrefab, _root).GetComponent<Enemy>();
            enemy.Bind(_constants.Enemy, _car, _path, _carHealth);

            // Subscribed once, for the life of the instance. Wiring this per rent is how a pooled
            // object ends up carrying a subscriber that outlives the thing it belonged to.
            enemy.Released += OnReleased;

            enemy.gameObject.SetActive(false);
            return enemy;
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
                // The hit is the one moment the player needs to feel rather than read, so it gets
                // the number, the burst and the shake together.
                _vfx?.Play(at, DamageColor);
                _popups?.Play($"-{Mathf.RoundToInt(_constants.Enemy.CollisionDamage)}", DamageColor, at + Vector3.up);
                _shake?.Shake();
            }

            _active.Remove(enemy);

            enemy.gameObject.SetActive(false);
            _free.Push(enemy);
        }

        protected override void OnDestroyed()
        {
            foreach (Enemy enemy in _active)
            {
                enemy.Released -= OnReleased;
            }

            foreach (Enemy enemy in _free)
            {
                enemy.Released -= OnReleased;
            }
        }
    }
}
