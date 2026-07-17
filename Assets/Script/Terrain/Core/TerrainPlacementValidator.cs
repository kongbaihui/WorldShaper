using System.Collections.Generic;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class TerrainPlacementValidator
    {
        private const float GapTolerance = 0.001f;

        private readonly List<TerrainEntity>
            _terrainBuffer =
                new List<TerrainEntity>(16);

        public TerrainPlacementResult Validate(
            TerrainDefinition definition,
            Transform creator,
            Vector2 snappedPosition,
            TerrainRegistry registry,
            Rect arenaBounds,
            Camera arenaCamera,
            Transform playerCreator,
            Transform bossDummy,
            float minimumFloatingPlatformGap,
            float maximumCreationDistance,
            bool cooldownReady)
        {
            if (creator == null)
            {
                return TerrainPlacementResult.Invalid(
                    "Creator missing",
                    snappedPosition);
            }

            if (!IsWithinCreationDistance(
                    creator,
                    snappedPosition,
                    maximumCreationDistance))
            {
                return TerrainPlacementResult.Invalid(
                    "Too far from creator",
                    snappedPosition);
            }

            if (arenaCamera == null ||
                !ContainsBounds(
                    arenaBounds,
                    snappedPosition,
                    definition.Size))
            {
                return TerrainPlacementResult.Invalid(
                    "Outside arena",
                    snappedPosition);
            }

            if (registry == null)
            {
                return TerrainPlacementResult.Invalid(
                    "Terrain registry unavailable",
                    snappedPosition);
            }

            /*
             * 使用地形实际 Collider 尺寸检测。
             *
             * 0.98f 是为了允许两个地形的边缘刚好接触，
             * 而不会因为浮点误差被判断为重叠。
             */
            Vector2 overlapSize =
                new Vector2(
                    Mathf.Max(
                        0.01f,
                        definition.Size.x * 0.98f),

                    Mathf.Max(
                        0.01f,
                        definition.Size.y * 0.98f));

            Collider2D[] overlaps =
                Physics2D.OverlapBoxAll(
                    snappedPosition,
                    overlapSize,
                    0f);

            for (int i = 0;
                 i < overlaps.Length;
                 i++)
            {
                Collider2D overlap =
                    overlaps[i];

                if (overlap == null ||
                    !overlap.enabled ||
                    overlap.isTrigger)
                {
                    continue;
                }

                /*
                 * 如果检测到的是创造者自己，
                 * 明确禁止创建在角色身体中。
                 */
                if (IsRelatedTransform(
                        overlap.transform,
                        creator))
                {
                    return TerrainPlacementResult.Invalid(
                        "Cannot create inside creator",
                        snappedPosition);
                }

                PrototypeDamageable actor =
                    overlap.GetComponentInParent<
                        PrototypeDamageable>();

                if (actor != null)
                {
                    if (actor.Owner ==
                            TerrainOwner.Player ||
                        IsRelatedTransform(
                            overlap.transform,
                            playerCreator))
                    {
                        return TerrainPlacementResult
                            .Invalid(
                                "Cannot create inside Player",
                                snappedPosition);
                    }

                    if (actor.Owner ==
                            TerrainOwner.Boss ||
                        IsRelatedTransform(
                            overlap.transform,
                            bossDummy))
                    {
                        return TerrainPlacementResult
                            .Invalid(
                                "Cannot create inside Boss",
                                snappedPosition);
                    }
                }

                if (IsRelatedTransform(
                        overlap.transform,
                        playerCreator))
                {
                    return TerrainPlacementResult.Invalid(
                        "Cannot create inside Player",
                        snappedPosition);
                }

                if (IsRelatedTransform(
                        overlap.transform,
                        bossDummy))
                {
                    return TerrainPlacementResult.Invalid(
                        "Cannot create inside Boss",
                        snappedPosition);
                }

                PermanentTerrainMarker
                    permanentTerrain =
                        overlap.GetComponentInParent<
                            PermanentTerrainMarker>();

                if (permanentTerrain != null &&
                    permanentTerrain.IsArenaBoundary)
                {
                    return TerrainPlacementResult.Invalid(
                        "Outside arena",
                        snappedPosition);
                }

                TerrainEntity existingTerrain =
                    overlap.GetComponentInParent<
                        TerrainEntity>();

                if (permanentTerrain != null ||
                    existingTerrain != null)
                {
                    return TerrainPlacementResult.Invalid(
                        "Spawn area occupied",
                        snappedPosition);
                }

                /*
                 * 其他普通 Collider 也不能被地形覆盖。
                 */
                return TerrainPlacementResult.Invalid(
                    "Spawn area occupied",
                    snappedPosition);
            }

            /*
             * 悬浮平台上下排列时，
             * 至少留出一个玩家高度。
             *
             * 左右连接的平台不受这个限制，
             * 因此可以用来搭桥。
             */
            if (definition.Type ==
                    TerrainType.FloatingPlatform &&
                !HasRequiredFloatingPlatformGap(
                    snappedPosition,
                    definition.Size,
                    minimumFloatingPlatformGap,
                    registry))
            {
                return TerrainPlacementResult.Invalid(
                    "Platform gap too small",
                    snappedPosition);
            }

            if (!cooldownReady)
            {
                return TerrainPlacementResult.Invalid(
                    "Creation cooldown",
                    snappedPosition);
            }

            return TerrainPlacementResult.Valid(
                snappedPosition);
        }

        private bool HasRequiredFloatingPlatformGap(
            Vector2 center,
            Vector2 size,
            float minimumGap,
            TerrainRegistry registry)
        {
            if (minimumGap <= GapTolerance)
            {
                return true;
            }

            float newLeft =
                center.x - size.x * 0.5f;

            float newRight =
                center.x + size.x * 0.5f;

            float newBottom =
                center.y - size.y * 0.5f;

            float newTop =
                center.y + size.y * 0.5f;

            registry.CopyActiveTerrain(
                _terrainBuffer);

            for (int i = 0;
                 i < _terrainBuffer.Count;
                 i++)
            {
                TerrainEntity terrain =
                    _terrainBuffer[i];

                if (terrain == null ||
                    terrain.TerrainType !=
                        TerrainType.FloatingPlatform)
                {
                    continue;
                }

                Collider2D platformCollider =
                    terrain.PrimaryCollider != null
                        ? terrain.PrimaryCollider
                        : terrain.GetComponent<
                            Collider2D>();

                if (platformCollider == null ||
                    !platformCollider.enabled)
                {
                    continue;
                }

                Bounds existingBounds =
                    platformCollider.bounds;

                /*
                 * 只有两个平台在水平方向有重叠，
                 * 才需要检查上下空隙。
                 *
                 * 左右排列的平台允许互相连接。
                 */
                bool horizontalRangesOverlap =
                    newLeft <
                        existingBounds.max.x -
                        GapTolerance &&
                    newRight >
                        existingBounds.min.x +
                        GapTolerance;

                if (!horizontalRangesOverlap)
                {
                    continue;
                }

                float verticalGap;

                if (newTop <=
                    existingBounds.min.y)
                {
                    verticalGap =
                        existingBounds.min.y -
                        newTop;
                }
                else if (newBottom >=
                         existingBounds.max.y)
                {
                    verticalGap =
                        newBottom -
                        existingBounds.max.y;
                }
                else
                {
                    /*
                     * 两个平台在竖直方向发生重叠。
                     */
                    verticalGap = 0f;
                }

                if (verticalGap +
                    GapTolerance <
                    minimumGap)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWithinCreationDistance(
            Transform creator,
            Vector2 targetPosition,
            float maximumDistance)
        {
            /*
             * 0 表示不限制创造距离。
             */
            if (maximumDistance <= 0f)
            {
                return true;
            }

            Vector2 creatorPosition =
                creator.position;

            float squareDistance =
                (targetPosition -
                 creatorPosition).sqrMagnitude;

            float squareMaximumDistance =
                maximumDistance *
                maximumDistance;

            return squareDistance <=
                   squareMaximumDistance;
        }

        private static bool ContainsBounds(
            Rect arenaBounds,
            Vector2 center,
            Vector2 size)
        {
            Vector2 extents =
                size * 0.5f;

            return
                center.x - extents.x >=
                    arenaBounds.xMin &&
                center.x + extents.x <=
                    arenaBounds.xMax &&
                center.y - extents.y >=
                    arenaBounds.yMin &&
                center.y + extents.y <=
                    arenaBounds.yMax;
        }

        private static bool IsRelatedTransform(
            Transform candidate,
            Transform reference)
        {
            if (candidate == null ||
                reference == null)
            {
                return false;
            }

            return
                candidate == reference ||
                candidate.IsChildOf(reference) ||
                reference.IsChildOf(candidate);
        }
    }
}