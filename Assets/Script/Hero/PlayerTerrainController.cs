using UnityEngine;
using UnityEngine.InputSystem;
using Challenge2.TerrainPrototype;

public sealed class PlayerTerrainController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private Transform player;

    [SerializeField]
    private TerrainCreationService
        creationService;

    [SerializeField]
    private TerrainDamageService
        damageService;

    [SerializeField]
    private SpriteRenderer previewRenderer;

    [SerializeField]
    private CreManager createManager;

    [Header("Preview Colors")]
    [SerializeField]
    private Color validColor =
        new Color(
            0.2f,
            1f,
            0.48f,
            0.6f);

    [SerializeField]
    private Color invalidColor =
        new Color(
            1f,
            0.2f,
            0.2f,
            0.6f);

    [SerializeField]
    private Color noChargeColor =
        new Color(
            1f,
            0.55f,
            0f,
            0.6f);

    public bool IsBuildMode
    {
        get;
        private set;
    }

    public TerrainDamageService DamageService =>
        damageService;

    public TerrainType SelectedType
    {
        get;
        private set;
    } = TerrainType.FloatingPlatform;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (player == null)
        {
            player = transform;
        }

        SetBuildMode(false);
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        Mouse mouse =
            Mouse.current;

        if (keyboard == null ||
            mouse == null)
        {
            return;
        }

        /*
         * L 只切换创造模式。
         * 不会消耗创造次数。
         */
        if (keyboard.lKey
            .wasPressedThisFrame)
        {
            SetBuildMode(
                !IsBuildMode);
        }

        if (!IsBuildMode)
        {
            return;
        }

        if (worldCamera == null ||
            creationService == null)
        {
            return;
        }

        ReadTerrainSelection(
            keyboard);

        Vector2 mouseWorldPosition =
            GetMouseWorldPosition(mouse);

        Vector2 snappedPosition =
            creationService.SnapToGrid(
                mouseWorldPosition);

        TerrainPlacementResult placement =
            creationService.EvaluatePlacement(
                SelectedType,
                TerrainOwner.Player,
                player,
                snappedPosition);

        UpdatePreview(placement);

        /*
         * 左键创建地形。
         */
        if (mouse.leftButton
            .wasPressedThisFrame)
        {
            TryCreateTerrain(
                snappedPosition);
        }

        /*
         * 右键摧毁地形。
         */
        if (mouse.rightButton
            .wasPressedThisFrame)
        {
            TryDamageTerrain(
                mouseWorldPosition);
        }
    }

    private void SetBuildMode(
        bool enabled)
    {
        IsBuildMode = enabled;

        if (previewRenderer != null)
        {
            previewRenderer.enabled =
                enabled;
        }

        if (enabled)
        {
            RefreshPreviewSprite();
        }
    }

    private void ReadTerrainSelection(
        Keyboard keyboard)
    {
        if (keyboard.digit1Key
            .wasPressedThisFrame)
        {
            SelectTerrain(
                TerrainType
                    .FloatingPlatform);
        }
        else if (keyboard.digit2Key
                 .wasPressedThisFrame)
        {
            SelectTerrain(
                TerrainType
                    .FallingStoneWall);
        }
        else if (keyboard.digit3Key
                 .wasPressedThisFrame)
        {
            SelectTerrain(
                TerrainType
                    .FallingStoneSpike);
        }
    }

    private void SelectTerrain(
        TerrainType type)
    {
        SelectedType = type;

        RefreshPreviewSprite();
    }

    private void TryCreateTerrain(
        Vector2 snappedPosition)
    {
        if (creationService == null)
        {
            Debug.LogWarning(
                "PlayerTerrainController: " +
                "Creation Service 未设置。");

            return;
        }

        if (createManager == null)
        {
            Debug.LogWarning(
                "PlayerTerrainController: " +
                "CreManager 未设置。");

            return;
        }

        /*
         * 必须至少有一整格能量。
         */
        if (!createManager.CanCreate())
        {
            createManager
                .ShowNotEnoughFeedback();

            return;
        }

        bool created =
            creationService.TryCreateTerrain(
                SelectedType,
                TerrainOwner.Player,
                player,
                snappedPosition);

        /*
         * 只有真正创建成功，
         * 才消耗一次。
         */
        if (created)
        {
            createManager
                .ConsumeCreateCharge();
        }
    }

    private void TryDamageTerrain(
        Vector2 mouseWorldPosition)
    {
        if (damageService == null)
        {
            Debug.LogWarning(
                "PlayerTerrainController: " +
                "Damage Service 未设置。");

            return;
        }

        damageService.TryDamageTerrain(
            TerrainOwner.Player,
            player,
            mouseWorldPosition);
    }

    private Vector2 GetMouseWorldPosition(
        Mouse mouse)
    {
        Vector3 screenPosition =
            mouse.position.ReadValue();

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                screenPosition);

        return new Vector2(
            worldPosition.x,
            worldPosition.y);
    }

    private void UpdatePreview(
        TerrainPlacementResult placement)
    {
        if (previewRenderer == null)
        {
            return;
        }

        previewRenderer.transform.position =
            new Vector3(
                placement.SnappedPosition.x,
                placement.SnappedPosition.y,
                -0.5f);

        /*
         * 没有创造次数时显示橙色。
         */
        if (createManager != null &&
            !createManager.CanCreate())
        {
            previewRenderer.color =
                noChargeColor;

            return;
        }

        /*
         * 绿色：位置合法。
         * 红色：位置非法。
         */
        previewRenderer.color =
            placement.IsValid
                ? validColor
                : invalidColor;
    }

    private void RefreshPreviewSprite()
    {
        if (previewRenderer == null ||
            creationService == null)
        {
            return;
        }

        previewRenderer.sprite =
            creationService.GetPreviewSprite(
                SelectedType);

        /*
         * 使用 Prefab 实际 Collider 尺寸
         * 计算预览 Scale。
         *
         * 修改 Prefab Scale 后，
         * 预览会同步变化。
         */
        previewRenderer.transform.localScale =
            creationService.GetPreviewScale(
                SelectedType);
    }
}
