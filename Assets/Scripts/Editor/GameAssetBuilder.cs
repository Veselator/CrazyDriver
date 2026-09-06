using System.IO;
using CrazyDriver.Config;
using UnityEditor;
using UnityEngine;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// Creates and tunes the two configuration assets.
    /// <para>
    /// Written as code so the tuning pass is reviewable in a diff and reproducible on a fresh
    /// clone, instead of living only inside a binary asset nobody can read.
    /// </para>
    /// </summary>
    public static class GameAssetBuilder
    {
        public const string DataFolder = "Assets/Data";

        public static GameConstantsSO Constants { get; private set; }
        public static LevelSO Level { get; private set; }

        /// <summary>
        /// When true, existing assets are rewritten with the values in this file. Off by default.
        /// <para>
        /// These two assets are tuned by hand in the inspector, and every value here is only a
        /// starting point. Rewriting them on a routine "rebuild" throws that tuning away silently,
        /// which is the single most expensive thing this tool can do.
        /// </para>
        /// </summary>
        public static bool Overwrite { get; set; }

        public static void BuildAll()
        {
            Directory.CreateDirectory(DataFolder);

            Constants = BuildConstants();
            Level = BuildLevel();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameConstantsSO BuildConstants()
        {
            const string path = DataFolder + "/GameConstants.asset";

            var existing = AssetDatabase.LoadAssetAtPath<GameConstantsSO>(path);
            if (existing != null && !Overwrite)
            {
                Debug.Log("[GameAssetBuilder] GameConstants.asset exists and was left alone.");
                return existing;
            }

            var asset = LoadOrCreate<GameConstantsSO>(path);
            var so = new SerializedObject(asset);

            // Car: 12 m/s covers the 500 m level in about 42 seconds.
            SetFloat(so, "_car._speed", 12f);
            SetFloat(so, "_car._accelerationTime", 0.9f);
            SetFloat(so, "_car._brakingTime", 1.6f);

            // Sway wavelengths are held well above 40 m. At 12 m/s a 40 m wave is a lean every
            // 3.5 seconds; the short octaves a noise function would normally add read as twitching
            // rather than as drift, so there are only two of them and the second is quiet.
            SetFloat(so, "_car._sway._amplitude", 0.75f);
            SetFloat(so, "_car._sway._wavelength", 70f);
            SetInt(so, "_car._sway._octaves", 2);
            SetFloat(so, "_car._sway._persistence", 0.35f);
            SetFloat(so, "_car._sway._lacunarity", 1.7f);
            SetFloat(so, "_car._sway._yawResponse", 1.3f);

            // A full swipe across the screen sweeps 150 degrees, comfortably more than the clamp,
            // so the player never has to lift and re-drag to reach the edge of the arc.
            SetFloat(so, "_turret._degreesPerScreenWidth", 150f);
            SetFloat(so, "_turret._maxAngle", 70f);
            SetFloat(so, "_turret._smoothingTime", 0.05f);

            // 34 damage against 100 hit points means three shots per enemy at five shots a second.
            SetFloat(so, "_weapon._shotsPerSecond", 5f);
            SetFloat(so, "_weapon._damage", 34f);
            SetFloat(so, "_weapon._projectileSpeed", 90f);
            SetFloat(so, "_weapon._projectileLifetime", 2.5f);
            SetFloat(so, "_weapon._hitRadius", 0.35f);

            SetFloat(so, "_enemy._maxHealth", 100f);
            SetFloat(so, "_enemy._moveSpeed", 6f);

            // Kept deliberately short. At 55 m an enemy spends four and a half seconds sprinting in
            // full view, so almost everything on screen is charging at once; at 30 m the field
            // reads as a crowd standing around with only the nearest few breaking into a run.
            SetFloat(so, "_enemy._activationDistance", 30f);
            SetFloat(so, "_enemy._despawnDistanceBehind", 25f);
            // Measured from the car's origin, and the body is 4.25 m long, so anything under about
            // 3 m puts the attacker inside the bodywork. This stops them at the bumper.
            SetFloat(so, "_enemy._attackRange", 3.2f);

            // One hit per enemy, since the impact destroys it. Against 200 hit points that is eight
            // enemies allowed through before the run ends.
            SetFloat(so, "_enemy._collisionDamage", 25f);
            SetFloat(so, "_enemy._interceptLead", 0.65f);

            // Framed to match the reference: high and steeply behind, with the road filling the
            // frame and no sky in view. Aiming at a ground point just in front of the car puts the
            // camera about 42 degrees nose-down, so with a 55 degree vertical FOV the top of the
            // frame still meets the ground around 50 m out -- well inside the streamed road -- and
            // the car sits a little below centre.
            SetVector3(so, "_camera._readyOffset", new Vector3(0f, 8.5f, -11.5f));
            SetVector3(so, "_camera._playOffset", new Vector3(0f, 12.5f, -13f));
            SetVector3(so, "_camera._lookAtOffset", new Vector3(0f, 0f, 1f));
            SetFloat(so, "_camera._stateBlendDuration", 1.1f);
            SetFloat(so, "_camera._positionSmoothingTime", 0.22f);
            SetFloat(so, "_camera._rotationSmoothingTime", 0.16f);
            SetFloat(so, "_camera._swayFollow", 0.35f);

            // Tile length is dictated by ground.fbx, which measures 75 m along Z.
            SetFloat(so, "_road._tileLength", PrefabBuilder.RoadTileLength);
            SetFloat(so, "_road._distanceAhead", 190f);
            SetFloat(so, "_road._distanceBehind", 85f);
            SetFloat(so, "_road._halfWidth", 6.5f);

            SetFloat(so, "_gate._distanceAhead", 18f);
            SetFloat(so, "_gate._openDuration", 0.85f);
            SetFloat(so, "_gate._openTravel", 3.4f);
            SetFloat(so, "_gate._carStartDelay", 0.25f);

            int hittableLayer = LayerMask.NameToLayer(PrefabBuilder.HittableLayerName);
            SetInt(so, "_projectileHitMask", hittableLayer >= 0 ? 1 << hittableLayer : ~0);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        private static LevelSO BuildLevel()
        {
            const string path = DataFolder + "/Level_Desert.asset";

            var existing = AssetDatabase.LoadAssetAtPath<LevelSO>(path);
            if (existing != null && !Overwrite)
            {
                Debug.Log("[GameAssetBuilder] Level_Desert.asset exists and was left alone.");
                return existing;
            }

            var asset = LoadOrCreate<LevelSO>(path);
            var so = new SerializedObject(asset);

            SetObjectArray(so, "_roadPrefabs", $"{PrefabBuilder.PrefabFolder}/RoadTile.prefab");
            SetObjectArray(so, "_bonusPrefabs", $"{PrefabBuilder.PrefabFolder}/Bonus.prefab");

            so.FindProperty("_enemyPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.PrefabFolder}/Enemy.prefab");
            so.FindProperty("_projectilePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.PrefabFolder}/Projectile.prefab");

            // 200 hit points against 20 damage per second means a single enemy that reaches the car
            // needs ten seconds to kill it: survivable if you deal with it, fatal if you ignore it.
            SetFloat(so, "_carMaxHealth", 200f);

            SerializedProperty maps = so.FindProperty("_maps");
            maps.arraySize = 1;

            SerializedProperty map = maps.GetArrayElementAtIndex(0);
            map.FindPropertyRelative("_name").stringValue = "Desert Run";
            map.FindPropertyRelative("_length").floatValue = 500f;
            map.FindPropertyRelative("_enemyFrequency").floatValue = 10f;
            map.FindPropertyRelative("_bonusFrequency").floatValue = 4f;
            map.FindPropertyRelative("_startClearance").floatValue = 45f;
            map.FindPropertyRelative("_endClearance").floatValue = 25f;

            // The wavelength is deliberately many times the 75 m road tile. Anything shorter and the
            // rigid tiles cut the corners off the height curve, leaving the car hovering over the
            // middle of every piece. GameConstantsSO.GetRoadChordError measures exactly this.
            map.FindPropertyRelative("_heightProfile._amplitude").floatValue = 1.2f;
            map.FindPropertyRelative("_heightProfile._wavelength").floatValue = 600f;
            map.FindPropertyRelative("_heightProfile._octaves").intValue = 2;
            map.FindPropertyRelative("_heightProfile._persistence").floatValue = 0.4f;
            map.FindPropertyRelative("_heightProfile._lacunarity").floatValue = 1.5f;

            SerializedProperty bonuses = map.FindPropertyRelative("_bonuses");
            bonuses.arraySize = 1;

            SerializedProperty bonus = bonuses.GetArrayElementAtIndex(0);
            bonus.FindPropertyRelative("_prefabIndex").intValue = 0;
            bonus.FindPropertyRelative("_coinReward").intValue = 25;
            bonus.FindPropertyRelative("_weight").floatValue = 1f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            return asset;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetObjectArray(SerializedObject so, string path, params string[] assetPaths)
        {
            SerializedProperty array = so.FindProperty(path);
            array.arraySize = assetPaths.Length;

            for (int i = 0; i < assetPaths.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i]);
            }
        }

        private static void SetFloat(SerializedObject so, string path, float value) =>
            Find(so, path).floatValue = value;

        private static void SetInt(SerializedObject so, string path, int value) =>
            Find(so, path).intValue = value;

        private static void SetVector3(SerializedObject so, string path, Vector3 value) =>
            Find(so, path).vector3Value = value;

        private static SerializedProperty Find(SerializedObject so, string path)
        {
            SerializedProperty property = so.FindProperty(path);
            if (property == null)
            {
                Debug.LogError($"[GameAssetBuilder] No serialized property at '{path}' on {so.targetObject.name}.");
            }

            return property;
        }
    }
}
