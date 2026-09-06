using System.Collections.Generic;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.Pooling;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Streams the map's hand-placed scenery in and out around the car.
    /// <para>
    /// It has no <c>Update</c>. The car is the only thing that moves the world, and it reports that
    /// through <see cref="PathTracker.DistanceChanged"/>, so this reacts to that event instead of
    /// asking every frame whether anything changed. Standing still costs nothing.
    /// </para>
    /// <para>
    /// Activation walks a distance-sorted list with a single head index, so the per-event cost is
    /// the number of objects that actually appeared, not the size of the map. That is the same
    /// comparison as scanning the whole array -- "is this object closer than the spawn radius" --
    /// with the part whose answer cannot have changed skipped.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapObjectsManager : MonoBehaviour
    {
        private struct Spawned
        {
            public Transform Instance;
            public GameObject Prefab;
            public float Distance;
        }

        [SerializeField] private PathTracker _path;

        [SerializeField, Tooltip("Parent for spawned scenery. Defaults to this object.")]
        private Transform _root;

        [SerializeField, Min(0f), Tooltip("Meters ahead of the car at which an object appears. " +
             "Keep it past the road's draw distance or scenery will pop in over visible tarmac.")]
        private float _spawnRadius = 160f;

        [SerializeField, Min(0f), Tooltip("Meters behind the car at which an object is pooled again.")]
        private float _despawnRadius = 60f;

        [SerializeField, Min(0), Tooltip("Instances of each distinct prefab built up front.")]
        private int _prewarmPerPrefab = 4;

        private readonly Dictionary<GameObject, PrefabPool<Transform>> _pools = new();
        private readonly List<Spawned> _spawned = new(32);

        private SpawnStream<MapObjectData> _stream;

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }
        }

        private void OnEnable()
        {
            if (_path != null)
            {
                _path.DistanceChanged += OnPlayerMoved;
            }
        }

        private void OnDisable()
        {
            if (_path != null)
            {
                _path.DistanceChanged -= OnPlayerMoved;
            }
        }

        /// <summary>Takes the scenery for a new run and populates whatever is already in range.</summary>
        public void Load(LevelPlan plan)
        {
            Clear();

            foreach (MapObjectData data in plan.MapObjects)
            {
                if (data.prefab != null && !_pools.ContainsKey(data.prefab))
                {
                    var pool = new PrefabPool<Transform>(data.prefab.transform, _root);
                    pool.Prewarm(_prewarmPerPrefab);

                    _pools.Add(data.prefab, pool);
                }
            }

            _stream = new SpawnStream<MapObjectData>(
                plan.MapObjects,
                static data => data.distance,
                _spawnRadius);

            // The car is parked at the start line, not at minus infinity: whatever stands near the
            // gate has to be there before the first meter is driven.
            OnPlayerMoved(_path != null ? _path.Distance : 0f);
        }

        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                Despawn(i);
            }

            _spawned.Clear();
            _stream?.Rewind();
        }

        private void OnPlayerMoved(float distance)
        {
            _stream?.Advance(distance, Spawn);

            // Scenery never moves, so its spawn distance is always its current distance.
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (distance - _spawned[i].Distance > _despawnRadius)
                {
                    Despawn(i);
                }
            }
        }

        private void Spawn(MapObjectData data)
        {
            if (data.prefab == null || !_pools.TryGetValue(data.prefab, out PrefabPool<Transform> pool))
            {
                return;
            }

            // The offset is read in road-local axes, so a barrier authored two meters right of the
            // centerline stays two meters right of it however the road bends and rises.
            PathSample sample = _path.Evaluate(data.distance, data.offset.x);
            Vector3 position = sample.Position
                               + sample.Rotation * new Vector3(0f, data.offset.y, data.offset.z);

            Transform instance = pool.Rent();
            instance.SetPositionAndRotation(
                position,
                sample.Rotation * Quaternion.Euler(0f, data.yaw, 0f));

            _spawned.Add(new Spawned
            {
                Instance = instance,
                Prefab = data.prefab,
                Distance = data.distance
            });
        }

        private void Despawn(int index)
        {
            Spawned entry = _spawned[index];
            _pools[entry.Prefab].Return(entry.Instance);

            int last = _spawned.Count - 1;
            _spawned[index] = _spawned[last];
            _spawned.RemoveAt(last);
        }
    }
}
