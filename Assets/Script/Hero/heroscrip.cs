using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Challenge2.TerrainPrototype;
using UnityEngine;
using UnityEngine.InputSystem;

public class heroscrip : MonoBehaviour
{
    private Rigidbody2D HeroPhysics;
    public float ShootInterval = 0.2f;
    private float BulletSpawnAt;
    public float xSpeed = 3f;
    public float ySpeed = 3f;
    private Collider2D SelfColli;
    //public float BulletInitialSpeed = 7f;
    public GameObject TheArrow;
    public bool IsMelee = false;
    public float MeleeRange = 2f;
    //used in shoot block
    public float MaxBulletSpeed = 50f;
    public float BulletSpeed = 0f;
    private bool HaveArrowInStay = false;

    //end use

    [Header("Ground Detection")]
    [Tooltip("Layers that can be queried below the Hero. Ground and Breakable are enabled by default.")]
    [SerializeField] private LayerMask jumpableLayers = (1 << 6) | (1 << 7);
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.65f;
    [SerializeField, Min(0f)] private float stableWallSpeed = 0.25f;
    [SerializeField, Min(0f)] private float stableWallAngularSpeed = 2f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private bool canDropThroughCurrentSurface;

    //添加的内容
    private PlayerTerrainController terrainController;
    // Start is called before the first frame update
    void Start()
    {
        terrainController = GetComponent<PlayerTerrainController>();

        HeroPhysics = GetComponent<Rigidbody2D>();
        TheArrow = GameObject.Find("Arrow");
        BulletSpawnAt = 0;
        SelfColli = GetComponent<Collider2D>();

        if (jumpableLayers.value == 0)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            int breakableLayer = LayerMask.NameToLayer("Breakable");
            jumpableLayers = (1 << groundLayer) | (1 << breakableLayer);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //control -x
        if (Keyboard.current.aKey.isPressed)
        {
            float tempYSpeed = HeroPhysics.velocity.y;
            HeroPhysics.velocity = new Vector2(-xSpeed, tempYSpeed);
        }
        //control x
        if (Keyboard.current.dKey.isPressed)
        {
            float tempYSpeed = HeroPhysics.velocity.y;
            HeroPhysics.velocity = new Vector2(xSpeed, tempYSpeed);
        }
        bool onGround = OnTheGround();

        //control y
        if (onGround)
        {
            if (Keyboard.current.spaceKey.isPressed && !Keyboard.current.sKey.isPressed)
            {
                float tempXSpeed = HeroPhysics.velocity.x;
                HeroPhysics.velocity = new Vector2(tempXSpeed, ySpeed);
            }
        }

        // 是否允许攻击：不在创造模式时才能攻击
        bool canAttack = terrainController == null || !terrainController.IsBuildMode;

        //shoot
        //change by chu at 7/18/16:00 为远程攻击增加蓄力及滞空
        if (canAttack)
        {
            if (!IsMelee)
            {
                CleanMeleeState();
                // check interval
                if ((Time.time - BulletSpawnAt) > ShootInterval)
                {
                    if (Mouse.current.leftButton.isPressed)
                    {
                        if (!onGround) { HeroPhysics.velocity = new Vector2(0, 0); }
                        HaveArrowInStay = true;
                        if (BulletSpeed < MaxBulletSpeed) { BulletSpeed += 0.1f; }
                    }
                    else
                    {
                        if (HaveArrowInStay)
                        {
                            ShootArrow(BulletSpeed);
                            BulletSpawnAt = Time.time;
                            CleanNotMeleeState();
                        }
                    }
                }

            }
            else
            {
                CleanNotMeleeState();
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    // check interval
                    if ((Time.time - BulletSpawnAt) > ShootInterval)
                    {
                        MeleeAttack();
                        BulletSpawnAt = Time.time;
                    }
                }
            }
        }
        //control drop
        if (onGround)
        {
            if (Keyboard.current.sKey.isPressed)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    //if not on the ground
                    if (canDropThroughCurrentSurface &&
                        !HeroPhysics.IsTouchingLayers(1 << LayerMask.NameToLayer("Ground")))
                    {
                        //enable trigger when tab s + space
                        SelfColli.isTrigger = true;
                    }
                }
            }
        }
        //change weapon
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            IsMelee = !IsMelee;
        }



    }
    //return whether on the ground
    private bool OnTheGround()
    {
        canDropThroughCurrentSurface = false;

        if (SelfColli == null)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(jumpableLayers);
        filter.useTriggers = false;

        int hitCount = SelfColli.Cast(
            Vector2.down,
            filter,
            groundHits,
            groundCheckDistance);

        int groundLayer = LayerMask.NameToLayer("Ground");
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundHits[i];
            if (hit.collider == null || hit.normal.y < minimumGroundNormalY)
            {
                continue;
            }

            // 断裂后的墙段已经离开根节点，要从段组件找到原地形。
            TerrainSegment segment =
                hit.collider.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : hit.collider.GetComponentInParent<TerrainEntity>();
            if (terrain == null)
            {
                if (hit.collider.gameObject.layer == groundLayer)
                {
                    return true;
                }

                continue;
            }

            if (terrain.TerrainType == TerrainType.FloatingPlatform)
            {
                canDropThroughCurrentSurface = true;
                return true;
            }

            if (terrain.TerrainType == TerrainType.FallingStoneWall &&
                IsStableWall(terrain, hit.collider.attachedRigidbody))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsStableWall(
        TerrainEntity terrain,
        Rigidbody2D contactedBody)
    {
        Rigidbody2D wallBody = contactedBody != null
            ? contactedBody
            : terrain.GetComponent<Rigidbody2D>();
        if (wallBody == null || !wallBody.simulated || wallBody.bodyType != RigidbodyType2D.Dynamic)
        {
            return true;
        }

        return wallBody.velocity.sqrMagnitude <= stableWallSpeed * stableWallSpeed &&
               Mathf.Abs(wallBody.angularVelocity) <= stableWallAngularSpeed;
    }

    private void ShootArrow(float BulletSpeed)
    {
        //inst obj
        GameObject bullet = Instantiate(Resources.Load("PreFabs/Bullet") as GameObject);
        //change direct and pos
        bullet.transform.localPosition = transform.position;
        bullet.transform.up = TheArrow.transform.up;
        //give speed
        Vector2 nomvct = bullet.transform.up;
        Rigidbody2D BulletPhysics = bullet.GetComponent<Rigidbody2D>();
        BulletPhysics.velocity = nomvct.normalized * BulletSpeed;

        bulletscrip bulletDamage = bullet.GetComponent<bulletscrip>();
        if (bulletDamage != null)
        {
            bulletDamage.InitializeDamageSource(
                transform,
                terrainController != null ? terrainController.DamageService : null);
        }
    }
    // disable trigger when out of the colli box
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            SelfColli.isTrigger = false;
        }
    }

    private void MeleeAttack()
    {
        GameObject melee = Instantiate(Resources.Load("PreFabs/melee") as GameObject);
        melee.transform.up = TheArrow.transform.up;
        melee.transform.localPosition = transform.position + melee.transform.up * MeleeRange;

        meleescrip meleeDamage = melee.GetComponent<meleescrip>();
        if (meleeDamage != null)
        {
            meleeDamage.InitializeDamageSource(
                transform,
                TheArrow.transform,
                terrainController != null ? terrainController.DamageService : null);
        }
    }



    private void CleanMeleeState()
    { }

    private void CleanNotMeleeState()
    {
        HaveArrowInStay = false;
        BulletSpeed = 0;
    }
}
