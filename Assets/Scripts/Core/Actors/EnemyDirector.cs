using System;
using System.Collections.Generic;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Configuration;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Pooling;
using UnityEngine;

namespace CrazyDriver.Core.Actors
{
    /// <summary>
    /// Owns every live enemy: streams them in ahead of the car, ticks them, and retires them once
    /// they are irrelevant. The view layer only listens to <see cref="Spawned"/>,
    /// <see cref="Killed"/> and <see cref="Retired"/> and binds a pooled model to each agent.
    /// </summary>
    public sealed class EnemyDirector
    {
        private readonly EnemySettings _settings;
        private readonly CarModel _car;
        private readonly PathProgress _progress;
        private readonly AgentPool<EnemyAgent> _pool;
        private readonly List<EnemyAgent> _active = new(32);
        private readonly List<(float Damage, Vector3 Position)> _impacts = new(8);

        private readonly IPathEvaluator _path;

        private SpawnStream<EnemySpawnPoint> _stream;

        public EnemyDirector(EnemySettings settings, CarModel car, PathProgress progress, IPathEvaluator path)
        {
            _settings = settings;
            _car = car;
            _progress = progress;
            _path = path;
            _pool = new AgentPool<EnemyAgent>(() => new EnemyAgent(settings), prewarm: 16);
        }

        /// <summary>An enemy entered the world and needs a view.</summary>
        public event Action<EnemyAgent> Spawned;

        /// <summary>An enemy was shot dead. The view should play its death effect and release.</summary>
        public event Action<EnemyAgent> Killed;

        /// <summary>An enemy left the streaming window alive. The view should release silently.</summary>
        public event Action<EnemyAgent> Retired;

        /// <summary>An enemy rammed the car, carrying the damage dealt and where it landed.</summary>
        public event Action<float, Vector3> CarDamaged;

        public int KillCount { get; private set; }

        public IReadOnlyList<EnemyAgent> Active => _active;

        public void Load(LevelPlan plan)
        {
            Clear();

            KillCount = 0;

            // Enemies are streamed in a little further out than they activate, so the model is
            // already standing in the world by the time it is allowed to start running.
            float lookAhead = _settings.ActivationDistance + 40f;
            _stream = new SpawnStream<EnemySpawnPoint>(plan.Enemies, static point => point.Distance, lookAhead);
        }

        public void Tick(float deltaTime)
        {
            if (_stream == null)
            {
                return;
            }

            float carDistance = _progress.Distance;

            _stream.Advance(carDistance, SpawnAt);

            // Iterating backwards lets a retired enemy be swap-removed in place without disturbing
            // the indices still to be visited.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EnemyAgent agent = _active[i];

                // Impacts are queued rather than reported immediately. Reporting inside the loop
                // lets a killing blow run the whole lose sequence -- which clears this very list --
                // while the loop still holds an index into it.
                float damage = agent.Tick(deltaTime, carDistance, _car.Position, _car.Velocity);
                if (damage > 0f)
                {
                    _impacts.Add((damage, agent.Position));
                }

                if (!agent.IsAlive)
                {
                    // An enemy that destroyed itself on the bumper is not a kill the player earned.
                    if (!agent.DiedOnImpact)
                    {
                        KillCount++;
                    }

                    Release(i, agent, Killed);
                    continue;
                }

                if (HasFallenBehind(agent, carDistance))
                {
                    agent.Retire();
                    Release(i, agent, Retired);
                }
            }

            for (int i = 0; i < _impacts.Count; i++)
            {
                CarDamaged?.Invoke(_impacts[i].Damage, _impacts[i].Position);
            }

            _impacts.Clear();
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EnemyAgent agent = _active[i];
                agent.Retire();
                Release(i, agent, Retired);
            }

            _active.Clear();
            _stream?.Rewind();
        }

        private void SpawnAt(EnemySpawnPoint point)
        {
            PathSample sample = _path.Evaluate(point.Distance, point.LateralOffset);

            EnemyAgent agent = _pool.Rent();
            agent.Activate(sample.Position, sample.Rotation, point.Distance);

            _active.Add(agent);
            Spawned?.Invoke(agent);
        }

        private bool HasFallenBehind(EnemyAgent agent, float carDistance)
        {
            if (agent.State == EnemyState.Idle)
            {
                return carDistance - agent.SpawnDistance > _settings.DespawnDistanceBehind;
            }

            // A chasing enemy has left its spawn point, so project its offset from the car onto the
            // path's forward axis. Measuring straight-line distance instead would keep an enemy
            // alive forever while it ran alongside the car.
            Vector3 forward = _car.PathRotation * Vector3.forward;
            float along = Vector3.Dot(agent.Position - _car.Position, forward);
            return -along > _settings.DespawnDistanceBehind;
        }

        private void Release(int index, EnemyAgent agent, Action<EnemyAgent> notify)
        {
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);

            notify?.Invoke(agent);
            _pool.Return(agent);
        }
    }
}
