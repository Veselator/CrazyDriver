using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrazyDriver.Pooling
{
    /// <summary>
    /// A free list of instances of one prefab.
    /// <para>
    /// Enemies, bonuses and scenery are created and retired constantly as the car advances.
    /// Instantiating and destroying for each would hand the mobile GC a steady drip of garbage and
    /// pay the instantiation cost over and over, for objects that are identical once released.
    /// </para>
    /// <para>
    /// <see cref="Created"/> exists so an owner can unsubscribe from every instance it ever wired
    /// up at teardown, including the ones currently rented out. Subscribing once at creation and
    /// releasing once at teardown is the only pattern that is correct for a pooled object;
    /// subscribing per rent leaves a handler behind on every recycle.
    /// </para>
    /// </summary>
    public sealed class PrefabPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly Action<T> _onCreated;

        private readonly Stack<T> _free = new();
        private readonly List<T> _created = new();

        public PrefabPool(T prefab, Transform root, Action<T> onCreated = null)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _root = root;
            _onCreated = onCreated;
        }

        /// <summary>Every instance this pool has ever made, rented or not.</summary>
        public IReadOnlyList<T> Created => _created;

        public void Prewarm(int count)
        {
            while (_free.Count < count)
            {
                _free.Push(Create());
            }
        }

        public T Rent()
        {
            T item = _free.Count > 0 ? _free.Pop() : Create();
            item.gameObject.SetActive(true);

            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }

            item.gameObject.SetActive(false);
            _free.Push(item);
        }

        private T Create()
        {
            T item = UnityEngine.Object.Instantiate(_prefab, _root);
            item.gameObject.SetActive(false);

            _created.Add(item);
            _onCreated?.Invoke(item);

            return item;
        }
    }
}
