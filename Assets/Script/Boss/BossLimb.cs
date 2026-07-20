using System.Collections;
using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    [RequireComponent(typeof(PrototypeDamageable), typeof(Collider2D))]
    public sealed class BossLimb : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float firstAttackDelay = 2f;
        [SerializeField, Min(0.1f)] private float attackInterval = 6f;
        [SerializeField, Min(0.1f)] private float positioningSpeed = 12f;
        [SerializeField, Min(0f)] private float chargeDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 45f;
        [SerializeField, Min(0f)] private float oppositeSideSpacing = 3f;
        [SerializeField, Min(0)] private int playerDamage = 20;
        [SerializeField, Min(0)] private int platformDamage = 3;
        [SerializeField, Min(0)] private int fallingWallDamage = 10;
        [SerializeField, Min(0)] private int fallingSpikeDamage = 38;

        [Header("Simple Health Bar")]
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 4f, 0f);
        [SerializeField] private Vector2 healthBarSize = new Vector2(80f, 8f);

        private readonly HashSet<int> damagedTerrain = new HashSet<int>();
        private readonly HashSet<int> fallingTerrainHits = new HashSet<int>();
        private static BossLimb activeAttacker;
        [SerializeField] private PrototypeDamageable player;
        private PrototypeDamageable health;
        private BossLimb otherArm;
        private Collider2D limbCollider;
        private SpriteRenderer limbRenderer;
        private Camera worldCamera;
        private Vector3 homePosition;
        private Coroutine attackLoop;
        private bool dashBlocked;
        private bool isDashing;
        private bool damagedPlayer;
        private bool homeFlipX;

        private void Awake()
        {
            health = GetComponent<PrototypeDamageable>();
            limbCollider = GetComponent<Collider2D>();
            limbRenderer = GetComponent<SpriteRenderer>();
            homeFlipX = limbRenderer != null && limbRenderer.flipX;
            homePosition = transform.position;
        }

        private void Start()
        {
            otherArm = FindOtherLivingArm();
            worldCamera = Camera.main;
            attackLoop = StartCoroutine(AttackLoop());
        }

        private void OnEnable()
        {
            health.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= HandleHealthChanged;
            }

            if (attackLoop != null)
            {
                StopCoroutine(attackLoop);
            }

            if (activeAttacker == this)
            {
                activeAttacker = null;
            }
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitForSeconds(firstAttackDelay);

            while (health != null && health.IsAlive)
            {
                if (player == null || !player.IsAlive)
                {
                    yield return null;
                    continue;
                }

                while (player != null && player.IsAlive && !CanTakeAttackTurn())
                {
                    yield return null;
                }

                if (player != null && player.IsAlive)
                {
                    activeAttacker = this;
                    yield return AttackFromBothSides();
                    activeAttacker = null;
                    yield return new WaitForSeconds(attackInterval);
                }
            }
        }

        private IEnumerator AttackFromBothSides()
        {
            float oppositeX = GetOppositeSideX();

            // 在自己这一侧先上下追踪玩家，再蓄力横冲到另一边。
            yield return AlignYWithPlayer();
            yield return new WaitForSeconds(chargeDuration);
            yield return DashToX(oppositeX);
            if (dashBlocked)
            {
                SetFacing(false);
                yield return MoveTo(homePosition);
                yield break;
            }

            // 到达另一侧后重新定位玩家，再横冲回来。
            yield return AlignYWithPlayer();
            SetFacing(true);
            yield return new WaitForSeconds(chargeDuration);
            yield return DashToX(homePosition.x);
            yield return MoveTo(homePosition);
            SetFacing(false);
        }

        private IEnumerator AlignYWithPlayer()
        {
            DestroyTouchingWallBelow();

            while (player != null && player.IsAlive &&
                   Mathf.Abs(transform.position.y - player.transform.position.y) > 0.1f)
            {
                Vector3 position = transform.position;
                position.y = Mathf.MoveTowards(
                    position.y,
                    player.transform.position.y,
                    positioningSpeed * Time.deltaTime);
                transform.position = position;
                DestroyTouchingWallBelow();
                yield return null;
            }
        }

        // 上下瞄准时，只有实际碰到手臂下方的石墙才整面销毁。
        private void DestroyTouchingWallBelow()
        {
            Physics2D.SyncTransforms();

            Collider2D[] hits = Physics2D.OverlapBoxAll(
                limbCollider.bounds.center,
                limbCollider.bounds.size,
                transform.eulerAngles.z);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == limbCollider ||
                    hits[i].bounds.center.y > limbCollider.bounds.center.y)
                {
                    continue;
                }

                TerrainSegment segment = hits[i].GetComponentInParent<TerrainSegment>();
                TerrainEntity terrain = segment != null
                    ? segment.ParentTerrain
                    : hits[i].GetComponentInParent<TerrainEntity>();

                if (terrain != null && !terrain.IsBeingDestroyed &&
                    terrain.TerrainType == TerrainType.FallingStoneWall)
                {
                    terrain.DestroyTerrain(true);
                }
            }
        }

        private IEnumerator DashToX(float targetX)
        {
            dashBlocked = false;
            isDashing = true;
            damagedTerrain.Clear();
            damagedPlayer = false;

            while (Mathf.Abs(transform.position.x - targetX) > 0.1f)
            {
                if (dashBlocked)
                {
                    isDashing = false;
                    yield break;
                }

                Vector3 nextPosition = transform.position;
                nextPosition.x = Mathf.MoveTowards(
                    nextPosition.x,
                    targetX,
                    dashSpeed * Time.deltaTime);

                if (CheckDashPath(nextPosition))
                {
                    dashBlocked = true;
                    isDashing = false;
                    yield break;
                }

                transform.position = nextPosition;
                yield return null;
            }

            isDashing = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryBlockMovingWall(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTakeFallingTerrainDamage(other);
            TryBlockMovingWall(other);
        }

        private void TryTakeFallingTerrainDamage(Collider2D other)
        {
            if (other == null || !IsAboveArm(other) ||
                other.attachedRigidbody == null ||
                other.attachedRigidbody.velocity.y >= -0.1f)
            {
                return;
            }

            TerrainSegment segment = other.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : other.GetComponentInParent<TerrainEntity>();

            if (terrain == null || terrain.IsBeingDestroyed ||
                terrain.Owner != TerrainOwner.Player ||
                !fallingTerrainHits.Add(terrain.GetInstanceID()))
            {
                return;
            }

            int damage = terrain.TerrainType == TerrainType.FallingStoneWall
                ? fallingWallDamage
                : terrain.TerrainType == TerrainType.FallingStoneSpike
                    ? fallingSpikeDamage
                    : 0;

            if (damage > 0)
            {
                health.TryApplyDamage(damage, TerrainOwner.Player, terrain.transform);
            }

            if (terrain.TerrainType == TerrainType.FallingStoneSpike)
            {
                terrain.DestroyTerrain(true);
            }
        }

        private void TryBlockMovingWall(Collider2D other)
        {
            if (!isDashing || dashBlocked || other == null)
            {
                return;
            }

            TerrainSegment segment = other.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : other.GetComponentInParent<TerrainEntity>();

            if (terrain == null || terrain.IsBeingDestroyed ||
                terrain.TerrainType != TerrainType.FallingStoneWall)
            {
                return;
            }

            // 从手臂上方落下来的墙不阻止横向冲刺。
            if (IsAboveArm(other))
            {
                return;
            }

            dashBlocked = true;
            terrain.DestroyTerrain(true);
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            while ((transform.position - destination).sqrMagnitude > 0.04f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    positioningSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;
        }

        // 只有玩家的石墙会终止冲锋；玩家和平台会受伤，但手臂继续冲到对面。
        private bool CheckDashPath(Vector3 nextPosition)
        {
            // 手臂由 Transform 驱动，查询前同步物理世界，避免高速冲刺漏掉石墙。
            Physics2D.SyncTransforms();

            Vector2 movement = nextPosition - transform.position;
            float distance = movement.magnitude;
            if (distance <= 0f)
            {
                return false;
            }

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                limbCollider.bounds.center,
                limbCollider.bounds.size,
                transform.eulerAngles.z,
                movement.normalized,
                distance);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hitCollider = hits[i].collider;
                if (hitCollider == null || hitCollider == limbCollider ||
                    hitCollider.GetComponentInParent<BossLimb>() != null)
                {
                    continue;
                }

                PrototypeDamageable actor = hitCollider.GetComponentInParent<PrototypeDamageable>();
                if (actor != null && actor.Owner == TerrainOwner.Player)
                {
                    if (!damagedPlayer)
                    {
                        actor.TryApplyDamage(playerDamage, TerrainOwner.Boss, transform);
                        damagedPlayer = true;
                    }

                    continue;
                }

                TerrainSegment segment = hitCollider.GetComponentInParent<TerrainSegment>();
                TerrainEntity terrain = segment != null
                    ? segment.ParentTerrain
                    : hitCollider.GetComponentInParent<TerrainEntity>();

                if (terrain == null || terrain.IsBeingDestroyed)
                {
                    continue;
                }

                if (terrain.TerrainType == TerrainType.FallingStoneWall)
                {
                    if (IsAboveArm(hitCollider))
                    {
                        continue;
                    }

                    terrain.DestroyTerrain(true);
                    return true;
                }

                int targetId = segment != null
                    ? segment.GetInstanceID()
                    : terrain.GetInstanceID();
                if (!damagedTerrain.Add(targetId))
                {
                    continue;
                }

                DamageTerrain(segment, terrain, platformDamage);
            }

            return false;
        }

        private bool IsAboveArm(Collider2D other)
        {
            return other.bounds.min.y >= limbCollider.bounds.center.y;
        }

        private void DamageTerrain(
            TerrainSegment segment,
            TerrainEntity terrain,
            int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            if (segment != null)
            {
                segment.TryApplyDamage(damage, TerrainOwner.Boss, transform);
            }
            else
            {
                terrain.TryApplyDamage(damage, TerrainOwner.Boss, transform);
            }
        }

        private void SetFacing(bool returning)
        {
            if (limbRenderer != null)
            {
                limbRenderer.flipX = returning ? !homeFlipX : homeFlipX;
            }
        }

        private bool CanTakeAttackTurn()
        {
            return activeAttacker == null;
        }

        private float GetOppositeSideX()
        {
            if (otherArm != null)
            {
                return otherArm.homePosition.x +
                       Mathf.Sign(homePosition.x - otherArm.homePosition.x) * oppositeSideSpacing;
            }

            float centerX = transform.root.position.x;
            return centerX - (homePosition.x - centerX);
        }

        private BossLimb FindOtherLivingArm()
        {
            BossLimb[] arms = transform.root.GetComponentsInChildren<BossLimb>();
            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i] != this && arms[i].health != null && arms[i].health.IsAlive)
                {
                    return arms[i];
                }
            }

            return null;
        }

        private void HandleHealthChanged(int currentHealth, int maximumHealth)
        {
            if (currentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void OnGUI()
        {
            if (health == null || !health.IsAlive || worldCamera == null)
            {
                return;
            }

            Vector3 screen = worldCamera.WorldToScreenPoint(transform.position + healthBarOffset);
            if (screen.z <= 0f)
            {
                return;
            }

            float rate = health.CurrentHealth / (float)health.MaximumHealth;
            Rect background = new Rect(
                screen.x - healthBarSize.x * 0.5f,
                Screen.height - screen.y,
                healthBarSize.x,
                healthBarSize.y);
            GUI.color = Color.black;
            GUI.DrawTexture(background, Texture2D.whiteTexture);
            GUI.color = Color.red;
            GUI.DrawTexture(
                new Rect(background.x + 1f, background.y + 1f,
                    (background.width - 2f) * rate, background.height - 2f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

    }
}
