using System;

namespace CrazyDriver.Core.Run
{
    public enum GameState
    {
        /// <summary>
        /// Level generated, car parked behind the closed gate, camera already framing it from
        /// behind. The turret does not respond to input. A tap starts the run.
        /// </summary>
        Ready,

        Playing,

        /// <summary>Reached the end of the map. A tap regenerates and returns to <see cref="Ready"/>.</summary>
        Won,

        /// <summary>Ran out of hit points. A tap regenerates and returns to <see cref="Ready"/>.</summary>
        Lost
    }

    /// <summary>
    /// The run's state, kept in one place so nothing has to infer "are we playing" from a mix of
    /// flags. Transitions are broadcast rather than polled.
    /// </summary>
    public sealed class GameStateMachine
    {
        /// <summary>Fired after the transition, carrying (previous, current).</summary>
        public event Action<GameState, GameState> Changed;

        public GameState Current { get; private set; } = GameState.Ready;

        /// <summary>True while the player is driving and may aim.</summary>
        public bool IsPlaying => Current == GameState.Playing;

        /// <summary>True in either terminal state, where a tap restarts the level.</summary>
        public bool IsFinished => Current is GameState.Won or GameState.Lost;

        public void Set(GameState next)
        {
            if (Current == next)
            {
                return;
            }

            GameState previous = Current;
            Current = next;
            Changed?.Invoke(previous, next);
        }
    }
}
