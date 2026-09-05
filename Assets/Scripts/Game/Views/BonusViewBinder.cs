using System.Collections.Generic;
using CrazyDriver.Core.Actors;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>Keeps one pooled <see cref="BonusView"/> alive for every bonus the director owns.</summary>
    public sealed class BonusViewBinder
    {
        private readonly BonusDirector _director;
        private readonly ViewPool<BonusView>[] _pools;
        private readonly Dictionary<BonusAgent, BonusView> _bound = new(16);
        private readonly Dictionary<BonusAgent, int> _prefabIndices = new(16);
        private readonly List<BonusView> _rendering = new(16);

        public BonusViewBinder(BonusDirector director, BonusView[] prefabs, Transform root)
        {
            _director = director;

            _pools = new ViewPool<BonusView>[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                _pools[i] = new ViewPool<BonusView>(prefabs[i], root, prewarm: 4);
            }

            _director.Spawned += OnSpawned;
            _director.Collected += OnRemoved;
            _director.Retired += OnRemoved;
        }

        public void Dispose()
        {
            _director.Spawned -= OnSpawned;
            _director.Collected -= OnRemoved;
            _director.Retired -= OnRemoved;
        }

        public void Render()
        {
            for (int i = 0; i < _rendering.Count; i++)
            {
                _rendering[i].Render();
            }
        }

        private void OnSpawned(BonusAgent agent, int prefabIndex)
        {
            if (_pools.Length == 0)
            {
                return;
            }

            // Clamp rather than throw: a map that references a prefab slot the level no longer has
            // should still be playable, just with the wrong-looking bonus.
            int index = Mathf.Clamp(prefabIndex, 0, _pools.Length - 1);

            BonusView view = _pools[index].Rent();
            view.Bind(agent);

            _bound[agent] = view;
            _prefabIndices[agent] = index;
            _rendering.Add(view);
        }

        private void OnRemoved(BonusAgent agent)
        {
            if (!_bound.Remove(agent, out BonusView view))
            {
                return;
            }

            _prefabIndices.Remove(agent, out int index);
            _rendering.Remove(view);
            view.Unbind();
            _pools[index].Return(view);
        }
    }
}
