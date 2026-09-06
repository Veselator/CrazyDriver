namespace CrazyDriver.Combat
{
    /// <summary>Anything a projectile can meaningfully hit.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(float amount);
    }
}
