using System;
using System.Collections.Generic;
using UnityEngine;

namespace Challenge2.TerrainPrototype
{
    public sealed class TerrainCreationService : MonoBehaviour
    {
        [Header("Shared Services")]
        [SerializeField] private TerrainRegistry _registry;
        [SerializeField] private Camera _arenaCamera;

        [Header("Actor References")]
        [SerializeField] private Transform _playerCreator;
        [SerializeField] private Transform _bossDummy;

        [Header("Terrain Prefabs")]
        [SerializeField] private GameObject _floatingPlatformPrefab;
        [SerializeField] private GameObject _fallingStoneWallPrefab;
        [SerializeField] private GameObject _fallingStoneSpikePrefab;

        [Header("Placement Rules")]
        [SerializeField]
        private Vector2 _arenaCenter =
            new Vector2(0f, 1f);

        [SerializeField]
        private Vector2 _arenaSize =
            new Vector2(17f, 9f);

        [SerializeField, Min(0.1f)]
        private float _gridSize = 0.5f;

        [SerializeField, Min(0f)]
        private float _creationCooldown = 0.25f;

        [Tooltip("玩家只能在这个距离内创造地形。设置为0表示不限制距离。")]
        [SerializeField, Min(0f)]
        private float _maximumCreationDistance = 12f;

        [Tooltip("上下排列的悬浮平台之间，额外增加的最小空隙。")]
        [SerializeField, Min(0f)]
        private float _additionalPlatformVerticalGap = 0f;

        private readonly TerrainPlacementValidator
            _placementValidator =
                new TerrainPlacementValidator();

        private readonly float[]
            _nextAllowedCreationTime =
                new float[3];

        private readonly List<TerrainEntity>
            _terrainBuffer =
                new List<TerrainEntity>(16);

        private float _minimumFloatingPlatformGap;
        private int _creationSequence;

        public event Action<TerrainPlacementResult>
            PlacementEvaluated;

        public TerrainPlacementResult
            LastPlacementResult
        { get; private set; }

        public float GridSize => _gridSize;

        public float CreationCooldown =>
            _creationCooldown;

        public float MaximumCreationDistance =>
            _maximumCreationDistance;

        public float MinimumFloatingPlatformGap =>
            _minimumFloatingPlatformGap;

        public TerrainRegistry Registry =>
            _registry;

        public Rect ArenaBounds =>
            new Rect(
                _arenaCenter - _arenaSize * 0.5f,
                _arenaSize);

        private void Awake()
        {
            RefreshPlacementRules();

            LastPlacementResult =
                TerrainPlacementResult.Invalid(
                    "Move cursor into arena",
                    Vector2.zero);
        }

        private void OnValidate()
        {
            _gridSize = Mathf.Max(0.1f, _gridSize);
            _creationCooldown =
                Mathf.Max(0f, _creationCooldown);

            _maximumCreationDistance =
                Mathf.Max(0f, _maximumCreationDistance);

            _additionalPlatformVerticalGap =
                Mathf.Max(
                    0f,
                    _additionalPlatformVerticalGap);
        }

        public void Configure(
            TerrainRegistry registry,
            Camera arenaCamera,
            Transform playerCreator,
            Transform bossDummy,
            GameObject floatingPlatformPrefab,
            GameObject fallingStoneWallPrefab,
            GameObject fallingStoneSpikePrefab,
            Vector2 arenaCenter,
            Vector2 arenaSize,
            float gridSize = 0.5f,
            float creationCooldown = 0.25f)
        {
            _registry = registry;
            _arenaCamera = arenaCamera;
            _playerCreator = playerCreator;
            _bossDummy = bossDummy;

            _floatingPlatformPrefab =
                floatingPlatformPrefab;

            _fallingStoneWallPrefab =
                fallingStoneWallPrefab;

            _fallingStoneSpikePrefab =
                fallingStoneSpikePrefab;

            _arenaCenter = arenaCenter;
            _arenaSize = arenaSize;

            _gridSize =
                Mathf.Max(0.1f, gridSize);

            _creationCooldown =
                Mathf.Max(0f, creationCooldown);

            RefreshPlacementRules();
        }

