using System.Collections.Generic;
using CrazyDriver.Core.Actors;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Keeps one pooled <see cref="EnemyView"/> alive for every agent the director owns.
    /// <para>
    /// This is the whole of the enemy presentation layer: the director decides who exists, this
    /// decides what that looks like. Neither knows anything about the other beyond three events.
    /// </para>
    /// </summary>
    public sealed class EnemyViewBinder
    {
        private readonly EnemyDirector _director;
        private readonly ViewPool<EnemyView> _pool;
        private readonly Dictionary<EnemyAgent, EnemyView> _bound = new(32);
        private readonly List<EnemyView> _rendering = new(32);

        public EnemyViewBinder(EnemyDirector director, EnemyView prefab, Transform root)
        {
            _director = director;
            _pool = new ViewPool<EnemyView>(prefab, root, prewarm: 12);

            _director.Spawned += OnSpawned;
            _director.Killed += OnRemoved;
            _director.Retired += OnRemoved;
        }

        public void Dispose()
        {
            _director.Spawned -= OnSpawned;
            _director.Killed -= OnRemoved;
            _director.Retired -= OnRemoved;
        }

        public void Render()
        {
            for (int i = 0; i < _rendering.Count; i++)
            {
                _rendering[i].Render();
            }
        }

        private void OnSpawned(EnemyAgent agent)
        {
            EnemyView view = _pool.Rent();
            view.Bind(agent);

            _bound[agent] = view;
            _rendering.Add(view);
        }

        private void OnRemoved(EnemyAgent agent)
        {
            if (!_bound.Remove(agent, out EnemyView view))
            {
                return;
            }

            _rendering.Remove(view);
            view.Unbind();
            _pool.Return(view);
        }
    }
}
