using System;
using System.Collections.Generic;
using CrazyDriver.Core.Configuration;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Pooling;

namespace CrazyDriver.Core.Actors
{
    /// <summary>
    /// Streams shootable bonuses alongside the enemies and pays their coins into the wallet.
    /// </summary>
    public sealed class BonusDirector
    {
        private readonly RoadSettings _road;
        private readonly PathProgress _progress;
        private readonly Wallet _wallet;
        private readonly AgentPool<BonusAgent> _pool;
        private readonly List<BonusAgent> _active = new(16);

        private readonly IPathEvaluator _path;

        private SpawnStream<BonusSpawnPoint> _stream;

        public BonusDirector(RoadSettings road, PathProgress progress, Wallet wallet, IPathEvaluator path)
        {
            _road = road;
            _progress = progress;
            _wallet = wallet;
            _path = path;
            _pool = new AgentPool<BonusAgent>(static () => new BonusAgent(), prewarm: 8);
        }

        /// <summary>A bonus entered the world and needs a view. Carries its prefab index.</summary>
        public event Action<BonusAgent, int> Spawned;

        /// <summary>A bonus was shot. The view should play the pickup effect and release.</summary>
        public event Action<BonusAgent> Collected;

        /// <summary>A bonus left the streaming window unshot. The view should release silently.</summary>
        public event Action<BonusAgent> Retired;

        public IReadOnlyList<BonusAgent> Active => _active;

        public void Load(LevelPlan plan)
        {
            Clear();

            _stream = new SpawnStream<BonusSpawnPoint>(
                plan.Bonuses,
                static point => point.Distance,
                _road.DistanceAhead);
        }

        public void Tick()
        {
            if (_stream == null)
            {
                return;
            }

            float carDistance = _progress.Distance;
            _stream.Advance(carDistance, SpawnAt);

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                BonusAgent agent = _active[i];

                if (agent.WasShot)
                {
                    _wallet.Add(agent.CoinReward);
                    Release(i, agent, Collected);
                    continue;
                }

                // Bonuses never move, so their spawn distance is always their current distance.
                if (carDistance - agent.SpawnDistance > _road.DistanceBehind)
                {
                    agent.Retire();
                    Release(i, agent, Retired);
                }
            }
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                BonusAgent agent = _active[i];
                agent.Retire();
                Release(i, agent, Retired);
            }

            _active.Clear();
            _stream?.Rewind();
        }

        private void SpawnAt(BonusSpawnPoint point)
        {
            PathSample sample = _path.Evaluate(point.Distance, point.LateralOffset);

            BonusAgent agent = _pool.Rent();
            agent.Activate(sample.Position, sample.Rotation, point.Distance, point.CoinReward);

            _active.Add(agent);
            Spawned?.Invoke(agent, point.PrefabIndex);
        }

        private void Release(int index, BonusAgent agent, Action<BonusAgent> notify)
        {
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);

            notify?.Invoke(agent);
            _pool.Return(agent);
        }
    }
}
