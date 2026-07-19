using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    [DisallowMultipleComponent]
    public sealed class BossTerrainAttackController : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private TerrainCreationService creationService;
        [SerializeField] private TerrainRegistry registry;
        [SerializeField] private BossAttackTelegraph telegraph;
        [SerializeField] private Transform player;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Transform bossTransform;
        [SerializeField] private PrototypeDamageable bossDamageable;

        [Header("Placement and Prediction")]
        [SerializeField, Min(0f)] private float shieldVerticalOffset = 5f;
        [SerializeField, Range(0f, 2f)] private float wallPredictionTime = 0.5f;
        [SerializeField, Range(0f, 2f)] private float spikePredictionTime = 0.42f;
        [SerializeField, Min(0f)] private float maximumPredictionDistance = 8f;
        [SerializeField] private LayerMask landingSurfaceMask = ~0;

        [Header("Attack Timing")]
        [SerializeField, Min(0f)] private float floatingPlatformTelegraphDuration = 0.8f;
        [SerializeField, Min(0f)] private float wallTelegraphDuration = 1f;
        [SerializeField, Min(0f)] private float spikeTelegraphDuration = 0.9f;
        [SerializeField, Min(0f)] private float collapseTelegraphDuration = 1f;

        [Header("Terrain Collapse")]
        [SerializeField, Min(0.1f)] private float destructionRange = 70f;

        private readonly List<TerrainEntity> terrainBuffer = new List<TerrainEntity>(32);
        private readonly RaycastHit2D[] raycastBuffer = new RaycastHit2D[32];
        private bool referencesReady;

        public float DestructionRange => destructionRange;

        private void Awake()
        {
            if (bossTransform == null)
            {
                bossTransform = transform;
            }

            if (bossDamageable == null)
            {
                bossDamageable = GetComponent<PrototypeDamageable>();
            }

            if (playerBody == null && player != null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
            }

            referencesReady = ValidateReferences();
        }

        private void OnDisable()
        {
            CancelActiveTelegraph();
        }

        public bool TryPrepareAttack(BossAttackType attackType, out BossAttackPlan plan)
        {
            plan = null;
            if (!referencesReady || bossDamageable == null || !bossDamageable.IsAlive)
            {
                return false;
            }

            if (attackType == BossAttackType.TerrainCollapse)
            {
                TerrainEntity collapseTarget = SelectCollapseTarget();
                if (collapseTarget == null)
                {
                    return false;
                }

                plan = new BossAttackPlan
                {
                    AttackType = attackType,
                    CollapseTarget = collapseTarget,
                    SpawnPosition = collapseTarget.transform.position,
                    LandingPosition = collapseTarget.transform.position,
                    AttackReferencePoint = player.position,
                    TelegraphDuration = collapseTelegraphDuration
                };
                return true;
            }

            TerrainType terrainType;
            Vector2 referencePoint;
            Vector2 requestedPosition;
            float telegraphDuration;

            switch (attackType)
            {
                case BossAttackType.FloatingPlatformShield:
                    terrainType = TerrainType.FloatingPlatform;
                    referencePoint = player.position;
                    float horizontalOffset = Random.Range(-8f, 8f);
                    if (Mathf.Abs(horizontalOffset) < 2f)
                    {
                        horizontalOffset = Random.Range(-8f, 8f);
                    }
                    requestedPosition = referencePoint +
                                        new Vector2(horizontalOffset,
                                            Random.Range(shieldVerticalOffset - 2.5f, shieldVerticalOffset + 1.5f));
                    telegraphDuration = floatingPlatformTelegraphDuration;
                    break;

                case BossAttackType.FallingStoneWall:
                    terrainType = TerrainType.FallingStoneWall;
                    referencePoint = PredictPlayerPosition(wallPredictionTime);
                    requestedPosition = GetRandomFallingSpawnPosition(
                        terrainType,
                        referencePoint);
                    telegraphDuration = wallTelegraphDuration;
                    break;

                case BossAttackType.FallingStoneSpike:
                    terrainType = TerrainType.FallingStoneSpike;
                    referencePoint = PredictPlayerPosition(spikePredictionTime);
                    requestedPosition = GetRandomFallingSpawnPosition(
                        terrainType,
                        referencePoint);
                    telegraphDuration = spikeTelegraphDuration;
                    break;

                default:
                    return false;
            }

            requestedPosition = ClampToArena(terrainType, requestedPosition);
            if (!TryFindValidPosition(terrainType, requestedPosition, out Vector2 validPosition))
            {
                return false;
            }

            Vector2 landingPosition = attackType == BossAttackType.FloatingPlatformShield
                ? validPosition
                : FindLandingPosition(validPosition);

            plan = new BossAttackPlan
            {
                AttackType = attackType,
                SpawnPosition = validPosition,
                AttackReferencePoint = referencePoint,
                LandingPosition = landingPosition,
                TelegraphDuration = telegraphDuration
            };
            return true;
        }

        public void BeginTelegraph(BossAttackPlan plan)
        {
            if (telegraph != null)
            {
                telegraph.Show(plan);
            }
        }

        public bool RefreshTrackedAttack(BossAttackPlan plan)
        {
            if (plan == null || !referencesReady || bossDamageable == null || !bossDamageable.IsAlive)
            {
                return false;
            }

            TerrainType terrainType;
            float predictionTime;

            switch (plan.AttackType)
            {
                case BossAttackType.FallingStoneWall:
                    terrainType = TerrainType.FallingStoneWall;
                    predictionTime = wallPredictionTime;
                    break;

                case BossAttackType.FallingStoneSpike:
                    terrainType = TerrainType.FallingStoneSpike;
                    predictionTime = spikePredictionTime;
                    break;

                case BossAttackType.TerrainCollapse:
                    return plan.CollapseTarget != null && !plan.CollapseTarget.IsBeingDestroyed &&
                           IsStillRegistered(plan.CollapseTarget);

                default:
                    return true;
            }

            Vector2 referencePoint = PredictPlayerPosition(predictionTime);
            // 2026-07-19：预警期间只继续追踪 X，Y 使用本次攻击抽到的高度。
            Vector2 requestedPosition = ClampFallingSpawnAtHeight(
                terrainType,
                referencePoint.x,
                plan.SpawnPosition.y);

            if (!TryFindValidPosition(terrainType, requestedPosition, out Vector2 validPosition))
            {
                return false;
            }

            plan.AttackReferencePoint = referencePoint;
            plan.SpawnPosition = validPosition;
            plan.LandingPosition = FindLandingPosition(validPosition);
            if (telegraph != null)
            {
                telegraph.UpdateTracking(plan);
            }

            return true;
        }

        public bool Execute(BossAttackPlan plan)
        {
            if (plan == null || !referencesReady || bossDamageable == null || !bossDamageable.IsAlive)
            {
                return false;
            }

            if (plan.AttackType == BossAttackType.TerrainCollapse)
            {
                TerrainEntity target = plan.CollapseTarget;
                if (target == null || target.IsBeingDestroyed || !IsStillRegistered(target))
                {
                    return false;
                }

                target.DestroyTerrain(true);
                return true;
            }

            TerrainType terrainType = GetTerrainType(plan.AttackType);
            if (terrainType == TerrainType.FallingStoneWall ||
                terrainType == TerrainType.FallingStoneSpike)
            {
                // 石墙和石锥到释放时间后只做一次最终结算。
                ResolveLockedFallingAttack(
                    terrainType,
                    plan.SpawnPosition);
                return true;
            }

            // 其他攻击即使最终放置失败，也结束本轮并进入原冷却。
            creationService.TryCreateTerrain(
                terrainType,
                TerrainOwner.Boss,
                bossTransform,
                plan.SpawnPosition);
            return true;
        }

        public void CancelActiveTelegraph()
        {
            if (telegraph != null)
            {
                telegraph.Clear();
            }
        }

        public TerrainEntity SelectCollapseTarget()
        {
            if (!referencesReady || registry == null || player == null || bossTransform == null)
            {
                return null;
            }

            registry.CopyActiveTerrain(terrainBuffer);
            TerrainEntity bestTarget = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < terrainBuffer.Count; i++)
            {
                TerrainEntity terrain = terrainBuffer[i];
                if (!IsValidCollapseTarget(terrain))
                {
                    continue;
                }

                int priority;
                if (terrain.Owner == TerrainOwner.Player)
                {
                    priority = 0;
                }
                else if (terrain.Owner == TerrainOwner.Neutral)
                {
                    priority = 1;
                }
                else if (terrain.Owner == TerrainOwner.Boss &&
                         terrain.TerrainType != TerrainType.FallingStoneSpike)
                {
                    priority = 2;
                }
                else
                {
                    priority = 3;
                }

                float playerDistance = Vector2.Distance(terrain.transform.position, player.position);
                float score = priority * 100000f + playerDistance;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = terrain;
                }
            }

            return bestTarget;
        }

        private bool ValidateReferences()
        {
            bool valid = creationService != null && registry != null && telegraph != null &&
                         player != null && bossTransform != null && bossDamageable != null;
            if (!valid)
            {
                Debug.LogWarning(
                    "BossTerrainAttackController is missing Terrain services, Telegraph, Player, Boss, or health references.",
                    this);
            }

            return valid;
        }

        private bool TryFindValidPosition(TerrainType terrainType, Vector2 requestedPosition, out Vector2 validPosition)
        {
            float grid = Mathf.Max(0.1f, creationService.GridSize);
            for (int i = 0; i < 9; i++)
            {
                Vector2 candidate = ClampToArena(terrainType, requestedPosition + GetCandidateOffset(i, grid));
                TerrainPlacementResult placement = creationService.EvaluatePlacement(
                    terrainType,
                    TerrainOwner.Boss,
                    bossTransform,
                    candidate);

                if (!placement.IsValid)
                {
                    continue;
                }

                validPosition = placement.SnappedPosition;
                return true;
            }

            validPosition = default;
            return false;
        }

        private Vector2 PredictPlayerPosition(float leadTime)
        {
            Vector2 velocity = playerBody != null ? playerBody.velocity : Vector2.zero;
            Vector2 offset = Vector2.ClampMagnitude(
                velocity * Mathf.Max(0f, leadTime),
                maximumPredictionDistance);
            return (Vector2)player.position + offset;
        }

        private Vector2 GetRandomFallingSpawnPosition(
            TerrainType terrainType,
            Vector2 referencePoint)
        {
            // 2026-07-19：每次攻击只抽一次高度，范围按场地和物体大小计算。
            Rect arena = creationService.ArenaBounds;
            Vector2 extents =
                creationService.GetTerrainWorldSize(terrainType) * 0.5f;

            // 物体中心要留在场内，并且尽量生成在玩家上方。
            float requestedX = Mathf.Clamp(
                referencePoint.x,
                arena.xMin + extents.x,
                arena.xMax - extents.x);
            float minimumY = Mathf.Max(
                arena.yMin + extents.y,
                referencePoint.y + extents.y);
            float maximumY = arena.yMax - extents.y;

            float maximumDistance = creationService.MaximumCreationDistance;
            if (maximumDistance > 0f)
            {
                // Boss 的创建距离是圆形范围，X 越远时可用的 Y 就越少。
                float safeDistance = Mathf.Max(
                    0f,
                    maximumDistance - creationService.GridSize);
                requestedX = Mathf.Clamp(
                    requestedX,
                    bossTransform.position.x - safeDistance,
                    bossTransform.position.x + safeDistance);

                float horizontalDistance =
                    requestedX - bossTransform.position.x;
                float verticalReach = Mathf.Sqrt(Mathf.Max(
                    0f,
                    safeDistance * safeDistance -
                    horizontalDistance * horizontalDistance));
                minimumY = Mathf.Max(
                    minimumY,
                    bossTransform.position.y - verticalReach);
                maximumY = Mathf.Min(
                    maximumY,
                    bossTransform.position.y + verticalReach);
            }

            // 没有完整随机区间时就用最高合法点，后面仍会走放置验证。
            float randomY = maximumY > minimumY
                ? Random.Range(minimumY, maximumY)
                : maximumY;

            return ClampFallingSpawnAtHeight(
                terrainType,
                requestedX,
                randomY);
        }

        private Vector2 ClampFallingSpawnAtHeight(
            TerrainType terrainType,
            float requestedX,
            float spawnY)
        {
            // Y 已经确定后，只限制 X 的追踪范围，不重新抽高度。
            Rect arena = creationService.ArenaBounds;
            Vector2 extents =
                creationService.GetTerrainWorldSize(terrainType) * 0.5f;
            float clampedY = Mathf.Clamp(
                spawnY,
                arena.yMin + extents.y,
                arena.yMax - extents.y);
            float minimumX = arena.xMin + extents.x;
            float maximumX = arena.xMax - extents.x;

            float maximumDistance = creationService.MaximumCreationDistance;
            if (maximumDistance > 0f)
            {
                float safeDistance = Mathf.Max(
                    0f,
                    maximumDistance - creationService.GridSize);
                float verticalDistance =
                    clampedY - bossTransform.position.y;
                float horizontalReach = Mathf.Sqrt(Mathf.Max(
                    0f,
                    safeDistance * safeDistance -
                    verticalDistance * verticalDistance));
                minimumX = Mathf.Max(
                    minimumX,
                    bossTransform.position.x - horizontalReach);
                maximumX = Mathf.Min(
                    maximumX,
                    bossTransform.position.x + horizontalReach);
            }

            return new Vector2(
                Mathf.Clamp(requestedX, minimumX, maximumX),
                clampedY);
        }

        private Vector2 FindLandingPosition(Vector2 spawnPosition)
        {
            Rect arena = creationService.ArenaBounds;
            float rayDistance = Mathf.Max(0.1f, spawnPosition.y - arena.yMin + 2f);
            int hitCount = Physics2D.RaycastNonAlloc(
                spawnPosition,
                Vector2.down,
                raycastBuffer,
                rayDistance,
                landingSurfaceMask);

            float nearestDistance = float.MaxValue;
            Vector2 landing = new Vector2(spawnPosition.x, arena.yMin);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = raycastBuffer[i];
                Collider2D collider = hit.collider;
                if (collider == null || !collider.enabled || collider.isTrigger ||
                    IsRelatedTransform(collider.transform, player) ||
                    IsRelatedTransform(collider.transform, bossTransform))
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    landing = hit.point;
                }
            }

            return landing;
        }

        private void ResolveLockedFallingAttack(
            TerrainType terrainType,
            Vector2 spawnPosition)
        {
            // 2026-07-19：最终位置只查这一次，预警期间不重复查。
            Vector2 terrainSize =
                creationService.GetTerrainWorldSize(terrainType);
            Vector2 overlapSize = new Vector2(
                Mathf.Max(0.01f, terrainSize.x * 0.98f),
                Mathf.Max(0.01f, terrainSize.y * 0.98f));
            Collider2D[] blockers = Physics2D.OverlapBoxAll(
                spawnPosition,
                overlapSize,
                0f);

            bool hasTerrainBlocker = false;
            Collider2D hardBlocker = null;

            for (int i = 0; i < blockers.Length; i++)
            {
                Collider2D blocker = blockers[i];
                if (blocker == null ||
                    !blocker.enabled ||
                    blocker.isTrigger)
                {
                    continue;
                }

                bool isTelegraph =
                    telegraph != null &&
                    telegraph.OwnsTransform(blocker.transform);
                TerrainEntity terrain = GetTerrainFromCollider(blocker);
                PermanentTerrainMarker permanentTerrain =
                    blocker.GetComponentInParent<PermanentTerrainMarker>();

                // 每个实际挡住的位置都记下来，测试时能直接看到是谁。
                LogFinalSpawnBlocker(
                    blocker,
                    isTelegraph,
                    terrain,
                    permanentTerrain);

                if (isTelegraph)
                {
                    // 预警自己以后即使加了 Collider，也不能挡住正式实体。
                    continue;
                }

                if (permanentTerrain != null || terrain == null)
                {
                    // 没有 TerrainEntity 的普通场景碰撞体也按永久物体处理。
                    if (hardBlocker == null)
                    {
                        hardBlocker = blocker;
                    }

                    continue;
                }

                hasTerrainBlocker = true;
            }

            if (hardBlocker != null)
            {
                Debug.LogWarning(
                    $"Boss attack cancelled at {spawnPosition}: " +
                    $"'{hardBlocker.name}' on layer " +
                    $"{GetLayerLabel(hardBlocker.gameObject.layer)} " +
                    "is a permanent or non-terrain blocker.",
                    hardBlocker);
                return;
            }

            // 石墙和石锥保留原碰撞行为，让可破坏掩体正常挡住它们。
            bool created =
                creationService.TryCreateTerrainAtResolvedPosition(
                    terrainType,
                    TerrainOwner.Boss,
                    bossTransform,
                    spawnPosition);

            if (!created)
            {
                Debug.LogWarning(
                    $"Boss attack at {spawnPosition} was settled " +
                    "without creating terrain because the locked " +
                    "position no longer passed the non-overlap rules.",
                    this);
            }
            else if (hasTerrainBlocker)
            {
                Debug.Log(
                    $"Boss {terrainType} activated against the " +
                    "blocking TerrainEntity at the locked position.",
                    this);
            }
        }

        private static TerrainEntity GetTerrainFromCollider(
            Collider2D collider)
        {
            // 断裂石墙已经离开父节点，需要先从分段组件找回原地形。
            TerrainSegment segment =
                collider.GetComponentInParent<TerrainSegment>();
            return segment != null
                ? segment.ParentTerrain
                : collider.GetComponentInParent<TerrainEntity>();
        }

        private void LogFinalSpawnBlocker(
            Collider2D blocker,
            bool isTelegraph,
            TerrainEntity terrain,
            PermanentTerrainMarker permanentTerrain)
        {
            Transform root = blocker.transform.root;
            Debug.Log(
                "Boss final spawn blocker | " +
                $"Collider: {blocker.name} | " +
                $"Layer: {GetLayerLabel(blocker.gameObject.layer)} | " +
                $"Tag: {blocker.tag} | " +
                $"Root: {(root != null ? root.name : "<none>")} | " +
                $"TelegraphSelf: {isTelegraph} | " +
                $"TerrainEntity: {terrain != null} | " +
                $"PermanentTerrainMarker: {permanentTerrain != null}",
                blocker);
        }

        private static string GetLayerLabel(int layer)
        {
            string layerName = LayerMask.LayerToName(layer);
            return string.IsNullOrEmpty(layerName)
                ? layer.ToString()
                : $"{layerName} ({layer})";
        }

        private Vector2 ClampToArena(TerrainType terrainType, Vector2 position)
        {
            Rect arena = creationService.ArenaBounds;
            Vector2 size = creationService.GetTerrainWorldSize(terrainType);
            Vector2 extents = size * 0.5f;
            position.x = Mathf.Clamp(position.x, arena.xMin + extents.x, arena.xMax - extents.x);
            position.y = Mathf.Clamp(position.y, arena.yMin + extents.y, arena.yMax - extents.y);

            float maximumDistance = creationService.MaximumCreationDistance;
            if (maximumDistance > 0f)
            {
                float safeDistance = Mathf.Max(0f, maximumDistance - creationService.GridSize);
                Vector2 fromBoss = position - (Vector2)bossTransform.position;
                position = (Vector2)bossTransform.position + Vector2.ClampMagnitude(fromBoss, safeDistance);
            }

            return position;
        }

        private bool IsValidCollapseTarget(TerrainEntity terrain)
        {
            return terrain != null && terrain.IsAlive && !terrain.IsBeingDestroyed &&
                   Vector2.Distance(bossTransform.position, terrain.transform.position) <= destructionRange;
        }

        private bool IsStillRegistered(TerrainEntity target)
        {
            if (target == null || registry == null)
            {
                return false;
            }

            registry.CopyActiveTerrain(terrainBuffer);
            for (int i = 0; i < terrainBuffer.Count; i++)
            {
                if (terrainBuffer[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 GetCandidateOffset(int index, float grid)
        {
            switch (index)
            {
                case 1: return Vector2.left * grid;
                case 2: return Vector2.right * grid;
                case 3: return Vector2.left * grid * 2f;
                case 4: return Vector2.right * grid * 2f;
                case 5: return Vector2.up * grid;
                case 6: return Vector2.down * grid;
                case 7: return new Vector2(-grid, grid);
                case 8: return new Vector2(grid, grid);
                default: return Vector2.zero;
            }
        }

        private static bool IsRelatedTransform(Transform candidate, Transform reference)
        {
            return candidate != null && reference != null &&
                   (candidate == reference || candidate.IsChildOf(reference) || reference.IsChildOf(candidate));
        }

        private static TerrainType GetTerrainType(BossAttackType attackType)
        {
            switch (attackType)
            {
                case BossAttackType.FallingStoneWall:
                    return TerrainType.FallingStoneWall;
                case BossAttackType.FallingStoneSpike:
                    return TerrainType.FallingStoneSpike;
                default:
                    return TerrainType.FloatingPlatform;
            }
        }
    }
}
