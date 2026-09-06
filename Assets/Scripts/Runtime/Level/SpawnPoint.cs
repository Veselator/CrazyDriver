namespace CrazyDriver.Level
{
    /// <summary>A planned enemy position and kind, both fixed at generation time.</summary>
    public readonly struct EnemySpawnPoint
    {
        public readonly float Distance;
        public readonly float LateralOffset;

        /// <summary>Index into the level's <see cref="EnemyEntry"/> roster.</summary>
        public readonly int EntryIndex;

        public EnemySpawnPoint(float distance, float lateralOffset, int entryIndex)
        {
            Distance = distance;
            LateralOffset = lateralOffset;
            EntryIndex = entryIndex;
        }
    }

    /// <summary>A planned bonus position together with the reward it carries.</summary>
    public readonly struct BonusSpawnPoint
    {
        public readonly float Distance;
        public readonly float LateralOffset;
        public readonly int PrefabIndex;
        public readonly int CoinReward;

        public BonusSpawnPoint(float distance, float lateralOffset, int prefabIndex, int coinReward)
        {
            Distance = distance;
            LateralOffset = lateralOffset;
            PrefabIndex = prefabIndex;
            CoinReward = coinReward;
        }
    }
}
