using System.IO;
using CrazyDriver.Core.Progression;
using UnityEngine;

namespace CrazyDriver.Game.Data
{
    /// <summary>
    /// Stores the player profile as JSON under <see cref="Application.persistentDataPath"/>.
    /// <para>
    /// A file rather than <c>PlayerPrefs</c>: PlayerPrefs is a flat key-value bag that would need
    /// one key per field and offers no way to version the shape, whereas this is one document that
    /// already carries a schema version.
    /// </para>
    /// </summary>
    public sealed class JsonProfileStorage : IProfileStorage
    {
        private const string DefaultFileName = "player-profile.json";

        private readonly string _path;

        /// <param name="fileName">
        /// Overridable so a test can round-trip through its own file. Without it the only way to
        /// exercise persistence would be to overwrite the player's actual save.
        /// </param>
        public JsonProfileStorage(string fileName = DefaultFileName)
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>Not named Path: that would shadow <see cref="System.IO.Path"/> inside this type.</summary>
        public string FilePath => _path;

        public PlayerProfile Load()
        {
            if (!File.Exists(_path))
            {
                return new PlayerProfile();
            }

            try
            {
                string json = File.ReadAllText(_path);
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);

                // A corrupt or truncated file deserializes to null rather than throwing, and a
                // profile from a future build is not safe to interpret with this build's rules.
                if (profile == null || profile.Version > PlayerProfile.CurrentVersion)
                {
                    Debug.LogWarning($"[PlayerProfile] Unusable save at {_path}; starting fresh.");
                    return new PlayerProfile();
                }

                return profile;
            }
            catch (IOException exception)
            {
                Debug.LogError($"[PlayerProfile] Could not read {_path}: {exception.Message}");
                return new PlayerProfile();
            }
        }

        public void Save(PlayerProfile profile)
        {
            // Written to a temporary file and then moved into place, so a crash or a kill mid-write
            // leaves the previous save intact instead of a half-written one.
            string temporary = _path + ".tmp";

            File.WriteAllText(temporary, JsonUtility.ToJson(profile, prettyPrint: true));
            File.Copy(temporary, _path, overwrite: true);
            File.Delete(temporary);
        }
    }
}
