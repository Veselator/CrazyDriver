namespace CrazyDriver.Core.Progression
{
    /// <summary>
    /// Where the profile is kept. The core defines the contract; the Unity assembly supplies the
    /// implementation, which is what keeps file paths and JSON out of the simulation and lets a
    /// test swap in an in-memory store.
    /// </summary>
    public interface IProfileStorage
    {
        /// <summary>Reads the stored profile, or a fresh one when nothing is saved yet.</summary>
        PlayerProfile Load();

        void Save(PlayerProfile profile);
    }
}
