using TMPro;
using UnityEditor;
using UnityEngine;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// One-click rebuild of everything that is generated rather than authored: prefabs, config
    /// assets and the gameplay scene.
    /// </summary>
    public static class ProjectSetupMenu
    {
        [MenuItem("CrazyDriver/Rebuild Project", priority = 0)]
        public static void RebuildProject()
        {
            if (!HasTextMeshProResources())
            {
                Debug.LogError(
                    "[CrazyDriver] TextMeshPro essential resources are missing. " +
                    "Import them via Window > TextMeshPro > Import TMP Essential Resources, then run this again.");
                return;
            }

            PrefabBuilder.BuildAll();
            GameAssetBuilder.BuildAll();
            SceneBuilder.Build();

            Debug.Log("[CrazyDriver] Project rebuilt.");
        }

        [MenuItem("CrazyDriver/Rebuild Prefabs And Data", priority = 1)]
        public static void RebuildAssetsOnly()
        {
            PrefabBuilder.BuildAll();
            GameAssetBuilder.BuildAll();
        }

        private static bool HasTextMeshProResources() =>
            TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null;
    }
}
