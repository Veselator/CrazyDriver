using System.Collections.Generic;
using CrazyDriver.Actors;
using CrazyDriver.Config;
using CrazyDriver.Level;
using CrazyDriver.Paths;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>Streams shootable bonuses alongside the enemies and pays their coins into the wallet.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Bonuses)]
    public sealed class BonusSpawner : MonoBehaviour
    {
        private static readonly Color CoinColor = new(1f, 0.82f, 0.25f);

        [SerializeField] private PathTracker _path;
        [SerializeField] private Transform _root;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private PopupPool _popups;

        private readonly List<Stack<Bonus>> _pools = new();
        private readonly Dictionary<Bonus, int> _poolOf = new(32);
        private readonly List<Bonus> _active = new(16);

        private GameConstantsSO _constants;
        private LevelSO _level;
        private CoinWallet _wallet;

        private SpawnStream<BonusSpawnPoint> _stream;

        [Inject]
        public void Construct(GameConstantsSO constants, LevelSO level, CoinWallet wallet)
        {
            _constants = constants;
            _level = level;
            _wallet = wallet;
        }

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }
        }

        public void Load(LevelPlan plan)
        {
            Clear();

            while (_pools.Count < _level.BonusPrefabs.Length)
            {
                _pools.Add(new Stack<Bonus>());
            }

            _stream = new SpawnStream<BonusSpawnPoint>(
                plan.Bonuses,
                static point => point.Distance,
                _constants.Road.DistanceAhead);
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Retire();
            }

            _active.Clear();
            _stream?.Rewind();
        }

        private void Update()
        {
            if (_stream == null)
            {
                return;
            }

            float distance = _path.Distance;
            _stream.Advance(distance, SpawnAt);

            // Bonuses never move, so their spawn distance is always their current distance.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (distance - _active[i].SpawnDistance > _constants.Road.DistanceBehind)
                {
                    _active[i].Retire();
                }
            }
        }

        private void SpawnAt(BonusSpawnPoint point)
        {
            if (_pools.Count == 0)
            {
                return;
            }

            // Clamped rather than thrown: a map referencing a prefab slot the level no longer has
            // should still be playable, just with the wrong-looking bonus.
            int index = Mathf.Clamp(point.PrefabIndex, 0, _pools.Count - 1);

            PathSample sample = _path.Evaluate(point.Distance, point.LateralOffset);

            Bonus bonus = Rent(index);
            bonus.gameObject.SetActive(true);
            bonus.Activate(sample.Position, sample.Rotation, point.Distance, point.CoinReward);

            _active.Add(bonus);
        }

        private Bonus Rent(int index)
        {
            Stack<Bonus> pool = _pools[index];
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            Bonus bonus = Instantiate(_level.BonusPrefabs[index], _root).GetComponent<Bonus>();
            bonus.Released += OnReleased;
            _poolOf[bonus] = index;

            bonus.gameObject.SetActive(false);
            return bonus;
        }

        private void OnReleased(Bonus bonus, bool wasShot)
        {
            if (wasShot)
            {
                _wallet.Add(bonus.CoinReward);

                Vector3 at = bonus.transform.position + Vector3.up;

                _vfx?.Play(at, CoinColor);
                _popups?.Play($"+{bonus.CoinReward}", CoinColor, at + Vector3.up);
            }

            _active.Remove(bonus);

            bonus.gameObject.SetActive(false);
            _pools[_poolOf[bonus]].Push(bonus);
        }

        private void OnDestroy()
        {
            foreach (Bonus bonus in _poolOf.Keys)
            {
                bonus.Released -= OnReleased;
            }
        }
    }
}
