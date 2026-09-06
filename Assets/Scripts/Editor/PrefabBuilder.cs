using System.IO;
using CrazyDriver.Actors;
using CrazyDriver.View;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// Builds every runtime prefab from the supplied FBX models.
    /// <para>
    /// Kept as a tool rather than done by hand so the whole project can be rebuilt from source
    /// assets in one click, and so the numbers that matter -- where the turret sits, how long a road
    /// tile is, which layer a hit box belongs to -- are written down instead of buried in a .prefab.
    /// </para>
    /// </summary>
    public static class PrefabBuilder
    {
        public const string PrefabFolder = "Assets/Prefabs";
        public const string MaterialFolder = "Assets/Materials";

        public const string HittableLayerName = "Hittable";

        /// <summary>Length of one <c>ground.fbx</c> tile along Z, measured from the imported mesh.</summary>
        public const float RoadTileLength = 75f;

        /// <summary>Half the tile's own width along X.</summary>
        private const float RoadTileHalfWidth = 50f;

        /// <summary>Seat of the turret in the pickup's cargo bed, in car-local space.</summary>
        private static readonly Vector3 TurretMountLocalPosition = new(0f, 0.92f, -1.25f);

        private const float TurretScale = 0.45f;

        /// <summary>
        /// Muzzle in turret-model space. Taken from the model's own extreme +Z vertex
        /// (-0.123, 1.302, 1.735), which is the tip of the barrel.
        /// </summary>
        private static readonly Vector3 MuzzleLocalPosition = new(0f, 1.3f, 1.78f);

        /// <summary>Height of the tyre marks above the car's own ground plane, in meters.</summary>
        public const float TyreTrackHeight = 0.18f;

        public static Material WorldMaterial { get; private set; }
        public static Material CarMaterial { get; private set; }

        /// <summary>
        /// When true, existing prefabs are overwritten. Off by default.
        /// <para>
        /// Regenerating a prefab replaces its contents wholesale, and that discards every override
        /// a scene instance had added to it -- a hand-placed child on the car, for one. Skipping
        /// prefabs that already exist makes the generator a scaffold rather than a periodic undo.
        /// </para>
        /// </summary>
        public static bool Overwrite { get; set; }

        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(MaterialFolder);

            EnsureHittableLayer();
            BuildMaterials();

            BuildRoadTile();
            BuildCar();
            BuildEnemy();
            BuildBonus();
            BuildProjectile();
            BuildGate();
            BuildFloatingText();
            BuildBurst();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            WorldMaterial = CreateMaterial(lit, $"{MaterialFolder}/World.mat", "Assets/Models/map.png");
            CarMaterial = CreateMaterial(lit, $"{MaterialFolder}/Car.mat", "Assets/Models/car.png");
        }

        private static Material CreateMaterial(Shader shader, string path, string texturePath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));

            // The art is flat-shaded and reads from a tiny palette texture; any specular response
            // just makes the facets shimmer as the car moves.
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildRoadTile()
        {
            var root = new GameObject("RoadTile");

            GameObject model = InstantiateModel("Assets/Models/ground.fbx", root.transform, WorldMaterial);

            // The mesh is centred on its own origin, but the streamer places tiles by their leading
            // edge. Offsetting the model here means VisualPath never has to know about the pivot.
            model.transform.localPosition = new Vector3(0f, 0f, RoadTileLength * 0.5f);

            Save(root, "RoadTile");
        }

        private static void BuildCar()
        {
            var root = new GameObject("Car");
            var carView = root.AddComponent<CarView>();

            GameObject body = InstantiateModel("Assets/Models/car.fbx", root.transform, CarMaterial);
            body.name = "Body";

            var turretRoot = new GameObject("Turret");
            turretRoot.transform.SetParent(body.transform, false);
            turretRoot.transform.localPosition = TurretMountLocalPosition;

            var turretView = turretRoot.AddComponent<TurretView>();

            GameObject turretModel = InstantiateModel("Assets/Models/turret.fbx", turretRoot.transform, CarMaterial);
            turretModel.name = "TurretModel";
            turretModel.transform.localScale = Vector3.one * TurretScale;

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(turretRoot.transform, false);
            muzzle.transform.localPosition = MuzzleLocalPosition * TurretScale;

            BuildTyreTrack(body.transform, -0.702f);
            BuildTyreTrack(body.transform, 0.702f);

            SerializeFields(turretView, ("_pivot", turretRoot.transform), ("_muzzle", muzzle.transform));
            SerializeFields(carView, ("_body", body.transform), ("_turret", turretView), ("_damageEffectAnchor", turretRoot.transform));

            Save(root, "Car");
        }

        /// <summary>
        /// A tyre mark trailing one rear wheel.
        /// <para>
        /// A trail renderer rather than decals: the car only ever moves forward along a known path,
        /// so there is nothing a decal projector would buy, and a trail costs one draw call for the
        /// whole streak.
        /// </para>
        /// </summary>
        private static void BuildTyreTrack(Transform body, float lateralOffset)
        {
            var track = new GameObject(lateralOffset < 0f ? "TyreTrackLeft" : "TyreTrackRight");
            track.transform.SetParent(body, false);

            // Clears the worst gap between the smooth path the car rides and the flat tiles laid
            // under it -- GameConstantsSO.GetRoadChordError measures that, currently 0.12 m. Sitting
            // any lower makes the track sink under the road wherever the path dips below the chord,
            // which shows up as the marks blinking in and out.
            track.transform.localPosition = new Vector3(lateralOffset, TyreTrackHeight, -1.21f);

            // TrailRenderer with TransformZ alignment faces its own +Z, so an unrotated emitter
            // produces a ribbon standing on edge like a wall along the path -- invisible from above
            // except when the car's sway tips it into view. Pointing +Z up lays it flat on the road.
            track.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var trail = track.AddComponent<TrailRenderer>();
            trail.time = 2.6f;
            trail.startWidth = 0.34f;
            trail.endWidth = 0.16f;
            trail.minVertexDistance = 0.35f;
            trail.autodestruct = false;
            trail.receiveShadows = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.alignment = LineAlignment.TransformZ;
            trail.material = CreateUnlitFadeMaterial("TyreTrack", new Color(0.10f, 0.07f, 0.05f, 0.55f));
            trail.textureMode = LineTextureMode.Stretch;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = fade;
        }

        private static void BuildFloatingText()
        {
            var root = new GameObject("FloatingText");
            var view = root.AddComponent<FloatingTextView>();

            var label = root.AddComponent<TextMeshPro>();
            label.text = "0";
            label.fontSize = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;

            // Drawn on top of the world: a number that disappears behind the car it belongs to is
            // worse than no number at all.
            label.GetComponent<MeshRenderer>().sortingOrder = 100;

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 1.2f);

            SerializeFields(view, ("_label", label));

            Save(root, "FloatingText");
        }

        private static void BuildBurst()
        {
            var root = new GameObject("VfxBurst");
            var view = root.AddComponent<VfxBurstView>();

            var particles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = BuiltinCubeMesh();
            renderer.material = CreateParticleMaterial("VfxBurst");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            SerializeFields(view, ("_particles", particles), ("_renderer", renderer));

            Save(root, "VfxBurst");
        }

        private static Mesh BuiltinCubeMesh()
        {
            GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temporary);
            return mesh;
        }

        /// <summary>
        /// Material for the burst.
        /// <para>
        /// It has to be one of URP's Particles shaders: the standard Lit shader ignores the
        /// per-particle vertex colour entirely, so every burst would come out the material's own
        /// colour no matter what tint the caller asked for.
        /// </para>
        /// </summary>
        private static Material CreateParticleMaterial(string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material.SetColor("_BaseColor", Color.white);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateUnlitFadeMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);

            // URP's Unlit needs its surface type switched to transparent explicitly; setting only
            // the colour's alpha leaves it rendering fully opaque.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Double sided, so the ribbon shows whichever way its normal ends up facing.
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.doubleSidedGI = true;

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildEnemy()
        {
            var root = new GameObject("Enemy") { layer = LayerMask.NameToLayer(HittableLayerName) };

            // Logic and view are separate components on the same object: Enemy decides, EnemyView
            // draws, and neither can quietly start doing the other's job.
            var enemy = root.AddComponent<Enemy>();
            var enemyView = root.AddComponent<EnemyView>();

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.45f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            GameObject model = InstantiateModel("Assets/Models/stickman.fbx", root.transform, WorldMaterial);
            model.name = "Model";

            // An Animator is added but left without a controller: the supplied stickman has no rig,
            // and EnemyView already writes the state a Mixamo clip would need.
            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            // Enemy implements IDamageable itself, so a projectile sweep resolves straight from the
            // collider to the enemy. The bridge component that used to sit between them is gone.
            SerializeFields(enemyView, ("_enemy", enemy), ("_animator", animator));

            Save(root, "Enemy");
        }

        private static void BuildBonus()
        {
            var root = new GameObject("Bonus") { layer = LayerMask.NameToLayer(HittableLayerName) };

            root.AddComponent<Bonus>();
            var bonusView = root.AddComponent<BonusView>();

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.1f, 1.1f, 1.1f);
            box.center = new Vector3(0f, 0.9f, 0f);

            var spinner = new GameObject("Spinner");
            spinner.transform.SetParent(root.transform, false);
            spinner.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            // No bonus model ships with the brief's asset pack, so this is a primitive placeholder.
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.SetParent(spinner.transform, false);
            crate.transform.localScale = Vector3.one * 0.85f;
            crate.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Object.DestroyImmediate(crate.GetComponent<BoxCollider>());
            crate.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial("Bonus", new Color(1f, 0.78f, 0.15f));

            SerializeFields(bonusView, ("_spinner", spinner.transform));

            Save(root, "Bonus");
        }

        private static void BuildProjectile()
        {
            var root = new GameObject("Projectile");
            var view = root.AddComponent<ProjectileView>();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Mesh";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = Vector3.one * 0.22f;

            // No collider: hits are resolved by a sphere cast along the shot's path instead.
            Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
            sphere.GetComponent<Renderer>().sharedMaterial = CreateColorMaterial("Tracer", new Color(1f, 0.85f, 0.3f));

            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.09f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0f;
            trail.material = CreateColorMaterial("Tracer", new Color(1f, 0.85f, 0.3f));
            trail.numCapVertices = 2;

            SerializeFields(view, ("_trail", trail));

            Save(root, "Projectile");
        }

        private static void BuildGate()
        {
            var root = new GameObject("Gate");
            var gateView = root.AddComponent<GateView>();

            Material frameMaterial = CreateColorMaterial("GateFrame", new Color(0.32f, 0.34f, 0.38f));

            Transform left = BuildGateWing(root.transform, "LeftWing", -1f, frameMaterial);
            Transform right = BuildGateWing(root.transform, "RightWing", 1f, frameMaterial);

            BuildGatePost(root.transform, -1f, frameMaterial);
            BuildGatePost(root.transform, 1f, frameMaterial);

            SerializeFields(gateView, ("_leftWing", left), ("_rightWing", right));

            Save(root, "Gate");
        }

        private static Transform BuildGateWing(Transform parent, string name, float side, Material material)
        {
            var wing = new GameObject(name);
            wing.transform.SetParent(parent, false);
            wing.transform.localPosition = new Vector3(side * 1.6f, 0f, 0f);

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(wing.transform, false);
            panel.transform.localScale = new Vector3(3.2f, 4f, 0.3f);
            panel.transform.localPosition = new Vector3(0f, 2f, 0f);
            Object.DestroyImmediate(panel.GetComponent<BoxCollider>());
            panel.GetComponent<Renderer>().sharedMaterial = material;

            return wing.transform;
        }

        private static void BuildGatePost(Transform parent, float side, Material material)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = side < 0f ? "LeftPost" : "RightPost";
            post.transform.SetParent(parent, false);
            post.transform.localScale = new Vector3(0.6f, 5f, 0.6f);
            post.transform.localPosition = new Vector3(side * 3.5f, 2.5f, 0f);
            Object.DestroyImmediate(post.GetComponent<BoxCollider>());
            post.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material CreateColorMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static GameObject InstantiateModel(string path, Transform parent, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                Debug.LogError($"[PrefabBuilder] Missing model: {path}");
                return new GameObject("MissingModel");
            }

            // Unpacked on purpose: these are nested model prefabs, and leaving the link intact makes
            // every override in the built prefab a nested-prefab override.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            return instance;
        }

        private static void SerializeFields(Object target, params (string Field, Object Value)[] assignments)
        {
            var serialized = new SerializedObject(target);

            foreach ((string field, Object value) in assignments)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[PrefabBuilder] {target.GetType().Name} has no field '{field}'.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";

            if (!Overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[PrefabBuilder] {name}.prefab already exists and was left alone. " +
                          "Use CrazyDriver/Force Rebuild Prefabs to regenerate it.");

                Object.DestroyImmediate(root);
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureHittableLayer()
        {
            if (LayerMask.NameToLayer(HittableLayerName) >= 0)
            {
                return;
            }

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");

            // Layers 0-7 are reserved by Unity, so user layers start at 8.
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = HittableLayerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[PrefabBuilder] Added layer '{HittableLayerName}' at index {i}.");
                    return;
                }
            }

            Debug.LogError("[PrefabBuilder] No free user layer available.");
        }
    }
}
