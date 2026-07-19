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
        [SerializeField, Min(0f)] private float wallSpawnHeight = 10f;
        [SerializeField, Min(0f)] private float spikeSpawnHeight = 11f;
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
                    requestedPosition = referencePoint + Vector2.up * wallSpawnHeight;
                    telegraphDuration = wallTelegraphDuration;
                    break;

                case BossAttackType.FallingStoneSpike:
                    terrainType = TerrainType.FallingStoneSpike;
                    referencePoint = PredictPlayerPosition(spikePredictionTime);
                    requestedPosition = referencePoint + Vector2.up * spikeSpawnHeight;
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
            float spawnHeight;

            switch (plan.AttackType)
            {
                case BossAttackType.FallingStoneWall:
                    terrainType = TerrainType.FallingStoneWall;
                    predictionTime = wallPredictionTime;
                    spawnHeight = wallSpawnHeight;
                    break;

                case BossAttackType.FallingStoneSpike:
                    terrainType = TerrainType.FallingStoneSpike;
                    predictionTime = spikePredictionTime;
                    spawnHeight = spikeSpawnHeight;
                    break;

                case BossAttackType.TerrainCollapse:
                    return plan.CollapseTarget != null && !plan.CollapseTarget.IsBeingDestroyed &&
                           IsStillRegistered(plan.CollapseTarget);

                default:
                    return true;
            }

            Vector2 referencePoint = PredictPlayerPosition(predictionTime);
            Vector2 requestedPosition = ClampToArena(
                terrainType,
                referencePoint + Vector2.up * spawnHeight);

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

            return creationService.TryCreateTerrain(
                GetTerrainType(plan.AttackType),
                TerrainOwner.Boss,
                bossTransform,
                plan.SpawnPosition);
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
