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

            Debug.Log("[CrazyDriver] Missing prefabs created and data rebuilt. " +
                      "Existing prefabs and the scene were not touched.");
        }

        /// <summary>
        /// Regenerates prefabs even if they already exist.
        /// <para>
        /// Separated behind its own menu entry and a confirmation because overwriting a prefab
        /// discards the overrides every scene instance had layered on it. That has cost real work
        /// here more than once.
        /// </para>
        /// </summary>
        [MenuItem("CrazyDriver/Force Rebuild Prefabs And Data", priority = 20)]
        public static void ForceRebuildPrefabs()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Force rebuild prefabs and data?",
                "Existing prefabs AND both config assets will be replaced. Anything a scene instance " +
                "added on top of a prefab, and every value tuned by hand in GameConstants or the " +
                "level asset, is discarded.",
                "Overwrite", "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                PrefabBuilder.Overwrite = true;
                GameAssetBuilder.Overwrite = true;

                PrefabBuilder.BuildAll();
                GameAssetBuilder.BuildAll();
            }
            finally
            {
                PrefabBuilder.Overwrite = false;
                GameAssetBuilder.Overwrite = false;
            }

            Debug.Log("[CrazyDriver] Prefabs and data force-rebuilt.");
        }
    }
}
