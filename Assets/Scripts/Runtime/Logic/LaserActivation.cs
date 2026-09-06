using CrazyDriver.Events;
using CrazyDriver.View;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Powers the laser up when a run begins and shuts it off the rest of the time.
    /// <para>
    /// The beam knows how to fade but not when to; the run knows when but nothing about beams. This
    /// is the seam, and it is the whole reason the bus exists: neither side holds a reference to
    /// the other.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaserActivation : MonoBehaviour
    {
        [SerializeField] private LaserBeamView _laser;

        private void Reset() => _laser = GetComponent<LaserBeamView>();

        private void OnEnable()
        {
            GameEvents.OnGameStarted += TurnOff;
            GameEvents.OnRunStarted += TurnOn;
            GameEvents.OnWin += OnFinished;
            GameEvents.OnLose += OnFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= TurnOff;
            GameEvents.OnRunStarted -= TurnOn;
            GameEvents.OnWin -= OnFinished;
            GameEvents.OnLose -= OnFinished;
        }

        private void TurnOn() => _laser.PlayActivation();

        private void TurnOff() => _laser.SetVisible(false);

        private void OnFinished(RunResult result) => TurnOff();
    }
}
