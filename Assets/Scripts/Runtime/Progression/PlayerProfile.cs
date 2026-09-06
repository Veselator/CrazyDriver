using System;
using UnityEngine;

namespace CrazyDriver.Progression
{
    /// <summary>
    /// Everything the game remembers about the player between sessions.
    /// <para>
    /// Deliberately a small, flat, serializable record rather than a service with behaviour: it is
    /// written to disk verbatim, so anything clever in here would become a migration problem the
    /// first time the shape changed.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private int _totalCoins;
        [SerializeField] private int _runsPlayed;
        [SerializeField] private int _runsWon;
        [SerializeField] private int _totalKills;
        [SerializeField] private float _bestDistance;

        public const int CurrentVersion = 1;

        /// <summary>Schema version, so a future format change can migrate rather than discard.</summary>
        public int Version
        {
            get => _version;
            set => _version = value;
        }

        /// <summary>Coins banked across every run.</summary>
        public int TotalCoins => _totalCoins;

        public int RunsPlayed => _runsPlayed;

        public int RunsWon => _runsWon;

        public int TotalKills => _totalKills;

        /// <summary>Furthest distance reached in a single run, in meters.</summary>
        public float BestDistance => _bestDistance;

        /// <summary>Folds one finished run into the running totals.</summary>
        public void RecordRun(bool won, int coinsEarned, int kills, float distance)
        {
            _runsPlayed++;
            _totalCoins += Mathf.Max(0, coinsEarned);
            _totalKills += Mathf.Max(0, kills);

            if (won)
            {
                _runsWon++;
            }

            if (distance > _bestDistance)
            {
                _bestDistance = distance;
            }
        }
    }
}
