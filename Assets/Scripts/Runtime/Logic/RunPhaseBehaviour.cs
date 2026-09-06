using System;
using CrazyDriver.Events;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>Phases of a run a component can declare itself active during.</summary>
    [Flags]
    public enum RunPhase
    {
        None = 0,

        /// <summary>Level generated, car parked, waiting for the starting tap.</summary>
        Ready = 1 << 0,

        Playing = 1 << 1,

        /// <summary>After a win or a loss, while the car rolls to a stop.</summary>
        Finished = 1 << 2,

        Always = Ready | Playing | Finished
    }

    /// <summary>
    /// A component that switches itself on and off with the run.
    /// <para>
    /// The alternative was a method on the runner listing every component and the phases it belongs
    /// in. That list has to be edited for each new system, and forgetting to is a bug that only
    /// shows up as something quietly still running after a loss. Here each component states its own
    /// phases and nothing central has to know it exists.
    /// </para>
    /// </summary>
    public abstract class RunPhaseBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("The phases this component is active during.")]
        private RunPhase _activeDuring = RunPhase.Playing;

        /// <summary>
        /// Subscribed in Awake and released in OnDestroy rather than in OnEnable and OnDisable.
        /// <para>
        /// That is deliberately the opposite of the rule everything else follows for the static
        /// bus. A component that disables itself in one phase still has to hear about the next one,
        /// and a subscription released in OnDisable would be gone the moment it was needed. The
        /// object is disabled here, not destroyed, so the subscription is not a dangling one.
        /// </para>
        /// </summary>
        private void Awake()
        {
            GameEvents.OnRunPrepared += EnterReady;
            GameEvents.OnRunStarted += EnterPlaying;
            GameEvents.OnWin += EnterFinished;
            GameEvents.OnLose += EnterFinished;

            // Start in whatever the component asked for in the idle phase. Without this a
            // Playing-only system would tick once before the first level even exists.
            Apply(RunPhase.Ready);

            OnAwake();
        }

        private void OnDestroy()
        {
            GameEvents.OnRunPrepared -= EnterReady;
            GameEvents.OnRunStarted -= EnterPlaying;
            GameEvents.OnWin -= EnterFinished;
            GameEvents.OnLose -= EnterFinished;

            OnDestroyed();
        }

        /// <summary>Awake for subclasses. The base class needs the real one for its subscriptions.</summary>
        protected virtual void OnAwake()
        {
        }

        protected virtual void OnDestroyed()
        {
        }

        /// <summary>Called whenever the phase changes, after <c>enabled</c> has been updated.</summary>
        protected virtual void OnPhaseChanged(RunPhase phase)
        {
        }

        private void EnterReady() => Apply(RunPhase.Ready);

        private void EnterPlaying() => Apply(RunPhase.Playing);

        private void EnterFinished(RunResult result) => Apply(RunPhase.Finished);

        private void Apply(RunPhase phase)
        {
            enabled = (_activeDuring & phase) != 0;
            OnPhaseChanged(phase);
        }
    }
}
