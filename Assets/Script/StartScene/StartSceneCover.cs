using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StartSceneCover : MonoBehaviour
{
    private const string BackgroundResourcePath = "Art/StartMenu/StartBackground";
    private const float FadeDuration = 0.9f;
    private const float MenuDelay = 0.3f;
    private const float MenuItemDuration = 0.48f;
    private const float MenuStagger = 0.09f;
    private const float HoverSpeed = 8f;

    private readonly string[] menuButtonNames =
    {
        "StartButton",
        "GameInstruction",
        "Credit",
        "LeaderboardButton",
        "ExitButton"
    };

    private Camera mainCamera;
    private SpriteRenderer backgroundRenderer;
    private AudioSource buttonAudioSource;
    private ParticleSystem caveDust;
    private RectTransform[] menuButtons;
    private Vector3[] menuBaseScales;
    private CanvasGroup[] menuCanvasGroups;
    private bool[] menuHovered;
    private float[] menuHoverAmounts;
    private Image fadeImage;
    private CanvasGroup fadeCanvasGroup;
    private float animationTime;
    private float backgroundEntranceProgress;
    private bool menuEntranceComplete;
    private Vector2 backgroundReferenceOffset;

    private void Awake()
    {
        mainCamera = Camera.main;
        buttonAudioSource = GetComponent<AudioSource>();
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        ConfigureCanvas(canvas);
        ConfigureReferenceBackground(canvas.transform);
        CreateBackground();
        CreateCaveDust();
        FindMenuElements(canvas.transform);
        CreateFadePanel(canvas.transform);
    }

    private void OnEnable()
    {
        StartCoroutine(PlayEntrance());
    }

    private void Update()
    {
        animationTime += Time.unscaledDeltaTime;
        UpdateBackground();
        UpdateMenuHover();
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigureReferenceBackground(Transform canvasTransform)
    {
        Transform oldBackground = canvasTransform.Find("Image");
        if (oldBackground != null)
        {
            RectTransform rect = oldBackground as RectTransform;
            if (rect != null)
            {
                backgroundReferenceOffset = rect.anchoredPosition;
            }

            Image image = oldBackground.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;
                image.raycastTarget = false;
            }
        }
    }

    private void CreateBackground()
    {
        Sprite sprite = Resources.Load<Sprite>(BackgroundResourcePath);
        if (sprite == null)
        {
            Debug.LogError("StartSceneCover: missing Resources/" + BackgroundResourcePath + ".png");
            return;
        }

        GameObject background = new GameObject("DynamicBackground");
        background.transform.SetParent(transform, false);
        backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = sprite;
        backgroundRenderer.sortingOrder = -100;
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null || mainCamera == null)
        {
            return;
        }

        float viewHeight = mainCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * mainCamera.aspect;
        Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
        float coverScale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
        float settledZoom = Mathf.Lerp(1.06f, 1.018f, SmoothStep(backgroundEntranceProgress));
        float breathingZoom = Mathf.Sin(animationTime * 0.34f) * 0.008f;
        backgroundRenderer.transform.localScale =
            Vector3.one * coverScale * (settledZoom + breathingZoom);

        float panStrength = SmoothStep(backgroundEntranceProgress);
        float panX = Mathf.Sin(animationTime * 0.11f) * viewWidth * 0.0045f * panStrength;
        float panY = Mathf.Cos(animationTime * 0.09f) * viewHeight * 0.003f * panStrength;
        float referenceOffsetX = backgroundReferenceOffset.x / 1920f * viewWidth;
        float referenceOffsetY = backgroundReferenceOffset.y / 1080f * viewHeight;
        Vector3 cameraPosition = mainCamera.transform.position;
        backgroundRenderer.transform.position =
            new Vector3(
                cameraPosition.x + referenceOffsetX + panX,
                cameraPosition.y + referenceOffsetY + panY,
                cameraPosition.z + 20f);
    }

    private void CreateCaveDust()
    {
        GameObject dustObject = new GameObject("CaveDust");
        dustObject.transform.SetParent(transform, false);
        caveDust = dustObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = caveDust.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.76f, 0.70f, 0.58f, 0.07f),
            new Color(0.95f, 0.88f, 0.70f, 0.18f));

        ParticleSystem.EmissionModule emission = caveDust.emission;
        emission.rateOverTime = 4f;

        ParticleSystem.ShapeModule shape = caveDust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        float viewHeight = mainCamera.orthographicSize * 2f;
        shape.scale = new Vector3(viewHeight * mainCamera.aspect, viewHeight, 0.1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = caveDust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.018f, 0.018f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.CollisionModule collision = caveDust.collision;
        collision.enabled = false;
        ParticleSystem.TrailModule trails = caveDust.trails;
        trails.enabled = false;
        ParticleSystem.LightsModule lights = caveDust.lights;
        lights.enabled = false;

        ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -50;
        dustObject.transform.position =
            new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0f);
    }

    private void FindMenuElements(Transform canvasTransform)
    {
        menuButtons = new RectTransform[menuButtonNames.Length];
        menuBaseScales = new Vector3[menuButtonNames.Length];
        menuCanvasGroups = new CanvasGroup[menuButtonNames.Length];
        menuHovered = new bool[menuButtonNames.Length];
        menuHoverAmounts = new float[menuButtonNames.Length];

        for (int i = 0; i < menuButtonNames.Length; i++)
        {
            Transform buttonTransform = canvasTransform.Find(menuButtonNames[i]);
            RectTransform button = buttonTransform as RectTransform;
            menuButtons[i] = button;
            if (button == null)
            {
                continue;
            }

            menuBaseScales[i] = button.localScale;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent<CanvasGroup>();
            }

            menuCanvasGroups[i] = group;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            button.localScale = menuBaseScales[i] * 0.96f;
            AddHoverTriggers(button, i);
        }
    }

    private void AddHoverTriggers(RectTransform button, int index)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new List<EventTrigger.Entry>();
        }

        EventTrigger.Entry enter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener(_ =>
        {
            menuHovered[index] = true;
            PlayButtonAudio();
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener(_ => menuHovered[index] = false);
        trigger.triggers.Add(exit);
    }

    private void PlayButtonAudio()
    {
        if (buttonAudioSource != null && buttonAudioSource.clip != null)
        {
            buttonAudioSource.PlayOneShot(buttonAudioSource.clip);
        }
    }

    private void CreateFadePanel(Transform canvasTransform)
    {
        GameObject fadePanel = new GameObject(
            "FadePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        fadePanel.transform.SetParent(canvasTransform, false);
        fadePanel.transform.SetAsLastSibling();

        RectTransform rect = (RectTransform)fadePanel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = fadePanel.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PlayEntrance()
    {
        float elapsed = 0f;
        float menuEndTime =
            MenuDelay + MenuStagger * (menuButtons.Length - 1) + MenuItemDuration;
        float totalDuration = Mathf.Max(FadeDuration, menuEndTime);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            backgroundEntranceProgress = Mathf.Clamp01(elapsed / FadeDuration);

            float fadeT = Mathf.Clamp01(elapsed / FadeDuration);
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f - SmoothStep(fadeT);
            }

            UpdateMenuEntrance(elapsed);
            yield return null;
        }

        backgroundEntranceProgress = 1f;
        CompleteMenuEntrance();
        menuEntranceComplete = true;
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void UpdateMenuEntrance(float elapsed)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null || menuCanvasGroups[i] == null)
            {
                continue;
            }

            float itemT = Mathf.Clamp01(
                (elapsed - MenuDelay - MenuStagger * i) / MenuItemDuration);
            float positionT = EaseOutBack(itemT);
            float alphaT = SmoothStep(itemT);

            menuButtons[i].localScale =
                Vector3.LerpUnclamped(menuBaseScales[i] * 0.96f, menuBaseScales[i], positionT);
            menuCanvasGroups[i].alpha = alphaT;
            menuCanvasGroups[i].interactable = itemT >= 1f;
            menuCanvasGroups[i].blocksRaycasts = itemT >= 1f;
        }
    }

    private void CompleteMenuEntrance()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null || menuCanvasGroups[i] == null)
            {
                continue;
            }

            menuButtons[i].localScale = menuBaseScales[i];
            menuCanvasGroups[i].alpha = 1f;
            menuCanvasGroups[i].interactable = true;
            menuCanvasGroups[i].blocksRaycasts = true;
        }
    }

    private void UpdateMenuHover()
    {
        if (!menuEntranceComplete || menuButtons == null)
        {
            return;
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            float target = menuHovered[i] ? 1f : 0f;
            menuHoverAmounts[i] = Mathf.MoveTowards(
                menuHoverAmounts[i],
                target,
                HoverSpeed * Time.unscaledDeltaTime);

            menuButtons[i].localScale =
                menuBaseScales[i] * (1f + 0.018f * menuHoverAmounts[i]);
        }
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.15f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted
            + overshoot * shifted * shifted;
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
