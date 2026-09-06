using CrazyDriver.Config;
using UnityEngine;

namespace CrazyDriver.Config
{
    /// <summary>
    /// Every tuning number in the game, in one asset.
    /// <para>
    /// The settings themselves are plain serializable classes that live in the core assembly, so
    /// gameplay code takes exactly the group it needs in its constructor and never sees a
    /// ScriptableObject. Splitting this into six separate assets would only add asset plumbing
    /// without changing a single dependency.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "GameConstants", menuName = "CrazyDriver/Game Constants")]
    public sealed class GameConstantsSO : ScriptableObject
    {
        [SerializeField] private CarSettings _car = new();
        [SerializeField] private TurretSettings _turret = new();
        [SerializeField] private WeaponSettings _weapon = new();
        [SerializeField] private EnemySettings _enemy = new();
        [SerializeField] private CameraSettings _camera = new();
        [SerializeField] private RoadSettings _road = new();
        [SerializeField] private GateSettings _gate = new();

        [SerializeField, Tooltip("Layers a projectile can hit. Keep this to enemies and bonuses so " +
             "sweeps never test the road, the car or the gate.")]
        private LayerMask _projectileHitMask = ~0;

        public CarSettings Car => _car;
        public TurretSettings Turret => _turret;
        public WeaponSettings Weapon => _weapon;
        public EnemySettings Enemy => _enemy;
        public CameraSettings Camera => _camera;
        public RoadSettings Road => _road;
        public GateSettings Gate => _gate;

        public LayerMask ProjectileHitMask => _projectileHitMask;

        private void OnValidate()
        {
            // Surface the intercept constraint at authoring time. A designer who raises the car's
            // speed without touching the enemy has just made every enemy on the map harmless, and
            // that failure is completely invisible in play.
            if (!_enemy.CanIntercept(_road.HalfWidth, _car.Speed))
            {
                Debug.LogWarning(
                    $"[{name}] Enemies spawned at the road edge cannot reach the car: " +
                    "raise Enemy.MoveSpeed or Enemy.ActivationDistance, or lower Car.Speed. " +
                    "The level generator will pull such spawns towards the centerline to compensate.",
                    this);
            }
        }

        /// <summary>
        /// Largest gap that will open between the smooth path and the flat road tiles laid along it,
        /// in meters.
        /// <para>
        /// Road pieces are rigid planes placed on the chord between one tile boundary and the next,
        /// so a height wave shorter than a tile leaves the car visibly floating over the middle of
        /// each piece. Levels validate their height profile against this.
        /// </para>
        /// </summary>
        public float GetRoadChordError(HeightProfileSettings height)
        {
            float error = 0f;
            float amplitude = 1f;
            float wavelength = height.Wavelength;
            float amplitudeSum = 0f;

            for (int i = 0; i < height.Octaves; i++)
            {
                amplitudeSum += amplitude;
                amplitude *= height.Persistence;
            }

            amplitude = amplitudeSum > 0f ? height.Amplitude / amplitudeSum : 0f;

            for (int i = 0; i < height.Octaves; i++)
            {
                // Sagitta of a chord spanning one tile across a sine of this wavelength.
                float halfAngle = Mathf.PI * _road.TileLength / Mathf.Max(0.01f, wavelength);
                error += amplitude * (1f - Mathf.Cos(Mathf.Min(halfAngle, Mathf.PI)));

                amplitude *= height.Persistence;
                wavelength /= height.Lacunarity;
            }

            return error;
        }
    }
}
