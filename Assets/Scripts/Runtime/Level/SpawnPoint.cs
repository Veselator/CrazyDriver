namespace CrazyDriver.Level
{
    /// <summary>A planned enemy position, fixed at generation time.</summary>
    public readonly struct EnemySpawnPoint
    {
        public readonly float Distance;
        public readonly float LateralOffset;

        public EnemySpawnPoint(float distance, float lateralOffset)
        {
            Distance = distance;
            LateralOffset = lateralOffset;
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
