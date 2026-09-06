using System;
using System.Collections.Generic;
using CrazyDriver.Actors;
using CrazyDriver.Config;
using CrazyDriver.Paths;
using UnityEngine;
using Random = System.Random;

namespace CrazyDriver.Level
{
    /// <summary>
    /// Turns a <see cref="MapConfig"/> plus a seed into a concrete <see cref="LevelPlan"/>.
    /// Generation is fully deterministic for a given seed, so a bad run can always be reproduced.
    /// </summary>
    public sealed class LevelGenerator
    {
        private readonly RoadSettings _road;
        private readonly CarSettings _car;
        private readonly EnemyEntry[] _enemyRoster;

        public LevelGenerator(RoadSettings road, CarSettings car, EnemyEntry[] enemyRoster)
        {
            _road = road;
            _car = car;
            _enemyRoster = enemyRoster ?? Array.Empty<EnemyEntry>();
        }

        public LevelPlan Generate(MapConfig map, int seed)
        {
            var random = new Random(seed);
            var path = new StraightPath(map.HeightProfile, seed);

            float from = map.StartClearance;
            float to = Mathf.Max(from, map.Length - map.EndClearance);

            EnemySpawnPoint[] enemies = GenerateEnemies(map, random, from, to);
            BonusSpawnPoint[] bonuses = GenerateBonuses(map, random, from, to);

            return new LevelPlan(map.Name, map.Length, path, enemies, bonuses, SortByDistance(map.MapObjects), seed);
        }

        private EnemySpawnPoint[] GenerateEnemies(MapConfig map, Random random, float from, float to)
        {
            float totalPart = 0f;
            foreach (EnemyEntry entry in _enemyRoster)
            {
                if (entry.enemyPrefab != null)
                {
                    totalPart += Mathf.Max(0f, entry.relativePart);
                }
            }

            if (totalPart <= 0f)
            {
                return Array.Empty<EnemySpawnPoint>();
            }

            int count = CountFor(map.EnemyFrequency, from, to);
            var result = new EnemySpawnPoint[count];

            foreach ((int index, float distance) in StratifiedDistances(count, from, to, random))
            {
                int entryIndex = PickEnemy(totalPart, random);
                Enemy prefab = _enemyRoster[entryIndex].enemyPrefab;

                float offset = RandomLateralOffset(random);

                // An enemy that cannot close its lateral gap before the car drives past would just
                // stand there decoratively. Pull it towards the centerline until it can engage --
                // and ask the prefab, because only it knows how it moves.
                while (Mathf.Abs(offset) > 0.05f && !prefab.CanIntercept(offset, _car.Speed))
                {
                    offset *= 0.8f;
                }

                result[index] = new EnemySpawnPoint(distance, offset, entryIndex);
            }

            return result;
        }

        private int PickEnemy(float totalPart, Random random)
        {
            float roll = (float)random.NextDouble() * totalPart;

            for (int i = 0; i < _enemyRoster.Length; i++)
            {
                if (_enemyRoster[i].enemyPrefab == null)
                {
                    continue;
                }

                roll -= Mathf.Max(0f, _enemyRoster[i].relativePart);
                if (roll <= 0f)
                {
                    return i;
                }
            }

            // Only reachable through floating-point drift at the very top of the range.
            for (int i = _enemyRoster.Length - 1; i >= 0; i--)
            {
                if (_enemyRoster[i].enemyPrefab != null)
                {
                    return i;
                }
            }

            return 0;
        }

        private BonusSpawnPoint[] GenerateBonuses(MapConfig map, Random random, float from, float to)
        {
            BonusEntry[] entries = map.Bonuses;
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<BonusSpawnPoint>();
            }

            float totalWeight = 0f;
            foreach (BonusEntry entry in entries)
            {
                totalWeight += Mathf.Max(0f, entry.Weight);
            }

            if (totalWeight <= 0f)
            {
                return Array.Empty<BonusSpawnPoint>();
            }

            int count = CountFor(map.BonusFrequency, from, to);
            var result = new BonusSpawnPoint[count];

            foreach ((int index, float distance) in StratifiedDistances(count, from, to, random))
            {
                BonusEntry entry = PickWeighted(entries, totalWeight, random);
                result[index] = new BonusSpawnPoint(
                    distance,
                    RandomLateralOffset(random),
                    entry.PrefabIndex,
                    entry.CoinReward);
            }

            return result;
        }

        /// <summary>
        /// Scenery is authored by hand and arrives in whatever order the inspector list happens to
        /// be in, but the streaming manager walks it with a head index and needs it sorted. Copied
        /// rather than sorted in place: the map asset is shared and must not be reordered by a run.
        /// </summary>
        private static MapObjectData[] SortByDistance(MapObjectData[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return Array.Empty<MapObjectData>();
            }

            var copy = new MapObjectData[objects.Length];
            Array.Copy(objects, copy, objects.Length);
            Array.Sort(copy, static (a, b) => a.distance.CompareTo(b.distance));

            return copy;
        }

        private static int CountFor(float per100Meters, float from, float to) =>
            Mathf.Max(0, Mathf.RoundToInt((to - from) / 100f * per100Meters));

        /// <summary>
        /// Stratified sampling: one item per equal slot, jittered inside it. Uniform random
        /// placement clumps badly at these counts, producing empty stretches next to unfair walls of
        /// enemies; stratification keeps the pacing even while still looking unplanned. Results come
        /// out already sorted, which the activation streams rely on.
        /// </summary>
        private static IEnumerable<(int Index, float Distance)> StratifiedDistances(
            int count,
            float from,
            float to,
            Random random)
        {
            if (count <= 0)
            {
                yield break;
            }

            float slot = (to - from) / count;
            for (int i = 0; i < count; i++)
            {
                float slotStart = from + i * slot;
                float jitter = (float)random.NextDouble() * slot;
                yield return (i, slotStart + jitter);
            }
        }

        private float RandomLateralOffset(Random random) =>
            ((float)random.NextDouble() * 2f - 1f) * _road.HalfWidth;

        private static BonusEntry PickWeighted(BonusEntry[] entries, float totalWeight, Random random)
        {
            float roll = (float)random.NextDouble() * totalWeight;
            foreach (BonusEntry entry in entries)
            {
                roll -= Mathf.Max(0f, entry.Weight);
                if (roll <= 0f)
                {
                    return entry;
                }
            }

            return entries[entries.Length - 1];
        }
    }
}
