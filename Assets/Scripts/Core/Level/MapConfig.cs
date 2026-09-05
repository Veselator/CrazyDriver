using System;
using CrazyDriver.Core.Configuration;
using UnityEngine;

namespace CrazyDriver.Core.Level
{
    /// <summary>
    /// One authored map variant. A level owns several of these and picks one at random per run.
    /// <para>
    /// A class rather than a struct on purpose: a mutable struct holding arrays copies by value
    /// while sharing the array references, which is a reliable source of aliasing bugs.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class MapConfig
    {
        [SerializeField] private string _name = "Map";
        [SerializeField, Min(1f)] private float _length = 900f;

        [SerializeField, Tooltip("Enemies per 100 meters of road.")]
        [Min(0f)] private float _enemyFrequency = 9f;

        [SerializeField, Tooltip("Bonuses per 100 meters of road.")]
        [Min(0f)] private float _bonusFrequency = 3.5f;

        [SerializeField] private BonusEntry[] _bonuses = Array.Empty<BonusEntry>();

        [SerializeField, Tooltip("Meters at the start of the map kept clear of spawns.")]
        [Min(0f)] private float _startClearance = 45f;

        [SerializeField, Tooltip("Meters at the end of the map kept clear of spawns.")]
        [Min(0f)] private float _endClearance = 25f;

        [SerializeField] private HeightProfileSettings _heightProfile = new();

        public string Name => _name;

        /// <summary>Total length of the map in meters. Reaching it wins the run.</summary>
        public float Length => _length;

        public float EnemyFrequency => _enemyFrequency;

        public float BonusFrequency => _bonusFrequency;

        public BonusEntry[] Bonuses => _bonuses;

        public float StartClearance => _startClearance;

        public float EndClearance => _endClearance;

        public HeightProfileSettings HeightProfile => _heightProfile;
    }
}
