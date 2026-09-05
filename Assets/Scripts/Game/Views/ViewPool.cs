using System.Collections.Generic;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// A pool of pooled prefab instances.
    /// <para>
    /// Enemies, bonuses, road tiles and projectiles all appear and disappear continuously as the
    /// car advances. Instantiating and destroying them would spike the frame every time an enemy
    /// spawned and hand the GC a steady stream of work; renting from a pool costs a SetActive.
    /// </para>
    /// </summary>
    public sealed class ViewPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _free;

        public ViewPool(T prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            _free = new Stack<T>(Mathf.Max(0, prewarm));

            for (int i = 0; i < prewarm; i++)
            {
                _free.Push(CreateInstance());
            }
        }

        public T Rent()
        {
            T instance = _free.Count > 0 ? _free.Pop() : CreateInstance();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.gameObject.SetActive(false);
            _free.Push(instance);
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
