using UnityEditor;
using UnityEngine;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// Regenerates the prefabs and configuration assets.
    /// <para>
    /// There is deliberately no "rebuild the scene" command any more. The scene is hand-authored --
    /// laser, tuned camera, adjusted UI -- and a generator that recreates it from a template is not
    /// a convenience, it is a silent undo of that work. Prefabs and data are still generated,
    /// because those are reproducible from source; the scene is not.
    /// </para>
    /// </summary>
    public static class ProjectSetupMenu
    {
        [MenuItem("CrazyDriver/Rebuild Prefabs And Data", priority = 0)]
        public static void RebuildAssets()
        {
            PrefabBuilder.BuildAll();
            GameAssetBuilder.BuildAll();

            Debug.Log("[CrazyDriver] Prefabs and data rebuilt. The scene was not touched.");
        }
    }
}
