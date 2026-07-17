using System.Collections.Generic;
using Challenge2.TerrainPrototype;
using UnityEngine;

namespace FinalGame.Boss
{
    [DisallowMultipleComponent]
    public sealed class BossAttackTelegraph : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private TerrainCreationService creationService;
        [SerializeField] private Transform telegraphRoot;

        [Header("Presentation")]
        [SerializeField] private Color platformColor = new Color(0.15f, 0.95f, 1f, 0.55f);
        [SerializeField] private Color wallColor = new Color(1f, 0.45f, 0.1f, 0.62f);
        [SerializeField] private Color spikeColor = new Color(1f, 0.05f, 0.05f, 0.72f);
        [SerializeField] private Color collapseColor = new Color(1f, 0.05f, 0.55f, 0.68f);
        [SerializeField, Min(0.05f)] private float warningLineWidth = 0.35f;
        [SerializeField, Min(0f)] private float landingAreaPadding = 2f;
        [SerializeField] private int sortingOrder = 40;

        private readonly List<SpriteRenderer> markers = new List<SpriteRenderer>(4);
        private readonly List<Color> markerColors = new List<Color>(4);
        private Texture2D markerTexture;
        private Sprite markerSprite;
        private SpriteRenderer spawnMarker;
        private SpriteRenderer pathMarker;
        private SpriteRenderer landingMarker;
        private SpriteRenderer collapseMarker;
        private TerrainEntity collapseTarget;

        private void Awake()
        {
            EnsureMarkerSprite();
        }

        private void Update()
        {
            float pulse = 0.5f + Mathf.PingPong(Time.time * 1.6f, 0.5f);
            for (int i = 0; i < markers.Count; i++)
            {
                SpriteRenderer marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                Color color = markerColors[i];
                color.a *= pulse;
                marker.color = color;
            }

            if (collapseMarker != null && collapseTarget != null && !collapseTarget.IsBeingDestroyed)
            {
                UpdateCollapseMarker(collapseTarget);
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
            if (markerSprite != null)
            {
                Destroy(markerSprite);
            }

            if (markerTexture != null)
            {
                Destroy(markerTexture);
            }
        }

        public void Show(BossAttackPlan plan)
        {
            Clear();
            if (plan == null || creationService == null)
            {
                return;
            }

            switch (plan.AttackType)
            {
                case BossAttackType.FloatingPlatformShield:
                    spawnMarker = CreateTerrainPreview(
                        "Telegraph_FloatingPlatformPreview",
                        TerrainType.FloatingPlatform,
                        plan.SpawnPosition,
                        platformColor);
                    break;

                case BossAttackType.FallingStoneWall:
                    spawnMarker = CreateTerrainPreview(
                        "Telegraph_WallSpawn",
                        TerrainType.FallingStoneWall,
                        plan.SpawnPosition,
                        wallColor);
                    CreateFallingPath(plan, TerrainType.FallingStoneWall, wallColor);
                    break;

                case BossAttackType.FallingStoneSpike:
                    spawnMarker = CreateTerrainPreview(
                        "Telegraph_SpikePreview",
                        TerrainType.FallingStoneSpike,
                        plan.SpawnPosition,
                        spikeColor);
                    CreateFallingPath(plan, TerrainType.FallingStoneSpike, spikeColor);
                    break;

                case BossAttackType.TerrainCollapse:
                    collapseTarget = plan.CollapseTarget;
                    if (collapseTarget != null)
                    {
                        collapseMarker = CreateMarker(
                            "Telegraph_CollapseTarget",
                            collapseTarget.VisualSprite != null ? collapseTarget.VisualSprite : markerSprite,
                            collapseTarget.transform.position,
                            GetCollapseWorldSize(collapseTarget),
                            collapseColor);
                        UpdateCollapseMarker(collapseTarget);
                    }
                    break;
            }
        }

        public void UpdateTracking(BossAttackPlan plan)
        {
            if (plan == null || creationService == null)
            {
                return;
            }

            if (plan.AttackType == BossAttackType.FallingStoneWall)
            {
                SetMarker(spawnMarker, plan.SpawnPosition,
                    creationService.GetPreviewScale(TerrainType.FallingStoneWall));
                UpdateFallingPath(plan, TerrainType.FallingStoneWall);
            }
            else if (plan.AttackType == BossAttackType.FallingStoneSpike)
            {
                SetMarker(spawnMarker, plan.SpawnPosition,
                    creationService.GetPreviewScale(TerrainType.FallingStoneSpike));
                UpdateFallingPath(plan, TerrainType.FallingStoneSpike);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] != null)
                {
                    Destroy(markers[i].gameObject);
                }
            }

