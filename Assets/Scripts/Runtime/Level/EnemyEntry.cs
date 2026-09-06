using System;
using CrazyDriver.Actors;
using UnityEngine;

namespace CrazyDriver.Level
{
    /// <summary>
    /// One kind of enemy a level can roll, and how much of the population it makes up.
    /// <para>
    /// The prefab is referenced directly rather than by index, unlike <see cref="BonusEntry"/>: the
    /// generator has to ask the enemy itself whether it can intercept the car from a given offset,
    /// and only the component on the prefab knows that. Its type is <see cref="Enemy"/>, the
    /// abstract base, so a level can mix runners, fliers or anything else written later without this
    /// type or the spawner changing.
    /// </para>
    /// </summary>
    [Serializable]
    public struct EnemyEntry
    {
        [Tooltip("Any Enemy subclass. All of its tuning lives on the prefab.")]
        public Enemy enemyPrefab;

        [Min(0f), Tooltip("Share of the map's enemies that are this kind, relative to the other " +
             "entries. Two entries at 1 and 3 give a quarter and three quarters; the numbers need " +
             "not add up to anything in particular.")]
        public float relativePart;
    }
}
