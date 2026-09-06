using CrazyDriver.Actors;
using System;
using UnityEngine;

namespace CrazyDriver.Events
{
    /// <summary>What a finished run produced. Carried by <see cref="GameEvents.OnWin"/> and <see cref="GameEvents.OnLose"/>.</summary>
    public readonly struct RunResult
    {
        public readonly float Distance;
        public readonly float Length;
        public readonly int Kills;
        public readonly int Coins;

        public RunResult(float distance, float length, int kills, int coins)
        {
            Distance = distance;
            Length = length;
            Kills = kills;
            Coins = coins;
        }
    }

    /// <summary>
    /// The game's event bus.
    /// <para>
    /// Anything can subscribe without holding a reference to the publisher, which is the whole
    /// point. The cost is that a static event outlives every object that subscribed to it, so two
    /// rules are not optional: subscribe in <c>OnEnable</c> and unsubscribe in <c>OnDisable</c>, and
    /// never subscribe from a constructor.
    /// </para>
    /// </summary>
    public static class GameEvents
    {
        /// <summary>The scene is up and every system has initialised. Raised once per load.</summary>
        public static event Action OnGameStarted;

        /// <summary>
        /// A level has been generated and the car is parked, waiting for a tap. Raised before every
        /// run, including each restart -- which is what <see cref="OnGameStarted"/> cannot do,
        /// since it fires once and a restart has to put the world back into its idle state.
        /// </summary>
        public static event Action OnRunPrepared;

        /// <summary>
        /// The player has tapped and the opening sequence has begun -- camera, countdown, gate.
        /// <para>
        /// Separate from <see cref="OnRunStarted"/>, which lands several seconds later when the car
        /// actually pulls away. Anything that has to react to the tap itself, rather than to the
        /// car moving, belongs here: the start prompt has to leave the screen the moment it is
        /// pressed, not once the countdown has finished.
        /// </para>
        /// </summary>
        public static event Action OnRunStarting;

        /// <summary>The player has committed to a run and the car is pulling away.</summary>
        public static event Action OnRunStarted;

        /// <summary>The car reached the end of the map.</summary>
        public static event Action<RunResult> OnWin;

        /// <summary>The car ran out of hit points.</summary>
        public static event Action<RunResult> OnLose;

        public static event Action<Enemy> OnEnemySpawned;
        public static event Action<Enemy> OnEnemyKilled;

        public static void RaiseGameStarted() => OnGameStarted?.Invoke();

        public static void RaiseRunPrepared() => OnRunPrepared?.Invoke();

        public static void RaiseRunStarting() => OnRunStarting?.Invoke();

        public static void RaiseRunStarted() => OnRunStarted?.Invoke();

        public static void RaiseWin(RunResult result) => OnWin?.Invoke(result);

        public static void RaiseLose(RunResult result) => OnLose?.Invoke(result);
        public static void RaiseEnemySpawned(Enemy enemy) => OnEnemySpawned?.Invoke(enemy);
        public static void RaiseEnemyKilledByBullets(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);
        public static void RaiseEnemyKilledByCar(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);

        /// <summary>
        /// Drops every subscription before a new play session begins.
        /// <para>
        /// A static event is not cleared by leaving play mode when Enter Play Mode Options has
        /// domain reload switched off. Without this the second session runs with the first
        /// session's dead subscribers still attached, and every handler fires against destroyed
        /// objects. This is the single trap that makes static buses a liability, so it is closed
        /// here rather than left to whoever forgets.
        /// </para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            OnGameStarted = null;
            OnRunPrepared = null;
            OnRunStarting = null;
            OnRunStarted = null;
            OnWin = null;
            OnLose = null;
            OnEnemySpawned = null;
            OnEnemyKilled = null;
        }
    }
}
