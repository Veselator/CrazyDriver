using System;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Views;
using VContainer.Unity;

namespace CrazyDriver.Game.Bootstrap
{
    /// <summary>
    /// Powers the laser up when a run begins and shuts it off the rest of the time.
    /// <para>
    /// The beam knows how to flicker but not when to; the run knows when but nothing about beams.
    /// This is the seam between the two, which is why <see cref="LaserBeamView"/> stays usable on
    /// its own and the run controller never learns that a laser exists.
    /// </para>
    /// </summary>
    public sealed class LaserActivationSystem : IInitializable, IDisposable
    {
        private readonly RunController _run;
        private readonly LaserBeamView _laser;

        public LaserActivationSystem(RunController run, LaserBeamView laser)
        {
            _run = run;
            _laser = laser;
        }

        public void Initialize()
        {
            _run.RunPrepared += OnRunPrepared;
            _run.RunStarted += OnRunStarted;
            _run.RunFinished += OnRunFinished;

            _laser.SetVisible(false);
        }

        public void Dispose()
        {
            _run.RunPrepared -= OnRunPrepared;
            _run.RunStarted -= OnRunStarted;
            _run.RunFinished -= OnRunFinished;
        }

        private void OnRunPrepared(Core.Level.LevelPlan plan) => _laser.SetVisible(false);

        private void OnRunStarted() => _laser.PlayActivation();

        private void OnRunFinished(bool won) => _laser.SetVisible(false);
    }
}