            markers.Clear();
            markerColors.Clear();
            spawnMarker = null;
            pathMarker = null;
            landingMarker = null;
            collapseMarker = null;
            collapseTarget = null;
        }

        private void CreateFallingPath(BossAttackPlan plan, TerrainType terrainType, Color color)
        {
            EnsureMarkerSprite();
            float height = Mathf.Max(0.1f, plan.SpawnPosition.y - plan.LandingPosition.y);
            pathMarker = CreateMarker(
                "Telegraph_DropPath",
                markerSprite,
                new Vector2(plan.SpawnPosition.x, plan.LandingPosition.y + height * 0.5f),
                new Vector2(warningLineWidth, height),
                color);

            Vector2 terrainSize = creationService.GetTerrainWorldSize(terrainType);
            landingMarker = CreateMarker(
                "Telegraph_LandingArea",
                markerSprite,
                plan.LandingPosition,
                new Vector2(terrainSize.x + landingAreaPadding, Mathf.Max(0.2f, terrainSize.y * 0.12f)),
                color);
        }

        private void UpdateFallingPath(BossAttackPlan plan, TerrainType terrainType)
        {
            float height = Mathf.Max(0.1f, plan.SpawnPosition.y - plan.LandingPosition.y);
            SetMarker(pathMarker,
                new Vector2(plan.SpawnPosition.x, plan.LandingPosition.y + height * 0.5f),
                new Vector3(warningLineWidth, height, 1f));

            Vector2 terrainSize = creationService.GetTerrainWorldSize(terrainType);
            SetMarker(landingMarker,
                plan.LandingPosition,
                new Vector3(terrainSize.x + landingAreaPadding, Mathf.Max(0.2f, terrainSize.y * 0.12f), 1f));
        }

        private SpriteRenderer CreateTerrainPreview(
            string markerName,
            TerrainType terrainType,
            Vector2 position,
            Color color)
        {
            Sprite previewSprite = creationService.GetPreviewSprite(terrainType);
            if (previewSprite == null)
            {
                EnsureMarkerSprite();
                previewSprite = markerSprite;
            }

            return CreateMarker(
                markerName,
                previewSprite,
                position,
                creationService.GetPreviewScale(terrainType),
                color);
        }

        private SpriteRenderer CreateMarker(
            string markerName,
            Sprite sprite,
            Vector2 position,
            Vector2 worldScale,
            Color color)
        {
            return CreateMarker(markerName, sprite, position, new Vector3(worldScale.x, worldScale.y, 1f), color);
        }

        private SpriteRenderer CreateMarker(
            string markerName,
            Sprite sprite,
            Vector2 position,
            Vector3 worldScale,
            Color color)
        {
            GameObject markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(telegraphRoot, true);
            SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            markers.Add(renderer);
            markerColors.Add(color);
            SetMarker(renderer, position, worldScale);
            return renderer;
        }

        private void UpdateCollapseMarker(TerrainEntity target)
        {
            if (collapseMarker == null || target == null)
            {
                return;
            }

            collapseMarker.transform.rotation = target.transform.rotation;
            SetMarker(collapseMarker, target.transform.position, GetCollapseWorldSize(target));
        }

        private static Vector2 GetCollapseWorldSize(TerrainEntity target)
        {
            if (target.PrimaryCollider != null)
            {
                return target.PrimaryCollider.bounds.size + new Vector3(0.5f, 0.5f, 0f);
            }

            return new Vector2(Mathf.Abs(target.transform.lossyScale.x), Mathf.Abs(target.transform.lossyScale.y));
        }

        private static void SetMarker(SpriteRenderer marker, Vector2 position, Vector3 worldScale)
        {
            if (marker == null)
            {
                return;
            }

            marker.transform.position = position;
            Transform parent = marker.transform.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            marker.transform.localScale = new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private void EnsureMarkerSprite()
        {
            if (markerSprite != null)
            {
                return;
            }

            markerTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Boss Telegraph Runtime Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            markerTexture.SetPixel(0, 0, Color.white);
            markerTexture.Apply();
            markerSprite = Sprite.Create(markerTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            markerSprite.name = "Boss Telegraph Runtime Sprite";
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }
    }
}
