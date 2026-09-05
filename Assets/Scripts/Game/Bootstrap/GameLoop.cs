using CrazyDriver.Core.Path;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Views;
using UnityEngine;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// The one place per-frame simulation is advanced, in an explicit order.
    /// <para>
    /// Nothing in the game has its own Update. Spreading ticks across dozens of MonoBehaviours would
    /// leave the order up to Unity's script execution settings; here it is four lines you can read.
    /// </para>
    /// </summary>
    public sealed class GameLoop : ITickable
    {
        private readonly RunController _run;
        private readonly ProjectileSystem _projectiles;
        private readonly VisualPath _road;
        private readonly PathProgress _progress;

        public GameLoop(RunController run, ProjectileSystem projectiles, VisualPath road, PathProgress progress)
        {
            _run = run;
            _projectiles = projectiles;
            _road = road;
            _progress = progress;
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;

            _run.Tick(deltaTime);
            _projectiles.Tick(deltaTime);

            // Streamed after the run has advanced, so tiles are built around this frame's position.
            _road.UpdateStreaming(_progress.Distance);
        }
    }
}
