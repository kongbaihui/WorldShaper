using UnityEngine;


namespace Challenge2.TerrainPrototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class FallingStoneSpikeTerrain : TerrainEntity
    {
        [SerializeField, Range(1, 100)] private int _spikeDamage = 38;
        private bool _hasImpacted;
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_hasImpacted || IsBeingDestroyed)
            {
                return;
            }
            _hasImpacted = true;
            PrototypeDamageable target = collision.collider.GetComponentInParent<PrototypeDamageable>();
            if (CanDamageTarget(target))
            {
                target.TryApplyDamage(_spikeDamage, Owner, Creator);
            }

            TerrainEntity terrainTarget = collision.collider.GetComponentInParent<TerrainEntity>();
            if (terrainTarget != null && terrainTarget != this &&
                terrainTarget.Owner != TerrainOwner.Neutral)
            {
                terrainTarget.TryApplyDamage(_spikeDamage, Owner, transform);
            }
            DestroyTerrain(true);
        }
    }
}
