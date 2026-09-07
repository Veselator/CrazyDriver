using System.Collections.Generic;
using CrazyDriver.Config;
using CrazyDriver.Pooling;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Creates projectiles and takes them back. Nothing else.
    /// <para>
    /// Each shot flies itself and resolves its own hit -- see <see cref="ProjectileView"/> -- and
    /// says so by raising <see cref="ProjectileView.OnProjectileDied"/>. This has no Update of its
    /// own: there is no per-frame work left here that is not already the shot's own business.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Projectiles)]
    public sealed class ProjectileController : MonoBehaviour
    {
        [SerializeField] private Transform _root;

        [SerializeField, Min(0), Tooltip("Shots built before the first trigger pull.")]
        private int _prewarm = 16;

        private readonly List<ProjectileView> _live = new(32);

        private GameConstantsSO _constants;
        private LevelSO _level;
        private PrefabPool<ProjectileView> _pool;

        [Inject]
        public void Construct(GameConstantsSO constants, LevelSO level)
        {
            _constants = constants;
            _level = level;
        }

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }
        }

        public void Fire(Vector3 origin, Quaternion rotation)
        {
            ProjectileView shot = Pool().Rent();
            shot.Launch(origin, rotation);

            _live.Add(shot);
        }

        /// <summary>Ends every shot in the air, as when a run finishes.</summary>
        public void Clear()
        {
            // Backwards: Kill raises OnProjectileDied, which removes the shot from this very list.
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                _live[i].Kill();
            }

            _live.Clear();
        }

        /// <summary>
        /// Built on first use rather than in Awake: the pool needs the injected level asset for the
        /// prefab and the constants for the shot's tuning, and container injection is not guaranteed
        /// to have run by the time Awake does.
        /// </summary>
        private PrefabPool<ProjectileView> Pool()
        {
            if (_pool != null)
            {
                return _pool;
            }

            ProjectileView prefab = _level.ProjectilePrefab.GetComponent<ProjectileView>();

            _pool = new PrefabPool<ProjectileView>(prefab, _root, OnCreated);
            _pool.Prewarm(_prewarm);

            return _pool;
        }

        private void OnCreated(ProjectileView shot)
        {
            shot.Bind(_constants.Weapon, _constants.ProjectileHitMask);

            // Subscribed once, for the life of the instance, never per rent.
            shot.OnProjectileDied += OnDied;
        }

        private void OnDied(ProjectileView shot)
        {
            _live.Remove(shot);
            _pool.Return(shot);
        }

        private void OnDestroy()
        {
            if (_pool == null)
            {
                return;
            }

            foreach (ProjectileView shot in _pool.Created)
            {
                shot.OnProjectileDied -= OnDied;
            }
        }
    }
}
