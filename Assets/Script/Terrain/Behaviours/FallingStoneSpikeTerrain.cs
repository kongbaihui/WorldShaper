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

            // 石墙断开后不再是根节点的子物体，从段组件取回原石墙。
            TerrainSegment segmentTarget =
                collision.collider.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrainTarget = segmentTarget != null
                ? segmentTarget.ParentTerrain
                : collision.collider.GetComponentInParent<TerrainEntity>();
            if (terrainTarget != null && terrainTarget != this &&
                terrainTarget.Owner != TerrainOwner.Neutral)
            {
                terrainTarget.TryApplyDamage(_spikeDamage, Owner, transform);
            }
            DestroyTerrain(true);
        }
    }
}
