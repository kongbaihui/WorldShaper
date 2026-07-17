using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public interface IPrototypeDamageable
    {
        TerrainOwner Owner { get; }
        Transform DamageTransform { get; }
        bool IsAlive { get; }

        bool TryApplyDamage(int amount, TerrainOwner attacker, Transform attackerTransform);
    }
}
