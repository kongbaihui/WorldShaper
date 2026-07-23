using Challenge2.TerrainPrototype;
using UnityEngine;

public class SecondBossBat : MonoBehaviour
{
    private Transform player;
    private Rigidbody2D body;
    private float moveSpeed;
    private int damage;
    private float dieAt;
    private bool hasHitPlayer;

    public void Initialize(
        Transform target,
        float speed,
        int contactDamage,
        float lifeTime)
    {
        player = target;
        moveSpeed = Mathf.Max(0.1f, speed);
        damage = Mathf.Max(1, contactDamage);
        dieAt = Time.time + Mathf.Max(0.1f, lifeTime);

        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || Time.time >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 direction =
            ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (body != null)
        {
            body.MovePosition(body.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position +=
                (Vector3)(direction * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitPlayer)
        {
            return;
        }

        PrototypeDamageable target =
            other.GetComponentInParent<PrototypeDamageable>();

        if (target == null || target.Owner != TerrainOwner.Player)
        {
            return;
        }

        if (target.TryApplyDamage(damage, TerrainOwner.Boss, transform))
        {
            hasHitPlayer = true;
            Destroy(gameObject);
        }
    }
}
