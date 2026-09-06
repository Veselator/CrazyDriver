using System;
using CrazyDriver.Level;
using UnityEngine;
using Random = System.Random;

namespace CrazyDriver.Config
{
    /// <summary>
    /// One level: the prefabs it is built from and the maps it can roll.
    /// <para>
    /// Prefab references live here rather than inside <see cref="MapConfig"/> so that the map data
    /// stays in the pure core assembly. Maps address prefabs by index, the same way road pieces
    /// already do.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "CrazyDriver/Level")]
    public sealed class LevelSO : ScriptableObject, IMapProvider
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject[] _roadPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] _bonusPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Run")]
        [SerializeField, Min(1f)] private float _carMaxHealth = 240f;

        [Header("Enemies")]
        [SerializeField, Tooltip("Every kind of enemy this level can roll, with its share of the " +
             "population. Each prefab carries its own tuning.")]
        private EnemyEntry[] _enemies = Array.Empty<EnemyEntry>();

        [Header("Maps")]
        [SerializeField] private MapConfig[] _maps = Array.Empty<MapConfig>();

        /// <summary>Interchangeable road pieces. One is picked at random per tile.</summary>
        public GameObject[] RoadPrefabs => _roadPrefabs;

        /// <summary>The enemy roster. Spawn points address these by index.</summary>
        public EnemyEntry[] Enemies => _enemies;

        /// <summary>Indexed by <see cref="BonusEntry.PrefabIndex"/>.</summary>
        public GameObject[] BonusPrefabs => _bonusPrefabs;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public float CarMaxHealth => _carMaxHealth;

        public MapConfig[] Maps => _maps;

        public MapConfig PickMap(int seed)
        {
            if (_maps == null || _maps.Length == 0)
            {
                throw new InvalidOperationException($"[{name}] has no maps configured.");
            }

            // Seeded rather than UnityEngine.Random so the chosen map is part of the reproducible
            // run, not an independent roll.
            int index = new Random(seed).Next(_maps.Length);
            return _maps[index];
        }
    }
}
