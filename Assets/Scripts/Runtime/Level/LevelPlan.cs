using CrazyDriver.Paths;

namespace CrazyDriver.Level
{
    /// <summary>
    /// Everything the run needs to know about the map it is about to play, resolved once at
    /// generation time. Both spawn arrays are sorted by ascending distance, which is what allows the
    /// activation streams to be O(1) per frame instead of scanning every entry.
    /// </summary>
    public sealed class LevelPlan
    {
        public LevelPlan(
            string mapName,
            float length,
            IPathEvaluator path,
            EnemySpawnPoint[] enemies,
            BonusSpawnPoint[] bonuses,
            int seed)
        {
            MapName = mapName;
            Length = length;
            Path = path;
            Enemies = enemies;
            Bonuses = bonuses;
            Seed = seed;
        }

        public string MapName { get; }

        public float Length { get; }

        public IPathEvaluator Path { get; }

        /// <summary>Enemy spawns, ascending by distance.</summary>
        public EnemySpawnPoint[] Enemies { get; }

        /// <summary>Bonus spawns, ascending by distance.</summary>
        public BonusSpawnPoint[] Bonuses { get; }

        /// <summary>The seed this plan was produced from. Logged so any run can be reproduced.</summary>
        public int Seed { get; }
    }
}
