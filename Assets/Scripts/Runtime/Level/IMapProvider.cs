namespace CrazyDriver.Level
{
    /// <summary>
    /// Supplies the map to play. Implemented by the level ScriptableObject in the Unity assembly,
    /// which is why the core can pick a map without ever referencing an asset type.
    /// </summary>
    public interface IMapProvider
    {
        /// <summary>Picks one of the level's maps at random using the given seed.</summary>
        MapConfig PickMap(int seed);

        /// <summary>Hit points the car starts a run with.</summary>
        float CarMaxHealth { get; }
    }
}