        public bool TryCreateTerrain(
            TerrainType terrainType,
            TerrainOwner owner,
            Transform creator,
            Vector2 worldPosition)
        {
            TerrainPlacementResult placement =
                EvaluatePlacement(
                    terrainType,
                    owner,
                    creator,
                    worldPosition);

            if (!placement.IsValid)
            {
                return false;
            }

            GameObject prefab =
                GetPrefab(terrainType);

            if (prefab == null)
            {
                LastPlacementResult =
                    TerrainPlacementResult.Invalid(
                        "Terrain prefab unavailable",
                        placement.SnappedPosition);

                PlacementEvaluated?.Invoke(
                    LastPlacementResult);

                return false;
            }

            GameObject terrainObject =
                Instantiate(
                    prefab,
                    placement.SnappedPosition,
                    Quaternion.identity);

            terrainObject.name =
                $"{owner} {terrainType} " +
                $"{_creationSequence++:00}";

            TerrainEntity terrain =
                terrainObject.GetComponent<TerrainEntity>();

            if (terrain == null)
            {
                Destroy(terrainObject);

                LastPlacementResult =
                    TerrainPlacementResult.Invalid(
                        "Terrain prefab is invalid",
                        placement.SnappedPosition);

                PlacementEvaluated?.Invoke(
                    LastPlacementResult);

                return false;
            }

            SpriteRenderer terrainRenderer =
                terrainObject.GetComponent<SpriteRenderer>();

            if (terrainRenderer == null)
            {
                terrainRenderer =
                    terrainObject.GetComponentInChildren<SpriteRenderer>();
            }

            terrain.ConfigurePrefab(
                terrainType,
                terrain.MaximumHealth,
                terrainRenderer);

            terrain.Initialize(
                owner,
                creator,
                _registry);

            _nextAllowedCreationTime[(int)owner] =
                Time.time + _creationCooldown;

            LastPlacementResult =
                TerrainPlacementResult.Valid(
                    placement.SnappedPosition);

            PlacementEvaluated?.Invoke(
                LastPlacementResult);

            return true;
        }

        public TerrainPlacementResult EvaluatePlacement(
            TerrainType terrainType,
            TerrainOwner owner,
            Transform creator,
            Vector2 worldPosition)
        {
            Vector2 snappedPosition =
                SnapToGrid(worldPosition);

            TerrainDefinition definition =
                GetRuntimeDefinition(terrainType);

            bool cooldownReady =
                Time.time >=
                _nextAllowedCreationTime[(int)owner];

            LastPlacementResult =
                _placementValidator.Validate(
                    definition,
                    creator,
                    snappedPosition,
                    _registry,
                    ArenaBounds,
                    _arenaCamera,
                    _playerCreator,
                    _bossDummy,
                    _minimumFloatingPlatformGap,
                    _maximumCreationDistance,
                    cooldownReady);

            PlacementEvaluated?.Invoke(
                LastPlacementResult);

            return LastPlacementResult;
        }

        public Vector2 SnapToGrid(
            Vector2 worldPosition)
        {
            return new Vector2(
                Mathf.Round(
                    worldPosition.x / _gridSize) *
                _gridSize,

                Mathf.Round(
                    worldPosition.y / _gridSize) *
                _gridSize);
        }

        /// <summary>
        /// 返回地形的基础属性。
        /// Size 会使用当前 Prefab 的实际 Collider 世界尺寸。
        /// </summary>
        public TerrainDefinition GetDefinition(
            TerrainType terrainType)
        {
            return GetRuntimeDefinition(terrainType);
        }

        /// <summary>
        /// 获取当前地形 Prefab 的实际碰撞尺寸。
        /// Prefab Scale 改变后，此尺寸会自动改变。
        /// </summary>
        public Vector2 GetTerrainWorldSize(
            TerrainType terrainType)
        {
            GameObject prefab =
                GetPrefab(terrainType);

            if (prefab == null)
            {
                return TerrainDefinitionCatalog
                    .Get(terrainType)
                    .Size;
            }

            BoxCollider2D boxCollider =
                prefab.GetComponent<BoxCollider2D>();

            if (boxCollider == null)
            {
                boxCollider =
                    prefab.GetComponentInChildren<
                        BoxCollider2D>();
            }

            if (boxCollider != null)
            {
                Vector3 colliderScale =
                    boxCollider.transform.lossyScale;

                return new Vector2(
                    boxCollider.size.x *
                    Mathf.Abs(colliderScale.x),

                    boxCollider.size.y *
                    Mathf.Abs(colliderScale.y));
            }

            CapsuleCollider2D capsuleCollider =
                prefab.GetComponent<
                    CapsuleCollider2D>();

            if (capsuleCollider == null)
            {
                capsuleCollider =
                    prefab.GetComponentInChildren<
                        CapsuleCollider2D>();
            }

            if (capsuleCollider != null)
            {
                Vector3 colliderScale =
                    capsuleCollider.transform.lossyScale;

                return new Vector2(
                    capsuleCollider.size.x *
                    Mathf.Abs(colliderScale.x),

                    capsuleCollider.size.y *
                    Mathf.Abs(colliderScale.y));
            }

            SpriteRenderer renderer =
                prefab.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer =
                    prefab.GetComponentInChildren<
                        SpriteRenderer>();
            }

