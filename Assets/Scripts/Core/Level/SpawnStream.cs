using System;

namespace CrazyDriver.Core.Level
{
    /// <summary>
    /// Walks a distance-sorted spawn array exactly once per run.
    /// <para>
    /// The naive reading of "spawn things as the player advances" scans every remaining entry every
    /// frame. Because the array is sorted, a single head index is enough: everything before it has
    /// already fired, everything after it is still too far away, and the per-frame cost is the
    /// number of items actually activated rather than the size of the level.
    /// </para>
    /// </summary>
    public sealed class SpawnStream<T>
    {
        private readonly T[] _items;
        private readonly Func<T, float> _distanceSelector;
        private readonly float _lookAhead;

        private int _head;

        public SpawnStream(T[] items, Func<T, float> distanceSelector, float lookAhead)
        {
            _items = items ?? Array.Empty<T>();
            _distanceSelector = distanceSelector;
            _lookAhead = lookAhead;
        }

        public bool IsExhausted => _head >= _items.Length;

        /// <summary>
        /// Invokes <paramref name="activate"/> for every item that has come within the look-ahead
        /// window of <paramref name="distance"/> since the previous call.
        /// </summary>
        public void Advance(float distance, Action<T> activate)
        {
            float threshold = distance + _lookAhead;

            while (_head < _items.Length && _distanceSelector(_items[_head]) <= threshold)
            {
                activate(_items[_head]);
                _head++;
            }
        }

        public void Rewind() => _head = 0;
    }
}
