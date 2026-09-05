using System;
using UnityEngine;

namespace CrazyDriver.Core.Progression
{
    /// <summary>
    /// Owns the loaded profile and is the only thing that writes it.
    /// <para>
    /// Loading is lazy and saving happens when a run ends rather than every time a coin is picked
    /// up: a run is the natural unit of progress, and writing a file mid-run on a phone is a
    /// guaranteed frame spike.
    /// </para>
    /// </summary>
    public sealed class PlayerProfileService
    {
        private readonly IProfileStorage _storage;
        private PlayerProfile _profile;

        public PlayerProfileService(IProfileStorage storage)
        {
            _storage = storage;
        }

        /// <summary>Raised whenever the persisted totals change.</summary>
        public event Action<PlayerProfile> Changed;

        public PlayerProfile Profile => _profile ??= _storage.Load();

        public void RecordRun(bool won, int coinsEarned, int kills, float distance)
        {
            Profile.RecordRun(won, coinsEarned, kills, distance);

            try
            {
                _storage.Save(Profile);
            }
            catch (Exception exception)
            {
                // A failed save must never take the game down with it: the player keeps their run,
                // they just lose the record of it.
                Debug.LogError($"[PlayerProfile] Save failed: {exception.Message}");
            }

            Changed?.Invoke(Profile);
        }
    }
}
