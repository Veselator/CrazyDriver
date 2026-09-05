using System;
using UnityEngine;

namespace CrazyDriver.Core.Level
{
    /// <summary>
    /// One kind of shootable bonus available on a map.
    /// <para>
    /// The prefab itself is deliberately not referenced here: this type lives in the pure-C# core,
    /// which knows nothing about <c>GameObject</c>. The index points into the level asset's bonus
    /// prefab array, mirroring how road prefabs are already addressed.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class BonusEntry
    {
        [SerializeField, Min(0)] private int _prefabIndex;
        [SerializeField, Min(0)] private int _coinReward = 10;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public int PrefabIndex => _prefabIndex;

        /// <summary>Coins granted when a projectile destroys this bonus.</summary>
        public int CoinReward => _coinReward;

        /// <summary>Relative likelihood of this entry being picked for a bonus slot.</summary>
        public float Weight => _weight;
    }
}
