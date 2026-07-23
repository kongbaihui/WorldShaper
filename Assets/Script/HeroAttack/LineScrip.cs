using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

public class LineScrip : MonoBehaviour
{
    public float NowScaleY = 5f;
    public float MaxScaleY = 100f;
    public float Speed = 240f;

    [Header("Damage")]
    public int Damage = 25;
    public int TerrainDamage = 3;

    public GameObject TheHero;
    public Vector3 InitialPosition;

    private TerrainDamageService terrainDamageService;

    // 防止一条激光在每一帧重复伤害同一个目标
    private readonly HashSet<int> damagedTargets =
        new HashSet<int>();

    private readonly HashSet<int> damagedTerrain =
        new HashSet<int>();

    private void Start()
    {
        TheHero = GameObject.Find("Hero");

        if (TheHero != null)
        {
            PlayerTerrainController terrainController =
                TheHero.GetComponent<PlayerTerrainController>();

            if (terrainController != null)
            {
                terrainDamageService =
                    terrainController.DamageService;
            }
        }
    }

    private void Update()
    {
        if (TheHero == null)
        {
            Destroy(gameObject);
            return;
        }

        if (NowScaleY < MaxScaleY)
        {
            IncreaseScale();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void IncreaseScale()
    {
        InitialPosition =
            TheHero.transform.position + transform.up;

        Vector3 newScale = transform.localScale;

        newScale.y = NowScaleY;
        transform.localScale = newScale;

        // 激光底部保持在角色附近
        transform.position =
            InitialPosition +
            transform.up * transform.localScale.y;

        float addValue = Speed * Time.deltaTime;

        NowScaleY =
            Mathf.Min(
                NowScaleY + addValue,
                MaxScaleY);
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
        if (collision == null)
        {
            return;
        }

        Transform attacker =
            TheHero != null
                ? TheHero.transform
                : transform;

        DamageTerrainObject(collision, attacker);
        DamageBossObject(collision, attacker);
    }

    private void DamageTerrainObject(
        Collider2D collision,
        Transform attacker)
    {
        TerrainSegment segment =
            collision.GetComponentInParent<TerrainSegment>();

        TerrainEntity terrain =
            segment != null
                ? segment.ParentTerrain
                : collision.GetComponentInParent<TerrainEntity>();

        if (terrain == null ||
            terrainDamageService == null)
        {
            return;
        }

        // 分段地形的根 Trigger 不直接受到伤害
        if (segment == null &&
            terrain is ITerrainSegmentHost)
        {
            return;
        }

        int targetId =
            segment != null
                ? segment.GetInstanceID()
                : terrain.GetInstanceID();

        if (damagedTerrain.Contains(targetId))
        {
            return;
        }

        bool damageApplied;

        if (segment != null)
        {
            damageApplied =
                terrainDamageService.TryDamageTerrain(
                    segment,
                    Mathf.Max(1, TerrainDamage),
                    TerrainOwner.Player,
                    attacker);
        }
        else
        {
            damageApplied =
                terrainDamageService.TryDamageTerrain(
                    terrain,
                    Mathf.Max(1, TerrainDamage),
                    TerrainOwner.Player,
                    attacker);
        }

        if (damageApplied)
        {
            damagedTerrain.Add(targetId);
        }
    }

    private void DamageBossObject(
        Collider2D collision,
        Transform attacker)
    {
        PrototypeDamageable target =
            collision.GetComponentInParent<PrototypeDamageable>();

        if (target == null)
        {
            return;
        }

        int targetId = target.GetInstanceID();

        if (damagedTargets.Contains(targetId))
        {
            return;
        }

        bool damageApplied =
            target.TryApplyDamage(
                Mathf.Max(1, Damage),
                TerrainOwner.Player,
                attacker);

        if (damageApplied)
        {
            damagedTargets.Add(targetId);
        }
    }

    private void OnDestroy()
    {
        if (TheHero == null)
        {
            return;
        }

        heroscrip hero =
            TheHero.GetComponent<heroscrip>();

        if (hero != null)
        {
            hero.IsShootLine = false;
        }
    }
}