            if (renderer != null &&
                renderer.sprite != null)
            {
                Vector2 spriteSize =
                    renderer.sprite.bounds.size;

                Vector3 rendererScale =
                    renderer.transform.lossyScale;

                return new Vector2(
                    spriteSize.x *
                    Mathf.Abs(rendererScale.x),

                    spriteSize.y *
                    Mathf.Abs(rendererScale.y));
            }

            return TerrainDefinitionCatalog
                .Get(terrainType)
                .Size;
        }

        /// <summary>
        /// 获取预览 Sprite。
        /// </summary>
        public Sprite GetPreviewSprite(
            TerrainType terrainType)
        {
            GameObject prefab =
                GetPrefab(terrainType);

            SpriteRenderer spriteRenderer =
                prefab != null
                    ? prefab.GetComponent<
                        SpriteRenderer>()
                    : null;

            if (spriteRenderer == null &&
                prefab != null)
            {
                spriteRenderer =
                    prefab.GetComponentInChildren<
                        SpriteRenderer>();
            }

            return spriteRenderer != null
                ? spriteRenderer.sprite
                : null;
        }

        /// <summary>
        /// 根据预览图片本身的尺寸，计算正确的预览 Scale。
        /// </summary>
        public Vector3 GetPreviewScale(
            TerrainType terrainType)
        {
            Sprite previewSprite =
                GetPreviewSprite(terrainType);

            Vector2 targetWorldSize =
                GetTerrainWorldSize(terrainType);

            if (previewSprite == null)
            {
                return new Vector3(
                    targetWorldSize.x,
                    targetWorldSize.y,
                    1f);
            }

            Vector2 spriteWorldSize =
                previewSprite.bounds.size;

            float scaleX =
                spriteWorldSize.x > 0.0001f
                    ? targetWorldSize.x /
                      spriteWorldSize.x
                    : 1f;

            float scaleY =
                spriteWorldSize.y > 0.0001f
                    ? targetWorldSize.y /
                      spriteWorldSize.y
                    : 1f;

            return new Vector3(
                scaleX,
                scaleY,
                1f);
        }

        public void DestroyAllTerrainCreatedBy(
            TerrainOwner owner)
        {
            if (_registry == null)
            {
                return;
            }

            _registry.CopyActiveTerrain(
                _terrainBuffer);

            for (int i = 0;
                 i < _terrainBuffer.Count;
                 i++)
            {
                TerrainEntity terrain =
                    _terrainBuffer[i];

                if (terrain != null &&
                    terrain.Owner == owner)
                {
                    terrain.DestroyTerrain(false);
                }
            }
        }

        public void ResetCooldowns()
        {
            for (int i = 0;
                 i < _nextAllowedCreationTime.Length;
                 i++)
            {
                _nextAllowedCreationTime[i] = 0f;
            }
        }

        private TerrainDefinition
            GetRuntimeDefinition(
                TerrainType terrainType)
        {
            TerrainDefinition catalogDefinition =
                TerrainDefinitionCatalog.Get(
                    terrainType);

            Vector2 actualSize =
                GetTerrainWorldSize(terrainType);

            return new TerrainDefinition(
                catalogDefinition.Type,
                actualSize,
                catalogDefinition.MaximumHealth,
                catalogDefinition.GravityScale,
                catalogDefinition.Color);
        }

        private GameObject GetPrefab(
            TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.FloatingPlatform:
                    return _floatingPlatformPrefab;

                case TerrainType.FallingStoneWall:
                    return _fallingStoneWallPrefab;

                case TerrainType.FallingStoneSpike:
                    return _fallingStoneSpikePrefab;

                default:
                    return null;
            }
        }

        private void RefreshPlacementRules()
        {
            float playerHeight =
                ResolvePlayerHeight(
                    _playerCreator);

            _minimumFloatingPlatformGap =
                playerHeight +
                _additionalPlatformVerticalGap;
        }

        private static float ResolvePlayerHeight(
            Transform playerCreator)
        {
            if (playerCreator == null)
            {
                return 0f;
            }

            Collider2D playerCollider =
                playerCreator.GetComponentInChildren<
                    Collider2D>();

            if (playerCollider != null)
            {
                return Mathf.Max(
                    0f,
                    playerCollider.bounds.size.y);
            }

            SpriteRenderer playerRenderer =
                playerCreator.GetComponentInChildren<
                    SpriteRenderer>();

            if (playerRenderer != null)
            {
                return Mathf.Max(
                    0f,
                    playerRenderer.bounds.size.y);
            }

            return Mathf.Abs(
                playerCreator.lossyScale.y);
        }
    }
}
