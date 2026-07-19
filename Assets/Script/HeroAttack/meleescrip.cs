using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

public class meleescrip : MonoBehaviour
{
    public float LiveTime = 300f;
    [SerializeField, Min(1)] private int damage = 18;
    [SerializeField, Min(1)] private int terrainDamage = 2;

    private GameObject TheHero;
    private GameObject TheArrow;
    private TerrainDamageService terrainDamageService;
    private readonly HashSet<int> damagedTargets = new HashSet<int>();
    private readonly HashSet<TerrainEntity> damagedTerrain = new HashSet<TerrainEntity>();

    void Start()
    {
        if (TheHero == null)
        {
            TheHero = GameObject.Find("Hero");
        }

        if (TheArrow == null)
        {
            TheArrow = GameObject.Find("Arrow");
        }
    }

    void Update()
    {
        if (TheHero == null || TheArrow == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.localPosition = TheHero.transform.position + TheArrow.transform.up * TheHero.GetComponent<heroscrip>().MeleeRange;
        transform.up = TheArrow.transform.up;
        if (LiveTime > 0)
        {
            LiveTime--;
        }
        else
        {
            Destroy(transform.gameObject);
        }
    }

    public void InitializeDamageSource(
        Transform hero,
        Transform aim,
        TerrainDamageService damageService)
    {
        TheHero = hero != null ? hero.gameObject : null;
        TheArrow = aim != null ? aim.gameObject : null;
        terrainDamageService = damageService;
        damagedTargets.Clear();
        damagedTerrain.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void TryDamage(Collider2D collision)
    {
        Transform attacker = TheHero != null ? TheHero.transform : transform;
        TerrainSegment segment =
            collision.GetComponentInParent<TerrainSegment>();
        TerrainEntity terrain = segment != null
            ? segment.ParentTerrain
            : collision.GetComponentInParent<TerrainEntity>();
        if (segment == null && terrain is ITerrainSegmentHost)
        {
            return;
        }

        if (terrain != null && damagedTerrain.Add(terrain) && terrainDamageService != null)
        {
            if (segment != null)
            {
                terrainDamageService.TryDamageTerrain(
                    segment,
                    terrainDamage,
                    TerrainOwner.Player,
                    attacker);
            }
            else
            {
                terrainDamageService.TryDamageTerrain(
                    terrain,
                    terrainDamage,
                    TerrainOwner.Player,
                    attacker);
            }
        }

        PrototypeDamageable target = collision.GetComponentInParent<PrototypeDamageable>();
        if (target == null)
        {
            return;
        }

        int targetId = target.GetInstanceID();
        if (damagedTargets.Contains(targetId))
        {
            return;
        }

        if (target.TryApplyDamage(damage, TerrainOwner.Player, attacker))
        {
            damagedTargets.Add(targetId);
        }
    }
}
