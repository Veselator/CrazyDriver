using CrazyDriver.Game.Bootstrap;
using CrazyDriver.Game.UI;
using CrazyDriver.Game.Views;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// Builds the single gameplay scene and wires every serialized reference.
    /// <para>
    /// The whole game runs in one scene, as the brief requires, so this also serves as the
    /// authoritative description of what that scene is supposed to contain.
    /// </para>
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";

        private static readonly Vector2 ReferenceResolution = new(1080f, 1920f);

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();

            CameraRig cameraRig = BuildCamera();
            VisualPath visualPath = BuildVisualPath();

            CarView carView = InstantiatePrefab<CarView>("Car");
            GateView gateView = InstantiatePrefab<GateView>("Gate");

            Transform pools = new GameObject("Pools").transform;
            Transform enemyRoot = CreateChild(pools, "Enemies");
            Transform bonusRoot = CreateChild(pools, "Bonuses");
            Transform projectileRoot = CreateChild(pools, "Projectiles");
            Transform feedbackRoot = CreateChild(pools, "Feedback");

            BuildUi(out HudView hud, out ResultView result);

            BuildStartupManager(
                carView, cameraRig, visualPath, gateView, hud, result,
                enemyRoot, bonusRoot, projectileRoot, feedbackRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"[SceneBuilder] Built {ScenePath}.");
        }

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.89f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.53f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.31f, 0.26f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.80f, 0.86f);

            // Fog starts just past the streaming horizon so tiles fade in rather than popping.
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 260f;
        }

        private static CameraRig BuildCamera()
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.68f, 0.85f);

            cameraGo.AddComponent<AudioListener>();

            var rig = cameraGo.AddComponent<CameraRig>();
            SetReference(rig, "_camera", camera);

            return rig;
        }

        private static VisualPath BuildVisualPath()
        {
            var root = new GameObject("VisualPath");
            var visualPath = root.AddComponent<VisualPath>();

            Transform tileRoot = CreateChild(root.transform, "RoadTiles");
            SetReference(visualPath, "_tileRoot", tileRoot);

            return visualPath;
        }

        private static void BuildUi(out HudView hud, out ResultView result)
        {
            var canvasGo = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            hud = BuildHud(canvasGo.transform);
            result = BuildResult(canvasGo.transform);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private static HudView BuildHud(Transform canvas)
        {
            RectTransform root = CreateUiNode(canvas, "HUD");
            Stretch(root);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var hud = root.gameObject.AddComponent<HudView>();

            Image healthFill = CreateBar(root, "HealthBar", new Vector2(0f, 1f), new Vector2(40f, -60f),
                new Vector2(460f, 34f), new Color(0.85f, 0.22f, 0.2f));

            Image progressFill = CreateBar(root, "ProgressBar", new Vector2(0f, 1f), new Vector2(40f, -108f),
                new Vector2(460f, 18f), new Color(0.95f, 0.75f, 0.2f));

            TMP_Text coins = CreateLabel(root, "CoinsLabel", new Vector2(1f, 1f), new Vector2(-40f, -62f),
                new Vector2(320f, 60f), TextAlignmentOptions.Right, 48f);

            TMP_Text distance = CreateLabel(root, "DistanceLabel", new Vector2(1f, 1f), new Vector2(-40f, -128f),
                new Vector2(320f, 52f), TextAlignmentOptions.Right, 40f);

            SetReferences(hud,
                ("_group", group),
                ("_healthFill", healthFill),
                ("_progressFill", progressFill),
                ("_coinsLabel", coins),
                ("_distanceLabel", distance));

            return hud;
        }

        private static ResultView BuildResult(Transform canvas)
        {
            RectTransform root = CreateUiNode(canvas, "Result");
            Stretch(root);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            var result = root.gameObject.AddComponent<ResultView>();

            // Dim the gameplay behind the panel so the numbers read against any part of the road.
            RectTransform backdropRect = CreateUiNode(root, "Backdrop");
            Stretch(backdropRect);
            var backdrop = backdropRect.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.62f);

            RectTransform bannerRect = CreateUiNode(root, "Banner");
            Anchor(bannerRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(980f, 190f));
            var banner = bannerRect.gameObject.AddComponent<Image>();
            banner.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            banner.type = Image.Type.Sliced;

            TMP_Text title = CreateLabel(bannerRect, "TitleLabel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(940f, 170f), TextAlignmentOptions.Center, 110f);

            // Monospaced so the stat rows line up into columns instead of ragged text.
            TMP_Text runStats = CreateLabel(root, "RunStatsLabel", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
                new Vector2(900f, 300f), TextAlignmentOptions.Center, 52f);
            runStats.enableAutoSizing = false;

            TMP_Text profileStats = CreateLabel(root, "ProfileStatsLabel", new Vector2(0.5f, 0.5f), new Vector2(0f, -190f),
                new Vector2(980f, 90f), TextAlignmentOptions.Center, 40f);
            profileStats.color = new Color(1f, 1f, 1f, 0.72f);

            TMP_Text prompt = CreateLabel(root, "PromptLabel", new Vector2(0.5f, 0f), new Vector2(0f, 220f),
                new Vector2(900f, 80f), TextAlignmentOptions.Center, 46f);
            prompt.color = new Color(1f, 1f, 1f, 0.85f);

            SetReferences(result,
                ("_group", group),
                ("_backdrop", backdrop),
                ("_banner", banner),
                ("_titleLabel", title),
                ("_runStatsLabel", runStats),
                ("_profileStatsLabel", profileStats),
                ("_promptLabel", prompt));

            return result;
        }

        private static void BuildStartupManager(
            CarView carView,
            CameraRig cameraRig,
            VisualPath visualPath,
            GateView gateView,
            HudView hud,
            ResultView result,
            Transform enemyRoot,
            Transform bonusRoot,
            Transform projectileRoot,
            Transform feedbackRoot)
        {
            var go = new GameObject("GameStartupManager");
            var manager = go.AddComponent<GameStartupManager>();

            SetReferences(manager,
                ("_constants", GameAssetBuilder.Constants),
                ("_level", GameAssetBuilder.Level),
                ("_carView", carView),
                ("_cameraRig", cameraRig),
                ("_visualPath", visualPath),
                ("_gateView", gateView),
                ("_hudView", hud),
                ("_resultView", result),
                ("_enemyRoot", enemyRoot),
                ("_bonusRoot", bonusRoot),
                ("_projectileRoot", projectileRoot),
                ("_feedbackRoot", feedbackRoot),
                ("_floatingTextPrefab", LoadPrefabComponent<FloatingTextView>("FloatingText")),
                ("_burstPrefab", LoadPrefabComponent<VfxBurstView>("VfxBurst")));
        }

        private static T LoadPrefabComponent<T>(string prefabName) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.PrefabFolder}/{prefabName}.prefab");
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static T InstantiatePrefab<T>(string prefabName) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.PrefabFolder}/{prefabName}.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefabName;

            return instance.GetComponent<T>();
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static RectTransform CreateUiNode(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image CreateBar(
            RectTransform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            RectTransform background = CreateUiNode(parent, name);
            Anchor(background, anchor, position, size);

            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform fill = CreateUiNode(background, "Fill");
            Stretch(fill);

            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = color;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            // Filled images need a sprite to draw at all; the built-in UI sprite is always present.
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            backgroundImage.sprite = sprite;
            fillImage.sprite = sprite;

            return fillImage;
        }

        private static TMP_Text CreateLabel(
            RectTransform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment,
            float fontSize)
        {
            RectTransform rect = CreateUiNode(parent, name);
            Anchor(rect, anchor, position, size);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.text = string.Empty;

            return label;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetReference(Object target, string field, Object value) =>
            SetReferences(target, (field, value));

        private static void SetReferences(Object target, params (string Field, Object Value)[] assignments)
        {
            var serialized = new SerializedObject(target);

            foreach ((string field, Object value) in assignments)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[SceneBuilder] {target.GetType().Name} has no field '{field}'.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
