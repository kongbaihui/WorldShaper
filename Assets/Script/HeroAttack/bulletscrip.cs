using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

public class bulletscrip : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(1)] private int terrainDamage = 1;

    private Rigidbody2D BulletPhysics;
    private Transform attackerTransform;
    private TerrainDamageService terrainDamageService;
    private bool hasDamagedActor;
    private readonly HashSet<int> damagedTerrainIds = new HashSet<int>();

    void Start()
    {
        BulletPhysics = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (BulletPhysics.velocity.magnitude > 0.01f)
        {
            transform.up = BulletPhysics.velocity.normalized;
        }
    }

    public void InitializeDamageSource(
        Transform attacker,
        TerrainDamageService damageService)
    {
        attackerTransform = attacker;
        terrainDamageService = damageService;
        damagedTerrainIds.Clear();
        hasDamagedActor = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TerrainEntity terrain = collision.GetComponentInParent<TerrainEntity>();
        if (terrain != null && damagedTerrainIds.Add(terrain.GetInstanceID()))
        {
            Transform attacker = attackerTransform != null ? attackerTransform : transform;
            if (terrainDamageService != null && terrainDamageService.TryDamageTerrain(
                    terrain,
                    terrainDamage,
                    TerrainOwner.Player,
                    attacker))
            {
                Destroy(gameObject);
                return;
            }
        }

        if (!hasDamagedActor)
        {
            PrototypeDamageable target = collision.GetComponentInParent<PrototypeDamageable>();
            if (target != null && target.TryApplyDamage(
                    damage,
                    TerrainOwner.Player,
                    attackerTransform != null ? attackerTransform : transform))
            {
                hasDamagedActor = true;
                Destroy(gameObject);
                return;
            }
        }

        if (collision.gameObject.layer == 6 || collision.gameObject.layer == 7)
        {
            Destroy(gameObject);
        }
    }
}
