using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    [RequireComponent(
        typeof(SpriteRenderer),
        typeof(BoxCollider2D),
        typeof(Rigidbody2D))]
    public sealed class DebrisProjectile : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 8;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        private PrototypeDamageable boss;
        private bool consumed;
        private Vector2 launchVelocity;

        public void Initialize(
            Vector2 velocity,
            PrototypeDamageable bossTarget)
        {
            boss = bossTarget;
            consumed = false;
            launchVelocity = velocity;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0.55f;
            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;
            body.angularVelocity = Random.Range(-260f, 260f);

            if (boss != null)
            {
                Collider2D ownCollider = GetComponent<Collider2D>();
                Collider2D[] bossColliders =
                    boss.GetComponentsInChildren<Collider2D>();
                for (int i = 0; i < bossColliders.Length; i++)
                {
                    Physics2D.IgnoreCollision(
                        ownCollider,
                        bossColliders[i],
                        true);
                }
            }

            Destroy(gameObject, lifetime);
        }

        public void Launch()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.velocity = launchVelocity;
            body.WakeUp();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (consumed)
            {
                return;
            }

            PrototypeDamageable target =
                collision.collider
                    .GetComponentInParent<PrototypeDamageable>();
            if (target == null ||
                target == boss ||
                target.Owner != TerrainOwner.Player)
            {
                return;
            }

            Transform attacker = boss != null
                ? boss.transform
                : transform;
            if (target.TryApplyDamage(
                    damage,
                    TerrainOwner.Boss,
                    attacker))
            {
                consumed = true;
                Destroy(gameObject);
            }
        }
    }
}
