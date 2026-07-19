using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public interface ITerrainSegmentHost
    {
        TerrainEntity TerrainEntity { get; }

        int GetSegmentCurrentHealth(int segmentIndex);

        bool TryApplySegmentDamage(
            int segmentIndex,
            int amount,
            TerrainOwner attacker,
            Transform attackerTransform);
    }

    // 2026-07-19：子段只记录命中了哪一块，销毁还是交给父地形处理。
    public sealed class TerrainSegment : MonoBehaviour
    {
        private ITerrainSegmentHost _host;

        public int SegmentIndex { get; private set; }
        public TerrainEntity ParentTerrain =>
            _host != null ? _host.TerrainEntity : null;
        public Collider2D SegmentCollider { get; private set; }
        public int CurrentHealth =>
            _host != null
                ? _host.GetSegmentCurrentHealth(SegmentIndex)
                : 0;

        public void Initialize(
            ITerrainSegmentHost host,
            int segmentIndex,
            Collider2D segmentCollider)
        {
            _host = host;
            SegmentIndex = segmentIndex;
            SegmentCollider = segmentCollider;
        }

        public bool TryApplyDamage(
            int amount,
            TerrainOwner attacker,
            Transform attackerTransform)
        {
            return _host != null &&
                   _host.TryApplySegmentDamage(
                       SegmentIndex,
                       amount,
                       attacker,
                       attackerTransform);
        }
    }
}
