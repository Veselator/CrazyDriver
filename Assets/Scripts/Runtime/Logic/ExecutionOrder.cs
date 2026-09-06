namespace CrazyDriver.Logic
{
    /// <summary>
    /// Script execution order for every logic component, in one readable list.
    /// <para>
    /// Splitting the simulation across MonoBehaviours means Unity, not the code, decides what runs
    /// first -- and the order genuinely matters here. The car must move before anything reads its
    /// position, or every enemy chases where the car was last frame. Input must be polled before
    /// the turret smooths towards it, or aiming is a frame behind.
    /// </para>
    /// <para>
    /// These constants go on <c>[DefaultExecutionOrder]</c> so the ordering lives in source and
    /// travels with the code, instead of in the Project Settings window where it is invisible in a
    /// diff and easy to lose.
    /// </para>
    /// </summary>
    public static class ExecutionOrder
    {
        /// <summary>Polls the pointer. Must precede anything that consumes aim.</summary>
        public const int Input = -200;

        /// <summary>Owns run state; enables and disables the components below.</summary>
        public const int GameRunner = -100;

        /// <summary>Advances distance and the car's pose. Everything below reads both.</summary>
        public const int CarMotor = 0;

        /// <summary>Smooths towards the aim angle input just set.</summary>
        public const int Turret = 10;

        /// <summary>Fires against the angle the turret just settled on.</summary>
        public const int Weapon = 20;

        /// <summary>Streams and ticks enemies against this frame's car position.</summary>
        public const int Enemies = 30;

        /// <summary>Streams bonuses. They do not move, so they only need the new distance.</summary>
        public const int Bonuses = 40;

        /// <summary>Moves and resolves projectiles spawned earlier this frame.</summary>
        public const int Projectiles = 50;

        /// <summary>Builds road tiles around the position the car has now reached.</summary>
        public const int Road = 60;
    }
}
