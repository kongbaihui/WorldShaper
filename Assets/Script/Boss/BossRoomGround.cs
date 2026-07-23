using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

public class BossRoomGround : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0.05f)] private float damageInterval = 0.5f;

    private readonly Dictionary<int, float> nextDamageTimes =
        new Dictionary<int, float>();
    private readonly Collider2D[] contactBuffer = new Collider2D[8];

    private Collider2D groundCollider;
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        groundCollider = GetComponent<Collider2D>();
        contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
    }

    private void FixedUpdate()
    {
        if (groundCollider == null)
        {
            return;
        }

        HandleContacts(groundCollider.GetContacts(
            contactFilter,
            contactBuffer));
        HandleContacts(groundCollider.OverlapCollider(
            contactFilter,
            contactBuffer));
    }

    private void HandleContacts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            HandleContact(contactBuffer[i]);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DamagePlayer(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        ForgetPlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ForgetPlayer(other);
    }

    private void HandleContact(Collider2D other)
    {
        DestroyStoneWall(other);
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider2D other)
    {
        PrototypeDamageable target = GetPlayerDamageable(other);
        if (target == null)
        {
            return;
        }

        int targetId = target.GetInstanceID();
        if (nextDamageTimes.TryGetValue(targetId, out float nextTime) &&
            Time.time < nextTime)
        {
            return;
        }

        if (target.TryApplyDamage(damage, TerrainOwner.Neutral, transform))
        {
            nextDamageTimes[targetId] = Time.time + damageInterval;
        }
    }

    private void ForgetPlayer(Collider2D other)
    {
        PrototypeDamageable target = GetPlayerDamageable(other);
        if (target != null)
        {
            nextDamageTimes.Remove(target.GetInstanceID());
        }
    }

    private static PrototypeDamageable GetPlayerDamageable(
        Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        heroscrip hero = other.GetComponentInParent<heroscrip>();
        if (hero == null)
        {
            return null;
        }

        PrototypeDamageable target =
            hero.GetComponent<PrototypeDamageable>();

        return target != null && target.Owner == TerrainOwner.Player
            ? target
            : null;
    }

    private static void DestroyStoneWall(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        TerrainSegment segment =
            other.GetComponentInParent<TerrainSegment>();
        TerrainEntity terrain = segment != null
            ? segment.ParentTerrain
            : other.GetComponentInParent<TerrainEntity>();

        if (terrain is FallingStoneWallTerrain wall &&
            !wall.IsBeingDestroyed)
        {
            wall.DestroyTerrain(true);
        }
    }
}
