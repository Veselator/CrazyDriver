using CrazyDriver.Core.Combat;
using UnityEngine;

namespace CrazyDriver.Game.Views
{
    /// <summary>
    /// The bridge from a physics collider back to the agent that owns it.
    /// <para>
    /// Hits are resolved by collider, as the brief requires, but damage belongs to the model. This
    /// component is the only place the two meet: a projectile that sweeps into a collider looks for
    /// this and hands the damage to whatever <see cref="IDamageable"/> it points at.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageReceiver : MonoBehaviour
    {
        /// <summary>Set by the view that owns this collider when it binds to an agent.</summary>
        public IDamageable Target { get; set; }

        public void Apply(float damage) => Target?.TakeDamage(damage);
    }
}
