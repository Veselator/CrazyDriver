using System.Collections.Generic;
using CrazyDriver.Config;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.Config;
using UnityEngine;
using VContainer;
using Random = System.Random;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// Renders the path: streams pooled road tiles around the car and answers pose queries.
    /// <para>
    /// It answers those queries by delegating to <see cref="IPathEvaluator"/> rather than owning the
    /// maths, so the car, the enemies and the tiles are all guaranteed to be reading the exact same
    /// curve. Geometry and geometry-as-data would drift apart the moment they were computed twice.
    /// </para>
    /// </summary>
    public sealed class VisualPath : MonoBehaviour
    {
        [SerializeField] private Transform _tileRoot;

        private readonly Dictionary<int, Transform> _activeTiles = new();
        private readonly List<int> _expired = new();

        private RoadSettings _settings;
        private IPathEvaluator _path;
        private ViewPool<Transform>[] _tilePools;
        private int _seed;

        [Inject]
        public void Construct(RoadSettings settings, IPathEvaluator path, LevelSO level)
        {
            _settings = settings;
            _path = path;

            GameObject[] roadPrefabs = level.RoadPrefabs;

            if (_tileRoot == null)
            {
                _tileRoot = transform;
            }

            _tilePools = new ViewPool<Transform>[roadPrefabs.Length];
            for (int i = 0; i < roadPrefabs.Length; i++)
            {
                _tilePools[i] = new ViewPool<Transform>(roadPrefabs[i].transform, _tileRoot, prewarm: 4);
            }
        }

        public void Load(LevelPlan plan)
        {
            ReleaseAll();
            _seed = plan.Seed;
        }

        /// <summary>
        /// Pose at an absolute distance along the path, optionally displaced sideways. This is the
        /// public query the brief asks for; it returns a value rather than a <see cref="Transform"/>
        /// because a Transform cannot exist without a GameObject to hang off.
        /// </summary>
        public PathSample GetPos(float distance, float lateralOffset = 0f) =>
            _path?.Evaluate(distance, lateralOffset) ?? default;

        /// <summary>Streams the tile window around <paramref name="carDistance"/>.</summary>
        public void UpdateStreaming(float carDistance)
        {
            if (_path == null || _tilePools == null || _tilePools.Length == 0)
            {
                return;
            }

            float tileLength = _settings.TileLength;
            int first = Mathf.FloorToInt((carDistance - _settings.DistanceBehind) / tileLength);
            int last = Mathf.FloorToInt((carDistance + _settings.DistanceAhead) / tileLength);

            foreach (KeyValuePair<int, Transform> pair in _activeTiles)
            {
                if (pair.Key < first || pair.Key > last)
                {
                    _expired.Add(pair.Key);
                }
            }

            foreach (int index in _expired)
            {
                ReleaseTile(index);
            }

            _expired.Clear();

            for (int index = first; index <= last; index++)
            {
                if (!_activeTiles.ContainsKey(index))
                {
                    SpawnTile(index, tileLength);
                }
            }
        }

        public void ReleaseAll()
        {
            foreach (int index in _activeTiles.Keys)
            {
                _expired.Add(index);
            }

            foreach (int index in _expired)
            {
                ReleaseTile(index);
            }

            _expired.Clear();
        }

        private void SpawnTile(int index, float tileLength)
        {
            int poolIndex = PickPrefab(index);
            Transform tile = _tilePools[poolIndex].Rent();

            float startDistance = index * tileLength;
            Vector3 start = _path.Evaluate(startDistance).Position;
            Vector3 end = _path.Evaluate(startDistance + tileLength).Position;

            Vector3 span = end - start;
            float chord = span.magnitude;

            tile.SetPositionAndRotation(
                start,
                chord > 0.0001f ? Quaternion.LookRotation(span, Vector3.up) : Quaternion.identity);

            // The tile is a rigid plane exactly one tile-length long, but the chord it has to cover
            // is the hypotenuse of that length and the height change across it -- always slightly
            // longer. Laying the mesh down unscaled therefore leaves a hairline gap at every
            // boundary. Stretching it along its own forward axis by the chord ratio closes it
            // exactly rather than hiding it under an overlap fudge.
            float stretch = chord > 0.0001f ? chord / tileLength : 1f;
            tile.localScale = new Vector3(1f, 1f, stretch);

            _activeTiles[index] = tile;
        }

        private void ReleaseTile(int index)
        {
            if (!_activeTiles.TryGetValue(index, out Transform tile))
            {
                return;
            }

            _activeTiles.Remove(index);
            _tilePools[PickPrefab(index)].Return(tile);
        }

        /// <summary>
        /// Chooses a prefab from the tile's own index, so the same slot always draws the same piece.
        /// A running random would deal a different tile every time the player drove back into view.
        /// </summary>
        private int PickPrefab(int tileIndex)
        {
            if (_tilePools.Length == 1)
            {
                return 0;
            }

            return new Random(_seed ^ (tileIndex * 397)).Next(_tilePools.Length);
        }
    }
}
