using System.Collections.Generic;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class FallingStoneWallTerrain : TerrainEntity
    {
        [Header("Impact Damage")]
        [SerializeField, Min(0)] private int _impactDamage = 10;
        [SerializeField, Min(0f)] private float _impactSpeedThreshold = 4.5f;
        [SerializeField, Min(0.05f)] private float _perTargetCooldown = 0.75f;

        private readonly Dictionary<int, float> _nextDamageTimeByTarget = new Dictionary<int, float>();

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsBeingDestroyed || collision.relativeVelocity.magnitude < _impactSpeedThreshold)
            {
                return;
            }

            PrototypeDamageable target = collision.collider.GetComponentInParent<PrototypeDamageable>();
            if (!CanDamageTarget(target))
            {
                return;
            }

            int targetId = target.GetInstanceID();
            if (_nextDamageTimeByTarget.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
            {
                return;
            }

            if (target.TryApplyDamage(_impactDamage, Owner, Creator))
            {
                _nextDamageTimeByTarget[targetId] = Time.time + _perTargetCooldown;
            }
        }
    }
}
