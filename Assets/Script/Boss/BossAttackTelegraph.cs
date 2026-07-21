using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    /// <summary>
    /// 课程作业版攻击预警：只显示生成区域、落点和范围圆环。
    /// 复杂的分段染色、运行时纹理和命中特效交给美术资源扩展，而不放在 Boss 逻辑里。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossAttackTelegraph : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private TerrainCreationService creationService;
        [SerializeField] private Transform telegraphRoot;

        [Header("Presentation")]
        [SerializeField] private Sprite markerSprite;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color platformColor = new Color(0.15f, 0.95f, 1f, 0.55f);
        [SerializeField] private Color wallColor = new Color(1f, 0.45f, 0.1f, 0.62f);
        [SerializeField] private Color spikeColor = new Color(1f, 0.05f, 0.05f, 0.72f);
        [SerializeField] private Color lightWaveColor = new Color(0.25f, 0.9f, 1f, 0.82f);
        [SerializeField] private Color collapseColor = new Color(1f, 0.05f, 0.55f, 0.68f);
        [SerializeField, Min(0.05f)] private float lineWidth = 0.3f;
        [SerializeField, Min(0f)] private float landingAreaPadding = 2f;
        [SerializeField] private int sortingOrder = 40;

        private readonly List<GameObject> activeMarkers = new List<GameObject>(3);
        private readonly List<SpriteRenderer> collapseRenderers =
            new List<SpriteRenderer>(10);
        private readonly List<Color> collapseOriginalColors =
            new List<Color>(10);
        private Material runtimeLineMaterial;
        private TerrainEntity trackedCollapseTarget;
        private GameObject trackedCollapseMarker;

        private void Awake()
        {
            if (telegraphRoot == null)
            {
                telegraphRoot = transform;
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Update()
        {
            if (trackedCollapseTarget == null || trackedCollapseMarker == null ||
                trackedCollapseTarget.IsBeingDestroyed)
            {
                return;
            }

            GetTargetBounds(trackedCollapseTarget, out Vector2 center, out Vector2 size);
            trackedCollapseMarker.transform.position = center;
            SetMarkerWorldSize(trackedCollapseMarker, size);
        }

        private void OnDestroy()
        {
            Clear();
            if (runtimeLineMaterial != null)
            {
                Destroy(runtimeLineMaterial);
            }
        }

        public void Show(BossAttackPlan plan)
        {
            Clear();
            if (plan == null)
            {
                return;
            }

            switch (plan.AttackType)
            {
                case BossAttackType.FloatingPlatformShield:
                    CreateTerrainMarker(TerrainType.FloatingPlatform, plan.SpawnPosition, platformColor);
                    break;

                case BossAttackType.FallingStoneWall:
                    CreateLandingMarker(TerrainType.FallingStoneWall, plan.LandingPosition, wallColor);
                    break;

                case BossAttackType.FallingStoneSpike:
                    CreateLandingMarker(TerrainType.FallingStoneSpike, plan.LandingPosition, spikeColor);
                    break;

                case BossAttackType.CloseRangeLightWave:
                    CreateRing(plan.SpawnPosition, plan.AttackRadius, lightWaveColor);
                    break;

                case BossAttackType.TerrainCollapse:
                    if (plan.CollapseTarget != null)
                    {
                        CreateCollapseMarker(plan.CollapseTarget);
                    }
                    break;
            }
        }

        // 保留接口，避免旧场景或调用方失效。课程版攻击位置在预警开始时锁定。
        public void UpdateTracking(BossAttackPlan plan)
        {
        }

        public void Clear()
        {
            for (int i = 0; i < collapseRenderers.Count; i++)
            {
                if (collapseRenderers[i] != null)
                {
                    collapseRenderers[i].color = collapseOriginalColors[i];
                }
            }

            collapseRenderers.Clear();
            collapseOriginalColors.Clear();

            for (int i = 0; i < activeMarkers.Count; i++)
            {
                if (activeMarkers[i] != null)
                {
                    Destroy(activeMarkers[i]);
                }
            }

            activeMarkers.Clear();
            trackedCollapseTarget = null;
            trackedCollapseMarker = null;
        }

        // 不再额外生成冲击波命中特效，攻击前的圆环已经表达范围。
        public void PlayShockwaveImpact(Vector2 center, float radius)
        {
        }

        public bool OwnsTransform(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            for (int i = 0; i < activeMarkers.Count; i++)
            {
                GameObject marker = activeMarkers[i];
                if (marker != null &&
                    (candidate == marker.transform || candidate.IsChildOf(marker.transform)))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateTerrainMarker(TerrainType terrainType, Vector2 position, Color color)
        {
            Sprite sprite = creationService != null
                ? creationService.GetPreviewSprite(terrainType)
                : markerSprite;
            Vector2 size = creationService != null
                ? creationService.GetTerrainWorldSize(terrainType)
                : Vector2.one * 2f;
            CreateMarker(position, size, sprite, color);
        }

        private void CreateLandingMarker(TerrainType terrainType, Vector2 position, Color color)
        {
            Sprite sprite = creationService != null
                ? creationService.GetPreviewSprite(terrainType)
                : markerSprite;
            Vector2 terrainSize = creationService != null
                ? creationService.GetTerrainWorldSize(terrainType)
                : Vector2.one * 2f;
            Vector2 size = new Vector2(
                terrainSize.x + landingAreaPadding,
                Mathf.Max(0.4f, terrainSize.y * 0.15f));
            CreateMarker(position, size, sprite, color);
        }

        private void CreateCollapseMarker(TerrainEntity target)
        {
            if (target is FallingStoneWallTerrain wall)
            {
                wall.CopyActiveSegmentRenderers(collapseRenderers);
            }
            else if (target is FloatingPlatformTerrain platform)
            {
                platform.CopyActiveSegmentRenderers(collapseRenderers);
            }

            if (collapseRenderers.Count > 0)
            {
                for (int i = 0; i < collapseRenderers.Count; i++)
                {
                    SpriteRenderer renderer = collapseRenderers[i];
                    collapseOriginalColors.Add(
                        renderer != null ? renderer.color : Color.white);
                    if (renderer != null)
                    {
                        renderer.color = collapseColor;
                    }
                }

                return;
            }

            Sprite sprite = target.VisualSprite != null ? target.VisualSprite : markerSprite;
            GetTargetBounds(target, out Vector2 center, out Vector2 size);
            trackedCollapseTarget = target;
            trackedCollapseMarker = CreateMarker(center, size, sprite, collapseColor);
        }

        private GameObject CreateMarker(Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            GameObject marker = new GameObject("BossAttackWarning");
            marker.transform.SetParent(telegraphRoot != null ? telegraphRoot : transform, true);
            marker.transform.position = position;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            SetMarkerWorldSize(marker, size);
            activeMarkers.Add(marker);
            return marker;
        }

        private static void SetMarkerWorldSize(GameObject marker, Vector2 worldSize)
        {
            SpriteRenderer renderer = marker != null
                ? marker.GetComponent<SpriteRenderer>()
                : null;
            if (renderer == null)
            {
                return;
            }

            Vector2 spriteSize = renderer.sprite != null
                ? renderer.sprite.bounds.size
                : Vector2.one;
            Vector3 parentScale = marker.transform.parent != null
                ? marker.transform.parent.lossyScale
                : Vector3.one;
            float scaleX = worldSize.x /
                           Mathf.Max(0.0001f, spriteSize.x * Mathf.Abs(parentScale.x));
            float scaleY = worldSize.y /
                           Mathf.Max(0.0001f, spriteSize.y * Mathf.Abs(parentScale.y));
            marker.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private static void GetTargetBounds(
            TerrainEntity target,
            out Vector2 center,
            out Vector2 size)
        {
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                center = bounds.center;
                size = bounds.size;
                return;
            }

            Collider2D targetCollider = target.PrimaryCollider;
            if (targetCollider != null)
            {
                center = targetCollider.bounds.center;
                size = targetCollider.bounds.size;
                return;
            }

            center = target.transform.position;
            size = Vector2.one * 2f;
        }

        private void CreateRing(Vector2 center, float radius, Color color)
        {
            GameObject ringObject = new GameObject("BossAttackWarningRing");
            ringObject.transform.SetParent(telegraphRoot != null ? telegraphRoot : transform, true);

            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = GetLineMaterial();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = 40;
            ring.widthMultiplier = lineWidth;
            ring.sortingOrder = sortingOrder;
            ring.startColor = color;
            ring.endColor = color;

            float safeRadius = Mathf.Max(0.1f, radius);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * safeRadius);
            }

            activeMarkers.Add(ringObject);
        }

        private Material GetLineMaterial()
        {
            if (lineMaterial != null)
            {
                return lineMaterial;
            }

            if (runtimeLineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    runtimeLineMaterial = new Material(shader)
                    {
                        name = "Boss Warning Runtime Material"
                    };
                }
            }

            return runtimeLineMaterial;
        }
    }
}
