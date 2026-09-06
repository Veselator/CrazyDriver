using System;
using System.Collections.Generic;
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
        private readonly EnemySettings _enemies;
        private readonly CarSettings _car;

        public LevelGenerator(RoadSettings road, EnemySettings enemies, CarSettings car)
        {
            _road = road;
            _enemies = enemies;
            _car = car;
        }

        public LevelPlan Generate(MapConfig map, int seed)
        {
            var random = new Random(seed);
            var path = new StraightPath(map.HeightProfile, seed);

            float from = map.StartClearance;
            float to = Mathf.Max(from, map.Length - map.EndClearance);

            EnemySpawnPoint[] enemies = GenerateEnemies(map, random, from, to);
            BonusSpawnPoint[] bonuses = GenerateBonuses(map, random, from, to);

            return new LevelPlan(map.Name, map.Length, path, enemies, bonuses, seed);
        }

        private EnemySpawnPoint[] GenerateEnemies(MapConfig map, Random random, float from, float to)
        {
            int count = CountFor(map.EnemyFrequency, from, to);
            var result = new EnemySpawnPoint[count];

            foreach ((int index, float distance) in StratifiedDistances(count, from, to, random))
            {
                float offset = RandomLateralOffset(random);

                // An enemy that cannot close its lateral gap before the car drives past would just
                // stand there decoratively. Pull it towards the centerline until it can engage.
                while (Mathf.Abs(offset) > 0.05f && !_enemies.CanIntercept(offset, _car.Speed))
                {
                    offset *= 0.8f;
                }

                result[index] = new EnemySpawnPoint(distance, offset);
            }

            return result;
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
