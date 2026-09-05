using System;
using System.Collections.Generic;

namespace CrazyDriver.Core.Pooling
{
    /// <summary>
    /// A free list for plain C# agents.
    /// <para>
    /// Enemies and bonuses are created and retired constantly as the car advances. Allocating a
    /// fresh object each time would hand the mobile GC a steady drip of garbage for no reason; the
    /// objects are stateless once released, so reusing them is free.
    /// </para>
    /// </summary>
    public sealed class AgentPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _free;

        public AgentPool(Func<T> factory, int prewarm = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _free = new Stack<T>(Math.Max(0, prewarm));

            for (int i = 0; i < prewarm; i++)
            {
                _free.Push(_factory());
            }
        }

        public int FreeCount => _free.Count;

        public T Rent() => _free.Count > 0 ? _free.Pop() : _factory();

        public void Return(T item)
        {
            if (item != null)
            {
                _free.Push(item);
            }
        }
    }
}
